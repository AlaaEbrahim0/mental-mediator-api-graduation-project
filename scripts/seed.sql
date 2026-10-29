-- ============================================================================
-- Nexus demo dataset - scripts/seed.sql
--
-- WHAT THIS IS
--   A curated demo dataset for a fresh or dedicated DEMO database. The
--   application only applies EF Core migrations at startup (see
--   Infrastructure/Data/DbInitializer.cs); no data is seeded from the app.
--   Run this after the migrations, from the MonsterASP Control Panel (WebMSSQL)
--   or SQL Server Management Studio.
--
--   Nothing in here is a real person. Every account uses an @example.com address
--   (RFC 2606, reserved for documentation), every credential belongs to a
--   fictional character, and the community content was written for this file.
--   No row should be presented as real user activity.
--
-- SAFETY
--   * Section 0 refuses to run against a database that already holds an account
--     outside the @example.com demo domain, so a live production database cannot
--     be seeded by accident.
--   * Every insert is guarded with NOT EXISTS on a natural key, so the script is
--     safe to run more than once and never duplicates the demo.
--   * The only UPDATEs touch rows belonging to the demo accounts listed here,
--     and they re-anchor timestamps and demo-only columns - never a status or a
--     body of text that a presenter may have changed through the UI.
--   * One transaction: if any statement fails, nothing lands.
--
-- DEMO LOGINS (demo databases only - never a production credential)
--   Password for every seeded account: Password1!
--     doctor@example.com  Nadia Rahman   Doctor  - Mon-Fri 09:30-17:00, 45 min
--     omar@example.com    Omar El-Sharif Doctor  - Mon/Wed/Fri + Tue/Thu evening, 60 min
--     hana@example.com    Hana Farouk    Doctor  - weekends only, 30 min
--     yusuf@example.com   Yusuf Karim    Doctor  - Tue-Thu mornings, off next week
--     salma@example.com   Salma Aziz     Doctor  - not accepting new patients
--     amira@example.com   Amira Khalil   User    - booked, posts, chats, unread alerts
--     marek@example.com   Marek Haddad   User    - pending request, anonymous post
--     sofia@example.com   Sofia Nagy     User    - completed sessions, reported a message
--     dana@example.com    Dana Hasan     User    - room moderator, cancelled a slot
--     rami@example.com    Rami Nasser    User    - a declined request, weekend slot
--     admin@example.com   Portal Admin   Admin   - administrator
--
--   Only admin@example.com holds the Admin role; no public demo account is given
--   elevated privileges. Prefer these per-role logins over one shared account so
--   the identity shown on each message and the audit trail stay honest.
--
-- HOW THE DEMO STAYS FRESH
--   Appointments are anchored to the weekday they are run in, and every relative
--   timestamp is derived from an "hours ago" figure on its own row, so re-running
--   keeps upcoming sessions in the coming week and history in the week before it
--   for as long as the demo database lives. All seeded doctors keep
--   TimeZoneId 'UTC' so the stored UTC instants and the weekly periods line up
--   without any daylight-saving assumption.
--
--   Slot times below are written as (DayOfWeek, minutes past midnight) and must
--   sit on that doctor's grid: period start + n x consultation length.
--     Nadia   Mon-Fri 09:30-17:00, 45 min -> 09:30 10:15 11:00 ... 16:15
--     Omar    Mon/Wed/Fri 11:00-18:00, Tue/Thu 16:00-20:00, 60 min
--             -> 11:00 12:00 ... 17:00  /  16:00 17:00 18:00 19:00
--     Hana    Sat/Sun 10:00-14:00, 30 min -> 10:00 10:30 ... 13:30
--     Yusuf   Tue-Thu 09:00-13:00, 30 min -> 09:00 09:30 ... 12:30
--     Salma   Mon/Tue/Thu 12:00-18:00, 45 min -> 12:00 12:45 ... 17:15
-- ============================================================================

SET NOCOUNT ON;
SET DATEFIRST 1;  -- DATEPART(WEEKDAY, ...) => 1 = Monday .. 7 = Sunday

BEGIN TRY
BEGIN TRANSACTION;

-- ----------------------------------------------------------------------------
-- 0. Refuse anything that is not a demo database.
-- ----------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM AspNetUsers WHERE Email IS NOT NULL AND Email NOT LIKE N'%@example.com')
BEGIN
    RAISERROR('Refusing to seed: this database already contains accounts outside the @example.com demo domain. Run scripts/seed.sql only against a fresh or dedicated demo database.', 16, 1);
    RETURN;
END

-- ----------------------------------------------------------------------------
-- 1. Clock anchors and the demo account table.
-- ----------------------------------------------------------------------------
DECLARE @NowUtc datetime2 = SYSUTCDATETIME();
DECLARE @Today date = CAST(@NowUtc AS date);
DECLARE @Weekday int = DATEPART(WEEKDAY, @Today);

-- Roles are not created by the application any more, and a fresh database
-- without them makes every registration fail with an opaque Identity Error 500.
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT NEWID(), 'User', 'USER', REPLACE(NEWID(), '-', '')
WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'User');

INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT NEWID(), 'Doctor', 'DOCTOR', REPLACE(NEWID(), '-', '')
WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Doctor');

INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT NEWID(), 'Admin', 'ADMIN', REPLACE(NEWID(), '-', '')
WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin');

-- Ordinals 1-5 keep the identifiers the previous revision of this script used, so
-- an already-seeded database is recognised rather than duplicated.
DECLARE @Users TABLE (
    Ordinal      int PRIMARY KEY,
    Email        nvarchar(256) UNIQUE,
    FirstName    nvarchar(100),
    LastName     nvarchar(100),
    BirthDate    date,
    Gender       nvarchar(10),
    Biography    nvarchar(500),
    City         nvarchar(100),
    Headline     nvarchar(160),
    Pronouns     nvarchar(40),
    Languages    nvarchar(200),
    Interests    nvarchar(200),
    IsPublic     bit,
    Role         nvarchar(10),
    HashOrdinal  int
);

INSERT INTO @Users VALUES
 (1, N'doctor@example.com', N'Nadia', N'Rahman', '1988-04-17', N'female',
  N'Counseling psychologist focused on anxiety, burnout and young-adult mental health. I believe therapy should feel like a conversation, not an evaluation.',
  N'Cairo', N'Counseling psychologist - anxiety and burnout', N'she/her',
  N'Arabic,English', N'Supervision groups,Running,Books about sleep', 1, N'Doctor', 1),
 (2, N'amira@example.com', N'Amira', N'Khalil', '1999-11-30', N'female',
  N'Design student. Better sleep and journaling - a work in progress.',
  N'Cairo', N'Design student', NULL, N'Arabic,English', N'Illustration,Ceramics', 1, N'User', 2),
 (3, N'marek@example.com', N'Marek', N'Haddad', '1994-06-08', N'male',
  N'Warehouse shift lead. Learning to say that I am not fine.',
  N'Alexandria', NULL, NULL, N'Arabic', N'Weightlifting', 0, N'User', 3),
 (4, N'sofia@example.com', N'Sofia', N'Nagy', '1996-02-21', N'female',
  N'Junior doctor. Interested in how stress shows up in the body.',
  N'Giza', NULL, NULL, N'Arabic,English', N'Running', 0, N'User', 4),
 (5, N'admin@example.com', N'Portal', N'Admin', '1990-01-01', N'female',
  NULL, NULL, NULL, NULL, NULL, NULL, 0, N'Admin', 5),
 (6, N'omar@example.com', N'Omar', N'El-Sharif', '1979-09-02', N'male',
  N'Clinical psychologist working with adults under sustained pressure - doctors, drivers, students. Sessions run an hour because I would rather finish a thought than watch the clock.',
  N'Alexandria', N'Clinical psychologist - stress, burnout and recovery', N'he/him',
  N'Arabic,English,French', N'Chess,Swimming,Clinical supervision', 1, N'Doctor', 2),
 (7, N'hana@example.com', N'Hana', N'Farouk', '1992-01-25', N'female',
  N'School psychologist. Most of my week goes into classrooms and parent meetings, so I keep weekends open for students and early-career staff who need a session.',
  N'Nasr City, Cairo', N'School psychologist - exam pressure and adolescent anxiety', N'she/her',
  N'Arabic,English', N'Choir,Baking', 1, N'Doctor', 3),
 (8, N'yusuf@example.com', N'Yusuf', N'Karim', '1983-07-11', N'male',
  N'Neuropsychologist. I do assessments and follow-ups for attention and memory, and I write up what I find in plain language so families can actually use it.',
  N'Giza', N'Neuropsychologist - assessment and follow-up', N'he/him',
  N'Arabic,English', N'Cycling,Archery', 1, N'Doctor', 4),
 (9, N'salma@example.com', N'Salma', N'Aziz', '1986-12-30', N'female',
  N'Health psychologist working alongside chronic-illness care. My list is full for this term - please check back rather than booking into a slot I cannot give you properly.',
  N'6 October City, Giza', N'Health psychologist - living with chronic illness', N'she/her',
  N'Arabic,English', N'Swimming,Community theatre', 1, N'Doctor', 5),
 (10, N'dana@example.com', N'Dana', N'Hasan', '1991-03-14', N'female',
  N'Translator, two kids, one long panic-attack history and a slowly improving sleep routine.',
  N'Cairo', N'Freelance translator', NULL, N'Arabic,English,French', N'Gardening', 0, N'User', 1),
 (11, N'rami@example.com', N'Rami', N'Nasser', '2002-05-19', N'male',
  N'Final-year engineering student. Exam season is a recurring villain.',
  N'Cairo', NULL, NULL, N'Arabic,English', N'Football,Robotics', 0, N'User', 5);

