using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaiApi.Migrations
{
    /// <inheritdoc />
    public partial class initial_create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastResetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DailyEmailLimit = table.Column<int>(type: "integer", nullable: false),
                    EmailsSentToday = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailUsers_ApiKey",
                table: "EmailUsers",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailUsers_Email",
                table: "EmailUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailUsers_Username",
                table: "EmailUsers",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailUsers");
        }
    }
}
