using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class temp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointment_Doctors_DoctorId",
                table: "Appointment");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointment_Doctors_DoctorId",
                table: "Appointment",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointment_Doctors_DoctorId",
                table: "Appointment");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointment_Doctors_DoctorId",
                table: "Appointment",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id");
        }
    }
}
