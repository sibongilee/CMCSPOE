using System.Data.SqlClient;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Mvc;

namespace CMCSPOE.Controllers
{
    public class LecturerController : Controller
    {
        private readonly DatabaseConnection db = new DatabaseConnection();

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Lecturers model)
        {
            try
            {
                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO Lecturer (UserId, Department, HourlyRate) VALUES (@UserId, @Department, @Rate)",
                        conn
                    );

                    cmd.Parameters.AddWithValue("@UserId", model.UserId);
                    cmd.Parameters.AddWithValue("@Department", model.Department);
                    cmd.Parameters.AddWithValue("@Rate", model.HourlyRate);

                    cmd.ExecuteNonQuery();
                }

                TempData["Success"] = "Lecturer added successfully.";
                return RedirectToAction("LecturerList");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Failed: " + ex.Message;
                return View(model);
            }
        }

        //==============================================
        // LIST LECTURERS
        //==============================================
        public IActionResult LecturerList()
        {
            List<Lecturers> list = new List<Lecturers>();

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    @"SELECT L.LecturerId, U.FullName, L.Department, L.HourlyRate 
                      FROM Lecturer L 
                      JOIN Users U ON L.UserId = U.UserId",
                    conn
                );

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new Lecturers
                    {
                        LecturerId = (int)dr["LecturerId"],
                        FullName = dr["FullName"].ToString(),
                        Department = dr["Department"].ToString(),
                        HourlyRate = Convert.ToDecimal(dr["HourlyRate"])
                    });
                }
            }

            return View(list);
        }
    }
}

