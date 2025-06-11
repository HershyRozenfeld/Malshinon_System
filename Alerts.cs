using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    internal class Alerts
    {
        public int Id { get; set; }
        public int TargetId { get; set; }
        public string Reason { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime CreatedAt { get; set; }

        public Alerts(int id, int targetId, string reason, DateTime startTime, DateTime endTime, DateTime createdAt)
        {
            Id = id;
            TargetId = targetId;
            Reason = reason;
            StartTime = startTime;
            EndTime = endTime;
            CreatedAt = createdAt;
        }

        public Alerts(int targetId, string reason, DateTime startTime, DateTime endTime)
        {
            TargetId = targetId;
            Reason = reason;
            StartTime = startTime;
            EndTime = endTime;
            CreatedAt = DateTime.Now;
        }

        public Alerts() { }
    }
}