-- Deterministic key per demo account: its ordinal, zero padded into the same
-- shape the earlier revisions used.
DECLARE @Demo TABLE (
    Id nvarchar(450), Email nvarchar(256), UserName nvarchar(256),
    Hash nvarchar(max), FirstName nvarchar(100), LastName nvarchar(100),
    BirthDate date, Gender nvarchar(10), Biography nvarchar(500), City nvarchar(100),
    Headline nvarchar(160), Pronouns nvarchar(40), Languages nvarchar(200),
    Interests nvarchar(200), IsPublic bit, Role nvarchar(10));

INSERT INTO @Demo
SELECT N'00000000-0000-0000-0000-' + RIGHT(REPLICATE('0', 12) + CAST(u.Ordinal AS varchar(12)), 12),
       u.Email,
       LEFT(u.Email, CHARINDEX(N'@', u.Email) - 1),
       h.Hash, u.FirstName, u.LastName, u.BirthDate, u.Gender, u.Biography, u.City,
       u.Headline, u.Pronouns, u.Languages, u.Interests, u.IsPublic, u.Role
FROM @Users u
JOIN (VALUES
 (1, N'AQAAAAIAAYagAAAAEAmYYhPy+wh2cXBO5oSGznOEtX1OQY2/scV111VZw8RJF+eatqTfZTkOlpS6YGmZxw=='),
 (2, N'AQAAAAIAAYagAAAAEDFVO5ypxTwLo9buI4t3VWngJzkgSp8wLCNvFD028vdHPlIbP6oVL1vqp2FdJxoCKQ=='),
 (3, N'AQAAAAIAAYagAAAAEKnJHj82MB77VKKjlMSfB74dBn9YRDy/PL90eXoB600iW/BF52s/bulzbyNZvHhBHQ=='),
 (4, N'AQAAAAIAAYagAAAAEIQQbUr5fkIGg7Ga7wX7esgCsLn99t12AUiXZD5xU+OpfvbwfRcpDF//S92Ji+WQwg=='),
 (5, N'AQAAAAIAAYagAAAAEHEjVmCLuTc6AatBl774gLHaKZ9h1L7rP8QO80a9sOmTyyhkMD01BnM9k8Z6SeSdIA==')
) h (Ordinal, Hash) ON h.Ordinal = u.HashOrdinal;

-- BaseUser recomputes UserName and the Normalized* values in C#, but the columns
-- are NOT NULL and EF writes the backing field, so each is set explicitly here.
-- PhotoUrl stays NULL on purpose: the portals fall back to a monogram avatar,
-- which renders offline and does not put a synthetic face beside a fictional
-- clinician's name.
MERGE AspNetUsers AS tgt
USING @Demo AS src ON tgt.Id = src.Id
WHEN NOT MATCHED THEN INSERT
    (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
     PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
     TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName,
     BirthDate, Gender, Biography, City, PhotoUrl, IsDeleted, IsProfilePublic,
     Headline, Pronouns, Languages, Interests)
    VALUES (src.Id, src.UserName, UPPER(src.UserName), src.Email, UPPER(src.Email), 1,
            src.Hash, src.Id, src.Id, NULL, 0, 0, 0, 0,
            src.FirstName, src.LastName, src.BirthDate, src.Gender, src.Biography, src.City,
            NULL, 0, src.IsPublic, src.Headline, src.Pronouns, src.Languages, src.Interests)
WHEN MATCHED THEN UPDATE SET
    -- Only fill in columns this revision added, and never over anything a human
    -- has since typed into the profile editor.
    Headline = COALESCE(NULLIF(tgt.Headline, N''), src.Headline),
    Pronouns = COALESCE(NULLIF(tgt.Pronouns, N''), src.Pronouns),
    Languages = COALESCE(NULLIF(tgt.Languages, N''), src.Languages),
    Interests = COALESCE(NULLIF(tgt.Interests, N''), src.Interests),
    Biography = COALESCE(NULLIF(tgt.Biography, N''), src.Biography),
    City = COALESCE(NULLIF(tgt.City, N''), src.City);

-- AuthService refuses an unconfirmed account, so a seeded login nobody can
-- confirm would be a dead end.
UPDATE AspNetUsers SET EmailConfirmed = 1
WHERE Email LIKE N'%@example.com' AND EmailConfirmed = 0;

-- The earlier revision wrote Gender capitalised; the app writes it lowercase.
UPDATE AspNetUsers SET Gender = LOWER(Gender)
WHERE Email LIKE N'%@example.com' AND Gender IS NOT NULL AND Gender <> LOWER(Gender);

-- TPT child rows: Doctors / Users / Admins, keyed by the same Id.
INSERT INTO Doctors (Id)
SELECT d.Id FROM @Demo d WHERE d.Role = N'Doctor'
  AND NOT EXISTS (SELECT 1 FROM Doctors x WHERE x.Id = d.Id);

INSERT INTO Users (Id)
SELECT d.Id FROM @Demo d WHERE d.Role = N'User'
  AND NOT EXISTS (SELECT 1 FROM Users x WHERE x.Id = d.Id);

INSERT INTO Admins (Id)
SELECT d.Id FROM @Demo d WHERE d.Role = N'Admin'
  AND NOT EXISTS (SELECT 1 FROM Admins x WHERE x.Id = d.Id);

-- Doctor practice details. Specialization is the DoctorSpecialization enum as
-- int: 0 Clinical, 1 Counseling, 2 Health, 3 Neuro, 4 Forensic, 5 School, 6 Social.
-- Salma is deliberately IsAvailable = 0 so that the "Not accepting new patients"
-- badge and a closed list are both demoable.
MERGE Doctors AS tgt
USING (VALUES
 (N'doctor@example.com', 1, N'Heliopolis, Cairo', 150.00, 1,
  N'Consultation counselor', N'Quiet Mind Studio', 9,
  N'MSc Counseling Psychology (fictional demo credential); CBT practitioner training; 9 years in private practice'),
 (N'omar@example.com',  0, N'Stanley, Alexandria', 220.00, 1,
  N'Clinical psychologist', N'Stanley Psychology Clinic', 18,
  N'PhD Clinical Psychology (fictional demo credential); 18 years in hospital and private practice'),
 (N'hana@example.com',  5, N'Nasr City, Cairo', 120.00, 1,
  N'School psychologist', N'Bright Steps Learning Centre', 6,
  N'MA School Psychology (fictional demo credential); 6 years in secondary education'),
 (N'yusuf@example.com', 3, N'Dokki, Giza', 300.00, 1,
  N'Neuropsychologist', N'Dokki Memory and Attention Unit', 12,
  N'Postgraduate diploma in Neuropsychology (fictional demo credential); 12 years in assessment'),
 (N'salma@example.com', 2, N'6 October City, Giza', 180.00, 0,
  N'Health psychologist', N'October Wellness Clinic', 11,
  N'MSc Health Psychology (fictional demo credential); 11 years in hospital settings')
) v (Email, Specialization, Location, SessionFees, IsAvailable,
     ProfessionalTitle, ClinicName, YearsOfExperience, Qualifications)
