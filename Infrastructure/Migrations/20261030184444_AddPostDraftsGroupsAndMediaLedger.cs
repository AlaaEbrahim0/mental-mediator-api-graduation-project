using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostDraftsGroupsAndMediaLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "Posts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TargetChatRoomId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PostMediaAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PostId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostMediaAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostMediaAssets_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PostMediaAssets_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_IsDraft_PostedOn",
                table: "Posts",
                columns: new[] { "IsDraft", "PostedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TargetChatRoomId",
                table: "Posts",
                column: "TargetChatRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Visibility_TargetChatRoomId",
                table: "Posts",
                columns: new[] { "Visibility", "TargetChatRoomId" });

            migrationBuilder.CreateIndex(
                name: "IX_PostMediaAssets_OwnerId_PostId_CreatedAtUtc",
                table: "PostMediaAssets",
                columns: new[] { "OwnerId", "PostId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PostMediaAssets_PostId",
                table: "PostMediaAssets",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostMediaAssets_Url",
                table: "PostMediaAssets",
                column: "Url",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_ChatRooms_TargetChatRoomId",
                table: "Posts",
                column: "TargetChatRoomId",
                principalTable: "ChatRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_ChatRooms_TargetChatRoomId",
                table: "Posts");

            migrationBuilder.DropTable(
                name: "PostMediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_Posts_IsDraft_PostedOn",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_TargetChatRoomId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Visibility_TargetChatRoomId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "TargetChatRoomId",
                table: "Posts");
        }
    }
}
