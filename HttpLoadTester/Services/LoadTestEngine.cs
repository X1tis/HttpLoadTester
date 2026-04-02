using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpLoadTester.Models;
using Newtonsoft.Json;

namespace HttpLoadTester.Services
{
    public class LoadTestEngine
    {
        public async Task<(TestResult Result, List<RequestLog> Logs)> RunAsync(
            TestConfiguration config,
            IProgress<LiveTestSnapshot> progress,
            CancellationToken cancellationToken)
        {
            var logs = new ConcurrentBag<RequestLog>();
            var errorMessages = new ConcurrentBag<string>();
            var responseTimes = new ConcurrentBag<int>();

            int totalRequests = 0;
            int successRequests = 0;
            int failedRequests = 0;
            long totalBytes = 0;

            DateTime startTime = DateTime.Now;
            DateTime finishAt = startTime.AddSeconds(config.DurationSeconds);
            Stopwatch totalStopwatch = Stopwatch.StartNew();

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(config.TimeoutMilliseconds);

                var tasks = new List<Task>();

                for (int i = 0; i < config.ConcurrentUsers; i++)
                {
                    int workerIndex = i;

                    tasks.Add(Task.Run(async () =>
                    {
                        if (config.RampUpSeconds > 0 && config.ConcurrentUsers > 0)
                        {
                            int delayMs = (int)((config.RampUpSeconds * 1000.0 / config.ConcurrentUsers) * workerIndex);
                            if (delayMs > 0)
                                await Task.Delay(delayMs, cancellationToken);
                        }

                        while (DateTime.Now < finishAt && !cancellationToken.IsCancellationRequested)
                        {
                            int requestNumber = Interlocked.Increment(ref totalRequests);
                            Stopwatch requestWatch = Stopwatch.StartNew();

                            try
                            {
                                using (var request = BuildRequest(config))
                                using (var response = await httpClient.SendAsync(request, cancellationToken))
                                {
                                    byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                                    requestWatch.Stop();

                                    int responseTime = (int)requestWatch.ElapsedMilliseconds;
                                    bool success = response.IsSuccessStatusCode;

                                    responseTimes.Add(responseTime);
                                    Interlocked.Add(ref totalBytes, bytes.LongLength);

                                    if (success)
                                        Interlocked.Increment(ref successRequests);
                                    else
                                        Interlocked.Increment(ref failedRequests);

                                    logs.Add(new RequestLog
                                    {
                                        RequestNumber = requestNumber,
                                        ResponseTimeMs = responseTime,
                                        StatusCode = (int)response.StatusCode,
                                        Success = success,
                                        Timestamp = DateTime.Now,
                                        ErrorMessage = success ? null : response.ReasonPhrase
                                    });

                                    ReportProgress(
                                        progress,
                                        logs,
                                        responseTimes,
                                        totalRequests,
                                        successRequests,
                                        failedRequests,
                                        totalStopwatch.Elapsed.TotalSeconds,
                                        config.DurationSeconds,
                                        responseTime);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                requestWatch.Stop();

                                int responseTime = (int)requestWatch.ElapsedMilliseconds;
                                responseTimes.Add(responseTime);
                                Interlocked.Increment(ref failedRequests);
                                errorMessages.Add(ex.Message);

                                logs.Add(new RequestLog
                                {
                                    RequestNumber = requestNumber,
                                    ResponseTimeMs = responseTime,
                                    StatusCode = 0,
                                    Success = false,
                                    Timestamp = DateTime.Now,
                                    ErrorMessage = ex.Message
                                });

                                ReportProgress(
                                    progress,
                                    logs,
                                    responseTimes,
                                    totalRequests,
                                    successRequests,
                                    failedRequests,
                                    totalStopwatch.Elapsed.TotalSeconds,
                                    config.DurationSeconds,
                                    responseTime);
                            }
                        }
                    }, cancellationToken));
                }

                await Task.WhenAll(tasks);
            }

            DateTime endTime = DateTime.Now;
            List<int> allTimes = responseTimes.ToList();

            var result = new TestResult
            {
                ConfigId = config.Id,
                StartTime = startTime,
                EndTime = endTime,
                TotalRequests = totalRequests,
                SuccessfulRequests = successRequests,
                FailedRequests = failedRequests,
                AverageResponseTimeMs = allTimes.Count == 0 ? 0 : allTimes.Average(),
                MinResponseTimeMs = allTimes.Count == 0 ? 0 : allTimes.Min(),
                MaxResponseTimeMs = allTimes.Count == 0 ? 0 : allTimes.Max(),
                RequestsPerSecond = totalStopwatch.Elapsed.TotalSeconds <= 0 ? 0 : totalRequests / totalStopwatch.Elapsed.TotalSeconds,
                TotalBytesReceived = totalBytes,
                ErrorMessages = string.Join(Environment.NewLine, errorMessages.Distinct())
            };

            return (result, logs.OrderBy(x => x.RequestNumber).ToList());
        }

        private HttpRequestMessage BuildRequest(TestConfiguration config)
        {
            var method = new HttpMethod((config.HttpMethod ?? "GET").ToUpperInvariant());
            var request = new HttpRequestMessage(method, config.Url);

            if (!string.IsNullOrWhiteSpace(config.Headers))
            {
                var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(config.Headers);

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                        {
                            if (request.Content == null)
                                request.Content = new StringContent(string.Empty);

                            request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }
            }

            if ((method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "DELETE")
                && !string.IsNullOrWhiteSpace(config.Body))
            {
                request.Content = new StringContent(config.Body, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private void ReportProgress(
            IProgress<LiveTestSnapshot> progress,
            ConcurrentBag<RequestLog> logs,
            ConcurrentBag<int> responseTimes,
            int totalRequests,
            int successRequests,
            int failedRequests,
            double elapsedSeconds,
            int durationSeconds,
            int lastResponseTime)
        {
            if (progress == null)
                return;

            var times = responseTimes.ToList();

            var recentLogs = logs
                .OrderByDescending(x => x.RequestNumber)
                .Take(20)
                .OrderBy(x => x.RequestNumber)
                .ToList();

            progress.Report(new LiveTestSnapshot
            {
                TotalRequests = totalRequests,
                SuccessfulRequests = successRequests,
                FailedRequests = failedRequests,
                AverageResponseTimeMs = times.Count == 0 ? 0 : times.Average(),
                RequestsPerSecond = elapsedSeconds <= 0 ? 0 : totalRequests / elapsedSeconds,
                CompletionPercent = durationSeconds <= 0 ? 0 : Math.Min(100, (int)((elapsedSeconds / durationSeconds) * 100)),
                LastResponseTimeMs = lastResponseTime,
                RecentLogs = recentLogs
            });
        }
    }
}
