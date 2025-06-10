using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Malshinon
{
    internal class PeopelDAL
    {
        private readonly string _connStr = "server=localhost;user=root;password=;database=Malshinon";
        private People MapReaderToPeople(MySqlDataReader reader)
        {
            var typeString = reader.GetString(reader.GetOrdinal("type"));
            PersonType type = (PersonType)Enum.Parse(typeof(PersonType), typeString, true);
            return new People(
                reader.GetInt32("id"),
                reader.GetString("first_name"),
                reader.GetString("last_name"),
                reader.GetString("secret_code"),
                type,
                reader.GetInt32("num_reports"),
                reader.GetInt32("num_mentions")
            );
        }
        public People GetPersonByName(string first_name, string last_name)
        {
            People people = null;
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = "SELECT * FROM People WHERE first_name = @first_name AND last_name = @last_name";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@first_name", first_name);
                        cmd.Parameters.AddWithValue("@last_name", last_name);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                people = MapReaderToPeople(reader);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error fetching people by name: " + e.Message);
                }
            }
            return people;
        }
        public People GetPersonBySecretCode(string secret_code)
        {
            People people = null;
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = "SELECT * FROM People WHERE secret_code = @secret_code";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@secret_code", secret_code);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                people = MapReaderToPeople(reader);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error fetching people by secret_code: " + e.Message);
                }
            }
            return people;
        }
        public void InsertNewPerson(People people)
        {
            using (var conn = new MySqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    var query = @"INSERT INTO People (first_name, last_name, secret_code, type)
                                        VALUES (@first_name, @last_name, @secret_code, @type)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@first_name", people.firstName);
                        cmd.Parameters.AddWithValue("@last_name", people.lastName);
                        cmd.Parameters.AddWithValue("@secret_code", people.secretCode);
                        cmd.Parameters.AddWithValue("@type", people.type);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error INSERT people to the DB: " + e.Message);
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
                    var query = "SELECT 1 FROM People WHERE first_name = @first_name AND last_name = @last_name LIMIT 1";
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
                    var query = "SELECT 1 FROM People WHERE secret_code = @secret_code LIMIT 1";
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

    }
}
