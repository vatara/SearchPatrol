using Geolocation;
using Microsoft.FlightSimulator.SimConnect;
using SearchPatrol.Common.SimObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SearchPatrol.Common
{
    public class SearchPatrolMain
    {
        private SimConnectWrapper wrapper;
        public SimConnect SimConnect => wrapper?.simConnect;
        public SimConnectWrapper Wrapper => wrapper;

        SimvarRequest latReq;
        SimvarRequest lngReq;
        SimvarRequest bankReq;

        Coordinate userLocation = new Coordinate();
        Coordinate targetLocation = new Coordinate();

        readonly static double radsToDeg = 180 / Math.PI;
        readonly static double degToRads = Math.PI / 180;
        readonly List<(DateTimeOffset time, double angle)> bankHistory = new List<(DateTimeOffset, double)>();
        readonly double waveDetectAngle = 15;
        readonly double waveDetectDuration = 5;

        readonly Random random = new Random();

        public double TargetRangeKmMin { get; set; } = 2;
        public double TargetRangeKmMax { get; set; } = 5;
        public int TargetDirection { get; set; }
        public int DirectionRandomness { get; set; } = 360;
        public double TargetFoundDistance { get; set; } = 1;

        uint? targetId = null;
        string targetName = "";

        (DateTimeOffset time, double distance) lastAnnounce = (DateTimeOffset.MinValue, double.MaxValue);
        public bool ShowTextAnnouncements { get; set; } = true;
        public delegate void AnnouncementEventHandler(string message);
        public event AnnouncementEventHandler OnAnnouncement;

        enum Requests
        {
            UserLat,
            UserLng,
            UserAlt,
            UserBank,
        }

        enum Definitions
        {
            UserLat,
            UserLng,
            UserAlt,
            UserBank
        }

        private uint requestId = (uint)Requests.UserBank + 1;
        private uint targetRequestId;

        private readonly string UnitsDegrees = "degrees";

        private bool connected;
        public bool Connected => connected;

        public double TargetBearing => GeoCalculator.GetBearing(userLocation, targetLocation);
        public string TargetFuzzedDirection
        {
            get
            {
                var bearing = GeoCalculator.GetBearing(userLocation, targetLocation);
                bearing += random.NextDouble() * 20 - 10;

                if (bearing > 22.5 && bearing <= 67.5) return "north east";
                if (bearing > 67.5 && bearing <= 112.5) return "east";
                if (bearing > 112.5 && bearing <= 157.5) return "south east";
                if (bearing > 157.5 && bearing <= 202.5) return "south";
                if (bearing > 202.5 && bearing <= 247.5) return "south west";
                if (bearing > 247.5 && bearing <= 292.5) return "west";
                if (bearing > 292.5 && bearing < 337.5) return "north west";
                else return "north";
            }
        }

        public double TargetDistance => GeoCalculator.GetDistance(userLocation, targetLocation, 1, DistanceUnit.Kilometers);
        public double TargetFuzzedDistance
        {
            get
            {
                var distance = TargetDistance;
                var fuzzAmount = distance * .2;
                var change = random.NextDouble() * fuzzAmount - fuzzAmount / 2;
                return Math.Round(distance + change);
            }
        }

        public double UserLat => userLocation.Latitude;
        public double UserLng => userLocation.Longitude;

        readonly HashSet<SimObject> targetChoices = new HashSet<SimObject>()
        {
            new GroundVehicle(),
            new Windmill(),
            new Boat(),
            new FishingBoat(),
            new Yacht(),
            new CargoShip(),
            new CruiseShip(),
            new Animal(),
            new Human(),
            new Windsock(),
            new Aircraft()
        };
        public List<string> TargetChoices => targetChoices.Select(t => t.GetType().Name).ToList();

        public void Connect(IntPtr hWnd)
        {
            wrapper = new SimConnectWrapper();
            var fsxCompatible = false;
            wrapper.Connect("SearchPatrol", hWnd, null, fsxCompatible ? (uint)1 : 0);

            SimConnect.OnRecvSimobjectData += SimConnect_OnRecvSimobjectData;
            SimConnect.OnRecvAssignedObjectId += SimConnect_OnRecvAssignedObjectId;
            SimConnect.OnRecvOpen += SimConnect_OnRecvOpen;
            SimConnect.OnRecvQuit += SimConnect_OnRecvQuit;

            RequestUserPlaneData();
        }

        public void Disconnect()
        {
            if (wrapper.simConnect != null)
            {
                /// Dispose serves the same purpose as SimConnect_Close()
                wrapper.simConnect.Dispose();
                wrapper.simConnect = null;
            }
        }

        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            connected = true;
        }

        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Disconnect();
            connected = false;
        }

        private void SimConnect_OnRecvAssignedObjectId(SimConnect sender, SIMCONNECT_RECV_ASSIGNED_OBJECT_ID data)
        {
            if (data.dwRequestID == targetRequestId)
            {
                targetId = data.dwObjectID;
                AnnounceTargetPositionIfNeeded();
            }
        }

        private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwRequestID == latReq.request)
            {
                double dValue = (double)data.dwData[0];
                latReq.value = dValue;
                latReq.m_bStillPending = false;
                userLocation.Latitude = latReq.value;
                AnnounceTargetPositionIfNeeded();
            }
            else if (data.dwRequestID == lngReq.request)
            {
                double dValue = (double)data.dwData[0];
                lngReq.value = dValue;
                lngReq.m_bStillPending = false;
                userLocation.Longitude = lngReq.value;
                AnnounceTargetPositionIfNeeded();
            }
            else if (data.dwRequestID == bankReq.request)
            {
                double dValue = (double)data.dwData[0];
                bankReq.value = dValue;
                bankReq.m_bStillPending = false;
                bankHistory.Add((DateTimeOffset.UtcNow, dValue));
            }

            CleanBankHistory();
            CheckTargetFound();
        }

        void CleanBankHistory()
        {
            while (bankHistory.Count > 0)
            {
                if ((DateTimeOffset.UtcNow - bankHistory[0].time).TotalSeconds > waveDetectDuration)
                {
                    bankHistory.RemoveAt(0);
                }
                else
                {
                    return;
                }
            }
        }

        public bool DetectWingWave()
        {
            return DetectWingWave(bankHistory, waveDetectAngle);
        }

        public static bool DetectWingWave(List<(DateTimeOffset time, double angle)> bankHistory, double waveDetectAngle)
        {
            if (bankHistory.Count == 0) return false;


            var angles = bankHistory.Select(i => i.angle).Where(a => Math.Abs(a) > waveDetectAngle);

            var directions = new int[] { -1, 1 };
            for (var i = 0; i < directions.Length; i++)
            {
                var waveCount = 0;
                var d = directions[i];
                foreach (var a in angles)
                {
                    var detectAngle = waveDetectAngle * d;
                    if (Math.Sign(a) == Math.Sign(detectAngle) && Math.Abs(a) > Math.Abs(detectAngle))
                    {
                        waveCount++;
                        d *= -1;

                        if (waveCount >= 3)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        void CheckTargetFound()
        {
            if (targetId == null) return;

            var wave = DetectWingWave(bankHistory, waveDetectAngle);
            if (!wave) return;

            bankHistory.Clear();

            var dist = GeoCalculator.GetDistance(UserLat, UserLng, targetLocation.Latitude, targetLocation.Longitude);
            if (dist > TargetFoundDistance)
            {
                AnnounceTargetPositionIfNeeded();
                return;
            }

            AnnounceMessage("Target found");
            Task.Run(async () =>
            {
                await Task.Delay(5000);
                PlaceRandomTarget();
            });
        }

        void AddAndRequestDataOnSimObject(SimvarRequest request, SIMCONNECT_PERIOD period = SIMCONNECT_PERIOD.SECOND)
        {
            var type = SimConnect.SIMCONNECT_OBJECT_ID_USER;
            var flags = SIMCONNECT_DATA_REQUEST_FLAG.CHANGED;

            /// Define a data structure
            SimConnect.AddToDataDefinition((Definitions)request.define, request.name, request.units, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            /// IMPORTANT: Register it with the simconnect managed wrapper marshaller
            /// If you skip this step, you will only receive a uint in the .dwData field.
            SimConnect.RegisterDataDefineStruct<double>((Definitions)request.define);
            SimConnect.RequestDataOnSimObject((Requests)request.request, (Definitions)request.define, type, period, flags, 0, 0, 0);
        }

        void RequestUserPlaneData()
        {
            latReq = wrapper.CreateSimvarRequest("PLANE LATITUDE", UnitsDegrees, (uint)Requests.UserLat, (uint)Definitions.UserLat);
            AddAndRequestDataOnSimObject(latReq);
            lngReq = wrapper.CreateSimvarRequest("PLANE LONGITUDE", UnitsDegrees, (uint)Requests.UserLng, (uint)Definitions.UserLng);
            AddAndRequestDataOnSimObject(lngReq);
            bankReq = wrapper.CreateSimvarRequest("PLANE BANK DEGREES", UnitsDegrees, (uint)Requests.UserBank, (uint)Definitions.UserBank);
            AddAndRequestDataOnSimObject(bankReq, SIMCONNECT_PERIOD.SIM_FRAME);
        }

        void CreateRandomSimObject(SIMCONNECT_DATA_INITPOSITION position, uint request)
        {
            if (SimConnect == null) return;
            if (targetChoices.Count == 0) return;

            var type = targetChoices.ToList()[random.Next(0, targetChoices.Count - 1)];
            if (type is Aircraft)
            {
                targetName = type.Random();
                wrapper.CreateAiNonAtcAircraft(targetName, position, request);
            }
            else
            {
                targetName = type.GetType().Name;
                wrapper.CreateAiSimulatedObject($"{type.Random()}", position, request);
            }
        }

        // https://www.movable-type.co.uk/scripts/latlong-vincenty.html
        /* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  */
        /* Vincenty Direct and Inverse Solution of Geodesics on the Ellipsoid (c) Chris Veness 2002-2019  */
        /*                                                                                   MIT Licence  */
        /* www.movable-type.co.uk/scripts/latlong-ellipsoidal-vincenty.html                               */
        /* www.movable-type.co.uk/scripts/geodesy-library.html#latlon-ellipsoidal-vincenty                */
        /* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  */
        public static Coordinate HeadingDistanceToCoords(double lat, double lon, double headingDeg, double distanceM)
        {
            var semiMajorAxis = 6_378_137.0;
            var semiMinorAxis = 6_356_752.314245;
            var f = 1 / 298.257223563;

            var headingRads = headingDeg * degToRads;

            var φ1 = lat * degToRads;
            var λ1 = lon * degToRads;

            var α1 = headingRads;
            var s = distanceM;

            var a = semiMajorAxis;
            var b = semiMinorAxis;

            var sinα1 = Math.Sin(α1);
            var cosα1 = Math.Cos(α1);

            var tanU1 = (1 - f) * Math.Tan(φ1);
            var cosU1 = 1 / Math.Sqrt(1 + tanU1 * tanU1);
            var sinU1 = tanU1 * cosU1;
            var σ1 = Math.Atan2(tanU1, cosα1); // σ1 = angular distance on the sphere from the equator to P1
            var sinα = cosU1 * sinα1;          // α = azimuth of the geodesic at the equator
            var cosSqα = 1 - sinα * sinα;
            var uSq = cosSqα * (a * a - b * b) / (b * b);
            var A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
            var B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));

            var σ = s / (b * A);
            double sinσ, cosσ, Δσ; // σ = angular distance P₁ P₂ on the sphere
            double cos2σₘ; // σₘ = angular distance on the sphere from the equator to the midpoint of the line

            double σʹ;
            //var iterations = 0;
            do
            {
                cos2σₘ = Math.Cos(2 * σ1 + σ);
                sinσ = Math.Sin(σ);
                cosσ = Math.Cos(σ);
                Δσ = B * sinσ * (cos2σₘ + B / 4 * (cosσ * (-1 + 2 * cos2σₘ * cos2σₘ) -
                    B / 6 * cos2σₘ * (-3 + 4 * sinσ * sinσ) * (-3 + 4 * cos2σₘ * cos2σₘ)));
                σʹ = σ;
                σ = s / (b * A) + Δσ;
                //} while (Math.Abs(σ - σʹ) > 1e-12 && ++iterations < 100);
            } while (Math.Abs(σ - σʹ) > 1e-12);
            //if (iterations >= 100) throw new EvalError('Vincenty formula failed to converge'); // not possible?

            var x = sinU1 * sinσ - cosU1 * cosσ * cosα1;
            var φ2 = Math.Atan2(sinU1 * cosσ + cosU1 * sinσ * cosα1, (1 - f) * Math.Sqrt(sinα * sinα + x * x));
            var λ = Math.Atan2(sinσ * sinα1, cosU1 * cosσ - sinU1 * sinσ * cosα1);
            var C = f / 16 * cosSqα * (4 + f * (4 - 3 * cosSqα));
            var L = λ - (1 - C) * f * sinα * (σ + C * sinσ * (cos2σₘ + C * cosσ * (-1 + 2 * cos2σₘ * cos2σₘ)));
            var λ2 = λ1 + L;

            //var α2 = Math.Atan2(sinα, -x);

            return new Coordinate(φ2 * radsToDeg, λ2 * radsToDeg);
        }

        public static Coordinate HeadingDistanceToCoords(Coordinate start, double headingDeg, double distanceM)
        {
            return HeadingDistanceToCoords(start.Latitude, start.Longitude, headingDeg, distanceM);
        }

        SIMCONNECT_DATA_INITPOSITION GetRandomTargetLocation()
        {
            var direction = (double)Math.Abs(TargetDirection);
            direction += random.NextDouble() * Math.Abs(DirectionRandomness);
            direction %= 360;

            var distance = TargetRangeKmMin + random.NextDouble() * (TargetRangeKmMax - TargetRangeKmMin);

            var pos = HeadingDistanceToCoords(userLocation, direction, distance * 1000);

            return new SIMCONNECT_DATA_INITPOSITION
            {
                Latitude = pos.Latitude,
                Longitude = pos.Longitude,
                OnGround = 1
            };
        }

        public void PlaceRandomTarget()
        {
            lastAnnounce = (DateTimeOffset.MinValue, double.MaxValue);

            var pos = GetRandomTargetLocation();
            if (targetId != null)
            {
                SimConnect.AIRemoveObject(targetId.Value, (Requests)targetRequestId);
                targetId = null;
            }
            CreateRandomSimObject(pos, requestId);
            targetRequestId = requestId;
            requestId++;
            targetLocation.Latitude = pos.Latitude;
            targetLocation.Longitude = pos.Longitude;

            AnnounceTargetPositionIfNeeded();
        }

        readonly int[] announcePoints = new int[] { 1, 2, 5, 10, 20, 50, 100, 150, 200, 300, 400, 500 };
        void AnnounceTargetPositionIfNeeded(bool force = false)
        {
            if (targetId == null) return;

            var announceName = false;

            var sinceLast = DateTimeOffset.UtcNow - lastAnnounce.time;
            if (sinceLast.TotalSeconds < 10 && !force) return;

            var distance = TargetDistance;

            // check for getting closer announcements
            for (var i = 0; i < announcePoints.Length; i++)
            {
                var announceDist = announcePoints[i];
                if (distance < announceDist && (lastAnnounce.distance > announceDist || force))
                {
                    AnnounceMessage(TargetPositionMessage(announceName, i == 0));
                    lastAnnounce = (DateTimeOffset.UtcNow, announceDist);
                    return;
                }
            }

            // check for getting further announcements
            for (var i = 1; i < announcePoints.Length; i++)
            {
                var p = announcePoints[i];
                if (distance > p && lastAnnounce.distance < p)
                {
                    AnnounceMessage(TargetPositionMessage(false, false));
                    lastAnnounce = (DateTimeOffset.UtcNow, p);
                    return;
                }
            }
        }

        void AnnounceMessage(string message)
        {
            OnAnnouncement?.Invoke(message);
            if (ShowTextAnnouncements)
            {
                SimConnect.Text(SIMCONNECT_TEXT_TYPE.PRINT_BLACK, 10, (Requests)requestId, message);
                requestId++;
            }
        }

        string TargetPositionMessage(bool includeName, bool nearby)
        {
            var text = $"Target {(includeName ? targetName : "")} ";
            if (nearby)
            {
                text += "should be near you";
            }
            else
            {
                text += $"is about {TargetFuzzedDistance} km {TargetFuzzedDirection}";
            }
            return text;
        }

        public void SetTargetEnabled(string type, bool enabled)
        {
            var list = targetChoices.Select(t => t.GetType().Name).ToList();
            if (!list.Contains(type) && enabled)
            {
                targetChoices.Add((SimObject)Activator.CreateInstance(Type.GetType("SearchPatrol.Common.SimObjects." + type)));
            }
            else if (list.Contains(type) && !enabled)
            {
                targetChoices.RemoveWhere(t => t.GetType().Name == type);
            }
        }
    }
}
