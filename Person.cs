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
    internal class Person
    {
        public int id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string secretCode { get; set; }
        public PersonType type { get; set; } = PersonType.Reporter;
        int numReports { get; set; }
        int numMentions { get; set; }
        public Person(int id, string firstName, string lastName, string secretCode, PersonType type, int numReports, int numMentions)
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
        public Person(string firstName, string lastName, string secretCode)
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
