using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    /// Represents the type of a person in the system.
    public enum PersonType
    {
        /// A person who reports information.
        Reporter,
        /// A person who is the target of reports.
        Target,
        /// A person who is both a reporter and a target.
        Both,
        /// A person who is a potential agent.
        PotentialAgent
    }
    /// Represents a person with identifying information and classification.
    internal class Person
    {
        public int id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        /// Gets or sets the secret code for the person.
        public string secretCode { get; set; }
        /// Gets or sets the type of the person.
        public PersonType type { get; set; } = PersonType.Reporter;
        /// The number of reports associated with the person.
        int numReports { get; set; }
        /// The number of mentions associated with the person.
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
        /// Generates a secret code based on the person's first and last name.
        private void SecretCodeMaker()
        {
            string combined = firstName + lastName;
            int hash = combined.GetHashCode();
            this.secretCode = Math.Abs(hash).ToString();
        }
    }
}