ON tgt.Id = (SELECT d.Id FROM @Demo d WHERE d.Email = v.Email)
WHEN MATCHED THEN UPDATE SET
    Specialization = v.Specialization,
    Location = v.Location,
    SessionFees = v.SessionFees,
    IsAvailable = v.IsAvailable,
    ProfessionalTitle = v.ProfessionalTitle,
    ClinicName = v.ClinicName,
    YearsOfExperience = v.YearsOfExperience,
    Qualifications = v.Qualifications;

-- Contact details are visible only to the account holder and to an admin, which
-- is exactly the rule the private patient rows above demonstrate.
UPDATE u SET ContactEmail = u.Email
FROM AspNetUsers u
JOIN @Demo d ON d.Id = u.Id
WHERE d.Role = N'Doctor' AND NULLIF(u.ContactEmail, N'') IS NULL;

UPDATE u SET ContactAddress = v.Address
FROM AspNetUsers u
JOIN @Demo d ON d.Id = u.Id
JOIN (VALUES
 (N'doctor@example.com', N'12 Elm Street, Heliopolis, Cairo'),
 (N'omar@example.com',  N'8 Corniche Road, Stanley, Alexandria'),
 (N'hana@example.com',  N'Bright Steps, Nasr City, Cairo'),
 (N'yusuf@example.com', N'Dokki Memory Unit, Dokki, Giza'),
 (N'salma@example.com', N'October Wellness Clinic, 6 October City, Giza')
) v (Email, Address) ON v.Email = d.Email
WHERE NULLIF(u.ContactAddress, N'') IS NULL;

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT d.Id, r.Id
FROM @Demo d
CROSS JOIN AspNetRoles r
WHERE r.Name = d.Role
  AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur WHERE ur.UserId = d.Id AND ur.RoleId = r.Id);

-- ----------------------------------------------------------------------------
-- 2. Credentials and education. These fill the profile cards and the certificate
--    and qualification modals, which otherwise render as empty sections.
--    Institutions and titles are invented, with no licence numbers - nothing that
--    implies the verification of a real practitioner.
-- ----------------------------------------------------------------------------
DECLARE @Qualifications TABLE (
    Email nvarchar(256), Degree nvarchar(160), Institution nvarchar(200),
    FieldOfStudy nvarchar(160), StartYear int, EndYear int, Description nvarchar(1000));
INSERT INTO @Qualifications VALUES
 (N'doctor@example.com', N'MSc', N'Heliopolis School of Psychology', N'Counseling Psychology', 2011, 2014, N'Thesis on burnout in early-career counselors.'),
 (N'doctor@example.com', N'BA', N'Nile Valley University', N'Psychology', 2007, 2011, NULL),
 (N'omar@example.com',  N'PhD', N'Alexandria Institute of Behavioral Science', N'Clinical Psychology', 2006, 2012, N'Hospital practicum in stress and adjustment disorders.'),
 (N'omar@example.com',  N'MSc', N'Alexandria Institute of Behavioral Science', N'Clinical Psychology', 2002, 2006, NULL),
 (N'hana@example.com',  N'MA',  N'Heliopolis School of Psychology', N'School Psychology', 2014, 2017, NULL),
 (N'yusuf@example.com', N'PGDip', N'Giza Neuroscience College', N'Neuropsychology', 2008, 2010, N'Assessment and report writing for attention and memory concerns.'),
 (N'salma@example.com', N'MSc', N'October Health Faculty', N'Health Psychology', 2009, 2012, NULL);

INSERT INTO UserQualifications (UserId, Degree, Institution, FieldOfStudy, StartYear, EndYear, Description)
SELECT d.Id, q.Degree, q.Institution, q.FieldOfStudy, q.StartYear, q.EndYear, q.Description
FROM @Qualifications q
JOIN @Demo d ON d.Email = q.Email
WHERE NOT EXISTS (SELECT 1 FROM UserQualifications uq
                  WHERE uq.UserId = d.Id AND uq.Degree = q.Degree AND uq.Institution = q.Institution);

DECLARE @Certificates TABLE (
    Email nvarchar(256), Title nvarchar(160), Organization nvarchar(160),
    IssueDate date, ExpiryDate date, Description nvarchar(1000));
INSERT INTO @Certificates VALUES
 (N'doctor@example.com', N'Certified CBT Practitioner (training programme)', N'Levant CBT Training Collective', '2016-05-20', '2029-05-20', N'Completed alongside private-practice supervision.'),
 (N'doctor@example.com', N'Group Facilitation for Peer Support', N'Quiet Mind Studio', '2021-02-10', NULL, N'Internal programme for running the community rooms.'),
 (N'omar@example.com',  N'Clinical Assessment and Formulation', N'Alexandria Institute of Behavioral Science', '2013-09-01', NULL, NULL),
 (N'hana@example.com',  N'Advising Parents on Exam Pressure', N'Bright Steps Learning Centre', '2019-11-15', '2028-11-15', NULL),
 (N'yusuf@example.com', N'Neurocognitive Test Battery', N'Giza Neuroscience College', '2011-06-30', NULL, NULL),
 (N'salma@example.com', N'Behavioral Change in Chronic Illness', N'October Health Faculty', '2015-03-12', NULL, NULL);

INSERT INTO UserCertificates (UserId, Title, IssuingOrganization, IssueDate, ExpiryDate, Description)
SELECT d.Id, c.Title, c.Organization, c.IssueDate, c.ExpiryDate, c.Description
FROM @Certificates c
JOIN @Demo d ON d.Email = c.Email
WHERE NOT EXISTS (SELECT 1 FROM UserCertificates uc WHERE uc.UserId = d.Id AND uc.Title = c.Title);

-- ----------------------------------------------------------------------------
-- 3. Scheduling. A weekly DoctorAvailabilityPeriod is a working window in the
--    doctor's own time zone; DoctorSchedulingSettings fixes the consultation
--    length; DoctorTimeOff removes a range of UTC instants. Bookable slots are
--    derived from all three - nothing here stores a slot.
-- ----------------------------------------------------------------------------
DECLARE @Settings TABLE (Email nvarchar(256), DurationMinutes int);
INSERT INTO @Settings VALUES
 (N'doctor@example.com', 45), (N'omar@example.com', 60), (N'hana@example.com', 30),
 (N'yusuf@example.com', 30), (N'salma@example.com', 45);

INSERT INTO DoctorSchedulingSettings (DoctorId, TimeZoneId, ConsultationDurationMinutes)
SELECT d.Id, N'UTC', s.DurationMinutes
FROM @Settings s
JOIN @Demo d ON d.Email = s.Email
WHERE NOT EXISTS (SELECT 1 FROM DoctorSchedulingSettings x WHERE x.DoctorId = d.Id);

-- Keep length and zone aligned with this file even on a database seeded earlier.
UPDATE x SET ConsultationDurationMinutes = s.DurationMinutes, TimeZoneId = N'UTC'
FROM DoctorSchedulingSettings x
JOIN @Demo d ON d.Id = x.DoctorId
JOIN @Settings s ON s.Email = d.Email
WHERE x.ConsultationDurationMinutes <> s.DurationMinutes OR x.TimeZoneId <> N'UTC';

-- DayOfWeek: 0 = Sunday .. 6 = Saturday. A weekday may carry several periods.
DECLARE @Periods TABLE (Email nvarchar(256), DayOfWeek int, StartTime time, EndTime time);
INSERT INTO @Periods VALUES
 (N'doctor@example.com', 1, '09:30:00', '17:00:00'), (N'doctor@example.com', 2, '09:30:00', '17:00:00'),
 (N'doctor@example.com', 3, '09:30:00', '17:00:00'), (N'doctor@example.com', 4, '09:30:00', '17:00:00'),
 (N'doctor@example.com', 5, '09:30:00', '17:00:00'),
 (N'omar@example.com', 1, '11:00:00', '18:00:00'), (N'omar@example.com', 3, '11:00:00', '18:00:00'),
 (N'omar@example.com', 5, '11:00:00', '18:00:00'), (N'omar@example.com', 2, '16:00:00', '20:00:00'),
 (N'omar@example.com', 4, '16:00:00', '20:00:00'),
 (N'hana@example.com', 6, '10:00:00', '14:00:00'), (N'hana@example.com', 0, '10:00:00', '14:00:00'),
 (N'yusuf@example.com', 2, '09:00:00', '13:00:00'), (N'yusuf@example.com', 3, '09:00:00', '13:00:00'),
 (N'yusuf@example.com', 4, '09:00:00', '13:00:00'),
 (N'salma@example.com', 1, '12:00:00', '18:00:00'), (N'salma@example.com', 2, '12:00:00', '18:00:00'),
 (N'salma@example.com', 4, '12:00:00', '18:00:00');

