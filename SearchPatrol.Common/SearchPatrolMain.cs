﻿using Geolocation;
using Microsoft.FlightSimulator.SimConnect;
using SearchPatrol.Common.SimObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SearchPatrol.Common
{
    public class SearchPatrolMain
    {
        private SimConnectWrapper wrapper;
        public SimConnect SimConnect => wrapper?.simConnect;
        public SimConnectWrapper Wrapper => wrapper;

        SimvarRequest latReq;
        SimvarRequest lngReq;
        //SimvarRequest altReq;
        SimvarRequest bankReq;

        //double lat;
        //double lng;
        //double alt;
        Coordinate userLocation = new Coordinate();
        Coordinate targetLocation = new Coordinate();

        readonly static double radsToDeg = 180 / Math.PI;
        readonly static double degToRads = Math.PI / 180;

        List<Tuple<DateTimeOffset, double>> bankHistory = new List<Tuple<DateTimeOffset, double>>();        
        double waveDetectAngle = 15 * Math.PI / 180;
        double waveDetectDuration = 5;

        readonly Random random = new Random();

        public double targetRangeKmMin = 2;
        public double targetRangeKmMax = 5;
        public int targetDirection;
        public int directionRandomness = 360;
        
        uint? targetId = null;

        Tuple<DateTimeOffset, double> lastAnnounce;

        public delegate void AnnouncementEventHandler(string message);
        public event AnnouncementEventHandler OnAnnouncement;

        enum Requests
        {
            UserLat,
            UserLng,
            UserAlt,
            UserBank,
            TargetCreate,
            TargetRemove,
            DisplayText
        }

        enum Definitions
        {
            UserLat,
            UserLng,
            UserAlt,
            UserBank,
            TargetCreate
        }

        private bool connected;
        public bool Connected => connected;

        public double TargetBearing => GeoCalculator.GetBearing(userLocation, targetLocation);
        public double TargetDistance => GeoCalculator.GetDistance(userLocation, targetLocation, 1, DistanceUnit.Kilometers);
        public string TargetFuzzedDirection
        {
            get
            {
                var bearing = GeoCalculator.GetBearing(userLocation, targetLocation);
                bearing += random.NextDouble() * 30 - 15;

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

        List<SimObject> targetChoices = new List<SimObject>()
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
            new Windsock()
        };

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
            if (data.dwRequestID == (uint)Requests.TargetCreate)
            {
                targetId = data.dwObjectID;
            }
        }

        private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwRequestID == latReq.request)
            {
                double dValue = (double)data.dwData[0];
                latReq.value = dValue;
                latReq.m_bStillPending = false;
                //lat = latReq.value;
                userLocation.Latitude = latReq.value * radsToDeg;
                AnnounceTargetPositionIfNeeded();
            }
            else if (data.dwRequestID == lngReq.request)
            {
                double dValue = (double)data.dwData[0];
                lngReq.value = dValue;
                lngReq.m_bStillPending = false;
                //lng = lngReq.value;
                userLocation.Longitude = lngReq.value * radsToDeg;
                AnnounceTargetPositionIfNeeded();
            }
            /*
            else if (data.dwRequestID == (uint)altReq.request)
            {
                double dValue = (double)data.dwData[0];
                altReq.value = dValue;
                altReq.m_bStillPending = false;
                alt = altReq.value;
            }
            */
            else if (data.dwRequestID == bankReq.request)
            {
                double dValue = (double)data.dwData[0];
                bankReq.value = dValue;
                bankReq.m_bStillPending = false;
                bankHistory.Add(new Tuple<DateTimeOffset, double>(DateTimeOffset.UtcNow, dValue));

                CleanBankHistory();
            }
        }

        void CleanBankHistory()
        {
            while (bankHistory.Count > 0)
            {
                if ((DateTimeOffset.UtcNow - bankHistory[0].Item1).TotalSeconds > waveDetectDuration)
                {
                    bankHistory.RemoveAt(0);
                }
                else
                {
                    return;
                }
            }
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
            latReq = wrapper.CreateSimvarRequest("PLANE LATITUDE", "radians", (uint)Requests.UserLat, (uint)Definitions.UserLat);
            AddAndRequestDataOnSimObject(latReq);
            lngReq = wrapper.CreateSimvarRequest("PLANE LONGITUDE", "radians", (uint)Requests.UserLng, (uint)Definitions.UserLng);
            AddAndRequestDataOnSimObject(lngReq);
            //altReq = wrapper.CreateSimvarRequest("PLANE ALTITUDE", "feet", (uint)Requests.UserAlt, (uint)Definitions.UserAlt);
            //AddAndRequestDataOnSimObject(altReq);
            bankReq = wrapper.CreateSimvarRequest("PLANE BANK DEGREES", "radians", (uint)Requests.UserBank, (uint)Definitions.UserBank);
            AddAndRequestDataOnSimObject(bankReq, SIMCONNECT_PERIOD.SIM_FRAME);
        }

        void CreateRandomSimObject(SIMCONNECT_DATA_INITPOSITION position, uint request, uint definition)
        {
            if (SimConnect == null) return;
            var type = targetChoices[random.Next(0, targetChoices.Count - 1)];
            wrapper.CreateAiSimulatedObject($"{type.Random()}", position, request, definition);
        }

        // cos(d) = sin(φА)·sin(φB) + cos(φА)·cos(φB)·cos(λА − λB),
        //  where φА, φB are latitudes and λА, λB are longitudes
        // Distance = d * R
        public static double DistanceBetweenPlaces(double lon1, double lat1, double lon2, double lat2)
        {
            double earthRadiusKm = 6371;

            double sLat1 = Math.Sin(lat1);
            double sLat2 = Math.Sin(lat2);
            double cLat1 = Math.Cos(lat1);
            double cLat2 = Math.Cos(lat2);
            double cLon = Math.Cos(lon1) - lon2;

            double cosD = sLat1 * sLat2 + cLat1 * cLat2 * cLon;

            double d = Math.Acos(cosD);

            double dist = earthRadiusKm * d;

            return dist;
        }

        // https://www.movable-type.co.uk/scripts/latlong-vincenty.html
        /* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  */
        /* Vincenty Direct and Inverse Solution of Geodesics on the Ellipsoid (c) Chris Veness 2002-2019  */
        /*                                                                                   MIT Licence  */
        /* www.movable-type.co.uk/scripts/latlong-ellipsoidal-vincenty.html                               */
        /* www.movable-type.co.uk/scripts/geodesy-library.html#latlon-ellipsoidal-vincenty                */
        /* - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  */
        public static Coordinate HeadingDistanceToCoords(Coordinate start, double headingDeg, double distanceM)
        {
            var semiMajorAxis = 6_378_137.0;
            var semiMinorAxis = 6_356_752.314245;
            var f = 1 / 298.257223563;

            var headingRads = headingDeg * degToRads;

            var φ1 = start.Latitude * degToRads;
            var λ1 = start.Longitude * degToRads;

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

        SIMCONNECT_DATA_INITPOSITION GetRandomTargetLocation()
        {
            var direction = (double)Math.Abs(targetDirection);
            direction += random.NextDouble() * Math.Abs(directionRandomness);
            direction %= 360;

            var distance = targetRangeKmMin + random.NextDouble() * (targetRangeKmMax - targetRangeKmMin);

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
            lastAnnounce = null;

            var pos = GetRandomTargetLocation();
            if (targetId != null)
            {
                SimConnect.AIRemoveObject(targetId.Value, Requests.TargetRemove);
                targetId = null;
            }
            CreateRandomSimObject(pos, (uint)Requests.TargetCreate, (uint)Definitions.TargetCreate);
            targetLocation.Latitude = pos.Latitude;
            targetLocation.Longitude = pos.Longitude;

            AnnounceTargetPositionIfNeeded();
        }

        void AnnounceTargetPositionIfNeeded()
        {
            if (targetId == null) return;

            if (lastAnnounce == null)
            {
                AnnounceTargetPosition(TargetPositionMessage());
                return;
            }

            var sinceLast = DateTimeOffset.UtcNow - lastAnnounce.Item1;
            if (sinceLast.TotalSeconds < 10) return;

            var distance = TargetDistance;

            if (distance < 5 && lastAnnounce.Item2 > 5)
            {
                AnnounceTargetPosition(TargetPositionMessage());
                return;
            }
            if (distance < 2 && lastAnnounce.Item2 > 2)
            {
                AnnounceTargetPosition(TargetPositionMessage());
                return;
            }
            if (distance < 2 && lastAnnounce.Item2 > 2)
            {
                AnnounceTargetPosition("Target should be near you");
                return;
            }
        }

        void AnnounceTargetPosition(string message)
        {
            OnAnnouncement?.Invoke(message);
            SimConnect.Text(SIMCONNECT_TEXT_TYPE.PRINT_BLACK, 10, Requests.DisplayText, message);
            lastAnnounce = new Tuple<DateTimeOffset, double>(DateTimeOffset.UtcNow, TargetDistance);
        }

        string TargetPositionMessage()
        {
            return $"Target is about {TargetFuzzedDistance} km {TargetFuzzedDirection}";
        }

        public bool DetectWingWave()
        {
            CleanBankHistory();

            if (bankHistory.Count == 0) return false;

            var angles = bankHistory.Select(i => i.Item2);
            var min = angles.Min();
            var max = angles.Max();
            if (min < -waveDetectAngle && max > waveDetectAngle)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}