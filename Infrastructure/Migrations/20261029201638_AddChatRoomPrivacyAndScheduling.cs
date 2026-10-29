using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatRoomPrivacyAndScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "ChatRooms",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "ChatRooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "ChatRooms",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledStartAtUtc",
                table: "ChatRooms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_IsArchived_IsLocked_LastMessageAtUtc",
                table: "ChatRooms",
                columns: new[] { "IsArchived", "IsLocked", "LastMessageAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_ScheduledStartAtUtc",
                table: "ChatRooms",
                column: "ScheduledStartAtUtc",
                filter: "[ScheduledStartAtUtc] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatRooms_IsArchived_IsLocked_LastMessageAtUtc",
                table: "ChatRooms");

            migrationBuilder.DropIndex(
                name: "IX_ChatRooms_ScheduledStartAtUtc",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "ScheduledStartAtUtc",
                table: "ChatRooms");
        }
    }
}
