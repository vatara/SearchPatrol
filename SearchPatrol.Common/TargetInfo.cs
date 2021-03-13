using Geolocation;

namespace SearchPatrol.Common
{
    public class TargetInfo
    {
        public string Name { get; set; } = "";
        public string ContainerTitle { get; set; } = "";
        public Coordinate Coordinate { get; set; } = new Coordinate();
    }
}
