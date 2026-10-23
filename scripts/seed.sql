-- ============================================================================
-- Seed script for the Nexus API database.
--
-- The application only applies EF Core migrations at startup (see
-- Infrastructure/Data/DbInitializer.cs) - no data is seeded from the app.
-- Run this script once after the migrations, from the MonsterASP Control
-- Panel (WebMSSQL) or SQL Server Management Studio.
--
-- The script is idempotent: every insert is guarded with NOT EXISTS, so it
-- is safe to run more than once and against a database that already has
-- data.
--
-- Seeded accounts (all share the password "Password1!"):
--   doctor@example.com   Doctor  - Dr. Nadia Rahman, counseling psychologist,
--                                  available Mon-Fri 09:30-17:00 (45 min slots)
--   amira@example.com    User    - patient, has posts/appointments
--   marek@example.com    User    - patient, anonymous posts + appointment request
--   sofia@example.com    User    - patient, past completed appointment
--   admin@example.com    Admin   - portal administrator
--
-- Password hashes were produced with the exact PasswordHasher settings the
-- app uses (defaults: PBKDF2 HMAC-SHA256, 100,000 iterations), so every
-- account logs in through the normal authentication flow immediately.
-- Change the passwords by updating the users afterwards.
--
-- Seeded content: doctor weekly schedule, appointments (confirmed, pending,
-- completed), posts (including one anonymous confession), comments, replies,
-- post likes, and notifications.
-- ============================================================================

SET NOCOUNT ON;
BEGIN TRY
BEGIN TRANSACTION;

-- ----------------------------------------------------------------------------
-- 1. Roles (User / Doctor / Admin) - same block as the original seed script.
-- ----------------------------------------------------------------------------
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT NEWID(), 'User', 'USER', REPLACE(NEWID(), '-', '')
WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'User');

INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT NEWID(), 'Doctor', 'DOCTOR', REPLACE(NEWID(), '-', '')
WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Doctor');

INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT NEWID(), 'Admin', 'ADMIN', REPLACE(NEWID(), '-', '')
WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin');

-- ----------------------------------------------------------------------------
-- 2. Identity accounts. TPT hierarchy: AspNetUsers carries the identity and
--    profile columns; Users / Doctors / Admins carry the role-specific data
--    and join on the same Id.
-- ----------------------------------------------------------------------------
DECLARE @DoctorId  nvarchar(450) = N'00000000-0000-0000-0000-000000000001';
DECLARE @AmiraId   nvarchar(450) = N'00000000-0000-0000-0000-000000000002';
DECLARE @MarekId   nvarchar(450) = N'00000000-0000-0000-0000-000000000003';
DECLARE @SofiaId   nvarchar(450) = N'00000000-0000-0000-0000-000000000004';
DECLARE @AdminId   nvarchar(450) = N'00000000-0000-0000-0000-000000000005';

-- Doctor: doctor@example.com / Password1!
INSERT INTO AspNetUsers
    (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
     PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
     TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName,
     BirthDate, Gender, Biography, City, PhotoUrl, IsDeleted)
SELECT
    @DoctorId, N'doctor', N'DOCTOR', N'doctor@example.com', N'DOCTOR@EXAMPLE.COM', 1,
    N'AQAAAAIAAYagAAAAEAmYYhPy+wh2cXBO5oSGznOEtX1OQY2/scV111VZw8RJF+eatqTfZTkOlpS6YGmZxw==',
    N'00000000-0000-0000-0000-000000000001', N'00000000-0000-0000-0000-000000000001', NULL, 0,
    0, 0, 0, N'Nadia', N'Rahman', '1988-04-17', N'Female',
    N'Counseling psychologist focused on anxiety, burnout and young-adult mental health. '
    + N'I believe therapy should feel like a conversation, not an evaluation.',
    N'Cairo', NULL, 0
WHERE NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @DoctorId);

INSERT INTO Doctors (Id, Specialization, Location, SessionFees, IsAvailable, Qualifications)
SELECT @DoctorId, 1, N'Nasr City Clinic, Cairo', 150.00, 1,
       N'MSc Clinical Psychology; certified CBT practitioner; 6+ years private practice'
