using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Malshinon
{
    internal class IntelReportsDAL
    {
        // מחרוזת חיבור – לא לשמור בקוד-מקור בפרויקט אמיתי
        private readonly string _connStr =
            "server=localhost;user=root;password=;database=Malshinon";

        /*--------------------------------------------------------
         * 1. הוספת דיווח חדש
         *-------------------------------------------------------*/
        public void InsertIntelReport(IntelReports intelReport)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                const string query = @"
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

        /*--------------------------------------------------------
         * 2. עדכון מונה דיווחים של מדווח
         *-------------------------------------------------------*/
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

        /*--------------------------------------------------------
         * 3. עדכון מונה אזכורים של מטרה
         *-------------------------------------------------------*/
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

        public List<ReporterStats> GetReporterStats()
        {
            var statsList = new List<ReporterStats>();

            const string query = @"
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

        /*--------------------------------------------------------
         * 5. סטטיסטיקות מטרות
         *    >>>  num_mentions  <<<  (תוקן)
         *-------------------------------------------------------*/
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

        /*--------------------------------------------------------
         * 6. החזרת כל ה-timestamps של מטרה נתונה
         *    >>>  target_id, timestamp  <<<  (תוקן)
         *-------------------------------------------------------*/
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

        /*--------------------------------------------------------
         * 7. סטטיסטיקה למדווח יחיד (לצורך קידום)
         *-------------------------------------------------------*/
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
        /*--------------------------------------------------------
 *  new – הכנסת דיווח עם חותמת זמן מותאמת
 *-------------------------------------------------------*/
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

    }
}
