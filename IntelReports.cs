using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    internal class IntelReports
    {
        public int id { get; set; }
        public int reporterId { get; set; }
        public int targetId { get; set; }
        public string text { get; set; }
        public DateTime time { get; set; }
        public IntelReports(int id, int reporterId, int targetId, string text, DateTime time)
        {
            this.id = id;
            this.reporterId = reporterId;
            this.targetId = targetId;
            this.text = text;
            this.time = time;
        }
        public IntelReports(int reporterId, int targetId, string text, DateTime time)
        {
            this.id = id;
            this.reporterId = reporterId;
            this.targetId = targetId;
            this.text = text;
            this.time = time;
        }
        public IntelReports(int reporterId, int targetId, string text)
        {
            this.reporterId = reporterId;
            this.targetId = targetId;
            this.text = text;
        }
    }
}
