using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Malshinon
{
    internal static class ReportHandler
    {
        private static PersonDAL _personDal;
        private static IntelReportsDAL _intelReportsDal;
        private static AlertsDAL _alertsDal;

        // יש לקרוא לפונקציה הזו פעם אחת לפני שימוש במחלקה
        public static void Initialize(PersonDAL personDal, IntelReportsDAL intelReportsDal, AlertsDAL alertsDal)
        {
            _personDal = personDal;
            _intelReportsDal = intelReportsDal;
            _alertsDal = alertsDal;
        }

        public static void HandleNewReport()
        {
            // --- שלב 1: זיהוי המדווח ---
            Console.WriteLine("\nPlease identify yourself (enter your full name):");
            string reporterName = Console.ReadLine();
            Person reporter = GetOrCreatePerson(reporterName, PersonType.Reporter);

            if (reporter == null)
            {
                Console.WriteLine("Error: Could not create or retrieve reporter. Please try again later.");
                return;
            }

            Console.WriteLine($"Welcome, {reporter.firstName}. Your secret code is: {reporter.secretCode}");

            // --- שלב 2: קבלת הדיווח וזיהוי המטרה ---
            Console.WriteLine("\nPlease enter your intel report. Make sure to mention the target's full name (e.g., 'Israel Israeli').");
            string reportText = Console.ReadLine();

            Person target = ExtractAndVerifyTarget(reportText);
            if (target == null)
            {
                Console.WriteLine("Could not identify a valid target in the report. Aborting.");
                return;
            }
            Console.WriteLine($"Target identified: {target.firstName} {target.lastName}");

            // --- שלב 3: שמירת הדיווח במסד הנתונים ---
            IntelReports newReport = new IntelReports(reporter.id, target.id, reportText);
            _intelReportsDal.InsertIntelReport(newReport);
            Console.WriteLine("Intel report successfully submitted.");

            // --- שלב 4: עדכון מדדים ---
            _intelReportsDal.UpdateReportCount(reporter.id);
            _intelReportsDal.UpdateMentionCount(target.id);
            Console.WriteLine("Reporter and target stats have been updated.");
        }

        public static void AnalyzeAndAlert(Person reporter, Person target)
        {
            // בדיקה 1: האם לקדם את המדווח?
            CheckAndPromoteReporter(reporter);

            // בדיקה 2: האם המטרה הפכה למסוכנת?
            CheckAndFlagTarget(target);
        }

        private static void CheckAndPromoteReporter(Person reporter)
        {
            // נמשיך רק אם האדם הוא כרגע REPORTER (אחרת אין מה לקדם)
            if (reporter.type != PersonType.Reporter) return;

            // שולפים נתונים עדכניים מה-DB
            Person dbReporter = _personDal.GetPersonByName(reporter.firstName, reporter.lastName);
            if (dbReporter == null) return;

            ReporterStats stats = _intelReportsDal.GetStatsForSingleReporter(dbReporter.id);
            if (stats == null) return;

            bool די_דוחות = stats.TotalReports >= 10;
            bool טקסט_ארוך = stats.AverageReportLength >= 100;

            if (די_דוחות && טקסט_ארוך)
            {
                // *** שינוי הסוג ל-potential_agent ***
                _personDal.UpdatePersonType(dbReporter.id, PersonType.PotentialAgent);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n*** קידום! {reporter.firstName} {reporter.lastName} הוגדר כ-סוכן פוטנציאלי ***\n");
                Console.ResetColor();

                // ניצור Alert כדי לציין זאת ביומן
                string reason =
                    $"קודם לסוכן פוטנציאלי בעקבות {stats.TotalReports} דוחות עם אורך ממוצע {stats.AverageReportLength:F0} תווים.";
                _alertsDal.CreateAlert(dbReporter.id, reason, DateTime.Now, DateTime.Now);
            }
        }

        private static void CheckAndFlagTarget(Person target)
        {
            // שולפים את הגרסה המעודכנת של המטרה מה-DB כדי לקבל את מספר האזכורים הנכון
            var updatedTarget = _personDal.GetPersonByName(target.firstName, target.lastName);
            if (updatedTarget == null) return;

            // בדיקה 1: האם המטרה הגיעה לסף 20 האזכורים?
            var targetStatsList = _intelReportsDal.GetTargetStats();
            var targetStats = targetStatsList?.FirstOrDefault(ts =>
                ts.FirstName == updatedTarget.firstName && ts.LastName == updatedTarget.lastName);
            int mentionCount = targetStats?.MentionCount ?? 0;

            if (mentionCount >= 20)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"*** DANGEROUS TARGET ALERT: {updatedTarget.firstName} {updatedTarget.lastName} has reached {mentionCount} mentions! ***");
                Console.ResetColor();
                string reason = $"Flagged as dangerous due to reaching {mentionCount} total mentions.";
                var lastAlert = _alertsDal.GetLastAlerts();
                if (lastAlert == null || lastAlert.Reason == null || !lastAlert.Reason.Contains("total mentions"))
                {
                    _alertsDal.CreateAlert(updatedTarget.id, reason, DateTime.Now, DateTime.Now);
                }
            }

            // בדיקה 2: האם היה פרץ דיווחים?
            var burstResult = FindBurstActivityForTarget(updatedTarget.id);
            if (burstResult.IsBurstFound)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"*** BURST ACTIVITY ALERT: {updatedTarget.firstName} {updatedTarget.lastName} has {burstResult.ReportCount} reports within 15 minutes! ***");
                Console.ResetColor();
                string reason = $"Burst activity detected: {burstResult.ReportCount} reports between {burstResult.StartTime} and {burstResult.EndTime}.";

                var lastAlert = _alertsDal.GetLastAlerts();
                if (lastAlert == null || lastAlert.Reason == null || !lastAlert.Reason.Contains("Burst activity"))
                {
                    _alertsDal.CreateAlert(updatedTarget.id, reason, burstResult.StartTime, burstResult.EndTime);
                }
            }
        }

        private static Person GetOrCreatePerson(string fullName, PersonType defaultType)
        {
            string[] nameParts = fullName.Split(new[] { ' ' }, 2);
            if (nameParts.Length < 2)
            {
                // אם המשתמש לא הכניס שם מלא, נבקש שוב או ניצור שם ברירת מחדל
                nameParts = new[] { fullName, "Unknown" };
            }

            string firstName = nameParts[0];
            string lastName = nameParts[1];

            Person person = _personDal.GetPersonByName(firstName, lastName);

            if (person == null) // אם המשתמש לא קיים, ניצור אחד חדש
            {
                Console.WriteLine("Person not found. Creating a new record.");
                string secretCode = Guid.NewGuid().ToString("N").Substring(0, 8); // יצירת קוד סודי ייחודי 

                person = new Person(0, firstName, lastName, secretCode, defaultType, 0, 0);
                _personDal.InsertNewPerson(person);

                // חשוב: לאחר ההוספה, צריך לשלוף את המשתמש שוב כדי לקבל את ה-ID שלו
                person = _personDal.GetPersonByName(firstName, lastName);
            }

            return person;
        }

        private static Person ExtractAndVerifyTarget(string reportText)
        {
            // Regex לזיהוי שם בפורמט "שם פרטי שם משפחה" עם אותיות גדולות בתחילת כל מילה
            var regex = new Regex(@"\b[A-Z][a-z]+ [A-Z][a-z]+\b");
            Match match = regex.Match(reportText);

            if (match.Success)
            {
                string targetName = match.Value;
                // לאחר שמצאנו שם, נשתמש בפונקציה הקיימת כדי ליצור אותו אם הוא לא קיים
                return GetOrCreatePerson(targetName, PersonType.Target);
            }

            return null;
        }

        public static BurstResult FindBurstActivityForTarget(int targetId)
        {
            List<DateTime> timestamps = _intelReportsDal.GetTimestampsForTarget(targetId);

            if (timestamps.Count < 3)
            {
                return new BurstResult { IsBurstFound = false };
            }

            int n = timestamps.Count;
            BurstResult bestBurst = new BurstResult { IsBurstFound = false };

            for (int i = 0; i <= n - 3; i++)
            {
                for (int j = i + 2; j < n; j++)
                {
                    DateTime windowStart = timestamps[i];
                    DateTime windowEnd = timestamps[j];
                    int reportCount = j - i + 1;

                    if ((windowEnd - windowStart).TotalMinutes <= 15)
                    {
                        if (!bestBurst.IsBurstFound || reportCount > bestBurst.ReportCount)
                        {
                            bestBurst.IsBurstFound = true;
                            bestBurst.ReportCount = reportCount;
                            bestBurst.StartTime = windowStart;
                            bestBurst.EndTime = windowEnd;
                        }
                    }
                }
            }
            return bestBurst;
        }
    }
}