using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    internal class BurstResult
    {
        public bool IsBurstFound { get; set; }
        public int ReportCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BurstResult(bool isBurstFound, int reportCount, DateTime startTime, DateTime endTime)
        {
            IsBurstFound = isBurstFound;
            ReportCount = reportCount;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}
