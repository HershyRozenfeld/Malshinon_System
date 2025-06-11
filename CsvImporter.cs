using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Malshinon
{
    /// <summary>
    /// Class for importing reports from a CSV file
    /// </summary>
    internal class CsvImporter
    {
        private readonly PersonDAL _personDal;
        private readonly IntelReportsDAL _intelDal;

        // --- ctor ---
        public CsvImporter(PersonDAL personDal, IntelReportsDAL intelDal)
        {
            _personDal = personDal;
            _intelDal = intelDal;
        }

        /// <summary>
        /// Reads a CSV file and inserts the reports into the database
        /// </summary>
        public void Import(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("⚠ File not found: " + path);
                return;
            }

            string[] lines = File.ReadAllLines(path);

            // Iterate over each line using a for loop (as requested)
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;      // Skip empty lines

                string[] cols = line.Split(',');

                // Check: exactly 4 columns
                if (cols.Length != 4)
                {
                    Console.WriteLine($"⚠ Line {i + 1}: 4 columns required, skipping.");
                    continue;
                }

                string reporterRef = cols[0].Trim();
                string targetRef = cols[1].Trim();
                string text = cols[2].Trim();
                string tsString = cols[3].Trim();

                // Date – if invalid → now
                DateTime ts;
                if (!DateTime.TryParse(tsString, out ts))
                {
                    Console.WriteLine($"⚠ Line {i + 1}: Invalid timestamp, using DateTime.Now.");
                    ts = DateTime.Now;
                }

                // --- Find/create reporter ---
                Person reporter = ResolvePerson(reporterRef, PersonType.Reporter);
                // --- Find/create target ---
                Person target = ResolvePerson(targetRef, PersonType.Target);

                if (reporter == null || target == null)
                {
                    Console.WriteLine($"⚠ Line {i + 1}: Could not locate/create reporter or target, skipping.");
                    continue;
                }

                // Insert the report
                IntelReports rep = new IntelReports(reporter.id, target.id, text, ts);
                _intelDal.InsertIntelReportWithTimestamp(rep, ts);

                // Update counters
                _intelDal.UpdateReportCount(reporter.id);
                _intelDal.UpdateMentionCount(target.id);

                // Threshold analysis and alerts (from Program class)
                Program.AnalyzeAndAlert(reporter, target);
            }

            Console.WriteLine("\n✅ Import completed successfully.");
        }

        // -------------------------------------------------
        //  Helper function: Get Person by secret code or full name
        // -------------------------------------------------
        private Person ResolvePerson(string reference, PersonType defaultType)
        {
            // If a secret code was entered (8 hex characters) – identify by code
            Regex codeRegex = new Regex(@"^[0-9a-fA-F]{8}$");
            if (codeRegex.IsMatch(reference))
            {
                Person byCode = _personDal.GetPersonBySecretCode(reference);
                if (byCode != null) return byCode;

                Console.WriteLine($"⚠ Code does not exist in the system: {reference}");
                return null; // Do not create a new user based on an invalid code
            }

            // Otherwise – treat as "FirstName LastName"
            string[] parts = reference.Split(new[] { ' ' }, 2);
            if (parts.Length < 2) parts = new[] { reference, "Unknown" };

            Person p = _personDal.GetPersonByName(parts[0], parts[1]);
            if (p == null)
            {
                // Create new user
                string secret = Guid.NewGuid().ToString("N").Substring(0, 8);
                p = new Person(0, parts[0], parts[1], secret, defaultType, 0, 0);
                _personDal.InsertNewPerson(p);
                p = _personDal.GetPersonByName(parts[0], parts[1]);
            }
            return p;
        }       
    }
}
