using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Malshinon
{
    internal class Program
    {
        // Assumption: DAL Instances are accessible to the entire class
        private static readonly PersonDAL _personDal = new PersonDAL();
        private static readonly IntelReportsDAL _intelReportsDal = new IntelReportsDAL();
        private static readonly AlertsDAL _alertsDal = new AlertsDAL();
        private static CsvImporter _csv;

        static void Main(string[] args)
        {
            // Create CSV importer object
            _csv = new CsvImporter(_personDal, _intelReportsDal);

            ShowMenu();
        }

        /// <summary>
        /// Main interactive menu
        /// </summary>
        private static void ShowMenu()
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n=== Malshinon System – Main Menu ===");
                Console.ResetColor();
                Console.WriteLine("1. Submit manual report");
                Console.WriteLine("2. Import CSV");
                Console.WriteLine("3. Show potential agents");
                Console.WriteLine("4. Show dangerous targets");
                Console.WriteLine("5. Show latest alerts");
                Console.WriteLine("0. Exit");
                Console.Write("Choice: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        HandleNewReport();
                        break;

                    case "2":
                        Console.Write("Enter path to CSV file: ");
                        string path = Console.ReadLine();
                        _csv.Import(path);
                        break;

                    case "3":
                        PrintPotentialAgents();
                        break;

                    case "4":
                        PrintDangerousTargets();
                        break;

                    case "5":
                        PrintLastAlert();
                        break;

                    case "0":
                        return;

                    default:
                        Console.WriteLine("Invalid choice, please try again.");
                        break;
                }
            }
        }

        private static void PrintPotentialAgents()
        {
            List<ReporterStats> list = _intelReportsDal.GetReporterStats();
            Console.WriteLine("\n--- Potential Agents ---");
            for (int i = 0; i < list.Count; i++)
            {
                ReporterStats s = list[i];
                if (s.TotalReports >= 10 && s.AverageReportLength >= 100)
                {
                    Console.WriteLine($"{i + 1}. {s.FirstName} {s.LastName} – {s.TotalReports} reports, average length {s.AverageReportLength:F0} characters");
                }
            }
        }

        private static void PrintDangerousTargets()
        {
            List<TargetStats> list = _intelReportsDal.GetTargetStats();
            Console.WriteLine("\n--- Dangerous Targets ---");
            for (int i = 0; i < list.Count; i++)
            {
                TargetStats t = list[i];
                if (t.MentionCount >= 20)
                {
                    Console.WriteLine($"{i + 1}. {t.FirstName} {t.LastName} – {t.MentionCount} mentions");
                }
            }
        }

        private static void PrintLastAlert()
        {
            Alerts a = _alertsDal.GetLastAlerts();
            if (a == null)
            {
                Console.WriteLine("\nNo alerts recorded.");
                return;
            }
            Console.WriteLine($"\n--- Latest Alert ---\nTarget: {a.TargetId}\nReason: {a.Reason}\nTime: {a.CreatedAt}");
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

            // --- שלב 5 (עתידי): בדיקת ספים והתראות ---
            // נוסיף כאן את הלוגיקה לבדיקת קידום סוכנים והתראות בהמשך
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
            // Person.numMentions is not public, so use TargetStats from DAL
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
                // נרצה למנוע התראות כפולות על אותו אירוע, לכן נבדוק את ההתראה האחרונה
                var lastAlert = _alertsDal.GetLastAlerts();
                if (lastAlert == null || lastAlert.Reason == null || !lastAlert.Reason.Contains("total mentions"))
                {
                    _alertsDal.CreateAlert(updatedTarget.id, reason, DateTime.Now, DateTime.Now);
                }
            }

            // בדיקה 2: האם היה פרץ דיווחים?
            var burstResult = new Program().FindBurstActivityForTarget(updatedTarget.id);
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

        // זו הפונקציה המקורית שלך לניתוח פרצי פעילות. נשלב אותה בהמשך.
        public BurstResult FindBurstActivityForTarget(int targetId)
        {
            List<DateTime> timestamps = _intelReportsDal.GetTimestampsForTarget(targetId);

            if (timestamps.Count < 3)
            {
                return new BurstResult { IsBurstFound = false };
            }

            int n = timestamps.Count;
            // The original logic had a potential issue of returning too early.
            // This improved version finds the best possible burst.
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
                        // We found a burst, check if it's better than a previous one
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