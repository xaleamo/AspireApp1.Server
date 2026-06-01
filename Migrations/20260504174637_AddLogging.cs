using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AspireApp1.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    RoleId = table.Column<int>(type: "integer", nullable: true),
                    ActionType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EntityId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionLogs_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActionLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MonitoredUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FlaggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HitCount = table.Column<int>(type: "integer", nullable: false),
                    Resolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoredUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonitoredUsers_Users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MonitoredUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_ActionType_Details_Timestamp",
                table: "ActionLogs",
                columns: new[] { "ActionType", "Details", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_RoleId",
                table: "ActionLogs",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_Timestamp",
                table: "ActionLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_UserId_ActionType_Timestamp",
                table: "ActionLogs",
                columns: new[] { "UserId", "ActionType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoredUsers_Identifier_Reason_Resolved",
                table: "MonitoredUsers",
                columns: new[] { "Identifier", "Reason", "Resolved" });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoredUsers_ResolvedByUserId",
                table: "MonitoredUsers",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoredUsers_UserId",
                table: "MonitoredUsers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionLogs");

            migrationBuilder.DropTable(
                name: "MonitoredUsers");
        }
    }
}
