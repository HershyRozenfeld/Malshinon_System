using Malshinon.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    internal class Program
    {
        public BurstResult FindBurstActivityForTarget(int targetId)
        {
            var dal = new IntelReportsDAL();
            List<DateTime> timestamps = dal.GetTimestampsForTarget(targetId);

            if (timestamps.Count < 3)
            {
                return new BurstResult { IsBurstFound = false };
            }

            int n = timestamps.Count;
            for (int i = 0; i < n - 2; i++)
            {
                for (int j = i + 2; j < n; j++)
                {
                    DateTime windowStart = timestamps[i];
                    DateTime windowEnd = timestamps[j];
                    int reportCount = j - i + 1;

                    if ((windowEnd - windowStart).TotalMinutes <= 15)
                    {
                        return new BurstResult()
                        {
                            IsBurstFound = true,
                            ReportCount = reportCount,
                            StartTime = windowStart,
                            EndTime = windowEnd
                        };
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return new BurstResult { IsBurstFound = false };
        }
        static void Main(string[] args)
        {
        }
    }
}
