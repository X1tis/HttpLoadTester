using System;
using System.Collections.Generic;
using HttpLoadTester.Models;
using MySql.Data.MySqlClient;

namespace HttpLoadTester.Data
{
    public class TestConfigurationRepository
    {
        public List<TestConfiguration> GetAll()
        {
            var list = new List<TestConfiguration>();

            using (var connection = DbConnectionFactory.Create())
            {
                connection.Open();

                using (var command = new MySqlCommand(@"
                    SELECT Id, TestName, Url, HttpMethod, Headers, Body, ConcurrentUsers,
                           DurationSeconds, RampUpSeconds, TimeoutMilliseconds, CreatedAt, Description
                    FROM TestConfigurations
                    ORDER BY CreatedAt DESC;", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new TestConfiguration
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            TestName = reader["TestName"].ToString(),
                            Url = reader["Url"].ToString(),
                            HttpMethod = reader["HttpMethod"].ToString(),
                            Headers = reader["Headers"]?.ToString(),
                            Body = reader["Body"]?.ToString(),
                            ConcurrentUsers = Convert.ToInt32(reader["ConcurrentUsers"]),
                            DurationSeconds = Convert.ToInt32(reader["DurationSeconds"]),
                            RampUpSeconds = Convert.ToInt32(reader["RampUpSeconds"]),
                            TimeoutMilliseconds = Convert.ToInt32(reader["TimeoutMilliseconds"]),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            Description = reader["Description"]?.ToString()
                        });
                    }
                }
            }

            return list;
        }

        public int Insert(TestConfiguration config)
        {
            using (var connection = DbConnectionFactory.Create())
            {
                connection.Open();

                using (var command = new MySqlCommand(@"
                    INSERT INTO TestConfigurations
                    (TestName, Url, HttpMethod, Headers, Body, ConcurrentUsers, DurationSeconds, RampUpSeconds, TimeoutMilliseconds, CreatedAt, Description)
                    VALUES
                    (@TestName, @Url, @HttpMethod, @Headers, @Body, @ConcurrentUsers, @DurationSeconds, @RampUpSeconds, @TimeoutMilliseconds, @CreatedAt, @Description);
                    SELECT LAST_INSERT_ID();", connection))
                {
                    command.Parameters.AddWithValue("@TestName", config.TestName);
                    command.Parameters.AddWithValue("@Url", config.Url);
                    command.Parameters.AddWithValue("@HttpMethod", config.HttpMethod);
                    command.Parameters.AddWithValue("@Headers", (object)config.Headers ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Body", (object)config.Body ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ConcurrentUsers", config.ConcurrentUsers);
                    command.Parameters.AddWithValue("@DurationSeconds", config.DurationSeconds);
                    command.Parameters.AddWithValue("@RampUpSeconds", config.RampUpSeconds);
                    command.Parameters.AddWithValue("@TimeoutMilliseconds", config.TimeoutMilliseconds);
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    command.Parameters.AddWithValue("@Description", (object)config.Description ?? DBNull.Value);

                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public void Update(TestConfiguration config)
        {
            using (var connection = DbConnectionFactory.Create())
            {
                connection.Open();

                using (var command = new MySqlCommand(@"
                    UPDATE TestConfigurations
                    SET TestName=@TestName,
                        Url=@Url,
                        HttpMethod=@HttpMethod,
                        Headers=@Headers,
                        Body=@Body,
                        ConcurrentUsers=@ConcurrentUsers,
                        DurationSeconds=@DurationSeconds,
                        RampUpSeconds=@RampUpSeconds,
                        TimeoutMilliseconds=@TimeoutMilliseconds,
                        Description=@Description
                    WHERE Id=@Id;", connection))
                {
                    command.Parameters.AddWithValue("@Id", config.Id);
                    command.Parameters.AddWithValue("@TestName", config.TestName);
                    command.Parameters.AddWithValue("@Url", config.Url);
                    command.Parameters.AddWithValue("@HttpMethod", config.HttpMethod);
                    command.Parameters.AddWithValue("@Headers", (object)config.Headers ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Body", (object)config.Body ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ConcurrentUsers", config.ConcurrentUsers);
                    command.Parameters.AddWithValue("@DurationSeconds", config.DurationSeconds);
                    command.Parameters.AddWithValue("@RampUpSeconds", config.RampUpSeconds);
                    command.Parameters.AddWithValue("@TimeoutMilliseconds", config.TimeoutMilliseconds);
                    command.Parameters.AddWithValue("@Description", (object)config.Description ?? DBNull.Value);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var connection = DbConnectionFactory.Create())
            {
                connection.Open();

                using (var command = new MySqlCommand("DELETE FROM TestConfigurations WHERE Id=@Id;", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