WHERE NOT EXISTS (SELECT 1 FROM Doctors WHERE Id = @DoctorId);

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT @DoctorId, r.Id
FROM AspNetRoles r
WHERE r.Name = 'Doctor'
  AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @DoctorId AND RoleId = r.Id);

-- Patient: amira@example.com / Password1!
INSERT INTO AspNetUsers
    (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
     PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
     TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName,
     BirthDate, Gender, Biography, City, PhotoUrl, IsDeleted)
SELECT
    @AmiraId, N'amira', N'AMIRA', N'amira@example.com', N'AMIRA@EXAMPLE.COM', 1,
    N'AQAAAAIAAYagAAAAEDFVO5ypxTwLo9buI4t3VWngJzkgSp8wLCNvFD028vdHPlIbP6oVL1vqp2FdJxoCKQ==',
    N'00000000-0000-0000-0000-000000000002', N'00000000-0000-0000-0000-000000000002', NULL, 0,
    0, 0, 0, N'Amira', N'Khalil', '1999-11-30', N'Female',
    N'Design student, better sleep and journaling - a work in progress.',
    N'Cairo', NULL, 0
WHERE NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @AmiraId);

INSERT INTO Users (Id) SELECT @AmiraId WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Id = @AmiraId);
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT @AmiraId, r.Id
FROM AspNetRoles r
WHERE r.Name = 'User'
  AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @AmiraId AND RoleId = r.Id);

-- Patient: marek@example.com / Password1!
INSERT INTO AspNetUsers
    (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
     PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
     TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName,
     BirthDate, Gender, Biography, City, PhotoUrl, IsDeleted)
SELECT
    @MarekId, N'marek', N'MAREK', N'marek@example.com', N'MAREK@EXAMPLE.COM', 1,
    N'AQAAAAIAAYagAAAAEKnJHj82MB77VKKjlMSfB74dBn9YRDy/PL90eXoB600iW/BF52s/bulzbyNZvHhBHQ==',
    N'00000000-0000-0000-0000-000000000003', N'00000000-0000-0000-0000-000000000003', NULL, 0,
    0, 0, 0, N'Marek', N'Haddad', '1994-06-08', N'Male', NULL, N'Alexandria', NULL, 0
WHERE NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @MarekId);

INSERT INTO Users (Id) SELECT @MarekId WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Id = @MarekId);
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT @MarekId, r.Id
FROM AspNetRoles r
WHERE r.Name = 'User'
  AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @MarekId AND RoleId = r.Id);

-- Patient: sofia@example.com / Password1!
INSERT INTO AspNetUsers
    (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
     PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
     TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName,
     BirthDate, Gender, Biography, City, PhotoUrl, IsDeleted)
SELECT
    @SofiaId, N'sofia', N'SOFIA', N'sofia@example.com', N'SOFIA@EXAMPLE.COM', 1,
    N'AQAAAAIAAYagAAAAEIQQbUr5fkIGg7Ga7wX7esgCsLn99t12AUiXZD5xU+OpfvbwfRcpDF//S92Ji+WQwg==',
    N'00000000-0000-0000-0000-000000000004', N'00000000-0000-0000-0000-000000000004', NULL, 0,
    0, 0, 0, N'Sofia', N'Nagy', '1996-02-21', N'Female', NULL, N'Giza', NULL, 0
WHERE NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @SofiaId);

INSERT INTO Users (Id) SELECT @SofiaId WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Id = @SofiaId);
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT @SofiaId, r.Id
FROM AspNetRoles r
WHERE r.Name = 'User'
  AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @SofiaId AND RoleId = r.Id);

-- Administrator: admin@example.com / Password1!
INSERT INTO AspNetUsers
    (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
     PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
     TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FirstName, LastName,
     BirthDate, Gender, Biography, City, PhotoUrl, IsDeleted)
