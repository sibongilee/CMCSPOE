using System.Data.SqlClient;
using System.Text;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Mvc;

namespace CMCSPOE.Controllers
{
    public class HrController : Controller
    {
        public IActionResult Index()
        {
            List<HrReport> reports = new List<HrReport>();
            DatabaseConnection db = new DatabaseConnection();

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT c.ClaimID, l.FullName, c.HoursWorked, c.HourlyRate,
                           (c.HoursWorked * c.HourlyRate) AS FinalAmount,
                           c.ApprovedDate
                    FROM Claims c
                    INNER JOIN Lecturers l ON c.LecturerID = l.LecturerID
                    WHERE c.Status = 'Approved'
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        reports.Add(new HrReport
                        {
                            ClaimId = (int)reader["ClaimID"],
                            LecturerName = reader["FullName"].ToString(),
                            HoursWorked = Convert.ToDecimal(reader["HoursWorked"]),
                            HourlyRate = Convert.ToDecimal(reader["HourlyRate"]),
                            TotalAmount = Convert.ToDecimal(reader["FinalAmount"]),
                            ApprovedDate = Convert.ToDateTime(reader["ApprovedDate"])
                        });
                    }
                }
            }

            return View(reports);
        }

        // Download CSV Report
        public IActionResult DownloadReport()
        {
            StringBuilder csv = new StringBuilder();

            csv.AppendLine("ClaimID,Lecturer,HoursWorked,HourlyRate,FinalAmount,ApprovedDate");

            DatabaseConnection db = new DatabaseConnection();

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT c.ClaimID, l.FullName, c.HoursWorked, c.HourlyRate,
                           (c.HoursWorked * c.HourlyRate) AS FinalAmount,
                           c.ApprovedDate
                    FROM Claims c
                    INNER JOIN Lecturers l ON c.LecturerID = l.LecturerID
                    WHERE c.Status = 'Approved'
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    SqlDataReader r = cmd.ExecuteReader();

                    while (r.Read())
                    {
                        csv.AppendLine(
                            $"{r["ClaimID"]},{r["FullName"]},{r["HoursWorked"]},{r["HourlyRate"]},{r["FinalAmount"]},{r["ApprovedDate"]}"
                        );
                    }
                }
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "HR_ApprovedClaims_Report.csv");
        }
    }
}
