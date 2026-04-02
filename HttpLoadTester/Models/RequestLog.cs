using System;

namespace HttpLoadTester.Models
{
    public class RequestLog
    {
        public int Id { get; set; }
        public int ResultId { get; set; }
        public int RequestNumber { get; set; }
        public int ResponseTimeMs { get; set; }
        public int StatusCode { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
        public string ErrorMessage { get; set; }
    }
}
