-- Seed script for the Nexus API production database.
--
-- The application no longer seeds anything at startup (see
-- Infrastructure/Data/DbInitializer.cs which only applies EF Core
-- migrations). Run this script once after the first deployment, from
-- the MonsterASP Control Panel (WebMSSQL) or SQL Server Management
-- Studio, connected to the production database.
--
-- The script is idempotent: it only inserts a role when the name is
-- still missing, so it is safe to run more than once and against a
-- database that already has data.
--
-- Table shape comes from the
-- 20240129013226_EntityTablesCreationAndSeeding migration:
--   AspNetRoles(Id nvarchar(450) PK, Name nvarchar(256),
--               NormalizedName nvarchar(256), ConcurrencyStamp nvarchar(max))
--
-- Authorization checks use the exact names below
-- (see Infrastructure/Data/DbInitializer.cs history and controllers):
--   User   - standard registered app user
--   Doctor - doctor portal accounts
--   Admin  - administrative accounts

INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT NEWID(), 'User', 'USER', REPLACE(NEWID(), '-', '')
WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'User');

INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT NEWID(), 'Doctor', 'DOCTOR', REPLACE(NEWID(), '-', '')
WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Doctor');

INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT NEWID(), 'Admin', 'ADMIN', REPLACE(NEWID(), '-', '')
WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin');

PRINT 'Role seeding complete.';