SELECT
    @AdminId, N'admin', N'ADMIN', N'admin@example.com', N'ADMIN@EXAMPLE.COM', 1,
    N'AQAAAAIAAYagAAAAEHEjVmCLuTc6AatBl774gLHaKZ9h1L7rP8QO80a9sOmTyyhkMD01BnM9k8Z6SeSdIA==',
    N'00000000-0000-0000-0000-000000000005', N'00000000-0000-0000-0000-000000000005', NULL, 0,
    0, 0, 0, N'Portal', N'Admin', '1990-01-01', N'Female', NULL, NULL, NULL, 0
WHERE NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @AdminId);

INSERT INTO Admins (Id) SELECT @AdminId WHERE NOT EXISTS (SELECT 1 FROM Admins WHERE Id = @AdminId);
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT @AdminId, r.Id
FROM AspNetRoles r
WHERE r.Name = 'Admin'
  AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @AdminId AND RoleId = r.Id);

-- ----------------------------------------------------------------------------
-- 3. Doctor weekly schedule (key: DoctorId + DayOfWeek;
--    DayOfWeek: 0=Sunday .. 6=Saturday). Monday-Friday, 09:30-17:00, 45 min.
-- ----------------------------------------------------------------------------
INSERT INTO ScheduleWeekDays (DoctorId, DayOfWeek, StartTime, SessionDuration, EndTime)
SELECT @DoctorId, v.DayOfWeek, '09:30:00', '00:45:00', '17:00:00'
FROM (VALUES (1), (2), (3), (4), (5)) v (DayOfWeek)
WHERE NOT EXISTS (
    SELECT 1 FROM ScheduleWeekDays
    WHERE DoctorId = @DoctorId AND DayOfWeek = v.DayOfWeek
);

-- ----------------------------------------------------------------------------
-- 4. Appointments. Status: 0=Pending, 1=Confirmed, 2=Completed,
--    3=Cancelled, 4=Rejected. EndTime is a computed column - not inserted.
-- ----------------------------------------------------------------------------
INSERT INTO Appointments (UserId, DoctorId, StartTime, Duration, Status, Location, Reason, CancellationReason, RejectionReason, Fees)
SELECT @AmiraId, @DoctorId, '2026-11-03 10:00:00', '00:45:00', 1, N'Nasr City Clinic, Cairo',
       N'Persistent anxiety and trouble sleeping', NULL, NULL, 150.00
WHERE NOT EXISTS (
    SELECT 1 FROM Appointments WHERE UserId = @AmiraId AND DoctorId = @DoctorId
);

INSERT INTO Appointments (UserId, DoctorId, StartTime, Duration, Status, Location, Reason, CancellationReason, RejectionReason, Fees)
SELECT @MarekId, @DoctorId, '2026-11-05 14:00:00', '00:45:00', 0, N'Nasr City Clinic, Cairo',
       N'Low mood and motivation lately', NULL, NULL, 150.00
WHERE NOT EXISTS (
    SELECT 1 FROM Appointments WHERE UserId = @MarekId AND DoctorId = @DoctorId
);

INSERT INTO Appointments (UserId, DoctorId, StartTime, Duration, Status, Location, Reason, CancellationReason, RejectionReason, Fees)
SELECT @SofiaId, @DoctorId, '2026-10-05 11:00:00', '00:45:00', 2, N'Nasr City Clinic, Cairo',
       N'Work-related stress', NULL, NULL, 150.00
WHERE NOT EXISTS (
    SELECT 1 FROM Appointments WHERE UserId = @SofiaId AND DoctorId = @DoctorId
);

-- ----------------------------------------------------------------------------
-- 5. Posts. IsAnonymous=1 hides the author in the community feed; the content
--    is still attributed to the user account.
-- ----------------------------------------------------------------------------
DECLARE @PostGuidanceId int,
        @PostJourneyId int,
        @PostConfessionId int;

