using System;

namespace DCAF.Model
{
    public class EventEntry
    {
        public string Role { get; set; }

        public string Spec { get; set; }

        public string Name { get; set; }

        public string Id { get; set; }

        public DateTime TimeStamp { get; set; }

        public MemberStatus Status { get; set; }
    }
}