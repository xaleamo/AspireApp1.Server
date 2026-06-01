using System.ComponentModel.DataAnnotations;

namespace AspireApp1.Server.Models
{
    public class MonitoredUser
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [MaxLength(200)]
        public string Identifier { get; set; } = "";

        [Required]
        [MaxLength(200)]
        public string Reason { get; set; } = "";

        public DateTime FlaggedAt { get; set; } = DateTime.UtcNow;

        public DateTime WindowStart { get; set; }

        public int HitCount { get; set; }

        public bool Resolved { get; set; } = false;

        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedByUserId { get; set; }
        public User? ResolvedBy { get; set; }
    }
}
