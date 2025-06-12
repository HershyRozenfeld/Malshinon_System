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
        /// <param name="intelReportsDal">The data access layer for intelligence reports and statistics.</param>
        /// <param name="alertsDal">The data access layer for alerts.</param>
        public static void ShowMenu(
            CsvImporter csvImporter,
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
                        FindBurstActivity(intelReportsDal);
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
            for (int i = 0; i < list.Count; i++)
            {
                ReporterStats s = list[i];
                if (s.TotalReports >= 10 && s.AverageReportLength >= 100)
                {
                    Console.WriteLine($"{i + 1}. {s.FirstName} {s.LastName} – {s.TotalReports} reports, average length {s.AverageReportLength:F0} characters");
                }
            }
        }

        /// Prints a list of dangerous targets based on target statistics.
        /// Only targets with at least 20 mentions are shown.
        /// <param name="intelReportsDal">The data access layer for intelligence reports and statistics.</param>
        private static void PrintDangerousTargets(IntelReportsDAL intelReportsDal)
        {
            List<TargetStats> list = intelReportsDal.GetTargetStats();
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
            Console.WriteLine($"\n--- Latest Alert ---\nTarget: {a.TargetId}\nReason: {a.Reason}\nTime: {a.CreatedAt}");
        }

        /// Finds and displays burst activity for a target by full name.
        private static void FindBurstActivity(IntelReportsDAL intelReportsDal)
        {
            Console.Write("Enter target's full name: ");
            string fullName = Console.ReadLine();
            string[] nameParts = fullName.Split(new[] { ' ' }, 2);
            if (nameParts.Length < 2)
            {
                Console.WriteLine("Please enter both first and last name.");
                return;
            }

            var personDal = new PersonDAL();
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
    }
}
