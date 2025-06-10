using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    internal class ReporterStats
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int TotalReports { get; set; }
        public double AverageReportLength { get; set; }
        public ReporterStats(string firstName, string lastName, int totalReports, double averageReportLength)
        {
            FirstName = firstName;
            LastName = lastName;
            TotalReports = totalReports;
            AverageReportLength = averageReportLength;
        }
    }
}
