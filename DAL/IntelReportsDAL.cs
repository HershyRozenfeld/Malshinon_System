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

        // Adds a new intelligence report to the database.
        /// Inserts a new IntelReports record.
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

        // Adds a new intelligence report with a custom timestamp.
        /// Inserts a new IntelReports record with a specified timestamp.
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

        // Increments the report count for a given person.
        /// Updates the num_reports field for a person by incrementing it.
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

        // Increments the mention count for a given person.
        /// Updates the num_mentions field for a person by incrementing it.
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

        // Retrieves statistics for all reporters, including average report length.
        /// Gets a list of ReporterStats for all reporters.
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

        // Retrieves statistics for a single reporter.
        /// Gets ReporterStats for a specific reporter by ID.
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

        // Retrieves statistics for all targets, including mention counts.
        /// Gets a list of TargetStats for all targets with mentions.
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

        // Retrieves all timestamps for reports about a specific target.
        /// Gets a list of timestamps for a given target's reports.
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
