using Microsoft.FlightSimulator.SimConnect;
using SearchPatrol.Common;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Windows.Threading;

namespace SearchPatrol.Wpf
{
    public class SearchPatrolViewModel : BaseViewModel, IBaseSimConnectWrapper
    {
        #region IBaseSimConnectWrapper implementation

        /// Window handle
        private IntPtr m_hWnd = new IntPtr(0);

        /// SimConnect object
        private SearchPatrolMain searchPatrol = new SearchPatrolMain();

        SpeechSynthesizer speechSynth = new SpeechSynthesizer();

        public bool bConnected
        {
            get { return m_bConnected; }
            private set { SetProperty(ref m_bConnected, value); }
        }
        private bool m_bConnected = false;

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

            timer.Stop();
            bOddTick = false;

            searchPatrol.Disconnect();

            sConnectButtonLabel = "Connect";
            bConnected = false;
        }

        #endregion

        #region UI bindings

        public string sConnectButtonLabel
        {
            get { return m_sConnectButtonLabel; }
            private set { SetProperty(ref m_sConnectButtonLabel, value); }
        }
        private string m_sConnectButtonLabel = "Connect";

        public bool bOddTick
        {
            get { return m_bOddTick; }
            set { SetProperty(ref m_bOddTick, value); }
        }
        private bool m_bOddTick = false;

        public ObservableCollection<string> lErrorMessages { get; private set; }


        public BaseCommand cmdToggleConnect { get; private set; }
        public BaseCommand cmdAddRequest { get; private set; }
        public BaseCommand cmdRemoveSelectedRequest { get; private set; }
        public BaseCommand cmdTrySetValue { get; private set; }
        public BaseCommand cmdLoadFiles { get; private set; }
        public BaseCommand cmdSaveFile { get; private set; }
        public BaseCommand cmdPlaceTarget { get; private set; }

        public Prop<string> statusText { get; set; } = new Prop<string>();
        public RoundedProp minRange { get; set; } = new RoundedProp(1);
        public RoundedProp maxRange { get; set; } = new RoundedProp(1);
        public Prop<bool> wingWave { get; set; } = new Prop<bool>();
        public Prop<int> targetDirection { get; set; } = new Prop<int>();
        public Prop<int> directionRandomness { get; set; } = new Prop<int>();

        #endregion

        #region Real time

        private DispatcherTimer timer = new DispatcherTimer();

        #endregion

        public SearchPatrolViewModel()
        {
            lErrorMessages = new ObservableCollection<string>();

            cmdToggleConnect = new BaseCommand((p) => { ToggleConnect(); });

            cmdPlaceTarget = new BaseCommand((p) => { PlaceTarget(); });

            minRange.PropertyChanged += (sender, args) =>
            {
                searchPatrol.targetRangeKmMin = minRange.Value;
                if (minRange.Value > maxRange.Value)
                {
                    maxRange.Value = minRange.Value;
                }
            };

            maxRange.PropertyChanged += (sender, args) =>
            {
                searchPatrol.targetRangeKmMax = maxRange.Value;
                if (maxRange.Value < minRange.Value)
                {
                    minRange.Value = maxRange.Value;
                }
            };

            maxRange.Value = searchPatrol.targetRangeKmMax;
            minRange.Value = searchPatrol.targetRangeKmMin;

            searchPatrol.OnAnnouncement += OnAnnouncement;

            timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            timer.Tick += new EventHandler(OnTick);
            timer.Start();
        }

        private void Connect()
        {
            Console.WriteLine("Connect");

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
                Console.WriteLine("Connection to KH failed: " + ex.Message);
            }
        }

        // May not be the best way to achive regular requests.
        // See SimConnect.RequestDataOnSimObject
        private void OnTick(object sender, EventArgs e)
        {
            Console.WriteLine("OnTick");

            if (searchPatrol.Connected)
            {
                bOddTick = !bOddTick;
            }
            else
            {
                bOddTick = false;
            }

            TryConnect();

            var text = $"User: {searchPatrol.UserLat:0.000}, {searchPatrol.UserLng:0.000}\n";
            text += $"Heading: {searchPatrol.TargetBearing:0}, Distance: {searchPatrol.TargetDistance:0.0}\n";
            text += $"Target is about {searchPatrol.TargetFuzzedDistance} km {searchPatrol.TargetFuzzedDirection}";
            statusText.Value = text;

            wingWave.Value = searchPatrol.DetectWingWave();
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

            sConnectButtonLabel = "Disconnect";
            bConnected = true;

            bOddTick = false;
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

            lErrorMessages.Add("SimConnect : " + eException.ToString());
        }

        public void PlaceTarget()
        {
            searchPatrol.PlaceRandomTarget();
        }

        private void OnAnnouncement(string message)
        {
            var text = message.Replace("km", "kilometers");
            speechSynth.SpeakAsync(text);
        }
    }
}
