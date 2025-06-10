using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    internal class IntelReports
    {
        int id { get; set; }
        string reporterId { get; set; }
        string targetId { get; set; }
        string text { get; set; }
        DateTime time { get; set; }
        public IntelReports(int id, string reporterId, string targetId, string text, DateTime time)
        {
            this.id = id;
            this.reporterId = reporterId;
            this.targetId = targetId;
            this.text = text;
            this.time = time;
        }
        public IntelReports(string reporterId, string targetId, string text)
        {
            this.reporterId = reporterId;
            this.targetId = targetId;
            this.text = text;
        }
    }
}