INSERT INTO DoctorAvailabilityPeriods (DoctorId, DayOfWeek, StartTime, EndTime)
SELECT d.Id, p.DayOfWeek, p.StartTime, p.EndTime
FROM @Periods p
JOIN @Demo d ON d.Email = p.Email
WHERE NOT EXISTS (SELECT 1 FROM DoctorAvailabilityPeriods x
                  WHERE x.DoctorId = d.Id AND x.DayOfWeek = p.DayOfWeek AND x.StartTime = p.StartTime);

-- Two different shapes of unavailability: Yusuf is away for the whole of next
-- Tuesday and Wednesday (those days read as booked out while later weeks stay
-- open), and Salma keeps a hospital day every Friday.
DECLARE @TimeOff TABLE (Email nvarchar(256), Reason nvarchar(500), DayOfWeek int,
                        Future bit, DurationHours int);
INSERT INTO @TimeOff VALUES
 (N'yusuf@example.com', N'Conference out of town', 2, 1, 54),
 (N'salma@example.com', N'Hospital rounds day', 5, 1, 6);

INSERT INTO DoctorTimeOff (DoctorId, StartTimeUtc, EndTimeUtc, Reason, CreatedAtUtc)
SELECT d.Id, anchor.StartUtc, DATEADD(HOUR, t.DurationHours, anchor.StartUtc),
       t.Reason, @NowUtc
FROM @TimeOff t
JOIN @Demo d ON d.Email = t.Email
CROSS APPLY (SELECT DATEADD(DAY,
       CASE WHEN t.Future = 1
            THEN CASE WHEN (t.DayOfWeek - @Weekday + 7) % 7 = 0 THEN 7 ELSE (t.DayOfWeek - @Weekday + 7) % 7 END
            ELSE -1 * CASE WHEN (@Weekday - t.DayOfWeek + 7) % 7 = 0 THEN 7 ELSE (@Weekday - t.DayOfWeek + 7) % 7 END
       END, CAST(@Today AS datetime2)) AS StartUtc) anchor
WHERE NOT EXISTS (SELECT 1 FROM DoctorTimeOff x WHERE x.DoctorId = d.Id AND x.Reason = t.Reason);

-- ----------------------------------------------------------------------------
-- 4. Appointments. Status: 0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled,
--    4=Rejected. Duration, clinic location and fee are read from the doctor's own
--    scheduling settings, so a seeded appointment can never disagree with the
--    availability the portals show. EndTime is computed - never inserted.
-- ----------------------------------------------------------------------------
DECLARE @Appointments TABLE (
    PatientEmail nvarchar(256), DoctorEmail nvarchar(256), Reason nvarchar(1000),
    DayOfWeek int, MinOfDay int, Future bit, Status int,
    CancellationReason nvarchar(1000), RejectionReason nvarchar(1000));
INSERT INTO @Appointments VALUES
 -- Nadia - Mon-Fri 09:30-17:00, 45 min
 (N'amira@example.com', N'doctor@example.com', N'Persistent anxiety and trouble sleeping',  1,  615, 1, 1, NULL, NULL),
 (N'marek@example.com', N'doctor@example.com', N'Low mood and motivation lately',           3,  840, 1, 0, NULL, NULL),
 (N'sofia@example.com', N'doctor@example.com', N'Work-related stress',                     1,  660, 0, 2, NULL, NULL),
 (N'dana@example.com',  N'doctor@example.com', N'Recent panic attacks at work',             2,  570, 0, 3, N'Rescheduled - a family trip came up', NULL),
 (N'rami@example.com',  N'doctor@example.com', N'Difficulty switching off at night',        3,  930, 0, 4, NULL, N'My list is full this term, please try again after the exams'),
 -- Omar - Mon/Wed/Fri 11:00-18:00, Tue/Thu 16:00-20:00, 60 min
 (N'amira@example.com', N'omar@example.com',   N'Second view on stress management',         5,  720, 1, 1, NULL, NULL),
 (N'sofia@example.com', N'omar@example.com',   N'Burnout recovery plan',                    1,  960, 1, 0, NULL, NULL),
 (N'marek@example.com', N'omar@example.com',   N'Sleep routine follow-up',                  4, 1020, 0, 2, NULL, NULL),
 -- Hana - Sat/Sun 10:00-14:00, 30 min
 (N'rami@example.com',  N'hana@example.com',   N'Exam-season anxiety',                      6,  630, 1, 1, NULL, NULL),
 (N'sofia@example.com', N'hana@example.com',   N'Talking through a difficult placement',    6,  660, 0, 2, NULL, NULL),
 -- Salma - Mon/Tue/Thu 12:00-18:00, 45 min. Past history only; her list is closed.
 (N'dana@example.com',  N'salma@example.com',  N'Stress and blood pressure',                4,  810, 0, 2, NULL, NULL),
 (N'rami@example.com',  N'salma@example.com',  N'Questions about a sibling diagnosis',      1,  765, 0, 3, N'Cancelled by the clinic - doctor on emergency leave', NULL);

-- Resolve every spec row once, so the insert and the re-anchoring update below
-- share one definition of what each demo appointment is.
DECLARE @ResolvedAppointments TABLE (
    UserId nvarchar(450), DoctorId nvarchar(450), Reason nvarchar(1000),
    StartTime datetime2, Duration time, Status int, Location nvarchar(200),
    Fees decimal(18,2), CancellationReason nvarchar(1000), RejectionReason nvarchar(1000));

INSERT INTO @ResolvedAppointments
SELECT p.Id, dr.Id, a.Reason,
       DATEADD(MINUTE, a.MinOfDay,
           DATEADD(DAY,
               CASE WHEN a.Future = 1
                    THEN CASE WHEN (a.DayOfWeek - @Weekday + 7) % 7 = 0 THEN 7 ELSE (a.DayOfWeek - @Weekday + 7) % 7 END
                    ELSE -1 * CASE WHEN (@Weekday - a.DayOfWeek + 7) % 7 = 0 THEN 7 ELSE (@Weekday - a.DayOfWeek + 7) % 7 END
               END, CAST(@Today AS datetime2))),
       CAST(DATEADD(MINUTE, s.DurationMinutes, CAST('00:00:00' AS time)) AS time),
       a.Status, doc.Location, doc.SessionFees, a.CancellationReason, a.RejectionReason
FROM @Appointments a
JOIN @Demo p ON p.Email = a.PatientEmail
JOIN @Demo dr ON dr.Email = a.DoctorEmail
JOIN Doctors doc ON doc.Id = dr.Id
JOIN @Settings s ON s.Email = a.DoctorEmail;

INSERT INTO Appointments (UserId, DoctorId, StartTime, Duration, Status, Location, Reason,
                          CancellationReason, RejectionReason, Fees)
SELECT r.UserId, r.DoctorId, r.StartTime, r.Duration, r.Status, r.Location, r.Reason,
       r.CancellationReason, r.RejectionReason, r.Fees
FROM @ResolvedAppointments r
WHERE NOT EXISTS (SELECT 1 FROM Appointments ap
                  WHERE ap.UserId = r.UserId AND ap.DoctorId = r.DoctorId AND ap.Reason = r.Reason);

-- Demo clock: an appointment placed by an earlier run has drifted into the past.
-- Moving it to this run's anchor keeps the weekday, and so the grid, intact, and
-- leaves the status alone in case a presenter changed it through the UI.
UPDATE ap SET StartTime = r.StartTime,
              Duration = r.Duration,
              Location = r.Location,
              Fees = r.Fees
FROM Appointments ap
JOIN @ResolvedAppointments r
  ON r.UserId = ap.UserId AND r.DoctorId = ap.DoctorId AND r.Reason = ap.Reason
WHERE ap.StartTime <> r.StartTime;

