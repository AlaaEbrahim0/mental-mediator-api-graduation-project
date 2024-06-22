using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class AddedLocationCityFeesToDoctorTable : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<string>(
				name: "Content",
				table: "Replies",
				type: "nvarchar(1000)",
				maxLength: 1000,
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(max)",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "AppUserId",
				table: "Replies",
				type: "nvarchar(450)",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(450)",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "Title",
				table: "Posts",
				type: "nvarchar(max)",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(max)",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "Content",
				table: "Posts",
				type: "nvarchar(max)",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(max)",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "AppUserId",
				table: "Posts",
				type: "nvarchar(450)",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(450)",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "AppUserId",
				table: "Notifications",
				type: "nvarchar(450)",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(450)",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "Biography",
				table: "Doctors",
				type: "nvarchar(1000)",
				maxLength: 1000,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(max)");

			migrationBuilder.AddColumn<string>(
				name: "City",
				table: "Doctors",
				type: "nvarchar(100)",
				maxLength: 100,
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "Location",
				table: "Doctors",
				type: "nvarchar(200)",
				maxLength: 200,
				nullable: true);

			migrationBuilder.AddColumn<decimal>(
				name: "SessionFees",
				table: "Doctors",
				type: "decimal(18,2)",
				nullable: false,
				defaultValue: 0m);

			migrationBuilder.AlterColumn<string>(
				name: "Content",
				table: "Comments",
				type: "nvarchar(1000)",
				maxLength: 1000,
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(max)",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "AppUserId",
				table: "Comments",
				type: "nvarchar(450)",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(450)",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "UserName",
				table: "AspNetUsers",
				type: "nvarchar(256)",
				maxLength: 256,
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(256)",
				oldMaxLength: 256,
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "NormalizedUserName",
				table: "AspNetUsers",
				type: "nvarchar(256)",
				maxLength: 256,
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(256)",
				oldMaxLength: 256,
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "NormalizedEmail",
				table: "AspNetUsers",
				type: "nvarchar(256)",
				maxLength: 256,
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(256)",
				oldMaxLength: 256,
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "LastName",
				table: "AspNetUsers",
				type: "nvarchar(max)",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(max)",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "Gender",
				table: "AspNetUsers",
				type: "nvarchar(max)",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(max)",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "FirstName",
				table: "AspNetUsers",
				type: "nvarchar(max)",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(max)",
				oldNullable: true);

			migrationBuilder.AddColumn<string>(
				name: "Reason",
				table: "Appointments",
				type: "nvarchar(1000)",
				maxLength: 1000,
				nullable: true);
		}
		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "City",
				table: "Doctors");

			migrationBuilder.DropColumn(
				name: "Location",
				table: "Doctors");

			migrationBuilder.DropColumn(
				name: "SessionFees",
				table: "Doctors");

			migrationBuilder.DropColumn(
				name: "Reason",
				table: "Appointments");

			migrationBuilder.AlterColumn<string>(
				name: "Content",
				table: "Replies",
				type: "nvarchar(max)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(1000)",
				oldMaxLength: 1000);

			migrationBuilder.AlterColumn<string>(
				name: "AppUserId",
				table: "Replies",
				type: "nvarchar(450)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(450)");

			migrationBuilder.AlterColumn<string>(
				name: "Title",
				table: "Posts",
				type: "nvarchar(max)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(max)");

			migrationBuilder.AlterColumn<string>(
				name: "Content",
				table: "Posts",
				type: "nvarchar(max)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(max)");

			migrationBuilder.AlterColumn<string>(
				name: "AppUserId",
				table: "Posts",
				type: "nvarchar(450)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(450)");

			migrationBuilder.AlterColumn<string>(
				name: "AppUserId",
				table: "Notifications",
				type: "nvarchar(450)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(450)");

			migrationBuilder.AlterColumn<string>(
				name: "Biography",
				table: "Doctors",
				type: "nvarchar(max)",
				nullable: false,
				defaultValue: "",
				oldClrType: typeof(string),
				oldType: "nvarchar(1000)",
				oldMaxLength: 1000,
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "Content",
				table: "Comments",
				type: "nvarchar(max)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(1000)",
				oldMaxLength: 1000);

			migrationBuilder.AlterColumn<string>(
				name: "AppUserId",
				table: "Comments",
				type: "nvarchar(450)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(450)");

			migrationBuilder.AlterColumn<string>(
				name: "UserName",
				table: "AspNetUsers",
				type: "nvarchar(256)",
				maxLength: 256,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(256)",
				oldMaxLength: 256);

			migrationBuilder.AlterColumn<string>(
				name: "NormalizedUserName",
				table: "AspNetUsers",
				type: "nvarchar(256)",
				maxLength: 256,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(256)",
				oldMaxLength: 256);

			migrationBuilder.AlterColumn<string>(
				name: "NormalizedEmail",
				table: "AspNetUsers",
				type: "nvarchar(256)",
				maxLength: 256,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(256)",
				oldMaxLength: 256);

			migrationBuilder.AlterColumn<string>(
				name: "LastName",
				table: "AspNetUsers",
				type: "nvarchar(max)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(max)");

			migrationBuilder.AlterColumn<string>(
				name: "Gender",
				table: "AspNetUsers",
				type: "nvarchar(max)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(max)");

			migrationBuilder.AlterColumn<string>(
				name: "FirstName",
				table: "AspNetUsers",
				type: "nvarchar(max)",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "nvarchar(max)");

			migrationBuilder.CreateIndex(
				name: "UserNameIndex",
				table: "AspNetUsers",
				column: "NormalizedUserName",
				unique: true,
				filter: "[NormalizedUserName] IS NOT NULL");

		}
	}
}
