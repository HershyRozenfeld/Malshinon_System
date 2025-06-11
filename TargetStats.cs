using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    internal class TargetStats
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int MentionCount { get; set; }

        public TargetStats(string firstName, string lastName, int mentionCount)
        {
            FirstName = firstName;
            LastName = lastName;
            MentionCount = mentionCount;
        }
    }
}
