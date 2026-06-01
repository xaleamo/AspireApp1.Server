using System;

namespace AspireApp1.Server.Auditing
{
    // Retired: replaced by [Authorize(Roles = "Admin")] once JWT authentication
    // landed. Kept as a no-op alias only so any forgotten reference compiles.
    [Obsolete("Use [Authorize(Roles = \"Admin\")] from Microsoft.AspNetCore.Authorization instead.")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AdminOnlyAttribute : Attribute
    {
    }
}
