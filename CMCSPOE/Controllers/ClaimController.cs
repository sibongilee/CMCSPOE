using System.Data.SqlClient;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace CMCSPOE.Controllers
{
    public class ClaimController : Controller
    {
        private readonly DatabaseConnection db = new DatabaseConnection();
        private readonly IWebHostEnvironment _env;
        public ClaimController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Submit Claim (Lecturer)
        [HttpGet]
        public IActionResult SubmitClaim()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SubmitClaim(Claim claim)
        {
            try
            {
                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string sql = @"INSERT INTO Claims 
                    (LecturerId, HoursWorked, HourlyRate, Notes, Status, DateSubmitted)
                    VALUES (@LecturerId, @HoursWorked, @HourlyRate, @Notes, 'Pending', GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@LecturerId", claim.LecturerId);
                        cmd.Parameters.AddWithValue("@HoursWorked", claim.HoursWorked);
                        cmd.Parameters.AddWithValue("@HourlyRate", claim.HourlyRate);
                        cmd.Parameters.AddWithValue("@Notes", claim.Notes ?? "");

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Claim submitted successfully!";
                return RedirectToAction("ViewClaims");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error submitting claim: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult UploadDocuments(int id)
        {
            ViewBag.ClaimId = id;
            return View();
        }

        [HttpPost]
        public IActionResult UploadDocuments(int claimId, IFormFile document)
        {
            if (document == null)
            {
                TempData["Error"] = "Please select a file.";
                return View();
            }

            try
            {
                // folder
                string folder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string filePath = Path.Combine(folder, document.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    document.CopyTo(stream);
                }

                // save file name to DB
                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();
                    string sql = "UPDATE Claims SET DocumentPath=@Document WHERE ClaimId=@ClaimId";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Document", document.FileName);
                        cmd.Parameters.AddWithValue("@ClaimId", claimId);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Document uploaded successfully!";
                return RedirectToAction("ViewClaims");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error uploading file: " + ex.Message;
                return View();
            }
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
                        HoursWorked = (int)reader["HoursWorked"],
                        HourlyRate = (decimal)reader["HourlyRate"],
                        Notes = reader["Notes"].ToString(),
                        Status = reader["Status"].ToString(),
                
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
                        HoursWorked = (int)reader["HoursWorked"],
                        HourlyRate = (decimal)reader["HourlyRate"],
                        Notes = reader["Notes"].ToString(),
                        Status = reader["Status"].ToString(),
                       
                    });
                }
            }

            return View(claims);
        }

        [HttpGet]
        public IActionResult VerifyClaim(int id)
        {
            Claim claim = new Claim();

            using (SqlConnection con = db.GetConnection())
            {
                con.Open();
                string query = @"SELECT c.ClaimId, u.FullName, c.HoursWorked, c.RatePerHour, 
                                c.TotalAmount, c.ClaimStatus, c.SupportingDocument, c.Remarks
                         FROM Claims c
                         JOIN Lecturers l ON c.LecturerId = l.LecturerId
                         JOIN Users u ON l.UserId = u.UserId
                         WHERE c.ClaimId = @ClaimId";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ClaimId", id);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    claim.ClaimId = (int)reader["ClaimId"];
                    claim.HoursWorked = (int)reader["HoursWorked"];
                    claim.HourlyRate = (decimal)reader["RatePerHour"];
                    claim.DocumentPath = reader["SupportingDocument"].ToString();
                    claim.Status = reader["ClaimStatus"].ToString();
                }
            }

            return View(claim);
        }

        [HttpPost]
        public IActionResult VerifyClaim(int ClaimId, string action)
        {
            string newStatus = action == "Approve" ? "Approved" : "Rejected";

            using (SqlConnection con = db.GetConnection())
            {
                con.Open();
                string updateQuery = "UPDATE Claims SET ClaimStatus = @Status WHERE ClaimId = @ClaimId";
                SqlCommand cmd = new SqlCommand(updateQuery, con);
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@ClaimId", ClaimId);
                cmd.ExecuteNonQuery();
            }

            TempData["Message"] = $"Claim #{ClaimId} has been {newStatus.ToLower()} successfully.";
            return RedirectToAction("ApproveClaim");
        }
    }
}





