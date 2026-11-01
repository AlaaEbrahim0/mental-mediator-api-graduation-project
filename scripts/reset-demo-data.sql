-- ============================================================================
-- Nexus demo reset - scripts/reset-demo-data.sql
--
-- WHAT IT DOES
--   Deletes all APPLICATION DATA rows so scripts/seed.sql can lay down a clean,
--   coherent demo dataset. It keeps:
--     * the schema (no DDL),
--     * [__EFMigrationsHistory] (so the app's startup migration check is intact),
--     * [AspNetRoles] (the seeded User / Doctor / Admin / Anonymous roles).
--   Everything else - accounts, appointments, posts, chat, notifications,
--   scheduling, workspace rows - is removed.
--
--   After this, run scripts/seed.sql. Its own guard (refuses a DB that already
--   holds a non-@example.com account) passes here because the DB is now empty of
--   accounts, so the demo seeds cleanly.
--
-- SAFETY GUARDS (all must hold or it aborts before touching anything)
--   1. You must set @Confirm to the exact string 'RESET_NEXUS_DEMO_DATA'.
--   2. DB_NAME() must be one of the allowed demo database names below.
--   3. The whole delete runs in ONE transaction; any error rolls back.
--
--   >>> THIS DELETES THE 5 NON-@example.com ACCOUNTS CURRENTLY IN THE HOSTED DB
--   >>> (alaaebrahim387@, alaaebrahim38@, heshamahmed019@, karemsaeed451@,
--   >>>  lovable.probe.1@) AND ALL THEIR DATA. Recoverable only from a backup.
--   >>> Take/verify a backup first. Run only if you accept that.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Confirm nvarchar(64) = N'';                 -- <<< set to RESET_NEXUS_DEMO_DATA to arm
DECLARE @AllowedDbs nvarchar(200) = N'db75421,MentalMediator,nexus_demo';

IF @Confirm <> N'RESET_NEXUS_DEMO_DATA'
BEGIN
    RAISERROR(N'Reset aborted: set @Confirm to RESET_NEXUS_DEMO_DATA to arm this script.', 16, 1);
    RETURN;
END

IF CHARINDEX(DB_NAME() + N',', @AllowedDbs + N',') = 0
BEGIN
    RAISERROR(N'Reset aborted: current database "%s" is not in the allowed demo list.', 16, 1, DB_NAME());
    RETURN;
END

BEGIN TRAN;

-- Suspend foreign-key checks so delete order cannot fail on a child row.
EXEC sp_MSforeachtable @command1 = 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

-- Child -> parent ordering is not required with checks off, but we still delete
-- the Identity children before the parent for clarity.
DELETE FROM [ChatMessageReports];
DELETE FROM [ChatRoomInvitations];
DELETE FROM [ChatMessages];
DELETE FROM [ChatRoomMembers];
DELETE FROM [ChatRooms];
DELETE FROM [PostLikes];
DELETE FROM [Replies];
DELETE FROM [Comments];
DELETE FROM [PostMediaAssets];
DELETE FROM [Posts];
DELETE FROM [Notifications];
DELETE FROM [FollowUps];
DELETE FROM [PatientNotes];
DELETE FROM [Appointments];
DELETE FROM [DepressionTestResult];
DELETE FROM [DoctorTimeOff];
DELETE FROM [DoctorAvailabilityPeriods];
DELETE FROM [DoctorSchedulingSettings];
DELETE FROM [UserCertificates];
DELETE FROM [UserQualifications];
DELETE FROM [Users];
DELETE FROM [Doctors];
DELETE FROM [Admins];
DELETE FROM [AspNetUserRoles];
DELETE FROM [AspNetUserClaims];
DELETE FROM [AspNetRoleClaims];
DELETE FROM [AspNetUserLogins];
DELETE FROM [AspNetUserTokens];
DELETE FROM [AspNetUsers];
-- [AspNetRoles] and [__EFMigrationsHistory] intentionally preserved.

-- Restore FK checks.
EXEC sp_MSforeachtable @command1 = 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';

COMMIT;

-- Verification: these should all be 0 except AspNetRoles and __EFMigrationsHistory.
SELECT
    (SELECT COUNT(*) FROM AspNetUsers)              AS Accounts,
    (SELECT COUNT(*) FROM Appointments)             AS Appointments,
    (SELECT COUNT(*) FROM Posts)                    AS Posts,
    (SELECT COUNT(*) FROM ChatRooms)                AS ChatRooms,
    (SELECT COUNT(*) FROM Notifications)            AS Notifications,
    (SELECT COUNT(*) FROM AspNetRoles)              AS RolesKept,
    (SELECT COUNT(*) FROM __EFMigrationsHistory)    AS MigrationsKept;
PRINT N'Reset complete. Now run scripts/seed.sql.';
