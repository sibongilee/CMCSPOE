using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CMCSPOE.Controllers
{
    [AllowAnonymous]
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
        public IActionResult ViewClaims()
        {
            var list = new List<Claim>();
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null) return RedirectToAction("Login", "Account");

                // Map user -> lecturer id
                int lecturerId = 0;
                using (var con = db.GetConnection())
                {
                    con.Open();
                    using (var cmd = new SqlCommand("SELECT TOP 1 LecturerId FROM Lecturer WHERE UserId = @UserId", con))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId.Value);
                        var r = cmd.ExecuteScalar();
                        if (r == null) { ViewBag.Error = "Lecturer profile not found.";
                            return View(list);
                        }
                        lecturerId = Convert.ToInt32(r);
                    }

                    string sql = "SELECT * FROM Claims WHERE LecturerId = @LecturerId ORDER BY DateSubmitted DESC";
                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@LecturerId", lecturerId);
                        using (var rd = cmd.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                list.Add(new Claim
                                {
                                    ClaimId = Convert.ToInt32(rd["ClaimId"]),
                                    LecturerId = Convert.ToInt32(rd["LecturerId"]),
                                    HoursWorked = Convert.ToInt32(rd["HoursWorked"]),
                                    HourlyRate = Convert.ToDecimal(rd["HourlyRate"]),
                                    TotalAmount = rd["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(rd["TotalAmount"]),
                                    Notes = rd["Description"] == DBNull.Value ? null : rd["Description"].ToString(),
                                    Status = rd["Status"].ToString(),
                                    DocumentPath = rd["DocumentPath"] == DBNull.Value ? null : rd["DocumentPath"].ToString(),
                                    ViolationReasons = rd["ViolationReasons"] == DBNull.Value ? null : rd["ViolationReasons"].ToString(),
                                    DateSubmitted = rd["DateSubmitted"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(rd["DateSubmitted"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading claims: " + ex.Message;
            }

            return View(list);
        }

        // GET: /Claim/VerifyClaims  (for Coordinators / Managers)
        public IActionResult VerifyClaims()
        {
            // check role
            var role = HttpContext.Session.GetString("Role");
            if (role == null || (role != "Programme Coordinator" && role != "Academic Manager"))
            {
                TempData["Error"] = "Unauthorized access.";
                return RedirectToAction("Index", "Dashboard");
            }

            var list = new List<Claim>();
            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT c.*, l.LecturerId, u.FullName
                                   FROM Claims c
                                   JOIN Lecturer l ON c.LecturerId = l.LecturerId
                                   JOIN Users u ON l.UserId = u.UserId
                                   WHERE c.Status IN ('Pending','Flagged')
                                   ORDER BY c.DateSubmitted DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            list.Add(new Claim
                            {
                                ClaimId = Convert.ToInt32(rd["ClaimId"]),
                                LecturerId = Convert.ToInt32(rd["LecturerId"]),
                                HoursWorked = Convert.ToInt32(rd["HoursWorked"]),
                                HourlyRate = Convert.ToDecimal(rd["HourlyRate"]),
                                TotalAmount = rd["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(rd["TotalAmount"]),
                                Notes = rd["Description"] == DBNull.Value ? null : rd["Description"].ToString(),
                                Status = rd["Status"].ToString(),
                                DocumentPath = rd["DocumentPath"] == DBNull.Value ? null : rd["DocumentPath"].ToString(),
                                ViolationReasons = rd["ViolationReasons"] == DBNull.Value ? null : rd["ViolationReasons"].ToString(),
                                DateSubmitted = rd["DateSubmitted"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(rd["DateSubmitted"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading pending claims: " + ex.Message;
            }

            return View(list);
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
        public IActionResult ApproveClaims(int claimId)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != null || (role != "Programme Coordinator" && role != "Academic Manager"))
            {
                TempData["Error"] = "Unauthorized access.";
                return RedirectToAction("VerifyClaims");
            }
            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("sp_ApproveClaim", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ClaimId", claimId);
                        cmd.Parameters.AddWithValue("@ApprovedBy", HttpContext.Session.GetString("FullName") ?? "System");
                        cmd.Parameters.AddWithValue("@Comments", "Approved via coordinator panel");
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Claim approved.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error approving claim: " + ex.Message;
            }

            return RedirectToAction("VerifyClaims");
        }

        // POST: /Claim/RejectClaim
        [HttpPost]
        public IActionResult RejectClaim(int claimId, string comment)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role == null || (role != "Programme Coordinator" && role != "Academic Manager"))
            {
                TempData["Error"] = "Unauthorized.";
                return RedirectToAction("VerifyClaims");
            }

            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("sp_RejectClaim", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ClaimId", claimId);
                        cmd.Parameters.AddWithValue("@ApprovedBy", HttpContext.Session.GetString("FullName") ?? "System");
                        cmd.Parameters.AddWithValue("@Comments", comment ?? "Rejected");
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Claim rejected.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error rejecting claim: " + ex.Message;
            }

            return RedirectToAction("VerifyClaims");

        }
    }
}





