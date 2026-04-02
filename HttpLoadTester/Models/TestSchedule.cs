using System;

namespace HttpLoadTester.Models
{
    public class TestSchedule
    {
        public int Id { get; set; }
        public int ConfigId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public bool Recurring { get; set; }
        public string RecurrenceInterval { get; set; }
        public bool IsActive { get; set; }
    }
}