INSERT INTO Posts (AppUserId, Title, Content, Category, Tags, IsAnonymous, PostedOn, PostPhotoUrl)
SELECT @DoctorId, N'How to prepare for your first counseling session',
       N'You do not need a perfect script - just bring the moment that pushed you to book, '
       + N'the nights that felt hardest, and one small question you keep carrying. '
       + N'Everything else we build together, at your pace.',
       N'Guidance', N'counseling,tips,first-session', 0, DATEADD(DAY, -3, SYSUTCDATETIME()), NULL
WHERE NOT EXISTS (SELECT 1 FROM Posts WHERE Title = N'How to prepare for your first counseling session');
SET @PostGuidanceId = (SELECT Id FROM Posts WHERE Title = N'How to prepare for your first counseling session');

INSERT INTO Posts (AppUserId, Title, Content, Category, Tags, IsAnonymous, PostedOn, PostPhotoUrl)
SELECT @AmiraId, N'One month of therapy, what actually changed',
       N'First session was frightening and the second was boring - turns out that is normal. '
       + N'What changed: I sleep before 2am, I stopped rereading every message five times, '
       + N'and I have actual scripts for the moments that used to spiral. Progress is quieter than I expected.',
       N'Journey', N'therapy,experience,progress', 0, DATEADD(DAY, -2, SYSUTCDATETIME()), NULL
WHERE NOT EXISTS (SELECT 1 FROM Posts WHERE Title = N'One month of therapy, what actually changed');
SET @PostJourneyId = (SELECT Id FROM Posts WHERE Title = N'One month of therapy, what actually changed');

INSERT INTO Posts (AppUserId, Title, Content, Category, Tags, IsAnonymous, PostedOn, PostPhotoUrl)
SELECT @MarekId, N'I keep everything inside',
       N'To everyone who thinks feeling nothing for months is normal: I thought so too, '
       + N'until I realized I could not remember my last honest conversation. '
       + N'Posting here anonymously because this is the only place I say it out loud.',
       N'Confession', N'anxiety,support', 1, DATEADD(DAY, -1, SYSUTCDATETIME()), NULL
WHERE NOT EXISTS (SELECT 1 FROM Posts WHERE Title = N'I keep everything inside');
SET @PostConfessionId = (SELECT Id FROM Posts WHERE Title = N'I keep everything inside');

-- ----------------------------------------------------------------------------
-- 6. Comments and replies.
-- ----------------------------------------------------------------------------
DECLARE @CommentAmiraId int, @CommentDoctorId int;

INSERT INTO Comments (PostId, AppUserId, Content, CommentedAt)
SELECT @PostJourneyId, @DoctorId,
       N'Thank you for trusting us with your story. The quiet wins you listed - earlier sleep, '
       + N'fewer rereads - are exactly the signs worth noticing. Keep going!',
       DATEADD(HOUR, -20, SYSUTCDATETIME())
WHERE NOT EXISTS (SELECT 1 FROM Comments WHERE PostId = @PostJourneyId AND AppUserId = @DoctorId);
SET @CommentDoctorId = (SELECT Id FROM Comments WHERE PostId = @PostJourneyId AND AppUserId = @DoctorId);

INSERT INTO Comments (PostId, AppUserId, Content, CommentedAt)
SELECT @PostGuidanceId, @AmiraId,
       N'This helped so much - the "no perfect script" part took the pressure off. Booking my first call this week!',
       DATEADD(HOUR, -16, SYSUTCDATETIME())
WHERE NOT EXISTS (SELECT 1 FROM Comments WHERE PostId = @PostGuidanceId AND AppUserId = @AmiraId);
SET @CommentAmiraId = (SELECT Id FROM Comments WHERE PostId = @PostGuidanceId AND AppUserId = @AmiraId);

INSERT INTO Replies (CommentId, AppUserId, Content, RepliedAt)
SELECT @CommentDoctorId, @MarekId,
       N'Reading this while working up the courage to book anything. The boring second session line genuinely helps.',
       DATEADD(HOUR, -10, SYSUTCDATETIME())
WHERE NOT EXISTS (SELECT 1 FROM Replies WHERE CommentId = @CommentDoctorId AND AppUserId = @MarekId);

