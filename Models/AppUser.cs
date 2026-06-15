using System.ComponentModel.DataAnnotations;

namespace ViettalAPI.Models
{
    public class AppUser
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.Customer;

        [Required]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty; // Store BCrypt hashed password
    }
}
