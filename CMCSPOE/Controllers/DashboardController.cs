using System;
using System.Data.SqlClient;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace CMCSPOE.Controllers
{
    public class DashboardController : Controller
    {
        private readonly DatabaseConnection db = new DatabaseConnection();
        public IActionResult Index()
        {
            string? role = HttpContext.Session.GetString("Role");

            if (role == "Lecturer")
                return RedirectToAction("LecturerDashboard");

            if (role == "Programme Coordinator" || role == "Academic Manager")
            {
                var claims = new List<Claim>();

                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();
                    string query = "SELECT c.*, u.FullName FROM Claims c JOIN Users u ON c.UserId = u.UserId";
                    SqlCommand cmd = new SqlCommand(query, con);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        claims.Add(new Claim
                        {
                            ClaimId = (int)reader["ClaimId"],
                            LecturerName = reader["FullName"].ToString(),
                            Month = reader["Month"].ToString(),
                            HoursWorked = (int)reader["HoursWorked"],
                            
                            Status = reader["Status"].ToString()
                        });
                    }
                }

                ViewBag.Pending = claims.Count(c => c.Status == "Pending");
                ViewBag.Approved = claims.Count(c => c.Status == "Approved");
                ViewBag.Rejected = claims.Count(c => c.Status == "Rejected");

                return View(claims);
            }

            return RedirectToAction("Login", "Account");
        }

        public IActionResult LecturerDashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var claims = new List<Claim>();

            using (SqlConnection con = db.GetConnection())
            {
                con.Open();
                string query = "SELECT * FROM Claims WHERE UserId=@UserId";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", userId);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    claims.Add(new Claim
                    {
                        ClaimId = (int)reader["ClaimId"],
                        Month = reader["Month"].ToString(),
                        HoursWorked = (int)reader["HoursWorked"],
                        
                        Status = reader["Status"].ToString(),
                        Notes = reader["Notes"].ToString()
                    });
                }
            }

            ViewBag.TotalClaims = claims.Count;
            ViewBag.Pending = claims.Count(c => c.Status == "Pending");
            ViewBag.Approved = claims.Count(c => c.Status == "Approved");
            ViewBag.Rejected = claims.Count(c => c.Status == "Rejected");

            return View(claims);
        }

    }
}