-- ----------------------------------------------------------------------------
-- 5. Community posts. Written for this file: a guidance thread, a recovery
--    reflection, an anonymous confession, a stress-management observation, an
--    advice question, a clinician's note on exam pressure, a small win, and a
--    coping-techniques list. No graphic content, no real stories, no treatment
--    claims. IsAnonymous = 1 hides the author in the feed.
-- ----------------------------------------------------------------------------
DECLARE @Posts TABLE (
    AuthorEmail nvarchar(256), Title nvarchar(2000), Content nvarchar(max),
    Category nvarchar(100), Tags nvarchar(1000), IsAnonymous bit, HoursAgo int);
INSERT INTO @Posts VALUES
 (N'doctor@example.com', N'How to prepare for your first counseling session',
  N'You do not need a perfect script - just bring the moment that pushed you to book, the nights that felt hardest, and one small question you keep carrying. Everything else we build together, at your pace.',
  N'Guidance', N'counseling,tips,first-session', 0, 74),
 (N'amira@example.com', N'One month of therapy, what actually changed',
  N'First session was frightening and the second was boring - turns out that is normal. What changed: I sleep before 2am, I stopped rereading every message five times, and I have actual scripts for the moments that used to spiral. Progress is quieter than I expected.',
  N'Journey', N'therapy,experience,progress', 0, 52),
 (N'marek@example.com', N'I keep everything inside',
  N'To everyone who thinks feeling nothing for months is normal: I thought so too, until I realized I could not remember my last honest conversation. Posting here anonymously because this is the only place I say it out loud.',
  N'Confession', N'anxiety,support', 1, 27),
 (N'sofia@example.com', N'The 3pm slump is not laziness',
  N'I used to treat every low afternoon as a character flaw. Then I started writing down what happened in the four hours before it - skipped lunch, no daylight, back-to-back wards - and it stopped being a mystery. Curious whether anyone else tracks it that way.',
  N'Coping', N'stress,energy,routine', 0, 19),
 (N'dana@example.com', N'Anyone else wake up at 4am with a racing mind?',
  N'Mine starts with one unfinished task and by 4:30 it is a full inventory of every mistake I have ever made. I would like to hear what has helped you - not as advice, just as evidence that I am not the only one.',
  N'Questions', N'sleep,anxiety,community', 0, 11),
 (N'hana@example.com', N'What I wish parents understood about exam pressure',
  N'The students who do worst are rarely the ones who study least. They are the ones whose whole week has one outcome riding on it. Two sentences that have helped more than any study plan: "this does not change what I think of you", and "let us eat first".',
  N'Guidance', N'students,exams,family', 0, 7),
 (N'rami@example.com', N'Small wins: the walk I took before the call',
  N'For months I have been cancelling things at the last minute. Today I put my shoes on, walked for twelve minutes, and then sat down and did the thing I was scared of. Posting it here so it counts as a real record.',
  N'Journey', N'progress,small-steps', 0, 3),
 (N'omar@example.com', N'Grounding techniques that actually stuck',
  N'Not the long list - the two that survived contact with real panic. Naming five things I can see, out loud, badly, and holding a cold glass. Both take seconds and neither requires me to calm down first.',
  N'Support', N'coping,panic,practical', 0, 1);

INSERT INTO Posts (AppUserId, Title, Content, Category, Tags, IsAnonymous, PostedOn, PostPhotoUrl)
SELECT d.Id, p.Title, p.Content, p.Category, p.Tags, p.IsAnonymous,
       DATEADD(HOUR, -1 * p.HoursAgo, @NowUtc), NULL
FROM @Posts p
JOIN @Demo d ON d.Email = p.AuthorEmail
WHERE NOT EXISTS (SELECT 1 FROM Posts x WHERE x.Title = p.Title);

-- Refresh a post's age on re-run without touching its text.
UPDATE po SET PostedOn = DATEADD(HOUR, -1 * p.HoursAgo, @NowUtc)
FROM Posts po
JOIN @Posts p ON p.Title = po.Title
WHERE po.PostedOn <> DATEADD(HOUR, -1 * p.HoursAgo, @NowUtc);

-- ----------------------------------------------------------------------------
-- 6. Comments and replies, keyed on (post, author) so a re-run cannot add a
--    second copy, and every reply points at a parent comment that exists.
-- ----------------------------------------------------------------------------
DECLARE @Comments TABLE (Title nvarchar(2000), AuthorEmail nvarchar(256), Content nvarchar(4000), HoursAgo int);
INSERT INTO @Comments VALUES
 (N'One month of therapy, what actually changed', N'doctor@example.com',
  N'Thank you for trusting us with your story. The quiet wins you listed - earlier sleep, fewer rereads - are exactly the signs worth noticing. Keep going.', 49),
 (N'How to prepare for your first counseling session', N'amira@example.com',
  N'This helped so much. The "no perfect script" part took the pressure off. Booking my first call this week.', 44),
 (N'How to prepare for your first counseling session', N'sofia@example.com',
  N'I printed that list and stuck it inside my laptop lid. Small thing, but I read it before every appointment now.', 38),
 (N'I keep everything inside', N'dana@example.com',
  N'Reading this on the bus and nodding. The part about not remembering your last honest conversation is the line I did not expect to be seen.', 25),
 (N'I keep everything inside', N'rami@example.com',
  N'You are not the only one here. Saying it anywhere is harder than saying it well.', 21),
 (N'The 3pm slump is not laziness', N'omar@example.com',
  N'The four-hours-before habit is doing a lot of quiet work in this post. Worth noting too that a slump which never arrives at the weekend is rarely about willpower.', 17),
 (N'Anyone else wake up at 4am with a racing mind?', N'amira@example.com',
  N'Yes, and the worst part is the arithmetic - by the time I stop counting hours there is nothing left to count.', 9),
 (N'Anyone else wake up at 4am with a racing mind?', N'doctor@example.com',
  N'What you describe is common enough that it has a name. Keeping the phone out of the room and writing the one open task down, rather than arguing with it at 4am, helps more people than any app.', 8),
 (N'What I wish parents understood about exam pressure', N'marek@example.com',
  N'"Let us eat first" is going into my house rules. My mother would have hated how much that would have changed things.', 6),
 (N'Small wins: the walk I took before the call', N'hana@example.com',
  N'Twelve minutes and then the thing itself. That is the whole trick, and it is not small.', 2),
 (N'Grounding techniques that actually stuck', N'sofia@example.com',
  N'The cold glass one worked for me during a night shift and I have never told anyone at work about it.', 1);

INSERT INTO Comments (PostId, AppUserId, Content, CommentedAt)
SELECT po.Id, d.Id, c.Content, DATEADD(HOUR, -1 * c.HoursAgo, @NowUtc)
FROM @Comments c
JOIN @Demo d ON d.Email = c.AuthorEmail
JOIN Posts po ON po.Title = c.Title
WHERE NOT EXISTS (SELECT 1 FROM Comments x WHERE x.PostId = po.Id AND x.AppUserId = d.Id);

UPDATE cm SET CommentedAt = v.CommentedAt
FROM Comments cm
JOIN (
    SELECT po.Id AS PostId, d.Id AS UserId,
           DATEADD(HOUR, -1 * c.HoursAgo, @NowUtc) AS CommentedAt
    FROM @Comments c
    JOIN @Demo d ON d.Email = c.AuthorEmail
    JOIN Posts po ON po.Title = c.Title
) v ON v.PostId = cm.PostId AND v.UserId = cm.AppUserId
WHERE cm.CommentedAt <> v.CommentedAt;

DECLARE @Replies TABLE (Title nvarchar(2000), ParentEmail nvarchar(256), AuthorEmail nvarchar(256), Content nvarchar(4000), HoursAgo int);
INSERT INTO @Replies VALUES
 (N'One month of therapy, what actually changed', N'doctor@example.com', N'marek@example.com',
  N'Reading this while working up the courage to book anything. The boring second session line genuinely helps.', 20),
 (N'One month of therapy, what actually changed', N'doctor@example.com', N'amira@example.com',
  N'Same here - I had convinced myself that a session had to be dramatic to count.', 14),
 (N'How to prepare for your first counseling session', N'amira@example.com', N'doctor@example.com',
  N'Good luck. Bring the awkward version of the question; those are usually the useful ones.', 40),
 (N'I keep everything inside', N'dana@example.com', N'marek@example.com',
  N'Thank you. It is strange how much lighter a thing gets once it has been written somewhere.', 23),
 (N'Anyone else wake up at 4am with a racing mind?', N'doctor@example.com', N'dana@example.com',
  N'The writing-it-down idea is going on the bedside table tonight. Thank you for saying it plainly.', 6);

