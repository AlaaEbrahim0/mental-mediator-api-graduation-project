using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyDoctorScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsultationDurationMinutes",
                table: "DoctorSchedulingSettings",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.CreateTable(
                name: "DoctorAvailabilityPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilityPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilityPeriods_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorTimeOff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorTimeOff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorTimeOff_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_DoctorSchedulingSettings_ConsultationDuration",
                table: "DoctorSchedulingSettings",
                sql: "[ConsultationDurationMinutes] IN (30, 45, 60)");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilityPeriods_DoctorId_DayOfWeek_StartTime",
                table: "DoctorAvailabilityPeriods",
                columns: new[] { "DoctorId", "DayOfWeek", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorTimeOff_DoctorId_StartTimeUtc_EndTimeUtc",
                table: "DoctorTimeOff",
                columns: new[] { "DoctorId", "StartTimeUtc", "EndTimeUtc" });

            // Convert the legacy weekly rows into availability periods. Each
            // 09:00-17:00 day with a 12:00-13:00 recurring break becomes
            // 09:00-12:00 and 13:00-17:00, preserving the old bookable window.
            // The split works at minute granularity, which matches how the old
            // UI captured times.
            migrationBuilder.Sql(
                """
                ;WITH Minutes AS (
                    SELECT CAST(0 AS int) AS N
                    UNION ALL
                    SELECT N + 1 FROM Minutes WHERE N < 1439
                ),
                ScheduleMinutes AS (
                    SELECT s.DoctorId,
                           s.DayOfWeek,
                           m.N
                    FROM ScheduleWeekDays AS s
                    JOIN Minutes AS m
                      ON m.N >= DATEDIFF(MINUTE, CAST('00:00:00' AS time), s.StartTime)
                     AND m.N <  DATEDIFF(MINUTE, CAST('00:00:00' AS time), s.EndTime)
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM DoctorRecurringBreaks AS b
                        WHERE b.DoctorId = s.DoctorId
                          AND b.DayOfWeek = s.DayOfWeek
                          AND b.IsActive = 1
                          AND DATEADD(MINUTE, m.N, CAST('00:00:00' AS time)) >= b.StartTime
                          AND DATEADD(MINUTE, m.N, CAST('00:00:00' AS time)) < b.EndTime
                    )
                ),
                Grouped AS (
                    SELECT DoctorId,
                           DayOfWeek,
                           N,
                           N - ROW_NUMBER() OVER (PARTITION BY DoctorId, DayOfWeek ORDER BY N) AS Grp
                    FROM ScheduleMinutes
                )
                INSERT INTO DoctorAvailabilityPeriods (DoctorId, DayOfWeek, StartTime, EndTime)
                SELECT DoctorId,
                       DayOfWeek,
                       DATEADD(MINUTE, MIN(N), CAST('00:00:00' AS time)),
                       DATEADD(MINUTE, MAX(N) + 1, CAST('00:00:00' AS time))
                FROM Grouped
                GROUP BY DoctorId, DayOfWeek, Grp
                OPTION (MAXRECURSION 1440);
                """);

            // The old schema let a doctor create arbitrary UTC blocks. Those map
            // directly onto time off. Cancelled blocks and empty ranges are not
            // migrated; type and audit metadata are intentionally dropped.
            migrationBuilder.Sql(
                """
                INSERT INTO DoctorTimeOff (DoctorId, StartTimeUtc, EndTimeUtc, Reason, CreatedAtUtc)
                SELECT DoctorId,
                       StartTimeUtc,
                       EndTimeUtc,
                       PrivateReason,
                       COALESCE(CreatedAtUtc, SYSUTCDATETIME())
                FROM DoctorTimeBlocks
                WHERE IsCancelled = 0
                  AND EndTimeUtc > StartTimeUtc;
                """);

            // Old per-date overrides (full-day leave, replaced hours, extra
            // sessions) have no equivalent in the two-input model. They are
            // dropped rather than guessed at; see the migration notes in
            // api/docs/doctor-community-appointment-contracts.md.

            migrationBuilder.DropTable(name: "DoctorAvailabilityOverrides");
            migrationBuilder.DropTable(name: "DoctorRecurringBreaks");
            migrationBuilder.DropTable(name: "DoctorTimeBlocks");
            migrationBuilder.DropTable(name: "ScheduleWeekDays");

            migrationBuilder.DropColumn(name: "BufferAfterMinutes", table: "DoctorSchedulingSettings");
            migrationBuilder.DropColumn(name: "BufferBeforeMinutes", table: "DoctorSchedulingSettings");
            migrationBuilder.DropColumn(name: "MaxBookingDaysAhead", table: "DoctorSchedulingSettings");
            migrationBuilder.DropColumn(name: "MinimumNoticeMinutes", table: "DoctorSchedulingSettings");
            migrationBuilder.DropColumn(name: "SlotIncrementMinutes", table: "DoctorSchedulingSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DoctorSchedulingSettings_ConsultationDuration",
                table: "DoctorSchedulingSettings");

            migrationBuilder.AddColumn<int>(
                name: "BufferAfterMinutes",
                table: "DoctorSchedulingSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BufferBeforeMinutes",
                table: "DoctorSchedulingSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxBookingDaysAhead",
                table: "DoctorSchedulingSettings",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "MinimumNoticeMinutes",
                table: "DoctorSchedulingSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SlotIncrementMinutes",
                table: "DoctorSchedulingSettings",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.CreateTable(
                name: "DoctorAvailabilityOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PrivateReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilityOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilityOverrides_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorRecurringBreaks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorRecurringBreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorRecurringBreaks_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorTimeBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    PrivateReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorTimeBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorTimeBlocks_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleWeekDays",
                columns: table => new
                {
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    SessionDuration = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleWeekDays", x => new { x.DoctorId, x.DayOfWeek });
                    table.ForeignKey(
                        name: "FK_ScheduleWeekDays_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilityOverrides_DoctorId_Date_Type_IsActive",
                table: "DoctorAvailabilityOverrides",
                columns: new[] { "DoctorId", "Date", "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorRecurringBreaks_DoctorId_DayOfWeek_IsActive",
                table: "DoctorRecurringBreaks",
                columns: new[] { "DoctorId", "DayOfWeek", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorTimeBlocks_DoctorId_StartTimeUtc_EndTimeUtc_IsCancelled",
                table: "DoctorTimeBlocks",
                columns: new[] { "DoctorId", "StartTimeUtc", "EndTimeUtc", "IsCancelled" });

            // Best-effort rollback: collapse multiple periods back into the old
            // one-row-per-weekday shape. The old model cannot represent the gaps
            // between periods, so a break becomes non-bookable time only if the
            // weekday is later edited again.
            migrationBuilder.Sql(
                """
                INSERT INTO ScheduleWeekDays (DoctorId, DayOfWeek, StartTime, SessionDuration, EndTime)
                SELECT p.DoctorId,
                       p.DayOfWeek,
                       MIN(p.StartTime),
                       DATEADD(MINUTE,
                           COALESCE(s.ConsultationDurationMinutes, 30),
                           CAST('00:00:00' AS time)),
                       MAX(p.EndTime)
                FROM DoctorAvailabilityPeriods AS p
                LEFT JOIN DoctorSchedulingSettings AS s ON s.DoctorId = p.DoctorId
                GROUP BY p.DoctorId, p.DayOfWeek, s.ConsultationDurationMinutes;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO DoctorTimeBlocks
                    (DoctorId, StartTimeUtc, EndTimeUtc, Type, PrivateReason,
                     CreatedAtUtc, IsCancelled)
                SELECT DoctorId,
                       StartTimeUtc,
                       EndTimeUtc,
                       4,
                       Reason,
                       CreatedAtUtc,
                       0
                FROM DoctorTimeOff;
                """);

            migrationBuilder.DropTable(name: "DoctorAvailabilityPeriods");
            migrationBuilder.DropTable(name: "DoctorTimeOff");

            migrationBuilder.DropColumn(name: "ConsultationDurationMinutes", table: "DoctorSchedulingSettings");
        }
    }
}
