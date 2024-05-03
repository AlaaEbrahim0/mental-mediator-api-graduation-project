using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class AddedNotificationResourcesColumn : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "ResourceId",
				table: "Notifications");

			migrationBuilder.AddColumn<string>(
				name: "Resources",
				table: "Notifications",
				type: "nvarchar(1000)",
				nullable: false,
				defaultValue: "");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "Resources",
				table: "Notifications");

			migrationBuilder.AddColumn<int>(
				name: "ResourceId",
				table: "Notifications",
				type: "int",
				nullable: false,
				defaultValue: 0);
		}
	}
}
