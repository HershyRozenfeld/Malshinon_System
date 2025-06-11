using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    internal class AlertsDAL
    {
        private readonly string _connStr = "server=localhost;user=root;password=;database=Malshinon";

        public void CreateAlert(int targetId, string reason, DateTime startTime, DateTime endTime)
        {
            string query = @"
            INSERT INTO Alerts (target_id, reason, start_time, end_time) 
            VALUES (@targetId, @reason, @startTime, @endTime);";

            using (var conn = new MySqlConnection(_connStr))
            {
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@targetId", targetId);
                cmd.Parameters.AddWithValue("@reason", reason);
                cmd.Parameters.AddWithValue("@startTime", startTime);
                cmd.Parameters.AddWithValue("@endTime", endTime);
                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException e)
                {
                    Console.WriteLine($"Database error in CreateAlert: {e.Message}");
                }
            }
        }

        public Alerts GetLastAlerts()
        {
            string query = "SELECT id, target_id, reason, start_time, end_time, created_at FROM Alerts ORDER BY created_at DESC LIMIT 1;";
            using (var conn = new MySqlConnection(_connStr))
            {
                var cmd = new MySqlCommand(query, conn);
                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int id = reader.GetInt32("id");
                            int targetId = reader.GetInt32("target_id");
                            string reason = reader.GetString("reason");
                            DateTime startTime = reader.GetDateTime("start_time");
                            DateTime endTime = reader.GetDateTime("end_time");
                            DateTime createdAt = reader.GetDateTime("created_at");
                            return new Alerts(id, targetId, reason, startTime, endTime, createdAt);
                        }
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine($"Database error in GetAlerts: {e.Message}");
                }
            }
            return null;
        }
    }
}
