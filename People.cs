using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    public enum PersonType
    {
        Reporter,
        Target,
        Both,
        PotentialAgent
    }
    internal class People
    {
        int id { get; set; }
        string firstName { get; set; }
        string lastName { get; set; }
        string secretCode { get; set; }
        PersonType type { get; set; } = PersonType.Reporter;
        int numReports { get; set; }
        int numMentions { get; set; }
        public People(int id, string firstName, string lastName, string secretCode, PersonType type, int numReports, int numMentions)
        {
            this.id = id;
            this.firstName = firstName;
            this.lastName = lastName;
            this.secretCode = secretCode;
            this.type = type;
            this.numReports = numReports;
            this.numMentions = numMentions;
            SecretCodeMaker();
        }
        public People(string firstName, string lastName, string secretCode)
        {
            this.firstName = firstName;
            this.lastName = lastName;
            this.secretCode = secretCode;
            SecretCodeMaker();
        }
        private void SecretCodeMaker()
        {
            string combined = firstName + lastName;
            int hash = combined.GetHashCode();
            this.secretCode = Math.Abs(hash).ToString();
        }
    }
}