-- ----------------------------------------------------------------------------
-- 7. Post likes (unique: one per user per post).
-- ----------------------------------------------------------------------------
INSERT INTO PostLikes (PostId, UserId, LikedOn)
SELECT @PostJourneyId, @MarekId, DATEADD(HOUR, -22, SYSUTCDATETIME())
WHERE NOT EXISTS (SELECT 1 FROM PostLikes WHERE PostId = @PostJourneyId AND UserId = @MarekId);

INSERT INTO PostLikes (PostId, UserId, LikedOn)
SELECT @PostGuidanceId, @MarekId, DATEADD(HOUR, -9, SYSUTCDATETIME())
WHERE NOT EXISTS (SELECT 1 FROM PostLikes WHERE PostId = @PostGuidanceId AND UserId = @MarekId);

INSERT INTO PostLikes (PostId, UserId, LikedOn)
SELECT @PostGuidanceId, @SofiaId, DATEADD(HOUR, -8, SYSUTCDATETIME())
WHERE NOT EXISTS (SELECT 1 FROM PostLikes WHERE PostId = @PostGuidanceId AND UserId = @SofiaId);

-- ----------------------------------------------------------------------------
-- 8. Notifications. Type is the enum name (Comment, Reply, AppointmentRequest,
--    AppointmentRejection, AppointmentConfirmation, AppointmentCancellation) and
--    Resources is a JSON object with camelCase int keys, matching how
--    NotificationSender / services create them.
-- ----------------------------------------------------------------------------
INSERT INTO Notifications (AppUserId, Message, DateCreated, Type, Resources, NotifierUserName, NotifierPhotoUrl, IsRead)
SELECT @DoctorId, N'amira has requested a new appointment', DATEADD(HOUR, -26, SYSUTCDATETIME()),
       N'AppointmentRequest',
       CONCAT('{"appointmentId": ', (SELECT Id FROM Appointments WHERE UserId = @AmiraId AND DoctorId = @DoctorId AND Status = 1), '}'),
       N'amira', NULL, 0
WHERE NOT EXISTS (
    SELECT 1 FROM Notifications WHERE AppUserId = @DoctorId AND Type = N'AppointmentRequest'
);

INSERT INTO Notifications (AppUserId, Message, DateCreated, Type, Resources, NotifierUserName, NotifierPhotoUrl, IsRead)
SELECT @AmiraId, N'doctor has commented on your post', DATEADD(HOUR, -19, SYSUTCDATETIME()),
       N'Comment',
       CONCAT('{"postId": ', @PostJourneyId, ', "commentId": ', @CommentDoctorId, '}'),
       N'doctor', NULL, 0
WHERE NOT EXISTS (
    SELECT 1 FROM Notifications WHERE AppUserId = @AmiraId AND Type = N'Comment'
);

COMMIT TRANSACTION;

-- ----------------------------------------------------------------------------
-- Summary (prints after success).
-- ----------------------------------------------------------------------------
SELECT 'AspNetUsers' AS TableName, COUNT(*) AS RowsCount FROM AspNetUsers
UNION ALL SELECT 'Users', COUNT(*) FROM Users
UNION ALL SELECT 'Doctors', COUNT(*) FROM Doctors
UNION ALL SELECT 'Admins', COUNT(*) FROM Admins
UNION ALL SELECT 'AspNetUserRoles', COUNT(*) FROM AspNetUserRoles
UNION ALL SELECT 'ScheduleWeekDays', COUNT(*) FROM ScheduleWeekDays
UNION ALL SELECT 'Appointments', COUNT(*) FROM Appointments
UNION ALL SELECT 'Posts', COUNT(*) FROM Posts
UNION ALL SELECT 'Comments', COUNT(*) FROM Comments
UNION ALL SELECT 'Replies', COUNT(*) FROM Replies
UNION ALL SELECT 'PostLikes', COUNT(*) FROM PostLikes
UNION ALL SELECT 'Notifications', COUNT(*) FROM Notifications;
PRINT 'Seed completed successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Seed failed: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
