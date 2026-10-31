using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationAggregation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RelatedCount",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("UPDATE [Notifications] SET [RelatedCount] = 1 WHERE [RelatedCount] = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AppUserId_DeduplicationKey",
                table: "Notifications",
                columns: new[] { "AppUserId", "DeduplicationKey" },
                unique: true,
                filter: "[DeduplicationKey] IS NOT NULL AND [Type] IN ('RoomMessage', 'PostLike', 'FollowUp', 'PatientNote', 'AppointmentCompleted')");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AppUserId_Type_RelatedResourceId",
                table: "Notifications",
                columns: new[] { "AppUserId", "Type", "RelatedResourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_AppUserId_DeduplicationKey",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_AppUserId_Type_RelatedResourceId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RelatedCount",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
