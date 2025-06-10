using System;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon.DAL
{
    internal class IntelReportsDAL
    {
        private readonly string _connStr = "server=localhost;user=root;password=;database=Malshinon";
        public void InsertIntelReport(IntelReports intelReports)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = @"INSERT INTO IntelReports (reporter_id, target_id, text)
                                  VALUES (@reporter_id, @target_id, @text)";
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@reporter_id", intelReports.reporterId);
                        cmd.Parameters.AddWithValue("@target_id", intelReports.targetId);
                        cmd.Parameters.AddWithValue("@text", intelReports.text);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error INSERT IntelReport to the DB: " + e.Message);
                }
            }
        }

        public void UpdateReportCount(string peopelId)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = @"UPDATE People SET num_reports = num_reports + 1 WHERE id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", peopelId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error updating report count: " + e.Message);
                }
            }
        }

        public void UpdateMentionCount(string peopelId)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = @"UPDATE People SET num_mentions = num_mentions + 1 WHERE id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", peopelId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error updating mentions count: " + e.Message);
                }
            }
        }

        public List<ReporterStats> GetReporterStats()
        {
            var statsList = new List<ReporterStats>();

            string query = @"
                           SELECT 
                               p.first_name, 
                               p.last_name, 
                               p.num_reports,
                               COALESCE(AVG(CHAR_LENGTH(ir.text)), 0) AS avg_report_length
                           FROM 
                               People p
                           LEFT JOIN 
                               IntelReports ir ON p.id = ir.reporter_id
                           WHERE 
                               p.type IN ('reporter', 'both', 'potential_agent')
                           GROUP BY 
                               p.id, p.first_name, p.last_name, p.num_reports
                           ORDER BY
                               p.num_reports DESC;
                            ";

            using (var connection = new MySqlConnection(_connStr))
            {
                var command = new MySqlCommand(query, connection);
                try
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var stats = new ReporterStats(
                                reader.GetString("first_name"),
                                reader.GetString("last_name"),
                                reader.GetInt32("num_reports"),
                                reader.GetDouble("avg_report_length")
                            );

                            statsList.Add(stats);
                        }
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine($"Database error in GetReporterStats: {e.Message}");
                    // In case of an error, return an empty list so as not to crash the entire program
                    return new List<ReporterStats>();
                }
            }
            return statsList;
        }

        public void GetTargetStats()
        {

        }

        public void CreateAlert()
        {

        }

        public void GetAlerts()
        {

        }
    }
}
