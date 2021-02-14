using System.Collections.Generic;

namespace SearchPatrol.Wpf
{
    public class SearchPatrolSettings
    {
        public double MinRange { get; set; }
        public double MaxRange { get; set; }
        public int TargetDirection { get; set; }
        public int DirectionRandomness { get; set; }
        public double TargetFoundDistance { get; set; }
        public List<string> TargetChoices { get; set; }
        public bool VoiceAnnouncement { get; set; } = true;
        public bool TextAnnouncement { get; set; }
    }
}
