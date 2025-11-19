using System.Data.SqlClient;

namespace CMCSPOE.Data
{
    public class DatabaseConnection
    {
        // Corrected the typo "Intial" -> "Initial"
        private static readonly string connectionString = 
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=CMCSPOE;Integrated Security=True;";

        /// <summary>
        /// Returns a new SqlConnection configured with the project's connection string.
        /// The connection is returned closed; caller should Open() and Dispose() it.
        /// </summary>
        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        /// <summary>
        /// Tests whether the database can be opened with the configured connection string.
        /// </summary>
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