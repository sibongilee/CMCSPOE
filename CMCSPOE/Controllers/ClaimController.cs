using System.Data.SqlClient;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CMCSPOE.Controllers
{
    public class ClaimController : Controller
    {
        private readonly DatabaseConnection db = new DatabaseConnection();
        // Submit Claim (Lecturer)
        [HttpGet]
        public IActionResult SubmitClaim()
        {
            if (HttpContext.Session.GetString("Role") != "Lecturer")
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [HttpPost]
        public IActionResult SubmitClaim(Claim claim, IFormFile Document)// IFormFile for file upload
        {
            int? lecturerId = HttpContext.Session.GetInt32("LecturerId");
            if (lecturerId == null)
            {
                ViewBag.Message = "Error: Lecturer not found in session.";
                return View();
            }

            string fileName = "";
            string filePath = "";

            if (Document != null && Document.Length > 0)
            {
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                fileName = Path.GetFileName(Document.FileName);
                filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    Document.CopyTo(stream);
                }
            }

            using (SqlConnection con = db.GetConnection())
            {
                con.Open();
                string query = @"INSERT INTO Claims (LecturerId, Month, HoursWorked, HourlyRate, Notes, FileName, FilePath, Status)
                                 VALUES (@LecturerId, @Month, @HoursWorked, @HourlyRate, @Notes, @FileName, @FilePath, 'Pending')";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@LecturerId", lecturerId);
                cmd.Parameters.AddWithValue("@Month", claim.Month);
                cmd.Parameters.AddWithValue("@HoursWorked", claim.HoursWorked);
                cmd.Parameters.AddWithValue("@HourlyRate", claim.HourlyRate);
                cmd.Parameters.AddWithValue("@Notes", claim.Notes ?? "");
                cmd.Parameters.AddWithValue("@FileName", fileName);
                cmd.Parameters.AddWithValue("@FilePath", filePath);
                cmd.ExecuteNonQuery();
            }

            TempData["Message"] = "Claim submitted successfully!";
            return RedirectToAction("ViewMyClaims");
        }

        // View My Claims (Lecturer)
        public IActionResult ViewMyClaims()
        {
            int? lecturerId = HttpContext.Session.GetInt32("LecturerId");
            if (lecturerId == null)
                return RedirectToAction("Login", "Account");

            List<Claim> claims = new List<Claim>();
            using (SqlConnection con = db.GetConnection())
            {
                con.Open();
                string query = "SELECT * FROM Claims WHERE LecturerId=@LecturerId";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@LecturerId", lecturerId);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    claims.Add(new Claim
                    {
                        ClaimId = (int)reader["ClaimId"],
                        LecturerId = (int)reader["LecturerId"],
                        Month = reader["Month"].ToString(),
                        HoursWorked = (decimal)reader["HoursWorked"],
                        HourlyRate = (decimal)reader["HourlyRate"],
                        Notes = reader["Notes"].ToString(),
                        Status = reader["Status"].ToString(),
                        FileName = reader["FileName"].ToString()
                    });
                }
            }

            return View(claims);
        }

        // Verify Claims (PC & AM)
        public IActionResult VerifyClaims()
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Programme Coordinator" && role != "Academic Manager")
                return RedirectToAction("Index", "Dashboard");

            List<Claim> claims = new List<Claim>();
            using (SqlConnection con = db.GetConnection())
            {
                con.Open();
                string query = "SELECT * FROM Claims WHERE Status='Pending'";
                SqlCommand cmd = new SqlCommand(query, con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    claims.Add(new Claim
                    {
                        ClaimId = (int)reader["ClaimId"],
                        LecturerId = (int)reader["LecturerId"],
                        Month = reader["Month"].ToString(),
                        HoursWorked = (decimal)reader["HoursWorked"],
                        HourlyRate = (decimal)reader["HourlyRate"],
                        Notes = reader["Notes"].ToString(),
                        Status = reader["Status"].ToString(),
                        FileName = reader["FileName"].ToString()
                    });
                }
            }

            return View(claims);
        }

        [HttpPost]
        public IActionResult ApproveOrReject(int claimId, string decision)
        {
            using (SqlConnection con = db.GetConnection())
            {
                con.Open();
                string query = "UPDATE Claims SET Status=@Decision WHERE ClaimId=@ClaimId";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Decision", decision);
                cmd.Parameters.AddWithValue("@ClaimId", claimId);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("VerifyClaims");
        }

    }
}
