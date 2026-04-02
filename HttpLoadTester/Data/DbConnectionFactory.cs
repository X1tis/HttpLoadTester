using System;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace HttpLoadTester.Data
{
    public static class DbConnectionFactory
    {
        public static MySqlConnection Create()
        {
            string connectionString = ConfigurationManager.AppSettings["MySqlConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("В App.config не найдена строка подключения MySqlConnectionString.");

            return new MySqlConnection(connectionString);
        }
    }
}
