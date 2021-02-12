namespace SearchPatrol.Common
{
    public class SimvarRequest
    {
        public uint define;
        public uint request;

        public string name { get; set; }
        public double value;
        public string units { get; set; }
        public bool m_bStillPending = false;
    };
}
