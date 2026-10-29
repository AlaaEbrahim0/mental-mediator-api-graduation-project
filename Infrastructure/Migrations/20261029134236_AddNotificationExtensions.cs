using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeduplicationKey",
                table: "Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReadAt",
                table: "Notifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RelatedResourceId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DeduplicationKey",
                table: "Notifications",
                column: "DeduplicationKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_DeduplicationKey",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeduplicationKey",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RelatedResourceId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Notifications");
        }
    }
}
