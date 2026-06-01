using AspireApp1.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Server
{
    public class AppDbContext : IdentityDbContext<User, Role, int>
    {
        public DbSet<Dessert> Desserts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ActionLog> ActionLogs { get; set; }
        public DbSet<MonitoredUser> MonitoredUsers { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserMfaFactor> UserMfaFactors { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Keep the existing table names so the rest of the codebase keeps working.
            modelBuilder.Entity<User>(b => b.ToTable("Users"));
            modelBuilder.Entity<Role>(b => b.ToTable("Roles"));
            modelBuilder.Entity<IdentityUserRole<int>>(b => b.ToTable("AspNetUserRoles"));
            modelBuilder.Entity<IdentityUserClaim<int>>(b => b.ToTable("AspNetUserClaims"));
            modelBuilder.Entity<IdentityUserLogin<int>>(b => b.ToTable("AspNetUserLogins"));
            modelBuilder.Entity<IdentityUserToken<int>>(b => b.ToTable("AspNetUserTokens"));
            modelBuilder.Entity<IdentityRoleClaim<int>>(b => b.ToTable("AspNetRoleClaims"));

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>(b =>
            {
                b.HasIndex(rt => rt.TokenHash).IsUnique();
                b.HasIndex(rt => rt.UserId);
                b.HasOne(rt => rt.User)
                    .WithMany()
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserMfaFactor>(b =>
            {
                b.HasKey(f => new { f.UserId, f.FactorName });
                b.HasOne(f => f.User)
                    .WithMany()
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            ConfigureAuditing(modelBuilder);

            SeedRolesAndPermissions(modelBuilder);
        }

        private static void ConfigureAuditing(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActionLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ActionLog>()
                .HasOne(a => a.Role)
                .WithMany()
                .HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ActionLog>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<ActionLog>()
                .HasIndex(a => new { a.UserId, a.ActionType, a.Timestamp });

            modelBuilder.Entity<ActionLog>()
                .HasIndex(a => new { a.ActionType, a.Details, a.Timestamp });

            modelBuilder.Entity<MonitoredUser>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MonitoredUser>()
                .HasOne(m => m.ResolvedBy)
                .WithMany()
                .HasForeignKey(m => m.ResolvedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MonitoredUser>()
                .HasIndex(m => new { m.Identifier, m.Reason, m.Resolved });
        }

        // Identity hashes embed random salts so we cannot stably seed a user via
        // HasData. Seed users at startup via UserManager.CreateAsync instead
        // (see DatabaseInitializer.SeedAsync). Roles and permissions are static
        // and safe to seed with HasData.
        private static void SeedRolesAndPermissions(ModelBuilder modelBuilder)
        {
            const int adminRoleId = 1;
            const int customerRoleId = 2;

            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = "8d2e2b5b-2c4e-4e1d-9c2a-1a1a1a1a1a01",
                },
                new Role
                {
                    Id = customerRoleId,
                    Name = "Customer",
                    NormalizedName = "CUSTOMER",
                    ConcurrencyStamp = "8d2e2b5b-2c4e-4e1d-9c2a-1a1a1a1a1a02",
                }
            );

            var permissions = new[]
            {
                new Permission { Id = 1,  Code = "desserts:read",      Description = "View desserts" },
                new Permission { Id = 2,  Code = "desserts:create",    Description = "Create desserts" },
                new Permission { Id = 3,  Code = "desserts:update",    Description = "Update desserts" },
                new Permission { Id = 4,  Code = "desserts:delete",    Description = "Delete desserts" },
                new Permission { Id = 5,  Code = "orders:read:all",    Description = "View all orders" },
                new Permission { Id = 6,  Code = "orders:read:own",    Description = "View own orders" },
                new Permission { Id = 7,  Code = "orders:create",      Description = "Place orders" },
                new Permission { Id = 8,  Code = "orders:archive",     Description = "Archive orders" },
                new Permission { Id = 9,  Code = "orders:delete",      Description = "Delete orders" },
                new Permission { Id = 10, Code = "statistics:view",    Description = "View statistics" },
            };
            modelBuilder.Entity<Permission>().HasData(permissions);

            var adminPermissionIds = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            // 9 = orders:delete — Customer needs it so they can cancel their
            // OWN orders. The inline ownership check in Mutation.DeleteOrder
            // still prevents customers from deleting anyone else's order.
            var customerPermissionIds = new[] { 1, 6, 7, 9 };

            var rolePermissions = adminPermissionIds
                .Select(pid => new RolePermission { RoleId = adminRoleId, PermissionId = pid })
                .Concat(customerPermissionIds
                    .Select(pid => new RolePermission { RoleId = customerRoleId, PermissionId = pid }))
                .ToArray();

            modelBuilder.Entity<RolePermission>().HasData(rolePermissions);
        }
    }
}
