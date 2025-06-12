using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    /// <summary>
    /// Data access layer for Person entities.
    /// Provides methods for inserting, updating, and retrieving Person records.
    /// </summary>
    internal class PersonDAL
    {
        private readonly string _connStr = "server=localhost;user=root;password=;database=Malshinon";

        
        /// Maps a MySqlDataReader row to a Person object.
        /// <param name="reader">The data reader containing person data.</param>
        /// <returns>A Person object populated from the reader.</returns>
        private Person MapReaderToPeople(MySqlDataReader reader)
        {
            var typeString = reader.GetString(reader.GetOrdinal("type"));
            PersonType type = (PersonType)Enum.Parse(typeof(PersonType), typeString, true);
            return new Person(
                reader.GetInt32("id"),
                reader.GetString("first_name"),
                reader.GetString("last_name"),
                reader.GetString("secret_code"),
                type,
                reader.GetInt32("num_reports"),
                reader.GetInt32("num_mentions")
            );
        }

        
        /// Retrieves a person by their first and last name.
        /// <param name="first_name">The first name of the person.</param>
        /// <param name="last_name">The last name of the person.</param>
        /// <returns>The Person object if found; otherwise, null.</returns>
        public Person GetPersonByName(string first_name, string last_name)
        {
            Person person = null;
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = "SELECT * FROM Person WHERE first_name = @first_name AND last_name = @last_name;";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@first_name", first_name);
                        cmd.Parameters.AddWithValue("@last_name", last_name);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                person = MapReaderToPeople(reader);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error fetching person by name: " + e.Message);
                }
            }
            return person;
        }

        
        /// Retrieves a person by their secret code.
        /// <param name="secret_code">The secret code of the person.</param>
        /// <returns>The Person object if found; otherwise, null.</returns>
        public Person GetPersonBySecretCode(string secret_code)
        {
            Person person = null;
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = "SELECT * FROM Person WHERE secret_code = @secret_code";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@secret_code", secret_code);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                person = MapReaderToPeople(reader);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error fetching person by secret_code: " + e.Message);
                }
            }
            return person;
        }

        public List<Person> GetAllPersons()
        {
            List<Person> persons = new List<Person>();
            string query = "SELECT * FROM Person;";
            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        persons.Add(MapReaderToPeople(reader));
                    }
                }
            }
            return persons;
        }

        /// Inserts a new person into the database.
        /// <param name="person">The Person object to insert.</param>
        public void InsertNewPerson(Person person)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = @"INSERT INTO Person (first_name, last_name, secret_code, type)
                                            VALUES (@first_name, @last_name, @secret_code, @type)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@first_name", person.firstName);
                        cmd.Parameters.AddWithValue("@last_name", person.lastName);
                        cmd.Parameters.AddWithValue("@secret_code", person.secretCode);
                        cmd.Parameters.AddWithValue("@type", person.type);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error INSERT person to the DB: " + e.Message);
                }
            }
        }

        
        /// Checks if a person exists in the database by their first and last name.
        /// <param name="first_name">The first name of the person.</param>
        /// <param name="last_name">The last name of the person.</param>
        /// <returns>True if the person exists; otherwise, false.</returns>
        public bool PersonExistsByName(string first_name, string last_name)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = "SELECT 1 FROM Person WHERE first_name = @first_name AND last_name = @last_name LIMIT 1";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@first_name", first_name);
                        cmd.Parameters.AddWithValue("@last_name", last_name);
                        using (var reader = cmd.ExecuteReader())
                        {
                            return reader.Read();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error checking person existence by name: " + e.Message);
                    return false;
                }
            }
        }

        
        /// Checks if a person exists in the database by their secret code.
        /// <param name="secret_code">The secret code of the person.</param>
        /// <returns>True if the person exists; otherwise, false.</returns>
        public bool PersonExistsBySecretCode(string secret_code)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = "SELECT 1 FROM Person WHERE secret_code = @secret_code LIMIT 1";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@secret_code", secret_code);
                        using (var reader = cmd.ExecuteReader())
                        {
                            return reader.Read();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error checking person existence by secret_code: " + e.Message);
                    return false;
                }
            }
        }

        
        /// Updates the type of a person in the database.
        /// <param name="personId">The ID of the person to update.</param>
        /// <param name="newType">The new PersonType to set.</param>
        public void UpdatePersonType(int personId, PersonType newType)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = "UPDATE Person SET type = @type WHERE id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", personId);
                        cmd.Parameters.AddWithValue("@type", newType.ToString()); // Convert enum to string
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error updating person type: " + e.Message);
                }
            }
        }
    }
}
