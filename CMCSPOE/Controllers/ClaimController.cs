using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CMCSPOE.Controllers
{
    [AllowAnonymous]
    public class ClaimController : Controller
    {
        // Keep using the helper, consider DI later for testability
        private readonly DatabaseConnection db = new DatabaseConnection();
        private readonly IWebHostEnvironment _env;

        public ClaimController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // compatibility route: /Claim/AP -> redirect to SubmitClaim
        [HttpGet]
        public IActionResult AP()
        {
            return RedirectToAction(nameof(SubmitClaim));
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
            if (!ModelState.IsValid)
            {
                return View(claim);
            }

            try
            {
                var loggedInUserId = HttpContext.Session.GetInt32("UserId");
                if (loggedInUserId == null)
                {
                    TempData["Error"] = "You must be logged in to submit a claim.";
                    return RedirectToAction("Login", "Account");
                }

                int lecturerId;

                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();

                    // Try find LecturerId for this user (supports table name 'Lecturers')
                    using (SqlCommand cmdFind = new SqlCommand(
                        "SELECT TOP 1 LecturerId FROM Lecturers WHERE UserId = @UserId", con))
                    {
                        cmdFind.Parameters.AddWithValue("@UserId", loggedInUserId.Value);
                        var obj = cmdFind.ExecuteScalar();
                        if (obj != null && obj != DBNull.Value)
                        {
                            lecturerId = Convert.ToInt32(obj);
                        }
                        else
                        {
                            // Lecturer not found -> create using data from Users table
                            string userFullName = "";
                            string userEmail = "";

                            using (SqlCommand cmdUser = new SqlCommand(
                                "SELECT FullName, Email FROM Users WHERE UserId = @UserId", con))
                            {
                                cmdUser.Parameters.AddWithValue("@UserId", loggedInUserId.Value);
                                using (var reader = cmdUser.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        userFullName = reader["FullName"]?.ToString() ?? "";
                                        userEmail = reader["Email"]?.ToString() ?? "";
                                    }
                                }
                            }

                            using (SqlCommand cmdInsertLect = new SqlCommand(
                                "INSERT INTO Lecturers (UserId, LecturerName, Email, Department) " +
                                "VALUES (@UserId, @LecturerName, @Email, @Department); SELECT SCOPE_IDENTITY();", con))
                            {
                                cmdInsertLect.Parameters.AddWithValue("@UserId", loggedInUserId.Value);
                                cmdInsertLect.Parameters.AddWithValue("@LecturerName", string.IsNullOrWhiteSpace(userFullName) ? "Unknown Lecturer" : userFullName);
                                cmdInsertLect.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(userEmail) ? (object)DBNull.Value : userEmail);
                                cmdInsertLect.Parameters.AddWithValue("@Department", (object)DBNull.Value);

                                var newIdObj = cmdInsertLect.ExecuteScalar();
                                // SCOPE_IDENTITY() returns decimal -> convert safely
                                lecturerId = Convert.ToInt32(Convert.ToDecimal(newIdObj));
                            }
                        }
                    }

                    // Insert the claim with the confirmed lecturerId
                    string sql = @"INSERT INTO Claims 
                    (LecturerId, HoursWorked, HourlyRate, Notes, Status, DateSubmitted)
                    VALUES (@LecturerId, @HoursWorked, @HourlyRate, @Notes, 'Pending', GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@LecturerId", lecturerId);
                        cmd.Parameters.AddWithValue("@HoursWorked", claim.HoursWorked);
                        cmd.Parameters.AddWithValue("@HourlyRate", claim.HourlyRate);
                        cmd.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(claim.Notes) ? (object)DBNull.Value : claim.Notes);

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Claim submitted successfully!";
                // Redirect to dashboard after successful submit
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error submitting claim: " + ex.Message;
                return View(claim);
            }
        }

        [HttpGet]
        public IActionResult UploadDocuments(int id = 0)
        {
            ViewBag.ClaimId = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadDocuments(int claimId, IFormFile document)
        {
            if (document == null || document.Length == 0)
            {
                TempData["Error"] = "Please select a file.";
                ViewBag.ClaimId = claimId;
                return View();
            }

            try
            {
                // ensure uploads folder exists
                string folder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // sanitize filename and generate unique name to avoid collisions
                var originalFileName = Path.GetFileName(document.FileName);
                var uniqueFileName = $"{Guid.NewGuid():N}_{originalFileName}";
                string filePath = Path.Combine(folder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    document.CopyTo(stream);
                }

                // save file name to DB (support both DocumentPath or SupportingDocument columns)
                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();
                    // Update both common column names to be defensive
                    string sql = @"
                        UPDATE Claims
                        SET DocumentPath = @Document,
                            SupportingDocument = @Document
                        WHERE ClaimId = @ClaimId";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@Document", uniqueFileName);
                        cmd.Parameters.AddWithValue("@ClaimId", claimId);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Document uploaded successfully!";
                // Redirect to dashboard after successful upload
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error uploading file: " + ex.Message;
                ViewBag.ClaimId = claimId;
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

                // Map user -> lecturer id (use Lecturers table)
                int lecturerId = 0;
                using (var con = db.GetConnection())
                {
                    con.Open();
                    using (var cmd = new SqlCommand("SELECT TOP 1 LecturerId FROM Lecturers WHERE UserId = @UserId", con))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId.Value);
                        var r = cmd.ExecuteScalar();
                        if (r == null)
                        {
                            ViewBag.Error = "Lecturer profile not found.";
                            return View(list);
                        }
                        lecturerId = Convert.ToInt32(r);
                    }

                    string sql = @"
                        SELECT 
                            c.ClaimId,
                            c.LecturerId,
                            c.HoursWorked,
                            COALESCE(c.HourlyRate, c.RatePerHour) AS HourlyRate,
                            COALESCE(c.TotalAmount, 0) AS TotalAmount,
                            COALESCE(c.Notes, c.Description) AS Notes,
                            COALESCE(c.Status, c.ClaimStatus) AS Status,
                            COALESCE(c.DocumentPath, c.SupportingDocument) AS DocumentPath,
                            COALESCE(c.ViolationReasons, c.Remarks) AS ViolationReasons,
                            c.DateSubmitted
                        FROM Claims c
                        WHERE c.LecturerId = @LecturerId
                        ORDER BY c.DateSubmitted DESC";

                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@LecturerId", lecturerId);
                        using (var rd = cmd.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                list.Add(new Claim
                                {
                                    ClaimId = rd["ClaimId"] == DBNull.Value ? 0 : Convert.ToInt32(rd["ClaimId"]),
                                    LecturerId = rd["LecturerId"] == DBNull.Value ? 0 : Convert.ToInt32(rd["LecturerId"]),
                                    HoursWorked = rd["HoursWorked"] == DBNull.Value ? 0 : Convert.ToInt32(rd["HoursWorked"]),
                                    HourlyRate = rd["HourlyRate"] == DBNull.Value ? 0 : Convert.ToDecimal(rd["HourlyRate"]),
                                    TotalAmount = rd["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(rd["TotalAmount"]),
                                    Notes = rd["Notes"] == DBNull.Value ? null : rd["Notes"].ToString(),
                                    Status = rd["Status"] == DBNull.Value ? null : rd["Status"].ToString(),
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
            var role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role) ||
                !(role.Equals("Programme Coordinator", StringComparison.OrdinalIgnoreCase)
                  || role.Equals("Academic Manager", StringComparison.OrdinalIgnoreCase)))
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

                    string sql = @"
                        SELECT 
                            c.ClaimId,
                            c.LecturerId,
                            c.HoursWorked,
                            COALESCE(c.HourlyRate, c.RatePerHour) AS HourlyRate,
                            COALESCE(c.TotalAmount, 0) AS TotalAmount,
                            COALESCE(c.Notes, c.Description) AS Notes,
                            COALESCE(c.Status, c.ClaimStatus) AS Status,
                            COALESCE(c.DocumentPath, c.SupportingDocument) AS DocumentPath,
                            COALESCE(c.ViolationReasons, c.Remarks) AS ViolationReasons,
                            c.DateSubmitted,
                            u.FullName AS LecturerName
                        FROM Claims c
                        JOIN Lecturers l ON c.LecturerId = l.LecturerId
                        JOIN Users u ON l.UserId = u.UserId
                        WHERE c.Status IN ('Pending','Flagged') OR c.ClaimStatus IN ('Pending','Flagged')
                        ORDER BY c.DateSubmitted DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            var claim = new Claim
                            {
                                ClaimId = rd["ClaimId"] == DBNull.Value ? 0 : Convert.ToInt32(rd["ClaimId"]),
                                LecturerId = rd["LecturerId"] == DBNull.Value ? 0 : Convert.ToInt32(rd["LecturerId"]),
                                HoursWorked = rd["HoursWorked"] == DBNull.Value ? 0 : Convert.ToInt32(rd["HoursWorked"]),
                                HourlyRate = rd["HourlyRate"] == DBNull.Value ? 0 : Convert.ToDecimal(rd["HourlyRate"]),
                                TotalAmount = rd["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(rd["TotalAmount"]),
                                Notes = rd["Notes"] == DBNull.Value ? null : rd["Notes"].ToString(),
                                Status = rd["Status"] == DBNull.Value ? null : rd["Status"].ToString(),
                                DocumentPath = rd["DocumentPath"] == DBNull.Value ? null : rd["DocumentPath"].ToString(),
                                ViolationReasons = rd["ViolationReasons"] == DBNull.Value ? null : rd["ViolationReasons"].ToString(),
                                DateSubmitted = rd["DateSubmitted"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(rd["DateSubmitted"]),
                                LecturerName = rd["LecturerName"] == DBNull.Value ? null : rd["LecturerName"].ToString()
                            };

                            list.Add(claim);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading pending claims: " + ex.Message;
            }

            return View("VerifyClaims", list);
        }

        [HttpGet]
        public IActionResult VerifyClaim(int id)
        {
            Claim claim = new Claim();

            using (SqlConnection con = db.GetConnection())
            {
                con.Open();
                string query = @"
                    SELECT 
                        c.ClaimId,
                        c.HoursWorked,
                        COALESCE(c.HourlyRate, c.RatePerHour) AS HourlyRate,
                        COALESCE(c.TotalAmount, 0) AS TotalAmount,
                        COALESCE(c.Notes, c.Description) AS Notes,
                        COALESCE(c.Status, c.ClaimStatus) AS Status,
                        COALESCE(c.DocumentPath, c.SupportingDocument) AS DocumentPath,
                        COALESCE(c.ViolationReasons, c.Remarks) AS ViolationReasons
                    FROM Claims c
                    WHERE c.ClaimId = @ClaimId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ClaimId", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            claim.ClaimId = reader["ClaimId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ClaimId"]);
                            claim.HoursWorked = reader["HoursWorked"] == DBNull.Value ? 0 : Convert.ToInt32(reader["HoursWorked"]);
                            claim.HourlyRate = reader["HourlyRate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["HourlyRate"]);
                            claim.TotalAmount = reader["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalAmount"]);
                            claim.Notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString();
                            claim.Status = reader["Status"] == DBNull.Value ? null : reader["Status"].ToString();
                            claim.DocumentPath = reader["DocumentPath"] == DBNull.Value ? null : reader["DocumentPath"].ToString();
                            claim.ViolationReasons = reader["ViolationReasons"] == DBNull.Value ? null : reader["ViolationReasons"].ToString();
                        }
                    }
                }
            }

            return View(claim);
        }

        [HttpGet]
        public IActionResult ApproveClaim()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveClaims(int claimId)
        {
            var role = HttpContext.Session.GetString("Role");
            // fixed role check (deny if null or not one of the allowed roles)
            if (role == null || (role != "Programme Coordinator" && role != "Academic Manager"))
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

            // Redirect to dashboard after approval
            return RedirectToAction("Index", "Dashboard");
        }

        // POST: /Claim/RejectClaim
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                        cmd.Parameters.AddWithValue("@Comments", string.IsNullOrWhiteSpace(comment) ? "Rejected" : comment);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Claim rejected.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error rejecting claim: " + ex.Message;
            }

            // Redirect to dashboard after rejection
            return RedirectToAction("Index", "Dashboard");
        }
    }
}









