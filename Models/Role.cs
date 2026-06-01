using Microsoft.AspNetCore.Identity;

namespace AspireApp1.Server.Models
{
    public class Role : IdentityRole<int>
    {
        public Role() : base() { }
        public Role(string roleName) : base(roleName) { }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
