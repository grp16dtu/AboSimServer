using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace EcoTest.Models
{
    public class MySQL
    {
        private MySqlConnection MySqlConnection { get; set; }
        private string server { get; set; }
        private string database { get; set; }
        private string userName { get; set; }
        private string password { get; set; }

        public MySQL()
        {
            server = "mysql8.gigahost.dk";
            database = "redlaz_ooad";
            userName = "redlaz";
            password = "Iben1234";
            Initialize();
        }

        private void Initialize()
        {
            string connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + userName + ";" + "PASSWORD=" + password + ";";

            MySqlConnection = new MySqlConnection(connectionString);
        }

        private void OpenConnection()
        {
            MySqlConnection.Open();
        }

        private void CloseConnection()
        {
            MySqlConnection.Close();
        }

        public void InsertTransactions(List<Transaktion> transactions )
        {
            OpenConnection();
            string query = "INSERT INTO transactions (year, month, debtorNumber, productNumber, quantity, amount) VALUES";

            foreach (Transaktion transaction in transactions)
            {
                query += "('" + transaction.Aar + "', '"+transaction.Maaned+"', '"+transaction.Debitornummer+"', '"+transaction.Produktnummer+"', '" +transaction.Antal+"', '"+transaction.Beloeb+"'),";
            }
            
            query = query.Remove(query.Length - 1, 1);
            query += " ON DUPLICATE KEY UPDATE quantity = quantity + VALUES(quantity), amount= amount + VALUES(amount)";
            
            MySqlCommand cmd = new MySqlCommand(query, MySqlConnection);    
            cmd.ExecuteNonQuery();
            CloseConnection();
            
        }
    }
}
