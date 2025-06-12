using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Malshinon
{
    /// <summary>
    /// Data access layer for IntelReports and related Person statistics.
    /// Contains methods for inserting reports, updating counters, and retrieving statistics.
    /// </summary>
    internal class IntelReportsDAL
    {
        private readonly string _connStr = "server=localhost;user=root;password=;database=Malshinon";

        
        /// Inserts a new IntelReports record.
        /// <param name="intelReport">The IntelReports object containing the report data to insert.</param>
        public void InsertIntelReport(IntelReports intelReport)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                var query = @"
                            INSERT INTO IntelReports (reporter_id, target_id, text)
                            VALUES (@reporterId, @targetId, @text);";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@reporterId", intelReport.reporterId);
                    cmd.Parameters.AddWithValue("@targetId", intelReport.targetId);
                    cmd.Parameters.AddWithValue("@text", intelReport.text);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        

        /// Inserts a new IntelReports record with a specified timestamp.
        /// <param name="intelReport">The IntelReports object containing the report data to insert.</param>
        /// <param name="ts">The timestamp to use for the report.</param>
        public void InsertIntelReportWithTimestamp(IntelReports intelReport, DateTime ts)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                const string q = @"
                        INSERT INTO IntelReports (reporter_id, target_id, text, timestamp)
                        VALUES (@reporterId, @targetId, @text, @ts);";

                using (var cmd = new MySqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@reporterId", intelReport.reporterId);
                    cmd.Parameters.AddWithValue("@targetId", intelReport.targetId);
                    cmd.Parameters.AddWithValue("@text", intelReport.text);
                    cmd.Parameters.AddWithValue("@ts", ts);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        
        /// Updates the num_reports field for a person by incrementing it.
        /// <param name="personId">The ID of the person whose report count will be incremented.</param>
        public void UpdateReportCount(int personId)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                const string query =
                    "UPDATE Person SET num_reports = num_reports + 1 WHERE id = @id;";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", personId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        
        /// Updates the num_mentions field for a person by incrementing it.
        /// <param name="personId">The ID of the person whose mention count will be incremented.</param>
        public void UpdateMentionCount(int personId)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                const string query =
                    "UPDATE Person SET num_mentions = num_mentions + 1 WHERE id = @id;";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", personId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        
        /// Gets a list of ReporterStats for all reporters.
        /// <returns>A list of ReporterStats objects containing statistics for all reporters.</returns>
        public List<ReporterStats> GetReporterStats()
        {
            var statsList = new List<ReporterStats>();

            var query = @"
                        SELECT  p.first_name,
                                p.last_name,
                                p.num_reports,
                                COALESCE(AVG(CHAR_LENGTH(ir.text)),0) AS avg_report_length
                        FROM    Person p
                        LEFT JOIN IntelReports ir ON p.id = ir.reporter_id
                        WHERE   p.type IN ('reporter','both','potential_agent')
                        GROUP BY p.id
                        ORDER BY p.num_reports DESC;";

            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        statsList.Add(new ReporterStats(
                            reader.GetString("first_name"),
                            reader.GetString("last_name"),
                            reader.GetInt32("num_reports"),
                            reader.GetDouble("avg_report_length")));
                    }
                }
            }
            return statsList;
        }

        public List<IntelReports> GetAllIntelReports()
        {
            var reports = new List<IntelReports>();
            string query = "SELECT id, reporter_id, target_id, text, timestamp FROM IntelReports ORDER BY timestamp DESC;";
            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // יש לוודא שקונסטרוקטור זה קיים במחלקת IntelReports
                        reports.Add(new IntelReports(
                            reader.GetInt32("id"),
                            reader.GetInt32("reporter_id"),
                            reader.GetInt32("target_id"),
                            reader.GetString("text"),
                            reader.GetDateTime("timestamp")
                        ));
                    }
                }
            }
            return reports;
        }

        /// Gets ReporterStats for a specific reporter by ID.
        /// <param name="reporterId">The ID of the reporter.</param>
        /// <returns>The ReporterStats object for the specified reporter, or null if not found.</returns>
        public ReporterStats GetStatsForSingleReporter(int reporterId)
        {
            ReporterStats stats = null;

            const string query = @"
                        SELECT  p.first_name,
                                p.last_name,
                                p.num_reports,
                                COALESCE(AVG(CHAR_LENGTH(ir.text)),0) AS avg_report_length
                        FROM    Person p
                        LEFT JOIN IntelReports ir ON p.id = ir.reporter_id
                        WHERE   p.id = @rid
                        GROUP BY p.id;";

            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@rid", reporterId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            stats = new ReporterStats(
                                reader.GetString("first_name"),
                                reader.GetString("last_name"),
                                reader.GetInt32("num_reports"),
                                reader.GetDouble("avg_report_length"));
                        }
                    }
                }
            }
            return stats;
        }

        
        /// Gets a list of TargetStats for all targets with mentions.
        /// <returns>A list of TargetStats objects containing statistics for all targets with mentions.</returns>
        public List<TargetStats> GetTargetStats()
        {
            var list = new List<TargetStats>();

            const string query = @"
                        SELECT  first_name,
                                last_name,
                                num_mentions
                        FROM    Person
                        WHERE   type IN ('target','both') AND num_mentions > 0
                        ORDER BY num_mentions DESC;";

            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new TargetStats(
                            reader.GetString("first_name"),
                            reader.GetString("last_name"),
                            reader.GetInt32("num_mentions")));
                    }
                }
            }
            return list;
        }

        
        /// Gets a list of timestamps for a given target's reports.
        /// <param name="targetId">The ID of the target person.</param>
        /// <returns>A list of DateTime objects representing the timestamps of reports about the target.</returns>
        public List<DateTime> GetTimestampsForTarget(int targetId)
        {
            var times = new List<DateTime>();

            const string query = @"
                        SELECT  timestamp
                        FROM    IntelReports
                        WHERE   target_id = @targetId
                        ORDER BY timestamp ASC;";

            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))     
                {
                    cmd.Parameters.AddWithValue("@targetId", targetId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            times.Add(reader.GetDateTime("timestamp"));
                        }
                    }
                }
            }
            return times;
        }
    }
}
