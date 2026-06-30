using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shoppy.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedPasswordResetCodeToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordResetCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PasswordResetCodeExpires",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordResetCodeExpires",
                table: "AspNetUsers");
        }
    }
}
