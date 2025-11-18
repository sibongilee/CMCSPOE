using System.ComponentModel.DataAnnotations;

namespace CMCSPOE.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }
        [Required]
        public int LecturerId { get; set; }
        [Required]
        [Range(1, 300)]
        public int HoursWorked { get; set; }
        [Required]
        [Range(50, 500)]
        public decimal HourlyRate { get; set; }
        public string Notes { get; set; }

        public string? DocumentPath { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string LecturerName { get; set; }
    }
}

  

   
