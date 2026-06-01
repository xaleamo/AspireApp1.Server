using System.ComponentModel.DataAnnotations;

namespace AspireApp1.Server.Models
{
    public class UserMfaFactor
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(32)]
        public string FactorName { get; set; } = "";

        public DateTime EnabledAt { get; set; } = DateTime.UtcNow;
    }
}