INSERT INTO Replies (CommentId, AppUserId, Content, RepliedAt)
SELECT cm.Id, a.Id, r.Content, DATEADD(HOUR, -1 * r.HoursAgo, @NowUtc)
FROM @Replies r
JOIN @Demo a ON a.Email = r.AuthorEmail
JOIN @Demo pn ON pn.Email = r.ParentEmail
JOIN Posts po ON po.Title = r.Title
JOIN Comments cm ON cm.PostId = po.Id AND cm.AppUserId = pn.Id
WHERE NOT EXISTS (SELECT 1 FROM Replies x WHERE x.CommentId = cm.Id AND x.AppUserId = a.Id);

UPDATE rp SET RepliedAt = DATEADD(HOUR, -1 * r.HoursAgo, @NowUtc)
FROM Replies rp
JOIN @Replies r ON r.Content = rp.Content
JOIN @Demo a ON a.Email = r.AuthorEmail
WHERE rp.AppUserId = a.Id AND rp.RepliedAt <> DATEADD(HOUR, -1 * r.HoursAgo, @NowUtc);

DECLARE @Likes TABLE (Title nvarchar(2000), Email nvarchar(256), HoursAgo int);
INSERT INTO @Likes VALUES
 (N'One month of therapy, what actually changed', N'marek@example.com', 30),
 (N'One month of therapy, what actually changed', N'dana@example.com', 28),
 (N'One month of therapy, what actually changed', N'sofia@example.com', 24),
 (N'One month of therapy, what actually changed', N'rami@example.com', 12),
 (N'How to prepare for your first counseling session', N'marek@example.com', 26),
 (N'How to prepare for your first counseling session', N'sofia@example.com', 25),
 (N'How to prepare for your first counseling session', N'amira@example.com', 43),
 (N'I keep everything inside', N'amira@example.com', 22),
 (N'I keep everything inside', N'dana@example.com', 21),
 (N'I keep everything inside', N'rami@example.com', 19),
 (N'The 3pm slump is not laziness', N'dana@example.com', 15),
 (N'The 3pm slump is not laziness', N'omar@example.com', 14),
 (N'The 3pm slump is not laziness', N'rami@example.com', 8),
 (N'Anyone else wake up at 4am with a racing mind?', N'amira@example.com', 10),
 (N'Anyone else wake up at 4am with a racing mind?', N'marek@example.com', 9),
 (N'Anyone else wake up at 4am with a racing mind?', N'doctor@example.com', 7),
 (N'What I wish parents understood about exam pressure', N'rami@example.com', 5),
 (N'What I wish parents understood about exam pressure', N'amira@example.com', 4),
 (N'What I wish parents understood about exam pressure', N'sofia@example.com', 3),
 (N'Small wins: the walk I took before the call', N'doctor@example.com', 3),
 (N'Small wins: the walk I took before the call', N'dana@example.com', 2),
 (N'Small wins: the walk I took before the call', N'hana@example.com', 2),
 (N'Grounding techniques that actually stuck', N'sofia@example.com', 1),
 (N'Grounding techniques that actually stuck', N'dana@example.com', 1),
 (N'Grounding techniques that actually stuck', N'amira@example.com', 1);

INSERT INTO PostLikes (PostId, UserId, LikedOn)
SELECT po.Id, d.Id, DATEADD(HOUR, -1 * l.HoursAgo, @NowUtc)
FROM @Likes l
JOIN @Demo d ON d.Email = l.Email
JOIN Posts po ON po.Title = l.Title
WHERE NOT EXISTS (SELECT 1 FROM PostLikes x WHERE x.PostId = po.Id AND x.UserId = d.Id);

-- ----------------------------------------------------------------------------
-- 7. Chat rooms. Members before messages: each message stores an immutable
--    snapshot of the member row that sent it, so history keeps showing the
--    identity that was visible at the time even if membership changes later.
--    Visibility 0 = Public, 1 = Private. Role 0 = Member, 1 = Moderator, 2 = Owner.
-- ----------------------------------------------------------------------------
DECLARE @Rooms TABLE (Name nvarchar(100), Description nvarchar(500), Visibility int,
                      CreatorEmail nvarchar(256), IsArchived bit, DaysAgo int);
INSERT INTO @Rooms VALUES
 (N'Anxiety and panic - peer support',
  N'A quiet room for the days when the body says no. Support, not diagnosis. Please keep personal medical details out.',
  0, N'doctor@example.com', 0, 21),
 (N'Sleep and burnout',
  N'Night shifts, racing minds and the 3pm wall. Swap what has actually helped you.',
  0, N'omar@example.com', 0, 18),
 (N'New clinicians',
  N'Private room for practitioners in their first years of practice - supervision talk and hard weeks.',
  1, N'doctor@example.com', 0, 9),
 (N'Exam-season check-in',
  N'Archived room from the last exam term, kept as an example of a closed conversation.',
  0, N'hana@example.com', 1, 70);

INSERT INTO ChatRooms (Name, Description, Visibility, CreatedById, CreatedAtUtc, LastMessageAtUtc, IsArchived)
SELECT r.Name, r.Description, r.Visibility, d.Id,
       DATEADD(DAY, -1 * r.DaysAgo, @NowUtc), NULL, r.IsArchived
FROM @Rooms r
JOIN @Demo d ON d.Email = r.CreatorEmail
WHERE NOT EXISTS (SELECT 1 FROM ChatRooms x WHERE x.Name = r.Name);

DECLARE @Members TABLE (RoomName nvarchar(100), Email nvarchar(256), Role int, DaysAgo int);
INSERT INTO @Members VALUES
 (N'Anxiety and panic - peer support', N'doctor@example.com', 2, 21),
 (N'Anxiety and panic - peer support', N'dana@example.com', 1, 20),
 (N'Anxiety and panic - peer support', N'amira@example.com', 0, 16),
 (N'Anxiety and panic - peer support', N'rami@example.com', 0, 12),
 (N'Sleep and burnout', N'omar@example.com', 2, 18),
 (N'Sleep and burnout', N'sofia@example.com', 0, 15),
 (N'Sleep and burnout', N'marek@example.com', 0, 10),
 (N'New clinicians', N'doctor@example.com', 2, 9),
 (N'New clinicians', N'hana@example.com', 1, 8),
 (N'New clinicians', N'yusuf@example.com', 0, 7),
 (N'Exam-season check-in', N'hana@example.com', 2, 70),
 (N'Exam-season check-in', N'rami@example.com', 0, 68),
 (N'Exam-season check-in', N'sofia@example.com', 0, 66);

INSERT INTO ChatRoomMembers (RoomId, UserId, Role, DisplayName, IsDoctor, JoinedAtUtc, LeftAtUtc)
SELECT cr.Id, d.Id, m.Role, d.FirstName + N' ' + d.LastName,
       CASE WHEN dr.Id IS NULL THEN 0 ELSE 1 END,
       DATEADD(DAY, -1 * m.DaysAgo, @NowUtc), NULL
FROM @Members m
JOIN @Demo d ON d.Email = m.Email
JOIN ChatRooms cr ON cr.Name = m.RoomName
LEFT JOIN Doctors dr ON dr.Id = d.Id
WHERE NOT EXISTS (SELECT 1 FROM ChatRoomMembers x WHERE x.RoomId = cr.Id AND x.UserId = d.Id);

-- ClientMessageId is unique per sender, so every demo message carries a fixed
-- GUID and a re-run can never duplicate it.
DECLARE @Messages TABLE (RoomName nvarchar(100), SenderEmail nvarchar(256), Content nvarchar(2000),
                         ClientMessageId uniqueidentifier, HoursAgo int);
