using System;
using System.Collections.Generic;

namespace RaidWeekPlanner.Domain
{
    public class Rotation
    {
        public DateTime StartDate { get; set; }
        public List<string> Boss1 { get; set; }
        public List<string> Boss2 { get; set; }
        public List<string> Boss3 { get; set; }
        public List<string> Boss4 { get; set; }

        public List<string>[] GetLists() => [Boss1, Boss2, Boss3, Boss4];
    }
}
