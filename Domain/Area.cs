using System.Collections.Generic;

namespace RaidWeekPlanner.Domain
{
    public class Area
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public List<Encounter> Encounters { get; set; } = new List<Encounter>();
    }
}
