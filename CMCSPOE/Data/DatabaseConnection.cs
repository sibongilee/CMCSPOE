using System.Data.SqlClient;
namespace CMCSPOE.Data
{
    public class DatabaseConnection
    {
        private static string connectionString = "Server=localhost;Database=master;Trusted_Connection=True;";
        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
        public bool TestConnection()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}