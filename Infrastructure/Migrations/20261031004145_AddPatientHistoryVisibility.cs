using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientHistoryVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPatientVisible",
                table: "PatientNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPatientVisible",
                table: "FollowUps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_PatientNotes_PatientId_IsPatientVisible",
                table: "PatientNotes",
                columns: new[] { "PatientId", "IsPatientVisible" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_PatientId_IsPatientVisible",
                table: "FollowUps",
                columns: new[] { "PatientId", "IsPatientVisible" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientNotes_PatientId_IsPatientVisible",
                table: "PatientNotes");

            migrationBuilder.DropIndex(
                name: "IX_FollowUps_PatientId_IsPatientVisible",
                table: "FollowUps");

            migrationBuilder.DropColumn(
                name: "IsPatientVisible",
                table: "PatientNotes");

            migrationBuilder.DropColumn(
                name: "IsPatientVisible",
                table: "FollowUps");
        }
    }
}
