using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityReactionsAndAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "Replies",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "Replies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommentId",
                table: "PostMediaAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReplyId",
                table: "PostMediaAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reaction",
                table: "PostLikes",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "like");

            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "Comments",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "Comments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatMessageReactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Reaction = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessageReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessageReactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChatMessageReactions_ChatMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "ChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Reaction = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentReactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommentReactions_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplyReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReplyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Reaction = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplyReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplyReactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReplyReactions_Replies_ReplyId",
                        column: x => x.ReplyId,
                        principalTable: "Replies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostMediaAssets_CommentId",
                table: "PostMediaAssets",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_PostMediaAssets_OwnerId_CommentId_CreatedAtUtc",
                table: "PostMediaAssets",
                columns: new[] { "OwnerId", "CommentId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PostMediaAssets_OwnerId_ReplyId_CreatedAtUtc",
                table: "PostMediaAssets",
                columns: new[] { "OwnerId", "ReplyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PostMediaAssets_ReplyId",
                table: "PostMediaAssets",
                column: "ReplyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageReactions_MessageId",
                table: "ChatMessageReactions",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageReactions_UserId_MessageId",
                table: "ChatMessageReactions",
                columns: new[] { "UserId", "MessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentReactions_CommentId",
                table: "CommentReactions",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReactions_UserId_CommentId",
                table: "CommentReactions",
                columns: new[] { "UserId", "CommentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplyReactions_ReplyId",
                table: "ReplyReactions",
                column: "ReplyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplyReactions_UserId_ReplyId",
                table: "ReplyReactions",
                columns: new[] { "UserId", "ReplyId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PostMediaAssets_Comments_CommentId",
                table: "PostMediaAssets",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PostMediaAssets_Replies_ReplyId",
                table: "PostMediaAssets",
                column: "ReplyId",
                principalTable: "Replies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostMediaAssets_Comments_CommentId",
                table: "PostMediaAssets");

            migrationBuilder.DropForeignKey(
                name: "FK_PostMediaAssets_Replies_ReplyId",
                table: "PostMediaAssets");

            migrationBuilder.DropTable(
                name: "ChatMessageReactions");

            migrationBuilder.DropTable(
                name: "CommentReactions");

            migrationBuilder.DropTable(
                name: "ReplyReactions");

            migrationBuilder.DropIndex(
                name: "IX_PostMediaAssets_CommentId",
                table: "PostMediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_PostMediaAssets_OwnerId_CommentId_CreatedAtUtc",
                table: "PostMediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_PostMediaAssets_OwnerId_ReplyId_CreatedAtUtc",
                table: "PostMediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_PostMediaAssets_ReplyId",
                table: "PostMediaAssets");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Replies");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "Replies");

            migrationBuilder.DropColumn(
                name: "CommentId",
                table: "PostMediaAssets");

            migrationBuilder.DropColumn(
                name: "ReplyId",
                table: "PostMediaAssets");

            migrationBuilder.DropColumn(
                name: "Reaction",
                table: "PostLikes");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "Comments");
        }
    }
}
