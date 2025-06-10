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

        public void UpdateReportCount()
        {

        }

        public void UpdateMentionCount()
        {

        }

        public void GetReporterStats()
        {

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
