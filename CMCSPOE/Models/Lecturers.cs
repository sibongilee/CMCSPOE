using System;

namespace CMCSPOE.Models
{
    public class Lecturers
    {
        public int LecturerId { get; set; }
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? Department { get; set; }
        public decimal HourlyRate { get; set; }
    }
}
