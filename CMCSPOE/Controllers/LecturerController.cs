using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CMCSPOE.Data;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMCSPOE.Controllers
{
    [AllowAnonymous]
    public class LecturerController : Controller
    {
        private readonly DatabaseConnection db = new DatabaseConnection();

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

                    using (var cmd = new SqlCommand(
                        "INSERT INTO Lecturers (UserId, Department, HourlyRate) VALUES (@UserId, @Department, @Rate)",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", model.UserId);
                        cmd.Parameters.AddWithValue("@Department", (object?)model.Department ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Rate", model.HourlyRate);

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Lecturer added successfully.";
                // Redirect to dashboard after create
                return RedirectToAction("Index", "Dashboard");
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
            var list = new List<Lecturers>();

            try
            {
                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    using (var cmd = new SqlCommand(
                        @"SELECT L.LecturerId, U.FullName, L.Department, L.HourlyRate 
                          FROM Lecturers L 
                          JOIN Users U ON L.UserId = U.UserId
                          ORDER BY U.FullName", conn))
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new Lecturers
                            {
                                LecturerId = dr["LecturerId"] != DBNull.Value ? Convert.ToInt32(dr["LecturerId"]) : 0,
                                FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : string.Empty,
                                Department = dr["Department"] != DBNull.Value ? dr["Department"].ToString() : string.Empty,
                                HourlyRate = dr["HourlyRate"] != DBNull.Value ? Convert.ToDecimal(dr["HourlyRate"]) : 0m
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed loading lecturers: " + ex.Message;
            }

            return View(list);
        }
    }
}

