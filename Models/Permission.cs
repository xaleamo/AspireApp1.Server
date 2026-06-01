using System.ComponentModel.DataAnnotations;

namespace AspireApp1.Server.Models
{
    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string Code { get; set; } = "";

        [MaxLength(200)]
        public string Description { get; set; } = "";

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
