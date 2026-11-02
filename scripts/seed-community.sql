/* ============================================================================
   seed-community.sql — additive enrichment of the Nexus demo community.

   Adds posts (several with photo attachments), comments, replies, likes,
   chat messages with media, room cover images and doctor ratings. Every
   insert is guarded by a natural key, so re-running changes nothing.

   Unlike seed.sql this script never creates accounts: it resolves the
   existing demo accounts by email, which also makes it safe against the
   hosted database holding real (non-@example.com) accounts.

   Run shape (mirrors scripts/seed.sql guards):
     1. @Confirm must be set to 'ENRICH_NEXUS_COMMUNITY' or the script refuses.
     2. The database name must be on the allowlist.
     3. @DryRun = 1 (default) rolls back and prints would-be counts.
     4. Requires the DoctorRatings table (feature deployed 2026-11-02).

   Media URLs are Cloudinary assets uploaded to the project account
   (folder posts/) under the same CLOUDINARY_URL the app uses.
   ========================================================================= */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Confirm nvarchar(40) = N'';          -- set to ENRICH_NEXUS_COMMUNITY to arm
DECLARE @DryRun  bit = 1;                      -- 1 = validate + roll back

IF @Confirm <> N'ENRICH_NEXUS_COMMUNITY'
BEGIN
    RAISERROR(N'Seed refused: set @Confirm to ENRICH_NEXUS_COMMUNITY first.', 16, 1);
    RETURN;
END

IF DB_NAME() NOT IN (N'db75421', N'MentalMediator', N'nexus_demo')
BEGIN
    DECLARE @db nvarchar(128) = DB_NAME();
    RAISERROR(N'Seed refused: database %s is not on the demo allowlist.', 16, 1, @db);
    RETURN;
END

IF OBJECT_ID(N'dbo.DoctorRatings') IS NULL
BEGIN
    RAISERROR(N'Seed refused: DoctorRatings table missing — deploy the ratings migration first.', 16, 1);
    RETURN;
END

DECLARE @NowUtc datetime2 = SYSUTCDATETIME();

DECLARE @Demo TABLE (
    Id nvarchar(450) PRIMARY KEY,
    Email nvarchar(256) UNIQUE,
    FirstName nvarchar(100),
    LastName nvarchar(100),
    IsDoctor bit);

INSERT INTO @Demo (Id, Email, FirstName, LastName, IsDoctor)
SELECT u.Id, u.Email, u.FirstName, u.LastName,
       CASE WHEN d.Id IS NULL THEN 0 ELSE 1 END
FROM AspNetUsers u
LEFT JOIN Doctors d ON d.Id = u.Id
WHERE u.Email LIKE N'%@example.com';

BEGIN TRY
BEGIN TRANSACTION;

/* ---------------------------------------------------------------------------
   1. Posts. Photo attachments use the app's MediaJson shape:
      [{"url":"https://res.cloudinary.com/...","type":"image"}]
   --------------------------------------------------------------------------- */
DECLARE @Posts TABLE (
    AuthorEmail nvarchar(256), Title nvarchar(2000), Content nvarchar(max),
    Category nvarchar(100), Tags nvarchar(1000), IsAnonymous bit, HoursAgo int,
    MediaUrl nvarchar(1000));

