using System.ComponentModel.DataAnnotations;

namespace AspireApp1.Server.Models
{
    public class ActionLog
    {
        public long Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        public int? RoleId { get; set; }
        public Role? Role { get; set; }

        [Required]
        [MaxLength(64)]
        public string ActionType { get; set; } = "";

        [MaxLength(64)]
        public string? EntityType { get; set; }

        [MaxLength(64)]
        public string? EntityId { get; set; }

        [MaxLength(1000)]
        public string? Details { get; set; }

        public bool Success { get; set; } = true;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