INSERT INTO @Messages VALUES
 (N'Anxiety and panic - peer support', N'amira@example.com',
  N'Bad night. Woke at four and did not get back under until six.', '00000000-0000-0000-0004-000000000001', 52),
 (N'Anxiety and panic - peer support', N'dana@example.com',
  N'Sorry to hear that. The four-thought spiral is real - I am in it most Tuesdays myself.', '00000000-0000-0000-0004-000000000002', 50),
 (N'Anxiety and panic - peer support', N'doctor@example.com',
  N'Before anything else: nobody here has to fix tonight. Naming it is enough.', '00000000-0000-0000-0004-000000000003', 48),
 (N'Anxiety and panic - peer support', N'rami@example.com',
  N'What helped me this month was writing the one open task on paper before bed. Not a system, just so my brain stops rehearsing it.', '00000000-0000-0000-0004-000000000004', 46),
 (N'Anxiety and panic - peer support', N'amira@example.com',
  N'Trying that tonight. Thank you.', '00000000-0000-0000-0004-000000000005', 44),
 (N'Anxiety and panic - peer support', N'dana@example.com',
  N'Mod note: this room is for support, not medication advice. If anyone is in danger, the report button is on every message.', '00000000-0000-0000-0004-000000000006', 40),
 (N'Sleep and burnout', N'sofia@example.com',
  N'Two nights on call back to back. I am running on tea and stubbornness.', '00000000-0000-0000-0004-000000000011', 33),
 (N'Sleep and burnout', N'marek@example.com',
  N'If you want a proper protocol I run paid workshops, ask me in DM.', '00000000-0000-0000-0004-000000000012', 31),
 (N'Sleep and burnout', N'omar@example.com',
  N'Please keep this room free of promotion.', '00000000-0000-0000-0004-000000000013', 30),
 (N'Sleep and burnout', N'sofia@example.com',
  N'Reporting the message above - that is advertising, not support.', '00000000-0000-0000-0004-000000000014', 29),
 (N'Sleep and burnout', N'sofia@example.com',
  N'Dropping off is fine too. I slept nine hours on Friday and woke up embarrassed about how much I needed it.', '00000000-0000-0000-0004-000000000015', 20),
 (N'Sleep and burnout', N'marek@example.com',
  N'Nine hours is not embarrassing, it is a receipt.', '00000000-0000-0000-0004-000000000016', 19),
 (N'New clinicians', N'hana@example.com',
  N'First supervision session today. I talked for forty minutes about one student and teared up a bit.', '00000000-0000-0000-0004-000000000021', 14),
 (N'New clinicians', N'yusuf@example.com',
  N'That is not a bad first session. It means you brought the real case rather than the safe one.', '00000000-0000-0000-0004-000000000022', 13),
 (N'New clinicians', N'doctor@example.com',
  N'Agreed. Bring it back next week unfinished rather than tidied up.', '00000000-0000-0000-0004-000000000023', 12),
 (N'New clinicians', N'hana@example.com',
  N'Thank you both. I will.', '00000000-0000-0000-0004-000000000024', 9),
 (N'Exam-season check-in', N'rami@example.com',
  N'Three papers in five days. Anyone else feel their hands shake before the first one?', '00000000-0000-0000-0004-000000000031', 200),
 (N'Exam-season check-in', N'hana@example.com',
  N'Plenty of people. Arrive early, one page of notes only, bring water. Then it is just a paper.', '00000000-0000-0000-0004-000000000032', 196),
 (N'Exam-season check-in', N'sofia@example.com',
  N'This room is going quiet but I read all of it. Good luck to everyone still in it.', '00000000-0000-0000-0004-000000000033', 190);

INSERT INTO ChatMessages (RoomId, SenderId, SenderMemberId, SenderDisplayName, SenderIsDoctor,
                          Content, ClientMessageId, SentAtUtc, IsDeleted, DeletedAtUtc, DeletedById)
SELECT cr.Id, d.Id, rm.Id, rm.DisplayName, rm.IsDoctor, msg.Content, msg.ClientMessageId,
       DATEADD(HOUR, -1 * msg.HoursAgo, @NowUtc), 0, NULL, NULL
FROM @Messages msg
JOIN @Demo d ON d.Email = msg.SenderEmail
JOIN ChatRooms cr ON cr.Name = msg.RoomName
JOIN ChatRoomMembers rm ON rm.RoomId = cr.Id AND rm.UserId = d.Id
WHERE NOT EXISTS (SELECT 1 FROM ChatMessages x
                  WHERE x.SenderId = d.Id AND x.ClientMessageId = msg.ClientMessageId);

UPDATE cm SET SentAtUtc = DATEADD(HOUR, -1 * msg.HoursAgo, @NowUtc)
FROM ChatMessages cm
JOIN @Messages msg ON msg.ClientMessageId = cm.ClientMessageId
WHERE cm.SentAtUtc <> DATEADD(HOUR, -1 * msg.HoursAgo, @NowUtc);

-- A room preview shows its newest message, so that pointer has to stay current.
UPDATE cr SET CreatedAtUtc = DATEADD(DAY, -1 * r.DaysAgo, @NowUtc),
              LastMessageAtUtc = (SELECT MAX(cm.SentAtUtc) FROM ChatMessages cm
                                  WHERE cm.RoomId = cr.Id AND cm.IsDeleted = 0)
FROM ChatRooms cr
JOIN @Rooms r ON r.Name = cr.Name;

-- One open report, so the moderation queue is not an empty table. The reported
-- message is promotional rather than harmful, which keeps the demo honest.
DECLARE @Reports TABLE (RoomName nvarchar(100), OffendingMessageId uniqueidentifier,
                        ReporterEmail nvarchar(256), Reason nvarchar(1000), HoursAgo int);
INSERT INTO @Reports VALUES
 (N'Sleep and burnout', '00000000-0000-0000-0004-000000000012', N'sofia@example.com',
  N'This reads as advertising rather than peer support.', 28);

INSERT INTO ChatMessageReports (MessageId, ReporterId, Reason, Status, CreatedAtUtc, ReviewedAtUtc, ReviewedById)
SELECT cm.Id, d.Id, rep.Reason, 0, DATEADD(HOUR, -1 * rep.HoursAgo, @NowUtc), NULL, NULL
FROM @Reports rep
JOIN ChatRooms cr ON cr.Name = rep.RoomName
JOIN ChatMessages cm ON cm.ClientMessageId = rep.OffendingMessageId AND cm.RoomId = cr.Id
JOIN @Demo d ON d.Email = rep.ReporterEmail
WHERE NOT EXISTS (SELECT 1 FROM ChatMessageReports x
                  WHERE x.MessageId = cm.Id AND x.ReporterId = d.Id);

-- ----------------------------------------------------------------------------
-- 8. Notifications. Type is the enum NAME, because the API parses it on read, and
--    Resources must be a valid JSON object - an empty string throws while the list
--    is being mapped. Wording and notifier name are built from the account that
--    performed the action, exactly as AppointmentService and the community
--    services do, and no clinical detail ever enters the payload.
-- ----------------------------------------------------------------------------
DECLARE @AppointmentAlerts TABLE (RecipientEmail nvarchar(256), ActorEmail nvarchar(256),
    Type nvarchar(50), Verb nvarchar(60), AppointmentReason nvarchar(1000),
    AppointmentUserEmail nvarchar(256), DoctorEmail nvarchar(256), HoursAgo int, IsRead bit);
INSERT INTO @AppointmentAlerts VALUES
 (N'doctor@example.com', N'marek@example.com', N'AppointmentRequest',      N'requested an appointment with you', N'Low mood and motivation lately',         N'marek@example.com', N'doctor@example.com', 26, 0),
 (N'doctor@example.com', N'dana@example.com',  N'AppointmentCancellation', N'cancelled the appointment',         N'Recent panic attacks at work',           N'dana@example.com',  N'doctor@example.com', 30, 1),
 (N'omar@example.com',   N'sofia@example.com', N'AppointmentRequest',      N'requested an appointment with you', N'Burnout recovery plan',                  N'sofia@example.com', N'omar@example.com',   18, 0),
 (N'hana@example.com',   N'rami@example.com',  N'AppointmentRequest',      N'requested an appointment with you', N'Exam-season anxiety',                    N'rami@example.com',  N'hana@example.com',    6, 0),
 (N'amira@example.com',  N'doctor@example.com', N'AppointmentConfirmation', N'confirmed the appointment',        N'Persistent anxiety and trouble sleeping', N'amira@example.com', N'doctor@example.com', 47, 1),
 (N'amira@example.com',  N'omar@example.com',  N'AppointmentConfirmation', N'confirmed the appointment',         N'Second view on stress management',       N'amira@example.com', N'omar@example.com',   41, 0),
 (N'rami@example.com',   N'doctor@example.com', N'AppointmentRejection',   N'rejected the appointment',          N'Difficulty switching off at night',      N'rami@example.com',  N'doctor@example.com', 34, 0),
 (N'rami@example.com',   N'salma@example.com', N'AppointmentCancellation', N'cancelled the appointment',         N'Questions about a sibling diagnosis',    N'rami@example.com',  N'salma@example.com',  72, 1);

