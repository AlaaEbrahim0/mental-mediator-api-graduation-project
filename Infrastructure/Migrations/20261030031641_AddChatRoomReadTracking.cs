using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatRoomReadTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReadAtUtc",
                table: "ChatRoomMembers",
                type: "datetime2",
                nullable: true);

            // Existing members start read up to their join time, which matches
            // the NULL fallback used by the unread query.
            migrationBuilder.Sql(
                "UPDATE [ChatRoomMembers] SET [LastReadAtUtc] = [JoinedAtUtc] WHERE [LastReadAtUtc] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReadAtUtc",
                table: "ChatRoomMembers");
        }
    }
}
