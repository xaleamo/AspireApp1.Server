using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AspireApp1.Server.Models
{
    public class User : IdentityUser<int>
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string Surname { get; set; } = "";

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}