INSERT INTO Notifications (AppUserId, Message, DateCreated, Type, Resources, NotifierUserName, NotifierPhotoUrl, IsRead)
SELECT rk.Id, ak.UserName + N' ' + a.Verb, DATEADD(HOUR, -1 * a.HoursAgo, @NowUtc), a.Type,
       CONCAT(N'{"appointmentId": ', ap.Id, N'}'), ak.UserName, NULL, a.IsRead
FROM @AppointmentAlerts a
JOIN @Demo rk ON rk.Email = a.RecipientEmail
JOIN @Demo ak ON ak.Email = a.ActorEmail
JOIN @Demo au ON au.Email = a.AppointmentUserEmail
JOIN @Demo du ON du.Email = a.DoctorEmail
JOIN Appointments ap ON ap.UserId = au.Id AND ap.DoctorId = du.Id AND ap.Reason = a.AppointmentReason
WHERE NOT EXISTS (SELECT 1 FROM Notifications n
                  WHERE n.AppUserId = rk.Id AND n.Type = a.Type
                    AND n.Message = ak.UserName + N' ' + a.Verb);

UPDATE n SET DateCreated = DATEADD(HOUR, -1 * a.HoursAgo, @NowUtc)
FROM Notifications n
JOIN @AppointmentAlerts a ON a.Type = n.Type
JOIN @Demo rk ON rk.Email = a.RecipientEmail AND n.AppUserId = rk.Id
JOIN @Demo ak ON ak.Email = a.ActorEmail
             AND n.Message = ak.UserName + N' ' + a.Verb
WHERE n.DateCreated <> DATEADD(HOUR, -1 * a.HoursAgo, @NowUtc);

-- Each row names the comment it refers to, so a link is only built when that
-- comment really exists.
DECLARE @CommunityAlerts TABLE (RecipientEmail nvarchar(256), NotifierEmail nvarchar(256),
    Type nvarchar(50), Title nvarchar(2000), CommentAuthorEmail nvarchar(256), HoursAgo int, IsRead bit);
INSERT INTO @CommunityAlerts VALUES
 (N'amira@example.com', N'doctor@example.com', N'Comment', N'One month of therapy, what actually changed',         N'doctor@example.com', 49, 0),
 (N'doctor@example.com', N'amira@example.com',  N'Comment', N'How to prepare for your first counseling session',   N'amira@example.com',  44, 0),
 (N'doctor@example.com', N'sofia@example.com',  N'Comment', N'How to prepare for your first counseling session',   N'sofia@example.com',  38, 1),
 (N'marek@example.com',  N'dana@example.com',   N'Comment', N'I keep everything inside',                           N'dana@example.com',   25, 0),
 (N'sofia@example.com',  N'omar@example.com',   N'Comment', N'The 3pm slump is not laziness',                      N'omar@example.com',   17, 0),
 (N'dana@example.com',   N'doctor@example.com', N'Comment', N'Anyone else wake up at 4am with a racing mind?',     N'doctor@example.com',  8, 1),
 (N'hana@example.com',   N'marek@example.com',  N'Comment', N'What I wish parents understood about exam pressure', N'marek@example.com',   6, 0),
 (N'rami@example.com',   N'hana@example.com',   N'Comment', N'Small wins: the walk I took before the call',        N'hana@example.com',    2, 0),
 (N'omar@example.com',   N'sofia@example.com',  N'Comment', N'Grounding techniques that actually stuck',           N'sofia@example.com',   1, 0),
 (N'doctor@example.com', N'marek@example.com',  N'Reply',   N'One month of therapy, what actually changed',        N'doctor@example.com', 20, 0),
 (N'doctor@example.com', N'amira@example.com',  N'Reply',   N'One month of therapy, what actually changed',        N'doctor@example.com', 14, 1),
 (N'amira@example.com',  N'doctor@example.com', N'Reply',   N'How to prepare for your first counseling session',   N'amira@example.com',  40, 0),
 (N'dana@example.com',   N'marek@example.com',  N'Reply',   N'I keep everything inside',                           N'dana@example.com',   23, 0);

INSERT INTO Notifications (AppUserId, Message, DateCreated, Type, Resources, NotifierUserName, NotifierPhotoUrl, IsRead)
SELECT rk.Id,
       nk.UserName + CASE WHEN a.Type = N'Reply' THEN N' has replied to your comment'
                         ELSE N' has commented on your post' END,
       DATEADD(HOUR, -1 * a.HoursAgo, @NowUtc), a.Type,
       CONCAT(N'{"postId": ', po.Id, N', "commentId": ', cm.Id, N'}'),
       nk.UserName, NULL, a.IsRead
FROM @CommunityAlerts a
JOIN @Demo rk ON rk.Email = a.RecipientEmail
JOIN @Demo nk ON nk.Email = a.NotifierEmail
JOIN @Demo ck ON ck.Email = a.CommentAuthorEmail
JOIN Posts po ON po.Title = a.Title
JOIN Comments cm ON cm.PostId = po.Id AND cm.AppUserId = ck.Id
WHERE NOT EXISTS (SELECT 1 FROM Notifications n
                  WHERE n.AppUserId = rk.Id AND n.Type = a.Type
                    AND n.Resources = CONCAT(N'{"postId": ', po.Id, N', "commentId": ', cm.Id, N'}'));

COMMIT TRANSACTION;

-- ----------------------------------------------------------------------------
-- 9. Summary. A presenter should see every appointment status represented, the
--    upcoming rows inside the coming week, and a message count per room.
-- ----------------------------------------------------------------------------
SELECT 'AspNetUsers' AS TableName, COUNT(*) AS RowsCount FROM AspNetUsers
UNION ALL SELECT 'Users', COUNT(*) FROM Users
UNION ALL SELECT 'Doctors', COUNT(*) FROM Doctors
UNION ALL SELECT 'Admins', COUNT(*) FROM Admins
UNION ALL SELECT 'AspNetUserRoles', COUNT(*) FROM AspNetUserRoles
UNION ALL SELECT 'UserQualifications', COUNT(*) FROM UserQualifications
UNION ALL SELECT 'UserCertificates', COUNT(*) FROM UserCertificates
UNION ALL SELECT 'DoctorSchedulingSettings', COUNT(*) FROM DoctorSchedulingSettings
UNION ALL SELECT 'DoctorAvailabilityPeriods', COUNT(*) FROM DoctorAvailabilityPeriods
UNION ALL SELECT 'DoctorTimeOff', COUNT(*) FROM DoctorTimeOff
UNION ALL SELECT 'Appointments', COUNT(*) FROM Appointments
UNION ALL SELECT 'Posts', COUNT(*) FROM Posts
UNION ALL SELECT 'Comments', COUNT(*) FROM Comments
UNION ALL SELECT 'Replies', COUNT(*) FROM Replies
UNION ALL SELECT 'PostLikes', COUNT(*) FROM PostLikes
UNION ALL SELECT 'ChatRooms', COUNT(*) FROM ChatRooms
UNION ALL SELECT 'ChatRoomMembers', COUNT(*) FROM ChatRoomMembers
UNION ALL SELECT 'ChatMessages', COUNT(*) FROM ChatMessages
UNION ALL SELECT 'ChatMessageReports', COUNT(*) FROM ChatMessageReports
UNION ALL SELECT 'Notifications', COUNT(*) FROM Notifications;

SELECT a.Status AS AppointmentStatus, COUNT(*) AS RowsCount,
       MIN(a.StartTime) AS Earliest, MAX(a.StartTime) AS Latest
FROM Appointments a
JOIN @Demo d ON d.Id = a.UserId
GROUP BY a.Status
ORDER BY a.Status;

SELECT cr.Name AS ChatRoom, cr.Visibility, cr.IsArchived,
       COUNT(cm.Id) AS Messages, cr.LastMessageAtUtc
FROM ChatRooms cr
JOIN @Rooms r ON r.Name = cr.Name
LEFT JOIN ChatMessages cm ON cm.RoomId = cr.Id
GROUP BY cr.Name, cr.Visibility, cr.IsArchived, cr.LastMessageAtUtc
ORDER BY cr.Name;

PRINT 'Seed completed successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Seed failed: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
