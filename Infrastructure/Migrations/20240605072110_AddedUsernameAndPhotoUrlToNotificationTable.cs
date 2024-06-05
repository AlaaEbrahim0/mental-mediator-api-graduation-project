using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class AddedUsernameAndPhotoUrlToNotificationTable : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "IsRead",
				table: "Notifications");

			migrationBuilder.AlterColumn<string>(
				name: "Type",
				table: "Notifications",
				type: "nvarchar(max)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(100)",
				oldMaxLength: 100);

			migrationBuilder.AlterColumn<string>(
				name: "Resources",
				table: "Notifications",
				type: "nvarchar(500)",
				maxLength: 500,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "nvarchar(1000)",
				oldMaxLength: 1000);

			migrationBuilder.AlterColumn<string>(
				name: "Message",
				table: "Notifications",
				type: "nvarchar(200)",
				maxLength: 200,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "nvarchar(500)",
				oldMaxLength: 500);

			migrationBuilder.AddColumn<string>(
				name: "NotifierPhotoUrl",
				table: "Notifications",
				type: "nvarchar(1000)",
				maxLength: 1000,
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "NotifierUserName",
				table: "Notifications",
				type: "nvarchar(64)",
				maxLength: 64,
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "NotifierPhotoUrl",
				table: "Notifications");

			migrationBuilder.DropColumn(
				name: "NotifierUserName",
				table: "Notifications");

			migrationBuilder.AlterColumn<string>(
				name: "Type",
				table: "Notifications",
				type: "nvarchar(100)",
				maxLength: 100,
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(max)",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "Resources",
				table: "Notifications",
				type: "nvarchar(1000)",
				maxLength: 1000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "nvarchar(500)",
				oldMaxLength: 500);

			migrationBuilder.AlterColumn<string>(
				name: "Message",
				table: "Notifications",
				type: "nvarchar(500)",
				maxLength: 500,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "nvarchar(200)",
				oldMaxLength: 200);

			migrationBuilder.AddColumn<bool>(
				name: "IsRead",
				table: "Notifications",
				type: "bit",
				nullable: false,
				defaultValue: false);
		}
	}
}
