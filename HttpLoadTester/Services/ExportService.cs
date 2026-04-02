using System.Collections.Generic;
using System.IO;
using System.Text;
using HttpLoadTester.Models;
using Newtonsoft.Json;

namespace HttpLoadTester.Services
{
    public class ExportService
    {
        public void ExportLogsToCsv(string filePath, IEnumerable<RequestLog> logs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("RequestNumber,ResponseTimeMs,StatusCode,Success,Timestamp,ErrorMessage");

            foreach (var log in logs)
            {
                var error = (log.ErrorMessage ?? string.Empty).Replace("\"", "\"\"");
                sb.AppendLine($"{log.RequestNumber},{log.ResponseTimeMs},{log.StatusCode},{log.Success},{log.Timestamp:yyyy-MM-dd HH:mm:ss},\"{error}\"");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public void ExportResultToJson(string filePath, TestResult result, IEnumerable<RequestLog> logs)
        {
            var payload = new
            {
                Result = result,
                Logs = logs
            };

            var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }
    }
}
