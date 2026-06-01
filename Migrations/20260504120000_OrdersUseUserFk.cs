using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspireApp1.Server.Migrations
{
    /// <inheritdoc />
    public partial class OrdersUseUserFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Orders"" o
                SET ""UserId"" = u.""Id""
                FROM ""Users"" u
                WHERE u.""Email"" = o.""CustomerEmail"";
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""Orders"" WHERE ""UserId"" IS NULL;
            ");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Orders",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE ""Orders"" o
                SET ""CustomerEmail"" = u.""Email"",
                    ""CustomerName"" = u.""FirstName"" || ' ' || u.""Surname""
                FROM ""Users"" u
                WHERE u.""Id"" = o.""UserId"";
            ");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Orders");
        }
    }
}
