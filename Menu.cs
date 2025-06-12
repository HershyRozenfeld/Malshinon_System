using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    /// <summary>
    /// Provides the main menu interface for the Malshinon system.
    /// Handles user interaction and delegates actions to the appropriate handlers and data access layers.
    /// </summary>
    internal static class Menu
    {
        /// Displays the main menu and processes user input in a loop until exit is selected.
        /// <param name="csvImporter">The CSV importer for importing reports from files.</param>
        /// <param name="personDal">The data access layer for person entities. (חדש)</param>
        /// <param name="intelReportsDal">The data access layer for intelligence reports and statistics.</param>
        /// <param name="alertsDal">The data access layer for alerts.</param>
        public static void ShowMenu(
            CsvImporter csvImporter,
            PersonDAL personDal, // פרמטר חדש
            IntelReportsDAL intelReportsDal,
            AlertsDAL alertsDal)
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
                Console.WriteLine("6. Find burst activity for target");
                Console.WriteLine("7. Person Management"); // אופציה חדשה
                Console.WriteLine("8. Show All Intel Reports"); // אופציה חדשה
                Console.WriteLine("9. Show All Alerts History"); // אופציה חדשה
                Console.WriteLine("0. Exit");
                Console.Write("Choice: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ReportHandler.HandleNewReport();
                        break;

                    case "2":
                        Console.Write("Enter path to CSV file: ");
                        string path = Console.ReadLine();
                        csvImporter.Import(path);
                        break;

                    case "3":
                        PrintPotentialAgents(intelReportsDal);
                        break;

                    case "4":
                        PrintDangerousTargets(intelReportsDal);
                        break;

                    case "5":
                        PrintLastAlert(alertsDal);
                        break;

                    case "6":
                        FindBurstActivity(intelReportsDal, personDal); // העברת personDal
                        break;

                    case "7": // מקרה חדש
                        ShowPersonManagementMenu(personDal); // PersonDAL מספיק כאן
                        break;

                    case "8": // מקרה חדש
                        PrintAllIntelReports(intelReportsDal, personDal); // צריך את שניהם
                        break;

                    case "9": // מקרה חדש
                        PrintAllAlertsHistory(alertsDal, personDal); // צריך את שניהם
                        break;

                    case "0":
                        return;

                    default:
                        Console.WriteLine("Invalid choice, please try again.");
                        break;
                }
            }
        }

        /// Prints a list of potential agents based on reporter statistics.
        /// Only reporters with at least 10 reports and an average report length of at least 100 characters are shown.
        /// <param name="intelReportsDal">The data access layer for intelligence reports and statistics.</param>
        private static void PrintPotentialAgents(IntelReportsDAL intelReportsDal)
        {
            List<ReporterStats> list = intelReportsDal.GetReporterStats();
            Console.WriteLine("\n--- Potential Agents ---");
            // שינוי קטן: אם אין מדווחים או אף אחד לא עומד בקריטריונים
            if (list == null || list.Count == 0)
            {
                Console.WriteLine("No potential agents found.");
                return;
            }

            bool found = false;
            for (int i = 0; i < list.Count; i++)
            {
                ReporterStats s = list[i];
                if (s.TotalReports >= 10 && s.AverageReportLength >= 100)
                {
                    Console.WriteLine($"{i + 1}. {s.FirstName} {s.LastName} – {s.TotalReports} reports, average length {s.AverageReportLength:F0} characters");
                    found = true;
                }
            }
            if (!found)
            {
                Console.WriteLine("No potential agents found matching criteria.");
            }
        }

        /// Prints a list of dangerous targets based on target statistics.
        /// Only targets with at least 20 mentions are shown.
        /// <param name="intelReportsDal">The data access layer for intelligence reports and statistics.</param>
        private static void PrintDangerousTargets(IntelReportsDAL intelReportsDal)
        {
            List<TargetStats> list = intelReportsDal.GetTargetStats();
            Console.WriteLine("\n--- Dangerous Targets ---");
            // שינוי קטן: אם אין יעדים או אף אחד לא עומד בקריטריונים
            if (list == null || list.Count == 0)
            {
                Console.WriteLine("No dangerous targets found.");
                return;
            }

            bool found = false;
            for (int i = 0; i < list.Count; i++)
            {
                TargetStats t = list[i];
                if (t.MentionCount >= 20)
                {
                    Console.WriteLine($"{i + 1}. {t.FirstName} {t.LastName} – {t.MentionCount} mentions");
                    found = true;
                }
            }
            if (!found)
            {
                Console.WriteLine("No dangerous targets found matching criteria.");
            }
        }

        /// Prints the most recent alert from the alerts data access layer.
        /// If no alerts are recorded, a message is displayed.
        /// <param name="alertsDal">The data access layer for alerts.</param>
        private static void PrintLastAlert(AlertsDAL alertsDal)
        {
            Alerts a = alertsDal.GetLastAlerts();
            if (a == null)
            {
                Console.WriteLine("\nNo alerts recorded.");
                return;
            }
            // שינוי: הוספת שם היעד אם אפשר (דורש GetPersonById מ-PersonDAL)
            // Person target = personDal.GetPersonById(a.TargetId); // אם היית מעביר PersonDAL לכאן
            // string targetName = target != null ? $"{target.firstName} {target.lastName}" : $"ID: {a.TargetId}";
            Console.WriteLine($"\n--- Latest Alert ---\nTarget ID: {a.TargetId}\nReason: {a.Reason}\nTime: {a.CreatedAt}");
        }

        /// Finds and displays burst activity for a target by full name.
        // שינוי: קבלת PersonDAL כפרמטר
        private static void FindBurstActivity(IntelReportsDAL intelReportsDal, PersonDAL personDal)
        {
            Console.Write("Enter target's full name: ");
            string fullName = Console.ReadLine();
            string[] nameParts = fullName.Split(new[] { ' ' }, 2);
            if (nameParts.Length < 2)
            {
                Console.WriteLine("Please enter both first and last name.");
                return;
            }

            // הסרת יצירת PersonDAL חדש - השתמש בפרמטר שהועבר
            var person = personDal.GetPersonByName(nameParts[0], nameParts[1]);
            if (person == null)
            {
                Console.WriteLine("Target not found.");
                return;
            }

            var burst = ReportHandler.FindBurstActivityForTarget(person.id);
            if (burst.IsBurstFound)
            {
                Console.WriteLine($"Burst found: {burst.ReportCount} reports between {burst.StartTime} and {burst.EndTime}");
            }
            else
            {
                Console.WriteLine("No burst activity found for this target.");
            }
        }

        // --- מתודות חדשות עבור "Person Management" ---
        private static void ShowPersonManagementMenu(PersonDAL personDal) // רק personDal נחוץ כאן
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n=== Person Management ===");
                Console.ResetColor();
                Console.WriteLine("1. List All Persons");
                Console.WriteLine("2. Find Person by Secret Code");
                Console.WriteLine("3. Update Person Type");
                Console.WriteLine("0. Back to Main Menu");
                Console.Write("Choice: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        PrintAllPersons(personDal);
                        break;
                    case "2":
                        FindPersonBySecretCode(personDal);
                        break;
                    case "3":
                        UpdatePersonType(personDal);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Invalid choice, please try again.");
                        break;
                }
            }
        }

        private static void PrintAllPersons(PersonDAL personDal)
        {
            List<Person> persons = personDal.GetAllPersons();
            Console.WriteLine("\n--- All Persons ---");
            if (persons.Count == 0)
            {
                Console.WriteLine("No persons in the system.");
                return;
            }
            for (int i = 0; i < persons.Count; i++)
            {
                Person p = persons[i];
                Console.WriteLine($"{i + 1}. ID: {p.id}, Name: {p.firstName} {p.lastName}, Type: {p.type}, Secret Code: {p.secretCode}, Reports: {p.numReports}, Mentions: {p.numMentions}");
            }
        }

        private static void FindPersonBySecretCode(PersonDAL personDal)
        {
            Console.Write("Enter secret code (8 hex chars): ");
            string secretCode = Console.ReadLine();
            Person p = personDal.GetPersonBySecretCode(secretCode);
            if (p != null)
            {
                Console.WriteLine($"\n--- Person Found ---");
                Console.WriteLine($"ID: {p.id}, Name: {p.firstName} {p.lastName}, Type: {p.type}, Secret Code: {p.secretCode}, Reports: {p.numReports}, Mentions: {p.numMentions}");
            }
            else
            {
                Console.WriteLine("Person not found with this secret code.");
            }
        }

        private static void UpdatePersonType(PersonDAL personDal)
        {
            Console.Write("Enter person's full name to update type: ");
            string fullName = Console.ReadLine();
            string[] nameParts = fullName.Split(new[] { ' ' }, 2);
            if (nameParts.Length < 2)
            {
                Console.WriteLine("Please enter both first and last name.");
                return;
            }

            Person p = personDal.GetPersonByName(nameParts[0], nameParts[1]);
            if (p == null)
            {
                Console.WriteLine("Person not found.");
                return;
            }

            Console.WriteLine($"Current type for {p.firstName} {p.lastName}: {p.type}");
            Console.WriteLine("Available types: Reporter, Target, Both, PotentialAgent");
            Console.Write("Enter new type: ");
            string newTypeString = Console.ReadLine();

            if (Enum.TryParse(newTypeString, true, out PersonType newType))
            {
                personDal.UpdatePersonType(p.id, newType);
                Console.WriteLine($"Type for {p.firstName} {p.lastName} updated to {newType}.");
            }
            else
            {
                Console.WriteLine("Invalid person type entered. Please use one of: Reporter, Target, Both, PotentialAgent.");
            }
        }

        // --- מתודות חדשות עבור "Show All Intel Reports" ו-"Show All Alerts History" ---

        private static void PrintAllIntelReports(IntelReportsDAL intelReportsDal, PersonDAL personDal)
        {
            List<IntelReports> reports = intelReportsDal.GetAllIntelReports();
            Console.WriteLine("\n--- All Intel Reports ---");
            if (reports.Count == 0)
            {
                Console.WriteLine("No intel reports in the system.");
                return;
            }

            foreach (var report in reports)
            {
                // כדי להציג שמות במקום ID, נשלוף אותם באמצעות PersonDAL.
                // הערה: שליפה פרטנית לכל דוח בנפרד עלולה להיות לא יעילה עבור מספר גדול של דוחות (בעיית N+1).
                // פתרון יעיל יותר יהיה לשלב את שמות המדווח והיעד כבר בשאילתת ה-SQL ב-GetAllIntelReports.
                Person reporter = personDal.GetPersonById(report.reporterId);
                Person target = personDal.GetPersonById(report.targetId);

                string reporterName = reporter != null ? $"{reporter.firstName} {reporter.lastName}" : $"ID:{report.reporterId}";
                string targetName = target != null ? $"{target.firstName} {target.lastName}" : $"ID:{report.targetId}";

                Console.WriteLine($"ID: {report.id}, Reporter: {reporterName}, Target: {targetName}, Text: \"{report.text}\", Timestamp: {report.time}");
            }
        }

        private static void PrintAllAlertsHistory(AlertsDAL alertsDal, PersonDAL personDal)
        {
            List<Alerts> alerts = alertsDal.GetAllAlerts();
            Console.WriteLine("\n--- All Alerts History ---");
            if (alerts.Count == 0)
            {
                Console.WriteLine("No alerts recorded.");
                return;
            }

            foreach (var alert in alerts)
            {
                // כדי להציג שמות יעדים במקום ID, נשלוף אותם באמצעות PersonDAL.
                // הערה: גם כאן, עדיף לשלב את השם בשאילתת ה-SQL ב-GetAllAlerts אם ביצועים קריטיים.
                Person target = personDal.GetPersonById(alert.TargetId);
                string targetName = target != null ? $"{target.firstName} {target.lastName}" : $"ID:{alert.TargetId}";

                Console.WriteLine($"ID: {alert.Id}, Target: {targetName}, Reason: {alert.Reason}, Time: {alert.CreatedAt} (Start: {alert.StartTime}, End: {alert.EndTime})");
            }
        }
    }
}