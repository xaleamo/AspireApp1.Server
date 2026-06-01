using System.ComponentModel.DataAnnotations;

namespace AspireApp1.Server.Models
{
    public class RefreshToken
    {
        public long Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(128)]
        public string TokenHash { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        [MaxLength(128)]
        public string? ReplacedByTokenHash { get; set; }

        [MaxLength(64)]
        public string? ReasonRevoked { get; set; }

        [MaxLength(45)]
        public string? RemoteIp { get; set; }

        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
