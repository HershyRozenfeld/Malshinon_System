using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    internal class PersonDAL
    {
        private readonly string _connStr = "server=localhost;user=root;password=;database=Malshinon";
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
        public Person GetPersonByName(string first_name, string last_name)
        {
            Person person = null;
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = "SELECT * FROM Person WHERE first_name = @first_name AND last_name = @last_name";
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
