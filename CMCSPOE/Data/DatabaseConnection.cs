using System.Data.SqlClient;
namespace CMCSPOE.Data
{
    public class DatabaseConnection
    {
        // connection string to connect to the SQL Server database
        private static string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=CMCSPOE;Integrated Security=True;";
        // method to get a sql connection instance
        public SqlConnection GetConnection()
        {
            SqlConnection connection = new SqlConnection(connectionString);
            return connection;
        }
    }
}

