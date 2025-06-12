using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Malshinon
{
    /// <summary>
    /// Handles the process of receiving, processing, and analyzing intel reports,
    /// including person identification, report storage, and alerting logic.
    /// </summary>
    internal static class ReportHandler
    {
        private static PersonDAL _personDal;
        private static IntelReportsDAL _intelReportsDal;
        private static AlertsDAL _alertsDal;

        /// Initializes the <see cref="ReportHandler"/> with required data access layers.
        /// This method must be called once before using any other methods in this class.
        /// <param name="personDal">The data access layer for person entities.</param>
        /// <param name="intelReportsDal">The data access layer for intel reports.</param>
        /// <param name="alertsDal">The data access layer for alerts.</param>
        public static void Initialize(PersonDAL personDal, IntelReportsDAL intelReportsDal, AlertsDAL alertsDal)
        {
            _personDal = personDal;
            _intelReportsDal = intelReportsDal;
            _alertsDal = alertsDal;
        }

        /// Handles the process of submitting a new intel report, including reporter identification,
        /// target extraction, report storage, and statistics update.
        public static void HandleNewReport()
        {
            Console.WriteLine("\nPlease identify yourself (enter your full name):");
            string reporterName = Console.ReadLine();
            Person reporter = GetOrCreatePerson(reporterName, PersonType.Reporter);

            if (reporter == null)
            {
                Console.WriteLine("Error: Could not create or retrieve reporter. Please try again later.");
                return;
            }

            Console.WriteLine($"Welcome, {reporter.firstName}. Your secret code is: {reporter.secretCode}");

            Console.WriteLine("\nPlease enter your intel report. Make sure to mention the target's full name (e.g., 'Israel Israeli').");
            string reportText = Console.ReadLine();

            Person target = ExtractAndVerifyTarget(reportText);
            if (target == null)
            {
                Console.WriteLine("Could not identify a valid target in the report. Aborting.");
                return;
            }
            Console.WriteLine($"Target identified: {target.firstName} {target.lastName}");

            IntelReports newReport = new IntelReports(reporter.id, target.id, reportText);
            _intelReportsDal.InsertIntelReport(newReport);
            Console.WriteLine("Intel report successfully submitted.");

            _intelReportsDal.UpdateReportCount(reporter.id);
            _intelReportsDal.UpdateMentionCount(target.id);
            Console.WriteLine("Reporter and target stats have been updated.");
        }

        /// Analyzes the reporter and target after a report is submitted, checking for promotion or danger alerts.
        /// <param name="reporter">The reporter person object.</param>
        /// <param name="target">The target person object.</param>
        public static void AnalyzeAndAlert(Person reporter, Person target)
        {
            CheckAndPromoteReporter(reporter);
            CheckAndFlagTarget(target);
        }

        /// Checks if the reporter qualifies for promotion to <see cref="PersonType.PotentialAgent"/>
        /// based on their reporting statistics, and promotes if criteria are met.
        /// <param name="reporter">The reporter to check and potentially promote.</param>
        private static void CheckAndPromoteReporter(Person reporter)
        {
            // Continue only if the person is currently a REPORTER (otherwise, nothing to promote)
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
                _personDal.UpdatePersonType(dbReporter.id, PersonType.PotentialAgent);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n*** Promotion! {reporter.firstName} {reporter.lastName} has been promoted to Potential Agent ***\n");
                Console.ResetColor();

                string reason =
                    $"Promoted to Potential Agent due to {stats.TotalReports} reports with an average length of {stats.AverageReportLength:F0} characters.";
                _alertsDal.CreateAlert(dbReporter.id, reason, DateTime.Now, DateTime.Now);
            }
        }

        /// Checks if the target should be flagged as dangerous or if burst activity is detected,
        /// and creates alerts if necessary.
        /// <param name="target">The target person to check.</param>
        private static void CheckAndFlagTarget(Person target)
        {
            var updatedTarget = _personDal.GetPersonByName(target.firstName, target.lastName);
            if (updatedTarget == null) return;

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

        /// Retrieves a person by full name, or creates a new person if not found.
        /// <param name="fullName">The full name of the person (first and last name).</param>
        /// <param name="defaultType">The default <see cref="PersonType"/> to assign if creating a new person.</param>
        /// <returns>The <see cref="Person"/> object, or null if creation failed.</returns>
        private static Person GetOrCreatePerson(string fullName, PersonType defaultType)
        {
            string[] nameParts = fullName.Split(new[] { ' ' }, 2);
            if (nameParts.Length < 2)
            {
                nameParts = new[] { fullName, "Unknown" };
            }

            string firstName = nameParts[0];
            string lastName = nameParts[1];

            Person person = _personDal.GetPersonByName(firstName, lastName);

            if (person == null)
            {
                Console.WriteLine("Person not found. Creating a new record.");
                string secretCode = Guid.NewGuid().ToString("N").Substring(0, 8);

                person = new Person(0, firstName, lastName, secretCode, defaultType, 0, 0);
                _personDal.InsertNewPerson(person);

                person = _personDal.GetPersonByName(firstName, lastName);
            }

            return person;
        }

        /// Extracts a target's full name from the report text and verifies or creates the target person.
        /// <param name="reportText">The text of the intel report.</param>
        /// <returns>The <see cref="Person"/> object representing the target, or null if not found.</returns>
        private static Person ExtractAndVerifyTarget(string reportText)
        {
            var regex = new Regex(@"\b[A-Z][a-z]+ [A-Z][a-z]+\b");
            Match match = regex.Match(reportText);

            if (match.Success)
            {
                string targetName = match.Value;
                return GetOrCreatePerson(targetName, PersonType.Target);
            }

            return null;
        }

        /// Finds burst activity for a target, defined as at least 3 reports within a 15-minute window.
        /// <param name="targetId">The ID of the target person.</param>
        /// <returns>A <see cref="BurstResult"/> indicating whether a burst was found, the count, and the time window.</returns>
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