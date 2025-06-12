using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Malshinon
{
    internal class Program
    {
        // DAL instances are accessible to the entire class
        private static readonly PersonDAL _personDal = new PersonDAL();
        private static readonly IntelReportsDAL _intelReportsDal = new IntelReportsDAL();
        private static readonly AlertsDAL _alertsDal = new AlertsDAL();
        private static CsvImporter _csvImporter;

        /// <summary>
        /// Main entry point. Initializes handlers and shows the main menu.
        /// </summary>
        static void Main(string[] args)
        {
            // Initialize ReportHandler with DALs
            ReportHandler.Initialize(_personDal, _intelReportsDal, _alertsDal);

            // Create CSV importer object
            _csvImporter = new CsvImporter(_personDal, _intelReportsDal);

            // Use Menu class for main menu
            Menu.ShowMenu(_csvImporter, _intelReportsDal, _alertsDal);
        }

        /// Handles the process of submitting a new intel report.
        public static void HandleNewReport()
        {
            // Step 1: Identify the reporter
            Console.WriteLine("\nPlease identify yourself (enter your full name):");
            string reporterName = Console.ReadLine();
            Person reporter = GetOrCreatePerson(reporterName, PersonType.Reporter);

            if (reporter == null)
            {
                Console.WriteLine("Error: Could not create or retrieve reporter. Please try again later.");
                return;
            }

            Console.WriteLine($"Welcome, {reporter.firstName}. Your secret code is: {reporter.secretCode}");

            // Step 2: Receive the report and identify the target
            Console.WriteLine("\nPlease enter your intel report. Make sure to mention the target's full name (e.g., 'Israel Israeli').");
            string reportText = Console.ReadLine();

            Person target = ExtractAndVerifyTarget(reportText);
            if (target == null)
            {
                Console.WriteLine("Could not identify a valid target in the report. Aborting.");
                return;
            }
            Console.WriteLine($"Target identified: {target.firstName} {target.lastName}");

            // Step 3: Save the report to the database
            IntelReports newReport = new IntelReports(reporter.id, target.id, reportText);
            _intelReportsDal.InsertIntelReport(newReport);
            Console.WriteLine("Intel report successfully submitted.");

            // Step 4: Update statistics
            _intelReportsDal.UpdateReportCount(reporter.id);
            _intelReportsDal.UpdateMentionCount(target.id);
            Console.WriteLine("Reporter and target stats have been updated.");
        }

        /// Analyzes the reporter and target for promotion or alert conditions.
        public static void AnalyzeAndAlert(Person reporter, Person target)
        {
            // Check if the reporter should be promoted
            CheckAndPromoteReporter(reporter);

            // Check if the target should be flagged as dangerous
            CheckAndFlagTarget(target);
        }

        /// Checks if a reporter meets the criteria for promotion to potential agent.
        private static void CheckAndPromoteReporter(Person reporter)
        {
            // Only proceed if the person is currently a REPORTER
            if (reporter.type != PersonType.Reporter) return;

            // Fetch up-to-date data from the DB
            Person dbReporter = _personDal.GetPersonByName(reporter.firstName, reporter.lastName);
            if (dbReporter == null) return;

            ReporterStats stats = _intelReportsDal.GetStatsForSingleReporter(dbReporter.id);
            if (stats == null) return;

            bool enoughReports = stats.TotalReports >= 10;
            bool longText = stats.AverageReportLength >= 100;

            if (enoughReports && longText)
            {
                // Change type to PotentialAgent
                _personDal.UpdatePersonType(dbReporter.id, PersonType.PotentialAgent);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n*** Promotion! {reporter.firstName} {reporter.lastName} has been set as a potential agent ***\n");
                Console.ResetColor();

                // Create an alert to log this event
                string reason =
                    $"Promoted to potential agent after {stats.TotalReports} reports with average length {stats.AverageReportLength:F0} characters.";
                _alertsDal.CreateAlert(dbReporter.id, reason, DateTime.Now, DateTime.Now);
            }
        }

        /// Checks if a target meets the criteria for being flagged as dangerous or for burst activity.
        private static void CheckAndFlagTarget(Person target)
        {
            // Fetch the updated version of the target from the DB to get the correct mention count
            var updatedTarget = _personDal.GetPersonByName(target.firstName, target.lastName);
            if (updatedTarget == null) return;

            // Check 1: Has the target reached the threshold of 20 mentions?
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
                // Prevent duplicate alerts for the same event by checking the last alert
                var lastAlert = _alertsDal.GetLastAlerts();
                if (lastAlert == null || lastAlert.Reason == null || !lastAlert.Reason.Contains("total mentions"))
                {
                    _alertsDal.CreateAlert(updatedTarget.id, reason, DateTime.Now, DateTime.Now);
                }
            }

            // Check 2: Was there a burst of reports?
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

        /// Gets an existing person by full name or creates a new one if not found.
        private static Person GetOrCreatePerson(string fullName, PersonType defaultType)
        {
            string[] nameParts = fullName.Split(new[] { ' ' }, 2);
            if (nameParts.Length < 2)
            {
                // If the user did not enter a full name, use a default last name
                nameParts = new[] { fullName, "Unknown" };
            }

            string firstName = nameParts[0];
            string lastName = nameParts[1];

            Person person = _personDal.GetPersonByName(firstName, lastName);

            if (person == null) // If the person does not exist, create a new one
            {
                Console.WriteLine("Person not found. Creating a new record.");
                string secretCode = Guid.NewGuid().ToString("N").Substring(0, 8); // Generate a unique secret code

                person = new Person(0, firstName, lastName, secretCode, defaultType, 0, 0);
                _personDal.InsertNewPerson(person);

                // After insertion, fetch the person again to get their ID
                person = _personDal.GetPersonByName(firstName, lastName);
            }

            return person;
        }

        /// Extracts and verifies the target's name from the report text.
        private static Person ExtractAndVerifyTarget(string reportText)
        {
            // Regex to identify a name in the format "FirstName LastName" with capital letters at the start of each word
            var regex = new Regex(@"\b[A-Z][a-z]+ [A-Z][a-z]+\b");
            Match match = regex.Match(reportText);

            if (match.Success)
            {
                string targetName = match.Value;
                // Use the existing function to create the person if not found
                return GetOrCreatePerson(targetName, PersonType.Target);
            }

            return null;
        }

        /// Analyzes report timestamps for a target to find burst activity (3+ reports in 15 minutes).
        public BurstResult FindBurstActivityForTarget(int targetId)
        {
            List<DateTime> timestamps = _intelReportsDal.GetTimestampsForTarget(targetId);

            if (timestamps.Count < 3)
            {
                return new BurstResult { IsBurstFound = false };
            }

            int n = timestamps.Count;
            // This logic finds the best possible burst window
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
                        // Found a burst, check if it's better than a previous one
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