using System;

namespace HttpLoadTester.Models
{
    public class TestResult
    {
        public int Id { get; set; }
        public int ConfigId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public int MinResponseTimeMs { get; set; }
        public int MaxResponseTimeMs { get; set; }
        public double RequestsPerSecond { get; set; }
        public long TotalBytesReceived { get; set; }
        public string ErrorMessages { get; set; }
        public string TestName { get; set; }
    }
}
