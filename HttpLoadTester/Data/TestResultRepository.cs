using System;
using System.Collections.Generic;
using HttpLoadTester.Models;
using MySql.Data.MySqlClient;

namespace HttpLoadTester.Data
{
    public class TestResultRepository
    {
        public int InsertResult(TestResult result)
        {
            using (var connection = DbConnectionFactory.Create())
            {
                connection.Open();

                using (var command = new MySqlCommand(@"
                    INSERT INTO TestResults
                    (ConfigId, StartTime, EndTime, TotalRequests, SuccessfulRequests, FailedRequests,
                     AverageResponseTimeMs, MinResponseTimeMs, MaxResponseTimeMs, RequestsPerSecond,
                     TotalBytesReceived, ErrorMessages)
                    VALUES
                    (@ConfigId, @StartTime, @EndTime, @TotalRequests, @SuccessfulRequests, @FailedRequests,
                     @AverageResponseTimeMs, @MinResponseTimeMs, @MaxResponseTimeMs, @RequestsPerSecond,
                     @TotalBytesReceived, @ErrorMessages);
                    SELECT LAST_INSERT_ID();", connection))
                {
                    command.Parameters.AddWithValue("@ConfigId", result.ConfigId);
                    command.Parameters.AddWithValue("@StartTime", result.StartTime);
                    command.Parameters.AddWithValue("@EndTime", result.EndTime);
                    command.Parameters.AddWithValue("@TotalRequests", result.TotalRequests);
                    command.Parameters.AddWithValue("@SuccessfulRequests", result.SuccessfulRequests);
                    command.Parameters.AddWithValue("@FailedRequests", result.FailedRequests);
                    command.Parameters.AddWithValue("@AverageResponseTimeMs", result.AverageResponseTimeMs);
                    command.Parameters.AddWithValue("@MinResponseTimeMs", result.MinResponseTimeMs);
                    command.Parameters.AddWithValue("@MaxResponseTimeMs", result.MaxResponseTimeMs);
                    command.Parameters.AddWithValue("@RequestsPerSecond", result.RequestsPerSecond);
                    command.Parameters.AddWithValue("@TotalBytesReceived", result.TotalBytesReceived);
                    command.Parameters.AddWithValue("@ErrorMessages", (object)result.ErrorMessages ?? DBNull.Value);

                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public void InsertLogs(int resultId, List<RequestLog> logs)
        {
            using (var connection = DbConnectionFactory.Create())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var log in logs)
                    {
                        using (var command = new MySqlCommand(@"
                            INSERT INTO RequestLogs
                            (ResultId, RequestNumber, ResponseTimeMs, StatusCode, Success, Timestamp, ErrorMessage)
                            VALUES
                            (@ResultId, @RequestNumber, @ResponseTimeMs, @StatusCode, @Success, @Timestamp, @ErrorMessage);",
                            connection, transaction))
                        {
                            command.Parameters.AddWithValue("@ResultId", resultId);
                            command.Parameters.AddWithValue("@RequestNumber", log.RequestNumber);
                            command.Parameters.AddWithValue("@ResponseTimeMs", log.ResponseTimeMs);
                            command.Parameters.AddWithValue("@StatusCode", log.StatusCode);
                            command.Parameters.AddWithValue("@Success", log.Success);
                            command.Parameters.AddWithValue("@Timestamp", log.Timestamp);
                            command.Parameters.AddWithValue("@ErrorMessage", (object)log.ErrorMessage ?? DBNull.Value);
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }
        }

        public List<TestResult> GetAllResults()
        {
            var list = new List<TestResult>();

            using (var connection = DbConnectionFactory.Create())
            {
                connection.Open();

                using (var command = new MySqlCommand(@"
                    SELECT tr.*, tc.TestName
                    FROM TestResults tr
                    INNER JOIN TestConfigurations tc ON tc.Id = tr.ConfigId
                    ORDER BY tr.StartTime DESC;", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new TestResult
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ConfigId = Convert.ToInt32(reader["ConfigId"]),
                            StartTime = Convert.ToDateTime(reader["StartTime"]),
                            EndTime = Convert.ToDateTime(reader["EndTime"]),
                            TotalRequests = Convert.ToInt32(reader["TotalRequests"]),
                            SuccessfulRequests = Convert.ToInt32(reader["SuccessfulRequests"]),
                            FailedRequests = Convert.ToInt32(reader["FailedRequests"]),
                            AverageResponseTimeMs = Convert.ToDouble(reader["AverageResponseTimeMs"]),
                            MinResponseTimeMs = Convert.ToInt32(reader["MinResponseTimeMs"]),
                            MaxResponseTimeMs = Convert.ToInt32(reader["MaxResponseTimeMs"]),
                            RequestsPerSecond = Convert.ToDouble(reader["RequestsPerSecond"]),
                            TotalBytesReceived = Convert.ToInt64(reader["TotalBytesReceived"]),
                            ErrorMessages = reader["ErrorMessages"]?.ToString(),
                            TestName = reader["TestName"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        public List<RequestLog> GetLogsByResultId(int resultId)
        {
            var list = new List<RequestLog>();

            using (var connection = DbConnectionFactory.Create())
            {
                connection.Open();

                using (var command = new MySqlCommand(@"
                    SELECT Id, ResultId, RequestNumber, ResponseTimeMs, StatusCode, Success, Timestamp, ErrorMessage
                    FROM RequestLogs
                    WHERE ResultId=@ResultId
                    ORDER BY RequestNumber ASC;", connection))
                {
                    command.Parameters.AddWithValue("@ResultId", resultId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new RequestLog
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                ResultId = Convert.ToInt32(reader["ResultId"]),
                                RequestNumber = Convert.ToInt32(reader["RequestNumber"]),
                                ResponseTimeMs = Convert.ToInt32(reader["ResponseTimeMs"]),
                                StatusCode = Convert.ToInt32(reader["StatusCode"]),
                                Success = Convert.ToBoolean(reader["Success"]),
                                Timestamp = Convert.ToDateTime(reader["Timestamp"]),
                                ErrorMessage = reader["ErrorMessage"]?.ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}
