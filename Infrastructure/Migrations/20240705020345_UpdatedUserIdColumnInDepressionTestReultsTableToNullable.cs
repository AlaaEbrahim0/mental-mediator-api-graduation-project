using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedUserIdColumnInDepressionTestReultsTableToNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepressionTestResult_Users_UserId",
                table: "DepressionTestResult");

            migrationBuilder.AddForeignKey(
                name: "FK_DepressionTestResult_Users_UserId",
                table: "DepressionTestResult",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepressionTestResult_Users_UserId",
                table: "DepressionTestResult");

            migrationBuilder.AddForeignKey(
                name: "FK_DepressionTestResult_Users_UserId",
                table: "DepressionTestResult",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