INSERT INTO @Posts VALUES
 (N'rami@example.com', N'The walk-and-talk rule my partner and I invented',
  N'Hard conversations happen on foot, side by side, never across a table. We got the idea from a therapist and it has defused three weeks in a row. If your person argues better face to face, try the sidewalk version.',
  N'Journey', N'relationships,communication,walking', 0, 96,
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654618/posts/231b53dcc1ac4df9b3c752099bf1e025.jpg.jpg'),
 (N'amira@example.com', N'Study buddies beat study apps (for me anyway)',
  N'I downloaded every focus app and ignored all of them. What worked was one classmate sitting two tables away, both of us too polite to scroll our phones. Attachment is the feature I kept deleting from my life.',
  N'Support', N'students,accountability,focus', 0, 88,
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654616/posts/fe893d381faa4f53a7730065f26fe223.jpg.jpg'),
 (N'sofia@example.com', N'What I write down when my head gets loud',
  N'Three lines only: what happened, what I felt, what I did. Not a journal, a receipt. The noise argues with evidence and loses most of the time.',
  N'Coping', N'journaling,anxiety,routine', 0, 77,
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654620/posts/4a97ffab3e344a33800f375374e06d95.jpg.jpg'),
 (N'marek@example.com', N'Two guys, one bench, no small talk',
  N'My father and I have never done talking face to face. But forty minutes on a sea wall, shoulders pointed at the horizon, and he told me about 1998. Some conversations need a direction, not an audience.',
  N'Journey', N'family,men,conversation', 0, 63,
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654622/posts/214f1017354b43a5907edfc9a3086b87.jpg.jpg'),
 (N'dana@example.com', N'We went to couples counseling before the crack got wide',
  N'Everyone said we were dramatic for going when things were merely hard. Things were merely hard for two more years and then they were not. I would rather be early and embarrassed than late and precise about the damage.',
  N'Guidance', N'relationships,counseling,early', 0, 55,
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654623/posts/1fd2fc3bd2e14d32bdad259849e96367.jpg.jpg'),
 (N'omar@example.com', N'Therapy during residency: the 20-minute version',
  N'Between wards I get twenty minutes, not fifty. My therapist built sessions around that reality. If you are in a schedule like mine, ask for the short form instead of skipping entirely.',
  N'Questions', N'healthcare-workers,busy,sessions', 0, 41,
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654625/posts/6a7c6cfe74f04aafaafc5589cbef15b3.jpg.jpg'),
 (N'amira@example.com', N'The letter I wrote and never sent',
  N'Two pages of everything I could not say out loud, folded into a drawer. I did not write it to be read. I wrote it so the sentences would exist somewhere other than my chest at 3am.',
  N'Confession', N'grief,writing,privacy', 1, 34,
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654627/posts/b3bfa1cd2e8249239c0132711bb0458d.jpg.jpg'),
 (N'hana@example.com', N'Exam season survival kit, crowdsourced',
  N'From every previous thread: water, one page of notes not ten, arrive early, eat before not after, and one person outside the hall who asks about the walk home instead of the paper.',
  N'Guidance', N'students,exams,practical', 0, 26,
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654629/posts/1249c8f3753741b9be8385220a22e554.jpg.jpg'),
 (N'doctor@example.com', N'Our peer circle met in person last week',
  N'Ten of us, two hours, one park pavilion. The rule held: no diagnosing each other, no advice unless asked. People laughed more than I expected. Same time next month if the weather behaves.',
  N'Support', N'community,meetup,peer-support', 0, 18,
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654614/posts/9eddac69429847aa808645ad38878d17.jpg.jpg'),
 (N'yusuf@example.com', N'Forest bathing is not a joke, says my cortisol',
  N'I made fun of the term for a year. Then I sat by the stream for an hour a week for six weeks and my resting heart rate moved somewhere it had not been since before the night shifts. The trees get the credit they are owed.',
  N'Coping', N'nature,walks,calm', 0, 12,
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654606/posts/f8865963a4e6424ab3499c7e84195256.jpg.jpg'),
 (N'sofia@example.com', N'Night shift fuel: what I actually eat at 2am',
  N'Not the salad people post. Salt, protein, and something warm in a mug because the building is cold and the vending machine is a lie. Curious what the other night people keep in their drawer.',
  N'Coping', N'shiftwork,sleep,food', 0, 7, NULL),
 (N'amira@example.com', N'How do you say "I need help" out loud?',
  N'I have said it to a doctor, to my mother, in this forum. The first one took eleven days of standing in the clinic doorway. What made me finally walk in? A text from a friend asking a different question than I expected.',
  N'Questions', N'anxiety,asking,community', 0, 3, NULL);

INSERT INTO Posts (AppUserId, Title, Content, Category, Tags, IsAnonymous, PostedOn,
                   PostPhotoUrl, MediaJson, Visibility, IsDraft)
SELECT d.Id, p.Title, p.Content, p.Category, p.Tags, p.IsAnonymous,
       DATEADD(HOUR, -1 * p.HoursAgo, @NowUtc),
       p.MediaUrl,
       CASE WHEN p.MediaUrl IS NULL THEN NULL
            ELSE N'[{"url":"' + p.MediaUrl + N'","type":"image"}]' END,
       0, 0
FROM @Posts p
JOIN @Demo d ON d.Email = p.AuthorEmail
WHERE NOT EXISTS (SELECT 1 FROM Posts x WHERE x.Title = p.Title);

/* ---------------------------------------------------------------------------
   2. Comments and replies on new and existing posts.
   --------------------------------------------------------------------------- */
DECLARE @Comments TABLE (Title nvarchar(2000), AuthorEmail nvarchar(256), Content nvarchar(4000), HoursAgo int);
INSERT INTO @Comments VALUES
 (N'The walk-and-talk rule my partner and I invented', N'doctor@example.com',
  N'This is a real pattern, not a trick. Side-by-side lowers the eye-contact cost of hard sentences. Thank you for naming it.', 90),
 (N'Study buddies beat study apps (for me anyway)', N'hana@example.com',
  N'The politeness thing is exactly it. I pay for a coworking desk for the same reason.', 80),
 (N'What I write down when my head gets loud', N'rami@example.com',
  N'Receipt is the perfect word for it. Started doing the same after reading your last post.', 70),
 (N'Two guys, one bench, no small talk', N'yusuf@example.com',
  N'Reading this on the sea wall, ironically. My dad and I are two benches down from this.', 58),
 (N'We went to couples counseling before the crack got wide', N'sofia@example.com',
  N'Early and embarrassed beats late and precise. Saving this phrasing.', 48),
 (N'Therapy during residency: the 20-minute version', N'doctor@example.com',
  N'Several colleagues do compressed sessions now. Ask for it explicitly, it is not a lesser therapy.', 38),
 (N'Exam season survival kit, crowdsourced', N'amira@example.com',
  N'The one person outside the hall is the whole kit. Everything else is logistics.', 20),
 (N'Our peer circle met in person last week', N'omar@example.com',
  N'No diagnosing, no unsolicited advice — the rules make the room. Glad the weather behaved.', 15),
 (N'Forest bathing is not a joke, says my cortisol', N'marek@example.com',
  N'Six weeks is a longer trial than most of us give anything. The stream won.', 9),
 (N'How do you say "I need help" out loud?', N'rami@example.com',
  N'A different question than expected is a good door. Eleven days in the doorway is also not failure.', 2);

INSERT INTO Comments (PostId, AppUserId, Content, CommentedAt)
SELECT po.Id, d.Id, c.Content, DATEADD(HOUR, -1 * c.HoursAgo, @NowUtc)
FROM @Comments c
JOIN @Demo d ON d.Email = c.AuthorEmail
JOIN Posts po ON po.Title = c.Title
WHERE NOT EXISTS (SELECT 1 FROM Comments x
                  WHERE x.PostId = po.Id AND x.AppUserId = d.Id AND x.Content = c.Content);

DECLARE @Replies TABLE (CommentMatch nvarchar(4000), AuthorEmail nvarchar(256), Content nvarchar(4000), HoursAgo int);
INSERT INTO @Replies VALUES
 (N'This is a real pattern, not a trick.', N'rami@example.com',
  N'The eye-contact cost explains so much. We also talk better in the car.', 88),
 (N'The politeness thing is exactly it.', N'amira@example.com',
  N'Right? Accountability by sheer not-wanting-to-look-bad.', 78),
 (N'Six weeks is a longer trial than most of us give anything.', N'yusuf@example.com',
  N'I counted it as an experiment precisely so I would not quit it like a habit.', 7),
 (N'A different question than expected is a good door.', N'amira@example.com',
  N'Thank you. The doorway days are over, barely, but over.', 1);

INSERT INTO Replies (CommentId, AppUserId, Content, RepliedAt)
SELECT cm.Id, d.Id, r.Content, DATEADD(HOUR, -1 * r.HoursAgo, @NowUtc)
FROM @Replies r
JOIN @Demo d ON d.Email = r.AuthorEmail
JOIN Comments cm ON cm.Content LIKE LEFT(r.CommentMatch, 100) + N'%'
WHERE NOT EXISTS (SELECT 1 FROM Replies x
                  WHERE x.CommentId = cm.Id AND x.AppUserId = d.Id AND x.Content = r.Content);

/* ---------------------------------------------------------------------------
   3. Likes. PostLikes carries UserId (not AppUserId) — verified against the
      Posts migration and PostLike.cs.
   --------------------------------------------------------------------------- */
DECLARE @Likes TABLE (Title nvarchar(2000), AuthorEmail nvarchar(256));
INSERT INTO @Likes VALUES
 (N'The walk-and-talk rule my partner and I invented', N'doctor@example.com'),
 (N'The walk-and-talk rule my partner and I invented', N'amira@example.com'),
 (N'The walk-and-talk rule my partner and I invented', N'yusuf@example.com'),
 (N'Study buddies beat study apps (for me anyway)', N'rami@example.com'),
 (N'Study buddies beat study apps (for me anyway)', N'hana@example.com'),
 (N'What I write down when my head gets loud', N'sofia@example.com'),
 (N'What I write down when my head gets loud', N'dana@example.com'),
 (N'What I write down when my head gets loud', N'marek@example.com'),
 (N'Two guys, one bench, no small talk', N'hana@example.com'),
 (N'Two guys, one bench, no small talk', N'doctor@example.com'),
 (N'We went to couples counseling before the crack got wide', N'omar@example.com'),
 (N'We went to couples counseling before the crack got wide', N'sofia@example.com'),
 (N'Therapy during residency: the 20-minute version', N'sofia@example.com'),
 (N'Therapy during residency: the 20-minute version', N'doctor@example.com'),
 (N'The letter I wrote and never sent', N'rami@example.com'),
 (N'The letter I wrote and never sent', N'dana@example.com'),
 (N'Exam season survival kit, crowdsourced', N'amira@example.com'),
 (N'Exam season survival kit, crowdsourced', N'yusuf@example.com'),
 (N'Our peer circle met in person last week', N'marek@example.com'),
 (N'Forest bathing is not a joke, says my cortisol', N'omar@example.com'),
 (N'Night shift fuel: what I actually eat at 2am', N'sofia@example.com'),
 (N'How do you say "I need help" out loud?', N'doctor@example.com'),
 (N'How do you say "I need help" out loud?', N'rami@example.com'),
 (N'Grounding techniques that actually stuck', N'amira@example.com'),
 (N'Small wins: the walk I took before the call', N'hana@example.com');

INSERT INTO PostLikes (PostId, UserId, LikedOn)
SELECT po.Id, d.Id, po.PostedOn
FROM @Likes l
JOIN @Demo d ON d.Email = l.AuthorEmail
JOIN Posts po ON po.Title = l.Title
WHERE NOT EXISTS (SELECT 1 FROM PostLikes x WHERE x.PostId = po.Id AND x.UserId = d.Id);

/* ---------------------------------------------------------------------------
   4. Chat messages with media. ClientMessageId block 0009- keeps the fixed-GUID
      convention so a re-run cannot duplicate a message.
   --------------------------------------------------------------------------- */
DECLARE @Messages TABLE (RoomName nvarchar(100), SenderEmail nvarchar(256), Content nvarchar(2000),
                         MediaUrl nvarchar(1000), MediaType nvarchar(10),
                         ClientMessageId uniqueidentifier, HoursAgo int);
INSERT INTO @Messages VALUES
 (N'Anxiety and panic - peer support', N'amira@example.com',
  N'Post-panic recovery corner. The plant survived the worst night too.',
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654613/posts/3e8c0e318a9f42f39945866529b057f2.jpg.jpg',
  N'image', '00000000-0000-0000-0009-000000000001', 47),
 (N'Anxiety and panic - peer support', N'rami@example.com',
  N'That corner is doing therapy. Mine is the fire escape.',
  NULL, NULL, '00000000-0000-0000-0009-000000000002', 45),
 (N'Anxiety and panic - peer support', N'doctor@example.com',
  N'Grounding in public, version two: name three things you can hear. The stream was doing half of this for me.',
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654606/posts/f8865963a4e6424ab3499c7e84195256.jpg.jpg',
  N'image', '00000000-0000-0000-0009-000000000003', 40),
 (N'Anxiety and panic - peer support', N'dana@example.com',
  N'Sound beats sight for me during a spiral. Adding hearing to the list.',
  NULL, NULL, '00000000-0000-0000-0009-000000000004', 38),
 (N'Sleep and burnout', N'sofia@example.com',
  N'Walk home after the night shift. The sky is cheating, it knows it helps.',
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654605/posts/b9223bd01b784c4793cf56066ba89bd8.jpg.jpg',
  N'image', '00000000-0000-0000-0009-000000000011', 36),
 (N'Sleep and burnout', N'marek@example.com',
  N'The 6am sky is the only coworker who never asks for anything.',
  NULL, NULL, '00000000-0000-0000-0009-000000000012', 34),
 (N'Sleep and burnout', N'omar@example.com',
  N'Pinned to the room description, because it is the whole point.',
  NULL, NULL, '00000000-0000-0000-0009-000000000013', 33),
 (N'New clinicians', N'hana@example.com',
  N'My desk corner before the first hard session. The plant is a gift from the last one.',
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654613/posts/3e8c0e318a9f42f39945866529b057f2.jpg.jpg',
  N'image', '00000000-0000-0000-0009-000000000021', 22),
 (N'New clinicians', N'yusuf@example.com',
  N'Bring the real case, keep the plant.',
  NULL, NULL, '00000000-0000-0000-0009-000000000022', 20);

INSERT INTO ChatMessages (RoomId, SenderId, SenderMemberId, SenderDisplayName, SenderIsDoctor,
                          Content, MediaUrl, MediaType, ClientMessageId, SentAtUtc,
                          IsDeleted, DeletedAtUtc, DeletedById)
SELECT cr.Id, d.Id, rm.Id, rm.DisplayName, rm.IsDoctor,
       msg.Content, msg.MediaUrl, msg.MediaType, msg.ClientMessageId,
       DATEADD(HOUR, -1 * msg.HoursAgo, @NowUtc), 0, NULL, NULL
FROM @Messages msg
JOIN @Demo d ON d.Email = msg.SenderEmail
JOIN ChatRooms cr ON cr.Name = msg.RoomName
JOIN ChatRoomMembers rm ON rm.RoomId = cr.Id AND rm.UserId = d.Id AND rm.LeftAtUtc IS NULL
WHERE NOT EXISTS (SELECT 1 FROM ChatMessages x WHERE x.ClientMessageId = msg.ClientMessageId);

UPDATE cr SET LastMessageAtUtc = m.LastSent
FROM ChatRooms cr
CROSS APPLY (SELECT MAX(SentAtUtc) AS LastSent FROM ChatMessages msg
             WHERE msg.RoomId = cr.Id AND msg.IsDeleted = 0) m
WHERE cr.LastMessageAtUtc IS NULL OR cr.LastMessageAtUtc < m.LastSent;

/* ---------------------------------------------------------------------------
   5. Room covers — only where none exists yet.
   --------------------------------------------------------------------------- */
UPDATE ChatRooms SET CoverImageUrl =
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654606/posts/f8865963a4e6424ab3499c7e84195256.jpg.jpg'
WHERE Name = N'Anxiety and panic - peer support' AND CoverImageUrl IS NULL;

UPDATE ChatRooms SET CoverImageUrl =
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654613/posts/3e8c0e318a9f42f39945866529b057f2.jpg.jpg'
WHERE Name = N'Sleep and burnout' AND CoverImageUrl IS NULL;

UPDATE ChatRooms SET CoverImageUrl =
  N'https://res.cloudinary.com/dlt0e09e7/image/upload/v1793654610/posts/e2b7ca2e73e2404d9abb5a544095286b.jpg.jpg'
WHERE Name = N'New clinicians' AND CoverImageUrl IS NULL;

/* ---------------------------------------------------------------------------
   6. Doctor ratings. Raters are patient accounts only; every pair is guarded
      by the unique (DoctorId, RaterUserId) index.
   --------------------------------------------------------------------------- */
DECLARE @Ratings TABLE (DoctorEmail nvarchar(256), RaterEmail nvarchar(256), Stars int,
                        Comment nvarchar(1000), DaysAgo int);
INSERT INTO @Ratings VALUES
 (N'doctor@example.com', N'user@example.com', 5,
  N'Booked her for the worst week of my year. Twenty minutes in I could breathe again. Direct, warm, never rushed.', 34),
 (N'doctor@example.com', N'amira@example.com', 4,
  N'Genuinely helpful sessions. Sometimes the scheduling is tight, but the therapy itself is excellent.', 21),
 (N'doctor@example.com', N'rami@example.com', 5,
  N'She gave me homework I actually do. Six weeks in and the 3am meetings with my own brain are shorter.', 12),
 (N'omar@example.com', N'sofia@example.com', 5,
  N'A clinician who answers messages between sessions and means it. Sleep work with him fixed what eight months of coffee discipline did not.', 40),
 (N'omar@example.com', N'marek@example.com', 4, N'Practical, no nonsense, good with shift workers.', 17),
 (N'omar@example.com', N'dana@example.com', 5,
  N'Referral from a friend in one of his rooms. Worth every minute of the wait for the first slot.', 9),
 (N'doctor@example.com', N'dana@example.com', 4,
  N'Very steady. I needed someone who would not overreact, and she never does.', 5),
 (N'hana@example.com', N'marek@example.com', 5,
  N'She runs the exam-season threads like a person who has been through it, because she has.', 26),
 (N'hana@example.com', N'user@example.com', 4,
  N'Clear boundaries, real plans. The follow-up notes after each session are gold.', 14),
 (N'hana@example.com', N'rami@example.com', 3,
  N'Good sessions, but our styles took a while to click. Worth giving it three tries.', 6),
 (N'yusuf@example.com', N'amira@example.com', 5,
  N'Talks like a normal human being. My anxiety stopped performing for the therapist and just... dropped.', 30),
 (N'yusuf@example.com', N'sofia@example.com', 4, N'Flexible with odd hours, which for me is the whole feature.', 11),
 (N'salma@example.com', N'marek@example.com', 4,
  N'Sharpeye on what is actually in front of you. Sessions never drift.', 8),
 (N'salma@example.com', N'dana@example.com', 5, N'Direct, warm, never condescending. Rare combination.', 4);

INSERT INTO DoctorRatings (DoctorId, RaterUserId, DisplayName, Stars, Comment, CreatedAtUtc)
SELECT doc.Id, raterUser.Id, raterUser.FirstName + N' ' + raterUser.LastName, r.Stars, r.Comment,
       DATEADD(DAY, -1 * r.DaysAgo, @NowUtc)
FROM @Ratings r
JOIN AspNetUsers doctorUser ON doctorUser.Email = r.DoctorEmail
JOIN Doctors doc ON doc.Id = doctorUser.Id
JOIN AspNetUsers raterUser ON raterUser.Email = r.RaterEmail
JOIN Users pat ON pat.Id = raterUser.Id
WHERE NOT EXISTS (SELECT 1 FROM DoctorRatings x
                  WHERE x.DoctorId = doc.Id AND x.RaterUserId = raterUser.Id);

/* ---------------------------------------------------------------------------
   Summary, then commit or roll back per @DryRun.
   --------------------------------------------------------------------------- */
SELECT N'new posts'      AS what, COUNT(*) AS n FROM Posts p JOIN @Posts sp ON sp.Title = p.Title
UNION ALL SELECT N'new comments', COUNT(*) FROM Comments c JOIN @Comments sc ON LEFT(c.Content, 200) = LEFT(sc.Content, 200)
UNION ALL SELECT N'new likes',    COUNT(*) FROM PostLikes x JOIN @Demo d ON d.Id = x.UserId JOIN Posts p ON p.Id = x.PostId JOIN @Posts sp ON sp.Title = p.Title
UNION ALL SELECT N'media messages', COUNT(*) FROM ChatMessages m JOIN @Messages sm ON sm.ClientMessageId = m.ClientMessageId
UNION ALL SELECT N'room covers',  COUNT(*) FROM ChatRooms WHERE CoverImageUrl IS NOT NULL
UNION ALL SELECT N'ratings',      COUNT(*) FROM DoctorRatings;

SELECT d.FirstName + N' ' + d.LastName AS doctor,
       ROUND(AVG(CAST(r.Stars AS decimal(9,2))), 1) AS average, COUNT(*) AS votes
FROM DoctorRatings r JOIN AspNetUsers d ON d.Id = r.DoctorId
GROUP BY d.FirstName, d.LastName;

IF @DryRun = 1
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'DRY RUN complete — everything validated and rolled back. Set @DryRun = 0 to commit.';
END
ELSE
BEGIN
    COMMIT TRANSACTION;
    PRINT N'Community seed committed.';
END
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    DECLARE @err nvarchar(2048) = ERROR_MESSAGE();
    RAISERROR(N'Seed failed and rolled back: %s', 16, 1, @err);
END CATCH;
