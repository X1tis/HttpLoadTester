using System.Collections.Generic;

namespace HttpLoadTester.Models
{
    public class LiveTestSnapshot
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double RequestsPerSecond { get; set; }
        public int CompletionPercent { get; set; }
        public int LastResponseTimeMs { get; set; }
        public List<RequestLog> RecentLogs { get; set; }
    }
}
