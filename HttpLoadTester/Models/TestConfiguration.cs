using System;

namespace HttpLoadTester.Models
{
    public class TestConfiguration
    {
        public int Id { get; set; }
        public string TestName { get; set; }
        public string Url { get; set; }
        public string HttpMethod { get; set; }
        public string Headers { get; set; }
        public string Body { get; set; }
        public int ConcurrentUsers { get; set; }
        public int DurationSeconds { get; set; }
        public int RampUpSeconds { get; set; }
        public int TimeoutMilliseconds { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; }

        public TestConfiguration Clone()
        {
            return (TestConfiguration)MemberwiseClone();
        }

        public override string ToString()
        {
            return $"{TestName} ({HttpMethod})";
        }
    }
}
