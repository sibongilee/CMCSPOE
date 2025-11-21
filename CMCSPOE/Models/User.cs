using System.ComponentModel.DataAnnotations;

namespace CMCSPOE.Models
{
    public class User
    {
        public int UserId { get; set; }
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; } // In production, store hashed passwords only

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } // Lecturer, Programme Coordinator, Academic Manager,HR

    }
}
