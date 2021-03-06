using Microsoft.FlightSimulator.SimConnect;
using Newtonsoft.Json;
using SearchPatrol.Common;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SearchPatrol.Wpf
{
    public class SearchPatrolViewModel : BaseViewModel, IBaseSimConnectWrapper
    {
        #region IBaseSimConnectWrapper implementation

        /// Window handle
        private IntPtr m_hWnd = new IntPtr(0);

        /// SimConnect object
        private readonly SearchPatrolMain searchPatrol = new SearchPatrolMain();

        readonly SpeechSynthesizer speechSynth = new SpeechSynthesizer();

        public bool Connected
        {
            get { return connected; }
            private set { SetProperty(ref connected, value); }
        }
        private bool connected = false;

        public uint GetUserSimConnectWinEvent()
        {
            return SimConnectWrapper.WM_USER_SIMCONNECT;
        }

        public void ReceiveSimConnectMessage()
        {
            searchPatrol.SimConnect?.ReceiveMessage();
        }

        public void SetWindowHandle(IntPtr _hWnd)
        {
            m_hWnd = _hWnd;
        }

        public void Disconnect()
        {
            Console.WriteLine("Disconnect");

            //timer.Stop();
            OddTick = false;

            searchPatrol.Disconnect();

            ConnectButtonLabel = "Connect";
            Connected = false;
        }

        #endregion

        #region UI bindings

        public string ConnectButtonLabel
        {
            get { return m_sConnectButtonLabel; }
            private set { SetProperty(ref m_sConnectButtonLabel, value); }
        }
        private string m_sConnectButtonLabel = "Connect";

        public bool OddTick
        {
            get { return m_bOddTick; }
            set { SetProperty(ref m_bOddTick, value); }
        }
        private bool m_bOddTick = false;

        public BaseCommand CmdPlaceTarget { get; private set; }

        public NamedProp<bool> AircraftIsChecked { get; set; } = new NamedProp<bool>("Aircraft");
        public NamedProp<bool> GroundVehicleIsChecked { get; set; } = new NamedProp<bool>("GroundVehicle");
        public NamedProp<bool> WindmillIsChecked { get; set; } = new NamedProp<bool>("Windmill");
        public NamedProp<bool> BoatIsChecked { get; set; } = new NamedProp<bool>("Boat");
        public NamedProp<bool> FishingBoatIsChecked { get; set; } = new NamedProp<bool>("FishingBoat");
        public NamedProp<bool> YachtIsChecked { get; set; } = new NamedProp<bool>("Yacht");
        public NamedProp<bool> CargoShipIsChecked { get; set; } = new NamedProp<bool>("CargoShip");
        public NamedProp<bool> CruiseShipIsChecked { get; set; } = new NamedProp<bool>("CruiseShip");
        public NamedProp<bool> AnimalIsChecked { get; set; } = new NamedProp<bool>("Animal");
        public NamedProp<bool> HumanIsChecked { get; set; } = new NamedProp<bool>("Human");
        public NamedProp<bool> WindsockIsChecked { get; set; } = new NamedProp<bool>("Windsock");

        public Prop<string> StatusText { get; set; } = new Prop<string>();
        public RoundedProp MinRange { get; set; } = new RoundedProp(1);
        public RoundedProp MaxRange { get; set; } = new RoundedProp(1);
        public Prop<bool> WingWave { get; set; } = new Prop<bool>();
        public Prop<int> TargetDirection { get; set; } = new Prop<int>();
        public Prop<int> DirectionRandomness { get; set; } = new Prop<int>();
        public RoundedProp TargetFoundDistance { get; set; } = new RoundedProp(1);

        public Prop<bool> VoiceAnnouncement { get; set; } = new Prop<bool>();
        public Prop<int> VoiceVolume { get; set; } = new Prop<int>();
        public Prop<bool> TextAnnouncement { get; set; } = new Prop<bool>();

        #endregion

        #region Real time

        private readonly DispatcherTimer timer = new DispatcherTimer();

        #endregion

        readonly SearchPatrolSettings settings = new SearchPatrolSettings();

        public Prop<bool> Debug { get; set; } = new Prop<bool>();

        public SearchPatrolViewModel()
        {
            settings = LoadSettings();
            foreach (var t in searchPatrol.TargetChoices)
            {
                if (settings.TargetChoices.Contains(t))
                {
                    searchPatrol.SetTargetEnabled(t, true);
                }
                else
                {
                    searchPatrol.SetTargetEnabled(t, false);
                }
            }

            searchPatrol.TargetRangeKmMin = MinRange.Value = settings.MinRange;
            searchPatrol.TargetRangeKmMax = MaxRange.Value = settings.MaxRange;
            searchPatrol.TargetDirection = TargetDirection.Value = settings.TargetDirection;
            searchPatrol.DirectionRandomness = DirectionRandomness.Value = settings.DirectionRandomness;
            searchPatrol.TargetFoundDistance = TargetFoundDistance.Value = settings.TargetFoundDistance;

            MinRange.PropertyChanged += (sender, args) =>
            {
                settings.MinRange = searchPatrol.TargetRangeKmMin = MinRange.Value;
                if (MinRange.Value > MaxRange.Value)
                {
                    settings.MaxRange = MaxRange.Value = MinRange.Value;
                }
                SaveSettings();
            };

            MaxRange.PropertyChanged += (sender, args) =>
            {
                settings.MaxRange = searchPatrol.TargetRangeKmMax = MaxRange.Value;
                if (MaxRange.Value < MinRange.Value)
                {
                    settings.MinRange = MinRange.Value = MaxRange.Value;
                }
                SaveSettings();
            };

            TargetDirection.PropertyChanged += (sender, args) =>
            {
                settings.TargetDirection = searchPatrol.TargetDirection = TargetDirection.Value;
                SaveSettings();
            };

            TargetFoundDistance.PropertyChanged += (sender, args) =>
            {
                settings.TargetFoundDistance = searchPatrol.TargetFoundDistance = TargetFoundDistance.Value;
                SaveSettings();
            };

            DirectionRandomness.PropertyChanged += (sender, args) =>
            {
                settings.DirectionRandomness = searchPatrol.DirectionRandomness = DirectionRandomness.Value;
                SaveSettings();
            };

            VoiceAnnouncement.Value = settings.VoiceAnnouncement;
            VoiceAnnouncement.PropertyChanged += (sender, args) =>
            {
                settings.VoiceAnnouncement = VoiceAnnouncement.Value;
                SaveSettings();
            };

            VoiceVolume.Value = settings.VoiceVolume;
            VoiceVolume.PropertyChanged += (sender, args) =>
            {
                if (VoiceVolume.Value < 0) VoiceVolume.Value = 0;
                if (VoiceVolume.Value > 100) VoiceVolume.Value = 100;
                speechSynth.Volume = settings.VoiceVolume = VoiceVolume.Value;
                SaveSettings();
            };

            searchPatrol.ShowTextAnnouncements = TextAnnouncement.Value = settings.TextAnnouncement;
            TextAnnouncement.PropertyChanged += (sender, args) =>
            {
                settings.TextAnnouncement = searchPatrol.ShowTextAnnouncements = TextAnnouncement.Value;
                SaveSettings();
            };

            CmdPlaceTarget = new BaseCommand((p) => { PlaceTarget(); });

            CreateTargetProp(AircraftIsChecked);
            CreateTargetProp(GroundVehicleIsChecked);
            CreateTargetProp(WindmillIsChecked);
            CreateTargetProp(BoatIsChecked);
            CreateTargetProp(FishingBoatIsChecked);
            CreateTargetProp(YachtIsChecked);
            CreateTargetProp(CargoShipIsChecked);
            CreateTargetProp(CruiseShipIsChecked);
            CreateTargetProp(AnimalIsChecked);
            CreateTargetProp(HumanIsChecked);
            CreateTargetProp(WindsockIsChecked);

            searchPatrol.OnAnnouncement += OnAnnouncement;

            timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            timer.Tick += new EventHandler(OnTick);
            timer.Start();
        }

        void CreateTargetProp(NamedProp<bool> prop)
        {
            prop.Value = settings.TargetChoices.Contains(prop.Name);
            prop.PropertyChanged += (sender, args) => SetTargetChecked(sender);
        }

        void SetTargetChecked(object sender)
        {
            var prop = sender as NamedProp<bool>;
            searchPatrol.SetTargetEnabled(prop.Name, prop.Value);
            settings.TargetChoices = searchPatrol.TargetChoices;
            SaveSettings();
        }

        static readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SearchPatrol/");
        readonly string settingsFile = Path.Combine(settingsPath, "searchPatrolSettings.json");

        void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            try
            {
                Directory.CreateDirectory(settingsPath);
                File.WriteAllText(settingsFile, json);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error saving settings: {e}");
            }
        }

        SearchPatrolSettings LoadSettings()
        {
            SearchPatrolSettings settings = null;
            try
            {
                if (File.Exists(settingsFile))
                {
                    var json = File.ReadAllText(settingsFile);
                    settings = JsonConvert.DeserializeObject<SearchPatrolSettings>(json);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error loading settings: {e}");
            }
            if (settings == null)
            {
                settings = new SearchPatrolSettings
                {
                    MinRange = searchPatrol.TargetRangeKmMin,
                    MaxRange = searchPatrol.TargetRangeKmMax,
                    TargetDirection = searchPatrol.TargetDirection,
                    DirectionRandomness = searchPatrol.DirectionRandomness,
                    TargetFoundDistance = searchPatrol.TargetFoundDistance,
                    TargetChoices = searchPatrol.TargetChoices,
                    TextAnnouncement = searchPatrol.ShowTextAnnouncements
                };
            }
            return settings;
        }

        private void Connect()
        {
            Task.Run(() =>
            {
                try
                {
                    /// The constructor is similar to SimConnect_Open in the native API
                    //simConnect.simConnect = new SimConnect("Simconnect - Simvar test", m_hWnd, WM_USER_SIMCONNECT, null, bFSXcompatible ? (uint)1 : 0);
                    searchPatrol.Connect(m_hWnd);

                    /// Listen to connect and quit msgs
                    searchPatrol.SimConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
                    searchPatrol.SimConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnRecvQuit);

                    /// Listen to exceptions
                    searchPatrol.SimConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnRecvException);

                    /// Catch a simobject data request
                    //searchPatrol.SimConnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(SimConnect_OnRecvSimobjectDataBytype);
                }
                catch (COMException ex)
                {
                    Console.WriteLine("Connection to MSFS failed: " + ex.Message);
                }
            });
        }

        // May not be the best way to achive regular requests.
        // See SimConnect.RequestDataOnSimObject
        private void OnTick(object sender, EventArgs e)
        {
            if (searchPatrol.Connected)
            {
                OddTick = !OddTick;
            }
            else
            {
                OddTick = false;
            }

            TryConnect();

            var text = $"User: {searchPatrol.UserLat:0.000}, {searchPatrol.UserLng:0.000}\n";
            text += $"Heading: {searchPatrol.TargetBearing:0}, Distance: {searchPatrol.TargetDistance:0.0}\n";
            text += $"Target is about {searchPatrol.TargetFuzzedDistance} km {searchPatrol.TargetFuzzedDirection}";
            StatusText.Value = text;

            WingWave.Value = searchPatrol.DetectWingWave();
        }

        void TryConnect()
        {
            if (!searchPatrol.Connected)
            {
                try
                {
                    Connect();
                }
                catch (COMException ex)
                {
                    Console.WriteLine("Unable to connect to MSFS: " + ex.Message);
                }
            }
        }

        private void ToggleConnect()
        {
            if (searchPatrol.SimConnect == null)
            {
                try
                {
                    Connect();
                }
                catch (COMException ex)
                {
                    Console.WriteLine("Unable to connect to MSFS: " + ex.Message);
                }
            }
            else
            {
                Disconnect();
            }
        }

        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Console.WriteLine("SimConnect_OnRecvOpen");
            Console.WriteLine("Connected to MSFS");

            ConnectButtonLabel = "Disconnect";
            Connected = true;

            OddTick = false;
        }

        /// The case where the user closes game
        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Console.WriteLine("SimConnect_OnRecvQuit");
            Console.WriteLine("KH has exited");

            Disconnect();
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            SIMCONNECT_EXCEPTION eException = (SIMCONNECT_EXCEPTION)data.dwException;
            Console.WriteLine("SimConnect_OnRecvException: " + eException.ToString());

            //ErrorMessages.Add("SimConnect : " + eException.ToString());
        }

        public void PlaceTarget()
        {
            searchPatrol.PlaceRandomTarget();
        }

        private void OnAnnouncement(string message)
        {
            if (!VoiceAnnouncement.Value) return;

            var text = message.Replace("km", "kilometers");
            speechSynth.SpeakAsyncCancelAll();
            speechSynth.SpeakAsync(text);
        }
    }
}
