IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(max) NULL,
        [LastName] nvarchar(max) NULL,
        [BirthDate] date NOT NULL,
        [Gender] nvarchar(max) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [UserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE TABLE [Posts] (
        [Id] int NOT NULL IDENTITY,
        [AppUserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [Content] nvarchar(2047) NOT NULL,
        [PostedOn] datetime2 NOT NULL,
        CONSTRAINT [PK_Posts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Posts_AspNetUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE TABLE [Comments] (
        [Id] int NOT NULL IDENTITY,
        [PostId] int NOT NULL,
        [AppUserId] nvarchar(450) NULL,
        [Content] nvarchar(max) NULL,
        [CommentedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Comments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Comments_AspNetUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_Comments_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE TABLE [Replies] (
        [Id] int NOT NULL IDENTITY,
        [CommentId] int NOT NULL,
        [AppUserId] nvarchar(450) NULL,
        [Content] nvarchar(max) NULL,
        [RepliedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Replies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Replies_AspNetUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_Replies_Comments_CommentId] FOREIGN KEY ([CommentId]) REFERENCES [Comments] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE INDEX [IX_Comments_AppUserId] ON [Comments] ([AppUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE INDEX [IX_Comments_PostId] ON [Comments] ([PostId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE INDEX [IX_Posts_AppUserId] ON [Posts] ([AppUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE INDEX [IX_Replies_AppUserId] ON [Replies] ([AppUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    CREATE INDEX [IX_Replies_CommentId] ON [Replies] ([CommentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240129013226_EntityTablesCreationAndSeeding'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240129013226_EntityTablesCreationAndSeeding', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240317142805_AddedPhotoUrlColumnToAppUserTable'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [PhotoUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240317142805_AddedPhotoUrlColumnToAppUserTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240317142805_AddedPhotoUrlColumnToAppUserTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240326150648_AddedIsAnonymousFieldToPostEntity'
)
BEGIN
    ALTER TABLE [Posts] ADD [IsAnonymous] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240326150648_AddedIsAnonymousFieldToPostEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240326150648_AddedIsAnonymousFieldToPostEntity', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240429054144_AddedNotificationsTable'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [AppUserId] nvarchar(450) NULL,
        [Message] nvarchar(500) NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        [IsRead] bit NOT NULL,
        [ResourceId] int NOT NULL,
        [Type] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_AspNetUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AspNetUsers] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240429054144_AddedNotificationsTable'
)
BEGIN
    CREATE INDEX [IX_Notifications_AppUserId] ON [Notifications] ([AppUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240429054144_AddedNotificationsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240429054144_AddedNotificationsTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240503002038_AddedNotificationResourcesColumn'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'ResourceId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Notifications] DROP COLUMN [ResourceId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240503002038_AddedNotificationResourcesColumn'
)
BEGIN
    ALTER TABLE [Notifications] ADD [Resources] nvarchar(1000) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240503002038_AddedNotificationResourcesColumn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240503002038_AddedNotificationResourcesColumn', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240504072139_AddedSeparateTablesForUserAndDoctorEntites'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Resources');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [Resources] nvarchar(1000) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240504072139_AddedSeparateTablesForUserAndDoctorEntites'
)
BEGIN
    CREATE TABLE [Doctors] (
        [Id] nvarchar(450) NOT NULL,
        [Biography] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Doctors] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Doctors_AspNetUsers_Id] FOREIGN KEY ([Id]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240504072139_AddedSeparateTablesForUserAndDoctorEntites'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_AspNetUsers_Id] FOREIGN KEY ([Id]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240504072139_AddedSeparateTablesForUserAndDoctorEntites'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240504072139_AddedSeparateTablesForUserAndDoctorEntites', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    ALTER TABLE [Notifications] DROP CONSTRAINT [FK_Notifications_AspNetUsers_AppUserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'Title');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Posts] ALTER COLUMN [Title] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'Content');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Posts] ALTER COLUMN [Content] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'AppUserId');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Posts] ALTER COLUMN [AppUserId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    ALTER TABLE [Doctors] ADD [Specialization] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    CREATE TABLE [Appointments] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [DoctorId] nvarchar(450) NOT NULL,
        [StartTime] datetime2 NOT NULL,
        [Duration] time NOT NULL,
        [Status] int NOT NULL,
        [Location] nvarchar(max) NOT NULL,
        [Reason] nvarchar(max) NULL,
        [CancellationReason] nvarchar(max) NULL,
        [RejectionReason] nvarchar(max) NULL,
        CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Appointments_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]),
        CONSTRAINT [FK_Appointments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    CREATE TABLE [WeeklySchedules] (
        [Id] int NOT NULL IDENTITY,
        [DoctorId] nvarchar(450) NULL,
        CONSTRAINT [PK_WeeklySchedules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WeeklySchedules_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    CREATE TABLE [AvailableDays] (
        [Id] int NOT NULL IDENTITY,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [SessionDuration] time NOT NULL,
        [EndTime] time NOT NULL,
        [WeeklyScheduleId] int NULL,
        CONSTRAINT [PK_AvailableDays] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AvailableDays_WeeklySchedules_WeeklyScheduleId] FOREIGN KEY ([WeeklyScheduleId]) REFERENCES [WeeklySchedules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    CREATE INDEX [IX_Appointments_DoctorId] ON [Appointments] ([DoctorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    CREATE INDEX [IX_Appointments_UserId] ON [Appointments] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    CREATE INDEX [IX_AvailableDays_WeeklyScheduleId] ON [AvailableDays] ([WeeklyScheduleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_WeeklySchedules_DoctorId] ON [WeeklySchedules] ([DoctorId]) WHERE [DoctorId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    ALTER TABLE [Notifications] ADD CONSTRAINT [FK_Notifications_AspNetUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240514072339_addReservationSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240514072339_addReservationSystem', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240517155839_ModifiedTheReseverationSystemEntites'
)
BEGIN
    DROP TABLE [AvailableDays];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240517155839_ModifiedTheReseverationSystemEntites'
)
BEGIN
    DROP TABLE [WeeklySchedules];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240517155839_ModifiedTheReseverationSystemEntites'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'Content');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Posts] ALTER COLUMN [Content] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240517155839_ModifiedTheReseverationSystemEntites'
)
BEGIN
    CREATE TABLE [ScheduleWeekDays] (
        [DoctorId] nvarchar(450) NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [SessionDuration] time NOT NULL,
        [EndTime] time NOT NULL,
        CONSTRAINT [PK_ScheduleWeekDays] PRIMARY KEY ([DoctorId], [DayOfWeek]),
        CONSTRAINT [FK_ScheduleWeekDays_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240517155839_ModifiedTheReseverationSystemEntites'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240517155839_ModifiedTheReseverationSystemEntites', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240602131543_AddedPostPhotoUrlColumnToPostsTable'
)
BEGIN
    ALTER TABLE [Posts] ADD [PostPhotoUrl] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240602131543_AddedPostPhotoUrlColumnToPostsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240602131543_AddedPostPhotoUrlColumnToPostsTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240602143454_AddedIndexOnPostedOnColumnOnPostsTable'
)
BEGIN
    CREATE INDEX [IX_Posts_PostedOn] ON [Posts] ([PostedOn]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240602143454_AddedIndexOnPostedOnColumnOnPostsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240602143454_AddedIndexOnPostedOnColumnOnPostsTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240605072110_AddedUsernameAndPhotoUrlToNotificationTable'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'IsRead');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Notifications] DROP COLUMN [IsRead];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240605072110_AddedUsernameAndPhotoUrlToNotificationTable'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Type');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [Type] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240605072110_AddedUsernameAndPhotoUrlToNotificationTable'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Resources');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [Resources] nvarchar(500) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240605072110_AddedUsernameAndPhotoUrlToNotificationTable'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Message');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [Message] nvarchar(200) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240605072110_AddedUsernameAndPhotoUrlToNotificationTable'
)
BEGIN
    ALTER TABLE [Notifications] ADD [NotifierPhotoUrl] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240605072110_AddedUsernameAndPhotoUrlToNotificationTable'
)
BEGIN
    ALTER TABLE [Notifications] ADD [NotifierUserName] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240605072110_AddedUsernameAndPhotoUrlToNotificationTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240605072110_AddedUsernameAndPhotoUrlToNotificationTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240605111711_AddedIsReadColumnToNotificationTable'
)
BEGIN
    ALTER TABLE [Notifications] ADD [IsRead] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240605111711_AddedIsReadColumnToNotificationTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240605111711_AddedIsReadColumnToNotificationTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240606125609_AddedAdminTable'
)
BEGIN
    CREATE TABLE [Admins] (
        [Id] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Admins] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240606125609_AddedAdminTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240606125609_AddedAdminTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240611164640_UpdatedAppointmentsTable'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Appointments]') AND [c].[name] = N'Reason');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Appointments] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [Appointments] DROP COLUMN [Reason];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240611164640_UpdatedAppointmentsTable'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Appointments]') AND [c].[name] = N'RejectionReason');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Appointments] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [Appointments] ALTER COLUMN [RejectionReason] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240611164640_UpdatedAppointmentsTable'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Appointments]') AND [c].[name] = N'CancellationReason');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Appointments] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [Appointments] ALTER COLUMN [CancellationReason] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240611164640_UpdatedAppointmentsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240611164640_UpdatedAppointmentsTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Replies]') AND [c].[name] = N'Content');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Replies] DROP CONSTRAINT [' + @var13 + '];');
    EXEC(N'UPDATE [Replies] SET [Content] = N'''' WHERE [Content] IS NULL');
    ALTER TABLE [Replies] ALTER COLUMN [Content] nvarchar(1000) NOT NULL;
    ALTER TABLE [Replies] ADD DEFAULT N'' FOR [Content];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DROP INDEX [IX_Replies_AppUserId] ON [Replies];
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Replies]') AND [c].[name] = N'AppUserId');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Replies] DROP CONSTRAINT [' + @var14 + '];');
    EXEC(N'UPDATE [Replies] SET [AppUserId] = N'''' WHERE [AppUserId] IS NULL');
    ALTER TABLE [Replies] ALTER COLUMN [AppUserId] nvarchar(450) NOT NULL;
    ALTER TABLE [Replies] ADD DEFAULT N'' FOR [AppUserId];
    CREATE INDEX [IX_Replies_AppUserId] ON [Replies] ([AppUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'Title');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT [' + @var15 + '];');
    EXEC(N'UPDATE [Posts] SET [Title] = N'''' WHERE [Title] IS NULL');
    ALTER TABLE [Posts] ALTER COLUMN [Title] nvarchar(max) NOT NULL;
    ALTER TABLE [Posts] ADD DEFAULT N'' FOR [Title];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'Content');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT [' + @var16 + '];');
    EXEC(N'UPDATE [Posts] SET [Content] = N'''' WHERE [Content] IS NULL');
    ALTER TABLE [Posts] ALTER COLUMN [Content] nvarchar(max) NOT NULL;
    ALTER TABLE [Posts] ADD DEFAULT N'' FOR [Content];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DROP INDEX [IX_Posts_AppUserId] ON [Posts];
    DECLARE @var17 sysname;
    SELECT @var17 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'AppUserId');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT [' + @var17 + '];');
    EXEC(N'UPDATE [Posts] SET [AppUserId] = N'''' WHERE [AppUserId] IS NULL');
    ALTER TABLE [Posts] ALTER COLUMN [AppUserId] nvarchar(450) NOT NULL;
    ALTER TABLE [Posts] ADD DEFAULT N'' FOR [AppUserId];
    CREATE INDEX [IX_Posts_AppUserId] ON [Posts] ([AppUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DROP INDEX [IX_Notifications_AppUserId] ON [Notifications];
    DECLARE @var18 sysname;
    SELECT @var18 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'AppUserId');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var18 + '];');
    EXEC(N'UPDATE [Notifications] SET [AppUserId] = N'''' WHERE [AppUserId] IS NULL');
    ALTER TABLE [Notifications] ALTER COLUMN [AppUserId] nvarchar(450) NOT NULL;
    ALTER TABLE [Notifications] ADD DEFAULT N'' FOR [AppUserId];
    CREATE INDEX [IX_Notifications_AppUserId] ON [Notifications] ([AppUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DECLARE @var19 sysname;
    SELECT @var19 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Doctors]') AND [c].[name] = N'Biography');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Doctors] DROP CONSTRAINT [' + @var19 + '];');
    ALTER TABLE [Doctors] ALTER COLUMN [Biography] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    ALTER TABLE [Doctors] ADD [City] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    ALTER TABLE [Doctors] ADD [Location] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    ALTER TABLE [Doctors] ADD [SessionFees] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DECLARE @var20 sysname;
    SELECT @var20 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Comments]') AND [c].[name] = N'Content');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Comments] DROP CONSTRAINT [' + @var20 + '];');
    EXEC(N'UPDATE [Comments] SET [Content] = N'''' WHERE [Content] IS NULL');
    ALTER TABLE [Comments] ALTER COLUMN [Content] nvarchar(1000) NOT NULL;
    ALTER TABLE [Comments] ADD DEFAULT N'' FOR [Content];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DROP INDEX [IX_Comments_AppUserId] ON [Comments];
    DECLARE @var21 sysname;
    SELECT @var21 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Comments]') AND [c].[name] = N'AppUserId');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Comments] DROP CONSTRAINT [' + @var21 + '];');
    EXEC(N'UPDATE [Comments] SET [AppUserId] = N'''' WHERE [AppUserId] IS NULL');
    ALTER TABLE [Comments] ALTER COLUMN [AppUserId] nvarchar(450) NOT NULL;
    ALTER TABLE [Comments] ADD DEFAULT N'' FOR [AppUserId];
    CREATE INDEX [IX_Comments_AppUserId] ON [Comments] ([AppUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DECLARE @var22 sysname;
    SELECT @var22 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'UserName');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var22 + '];');
    EXEC(N'UPDATE [AspNetUsers] SET [UserName] = N'''' WHERE [UserName] IS NULL');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [UserName] nvarchar(256) NOT NULL;
    ALTER TABLE [AspNetUsers] ADD DEFAULT N'' FOR [UserName];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DROP INDEX [UserNameIndex] ON [AspNetUsers];
    DECLARE @var23 sysname;
    SELECT @var23 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'NormalizedUserName');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var23 + '];');
    EXEC(N'UPDATE [AspNetUsers] SET [NormalizedUserName] = N'''' WHERE [NormalizedUserName] IS NULL');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [NormalizedUserName] nvarchar(256) NOT NULL;
    ALTER TABLE [AspNetUsers] ADD DEFAULT N'' FOR [NormalizedUserName];
    CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DROP INDEX [EmailIndex] ON [AspNetUsers];
    DECLARE @var24 sysname;
    SELECT @var24 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'NormalizedEmail');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var24 + '];');
    EXEC(N'UPDATE [AspNetUsers] SET [NormalizedEmail] = N'''' WHERE [NormalizedEmail] IS NULL');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [NormalizedEmail] nvarchar(256) NOT NULL;
    ALTER TABLE [AspNetUsers] ADD DEFAULT N'' FOR [NormalizedEmail];
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DECLARE @var25 sysname;
    SELECT @var25 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'LastName');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var25 + '];');
    EXEC(N'UPDATE [AspNetUsers] SET [LastName] = N'''' WHERE [LastName] IS NULL');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [LastName] nvarchar(max) NOT NULL;
    ALTER TABLE [AspNetUsers] ADD DEFAULT N'' FOR [LastName];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DECLARE @var26 sysname;
    SELECT @var26 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'Gender');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var26 + '];');
    EXEC(N'UPDATE [AspNetUsers] SET [Gender] = N'''' WHERE [Gender] IS NULL');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [Gender] nvarchar(max) NOT NULL;
    ALTER TABLE [AspNetUsers] ADD DEFAULT N'' FOR [Gender];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    DECLARE @var27 sysname;
    SELECT @var27 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'FirstName');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var27 + '];');
    EXEC(N'UPDATE [AspNetUsers] SET [FirstName] = N'''' WHERE [FirstName] IS NULL');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [FirstName] nvarchar(max) NOT NULL;
    ALTER TABLE [AspNetUsers] ADD DEFAULT N'' FOR [FirstName];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    ALTER TABLE [Appointments] ADD [Reason] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622170538_AddedLocationCityFeesToDoctorTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240622170538_AddedLocationCityFeesToDoctorTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622175103_AddedFeesToAppointmentTable'
)
BEGIN
    ALTER TABLE [Appointments] ADD [Fees] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240622175103_AddedFeesToAppointmentTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240622175103_AddedFeesToAppointmentTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240626045905_addedIsDeletedColumnToBaseUserTAble'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [isDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240626045905_addedIsDeletedColumnToBaseUserTAble'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240626045905_addedIsDeletedColumnToBaseUserTAble', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240704223919_AddedDepressionTestResultsTable'
)
BEGIN
    EXEC sp_rename N'[AspNetUsers].[isDeleted]', N'IsDeleted', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240704223919_AddedDepressionTestResultsTable'
)
BEGIN
    DECLARE @var28 sysname;
    SELECT @var28 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'Title');
    IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT [' + @var28 + '];');
    ALTER TABLE [Posts] ALTER COLUMN [Title] nvarchar(2000) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240704223919_AddedDepressionTestResultsTable'
)
BEGIN
    CREATE TABLE [DepressionTestResult] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NULL,
        [Result] nvarchar(100) NOT NULL,
        [Date] datetime2 NOT NULL,
        CONSTRAINT [PK_DepressionTestResult] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DepressionTestResult_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240704223919_AddedDepressionTestResultsTable'
)
BEGIN
    CREATE INDEX [IX_DepressionTestResult_UserId] ON [DepressionTestResult] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240704223919_AddedDepressionTestResultsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240704223919_AddedDepressionTestResultsTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240705020345_UpdatedUserIdColumnInDepressionTestReultsTableToNullable'
)
BEGIN
    ALTER TABLE [DepressionTestResult] DROP CONSTRAINT [FK_DepressionTestResult_Users_UserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240705020345_UpdatedUserIdColumnInDepressionTestReultsTableToNullable'
)
BEGIN
    ALTER TABLE [DepressionTestResult] ADD CONSTRAINT [FK_DepressionTestResult_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240705020345_UpdatedUserIdColumnInDepressionTestReultsTableToNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240705020345_UpdatedUserIdColumnInDepressionTestReultsTableToNullable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240705145559_AddedGenderAgeColumnsToDepressionTestResultTable'
)
BEGIN
    ALTER TABLE [DepressionTestResult] ADD [Age] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240705145559_AddedGenderAgeColumnsToDepressionTestResultTable'
)
BEGIN
    ALTER TABLE [DepressionTestResult] ADD [Gender] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20240705145559_AddedGenderAgeColumnsToDepressionTestResultTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20240705145559_AddedGenderAgeColumnsToDepressionTestResultTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Biography] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [City] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    UPDATE u SET Biography = d.Biography, City = d.City FROM AspNetUsers u INNER JOIN Doctors d ON d.Id = u.Id
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    DECLARE @var29 sysname;
    SELECT @var29 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Doctors]') AND [c].[name] = N'Biography');
    IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [Doctors] DROP CONSTRAINT [' + @var29 + '];');
    ALTER TABLE [Doctors] DROP COLUMN [Biography];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    DECLARE @var30 sysname;
    SELECT @var30 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Doctors]') AND [c].[name] = N'City');
    IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [Doctors] DROP CONSTRAINT [' + @var30 + '];');
    ALTER TABLE [Doctors] DROP COLUMN [City];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    DECLARE @var31 sysname;
    SELECT @var31 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Replies]') AND [c].[name] = N'Content');
    IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [Replies] DROP CONSTRAINT [' + @var31 + '];');
    ALTER TABLE [Replies] ALTER COLUMN [Content] nvarchar(4000) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    ALTER TABLE [Posts] ADD [Category] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    ALTER TABLE [Posts] ADD [Tags] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    DECLARE @var32 sysname;
    SELECT @var32 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Resources');
    IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var32 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [Resources] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    DECLARE @var33 sysname;
    SELECT @var33 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'NotifierUserName');
    IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var33 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [NotifierUserName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    DECLARE @var34 sysname;
    SELECT @var34 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'NotifierPhotoUrl');
    IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var34 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [NotifierPhotoUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    DECLARE @var35 sysname;
    SELECT @var35 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Message');
    IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var35 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [Message] nvarchar(2000) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    ALTER TABLE [Doctors] ADD [IsAvailable] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    ALTER TABLE [Doctors] ADD [Qualifications] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    DECLARE @var36 sysname;
    SELECT @var36 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Comments]') AND [c].[name] = N'Content');
    IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [Comments] DROP CONSTRAINT [' + @var36 + '];');
    ALTER TABLE [Comments] ALTER COLUMN [Content] nvarchar(4000) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    CREATE TABLE [PostLikes] (
        [Id] int NOT NULL IDENTITY,
        [PostId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [LikedOn] datetime2 NOT NULL,
        CONSTRAINT [PK_PostLikes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PostLikes_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_PostLikes_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    CREATE INDEX [IX_Posts_IsAnonymous] ON [Posts] ([IsAnonymous]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    CREATE INDEX [IX_Notifications_AppUserId_IsRead] ON [Notifications] ([AppUserId], [IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    CREATE INDEX [IX_Notifications_IsRead] ON [Notifications] ([IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    CREATE INDEX [IX_Doctors_Specialization] ON [Doctors] ([Specialization]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    CREATE INDEX [IX_Appointments_DoctorId_StartTime_Status] ON [Appointments] ([DoctorId], [StartTime], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    CREATE INDEX [IX_Appointments_StartTime] ON [Appointments] ([StartTime]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    CREATE INDEX [IX_Appointments_Status] ON [Appointments] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    CREATE INDEX [IX_PostLikes_PostId] ON [PostLikes] ([PostId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PostLikes_UserId_PostId] ON [PostLikes] ([UserId], [PostId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261021004532_ExpandDoctorProfileCommunity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021004532_ExpandDoctorProfileCommunity', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022183559_SyncSomeeSchema'
)
BEGIN
                    IF COL_LENGTH('AspNetUsers', 'Biography') IS NULL
                        ALTER TABLE [AspNetUsers] ADD [Biography] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022183559_SyncSomeeSchema'
)
BEGIN
                    IF COL_LENGTH('AspNetUsers', 'City') IS NULL
                        ALTER TABLE [AspNetUsers] ADD [City] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022183559_SyncSomeeSchema'
)
BEGIN
                    IF COL_LENGTH('Posts', 'Category') IS NULL
                        ALTER TABLE [Posts] ADD [Category] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022183559_SyncSomeeSchema'
)
BEGIN
                    IF COL_LENGTH('Posts', 'Tags') IS NULL
                        ALTER TABLE [Posts] ADD [Tags] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022183559_SyncSomeeSchema'
)
BEGIN
                    IF OBJECT_ID(N'[PostLikes]', N'U') IS NULL
                    BEGIN
                        CREATE TABLE [PostLikes] (
                            [Id] int NOT NULL IDENTITY,
                            [PostId] int NOT NULL,
                            [UserId] nvarchar(450) NOT NULL,
                            [LikedOn] datetime2 NOT NULL,
                            CONSTRAINT [PK_PostLikes] PRIMARY KEY CLUSTERED ([Id])
                        );
                    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022183559_SyncSomeeSchema'
)
BEGIN
                    IF OBJECT_ID(N'[PostLikes]', N'U') IS NOT NULL
                       AND OBJECT_ID(N'[FK_PostLikes_Posts_PostId]', N'F') IS NULL
                    BEGIN
                        ALTER TABLE [PostLikes] WITH NOCHECK ADD
                            CONSTRAINT [FK_PostLikes_Posts_PostId] FOREIGN KEY ([PostId])
                            REFERENCES [Posts] ([Id]) ON DELETE CASCADE;
                    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022183559_SyncSomeeSchema'
)
BEGIN
                    IF OBJECT_ID(N'[PostLikes]', N'U') IS NOT NULL
                       AND OBJECT_ID(N'[FK_PostLikes_AspNetUsers_UserId]', N'F') IS NULL
                    BEGIN
                        ALTER TABLE [PostLikes] WITH NOCHECK ADD
                            CONSTRAINT [FK_PostLikes_AspNetUsers_UserId] FOREIGN KEY ([UserId])
                            REFERENCES [AspNetUsers] ([Id]);
                    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022183559_SyncSomeeSchema'
)
BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                                   WHERE name = N'IX_PostLikes_UserId_PostId'
                                     AND object_id = OBJECT_ID(N'[PostLikes]'))
                    BEGIN
                        CREATE UNIQUE INDEX [IX_PostLikes_UserId_PostId]
                            ON [PostLikes] ([UserId], [PostId]);
                    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022183559_SyncSomeeSchema'
)
BEGIN
                    IF COL_LENGTH('Doctors', 'Biography') IS NOT NULL
                       AND COL_LENGTH('AspNetUsers', 'Biography') IS NOT NULL
                    BEGIN
                        EXEC(N'
                        UPDATE u
                        SET u.Biography = d.Biography,
                            u.City      = d.City
                        FROM [AspNetUsers] u
                        INNER JOIN [Doctors] d ON d.Id = u.Id
                        WHERE (u.Biography IS NULL AND d.Biography IS NOT NULL)
                           OR (u.City IS NULL AND d.City IS NOT NULL);');
                    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022183559_SyncSomeeSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022183559_SyncSomeeSchema', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE TABLE [ChatRooms] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Visibility] int NOT NULL,
        [CreatedById] nvarchar(450) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [LastMessageAtUtc] datetime2 NULL,
        [IsArchived] bit NOT NULL,
        CONSTRAINT [PK_ChatRooms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatRooms_AspNetUsers_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [AspNetUsers] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] bigint NOT NULL IDENTITY,
        [RoomId] int NOT NULL,
        [SenderId] nvarchar(450) NOT NULL,
        [SenderMemberId] int NOT NULL,
        [SenderDisplayName] nvarchar(100) NOT NULL,
        [SenderIsDoctor] bit NOT NULL,
        [Content] nvarchar(2000) NOT NULL,
        [ClientMessageId] uniqueidentifier NOT NULL,
        [SentAtUtc] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        [DeletedById] nvarchar(450) NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessages_AspNetUsers_DeletedById] FOREIGN KEY ([DeletedById]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ChatMessages_AspNetUsers_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ChatMessages_ChatRooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [ChatRooms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE TABLE [ChatRoomMembers] (
        [Id] int NOT NULL IDENTITY,
        [RoomId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Role] int NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        [IsDoctor] bit NOT NULL,
        [JoinedAtUtc] datetime2 NOT NULL,
        [LeftAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ChatRoomMembers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatRoomMembers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ChatRoomMembers_ChatRooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [ChatRooms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE TABLE [ChatMessageReports] (
        [Id] int NOT NULL IDENTITY,
        [MessageId] bigint NOT NULL,
        [ReporterId] nvarchar(450) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ReviewedAtUtc] datetime2 NULL,
        [ReviewedById] nvarchar(450) NULL,
        CONSTRAINT [PK_ChatMessageReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessageReports_AspNetUsers_ReporterId] FOREIGN KEY ([ReporterId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ChatMessageReports_AspNetUsers_ReviewedById] FOREIGN KEY ([ReviewedById]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ChatMessageReports_ChatMessages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [ChatMessages] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ChatMessageReports_MessageId_ReporterId] ON [ChatMessageReports] ([MessageId], [ReporterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE INDEX [IX_ChatMessageReports_ReporterId] ON [ChatMessageReports] ([ReporterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE INDEX [IX_ChatMessageReports_ReviewedById] ON [ChatMessageReports] ([ReviewedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE INDEX [IX_ChatMessageReports_Status_CreatedAtUtc] ON [ChatMessageReports] ([Status], [CreatedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_DeletedById] ON [ChatMessages] ([DeletedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_RoomId_Id] ON [ChatMessages] ([RoomId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ChatMessages_SenderId_ClientMessageId] ON [ChatMessages] ([SenderId], [ClientMessageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ChatRoomMembers_RoomId_UserId] ON [ChatRoomMembers] ([RoomId], [UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE INDEX [IX_ChatRoomMembers_UserId_LeftAtUtc] ON [ChatRoomMembers] ([UserId], [LeftAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE INDEX [IX_ChatRooms_CreatedById] ON [ChatRooms] ([CreatedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    CREATE INDEX [IX_ChatRooms_Visibility_IsArchived_LastMessageAtUtc] ON [ChatRooms] ([Visibility], [IsArchived], [LastMessageAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024001703_AddChatRooms'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024001703_AddChatRooms', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025150426_AddDoctorSchedulingPrimitives'
)
BEGIN
    IF OBJECT_ID(N'[DoctorScheduleBlocks]', N'U') IS NOT NULL
        DROP TABLE [DoctorScheduleBlocks];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025150426_AddDoctorSchedulingPrimitives'
)
BEGIN
    CREATE TABLE [DoctorAvailabilityOverrides] (
        [Id] int NOT NULL IDENTITY,
        [DoctorId] nvarchar(450) NOT NULL,
        [Date] date NOT NULL,
        [Type] int NOT NULL,
        [StartTime] time NULL,
        [EndTime] time NULL,
        [PrivateReason] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_DoctorAvailabilityOverrides] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DoctorAvailabilityOverrides_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025150426_AddDoctorSchedulingPrimitives'
)
BEGIN
    CREATE TABLE [DoctorRecurringBreaks] (
        [Id] int NOT NULL IDENTITY,
        [DoctorId] nvarchar(450) NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [Name] nvarchar(200) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_DoctorRecurringBreaks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DoctorRecurringBreaks_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025150426_AddDoctorSchedulingPrimitives'
)
BEGIN
    CREATE TABLE [DoctorSchedulingSettings] (
        [DoctorId] nvarchar(450) NOT NULL,
        [TimeZoneId] nvarchar(64) NOT NULL,
        [SlotIncrementMinutes] int NOT NULL,
        [BufferBeforeMinutes] int NOT NULL,
        [BufferAfterMinutes] int NOT NULL,
        [MinimumNoticeMinutes] int NOT NULL,
        [MaxBookingDaysAhead] int NOT NULL,
        CONSTRAINT [PK_DoctorSchedulingSettings] PRIMARY KEY ([DoctorId]),
        CONSTRAINT [FK_DoctorSchedulingSettings_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025150426_AddDoctorSchedulingPrimitives'
)
BEGIN
    CREATE TABLE [DoctorTimeBlocks] (
        [Id] int NOT NULL IDENTITY,
        [DoctorId] nvarchar(450) NOT NULL,
        [StartTimeUtc] datetime2 NOT NULL,
        [EndTimeUtc] datetime2 NOT NULL,
        [Type] int NOT NULL,
        [PrivateReason] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [CreatedByUserId] nvarchar(max) NULL,
        [IsCancelled] bit NOT NULL,
        [CancelledAtUtc] datetime2 NULL,
        CONSTRAINT [PK_DoctorTimeBlocks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DoctorTimeBlocks_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025150426_AddDoctorSchedulingPrimitives'
)
BEGIN
    CREATE INDEX [IX_DoctorAvailabilityOverrides_DoctorId_Date_Type_IsActive] ON [DoctorAvailabilityOverrides] ([DoctorId], [Date], [Type], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025150426_AddDoctorSchedulingPrimitives'
)
BEGIN
    CREATE INDEX [IX_DoctorRecurringBreaks_DoctorId_DayOfWeek_StartTime_EndTime] ON [DoctorRecurringBreaks] ([DoctorId], [DayOfWeek], [StartTime], [EndTime]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025150426_AddDoctorSchedulingPrimitives'
)
BEGIN
    CREATE INDEX [IX_DoctorTimeBlocks_DoctorId_StartTimeUtc_EndTimeUtc_IsCancelled] ON [DoctorTimeBlocks] ([DoctorId], [StartTimeUtc], [EndTimeUtc], [IsCancelled]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025150426_AddDoctorSchedulingPrimitives'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025150426_AddDoctorSchedulingPrimitives', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    ALTER TABLE [DoctorSchedulingSettings] ADD [ConsultationDurationMinutes] int NOT NULL DEFAULT 30;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    CREATE TABLE [DoctorAvailabilityPeriods] (
        [Id] int NOT NULL IDENTITY,
        [DoctorId] nvarchar(450) NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        CONSTRAINT [PK_DoctorAvailabilityPeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DoctorAvailabilityPeriods_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    CREATE TABLE [DoctorTimeOff] (
        [Id] int NOT NULL IDENTITY,
        [DoctorId] nvarchar(450) NOT NULL,
        [StartTimeUtc] datetime2 NOT NULL,
        [EndTimeUtc] datetime2 NOT NULL,
        [Reason] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_DoctorTimeOff] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DoctorTimeOff_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    EXEC(N'ALTER TABLE [DoctorSchedulingSettings] ADD CONSTRAINT [CK_DoctorSchedulingSettings_ConsultationDuration] CHECK ([ConsultationDurationMinutes] IN (30, 45, 60))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    CREATE INDEX [IX_DoctorAvailabilityPeriods_DoctorId_DayOfWeek_StartTime] ON [DoctorAvailabilityPeriods] ([DoctorId], [DayOfWeek], [StartTime]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    CREATE INDEX [IX_DoctorTimeOff_DoctorId_StartTimeUtc_EndTimeUtc] ON [DoctorTimeOff] ([DoctorId], [StartTimeUtc], [EndTimeUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    INSERT INTO DoctorTimeOff (DoctorId, StartTimeUtc, EndTimeUtc, Reason, CreatedAtUtc)
    SELECT DoctorId,
           StartTimeUtc,
           EndTimeUtc,
           PrivateReason,
           COALESCE(CreatedAtUtc, SYSUTCDATETIME())
    FROM DoctorTimeBlocks
    WHERE IsCancelled = 0
      AND EndTimeUtc > StartTimeUtc;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    DROP TABLE [DoctorAvailabilityOverrides];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    DROP TABLE [DoctorRecurringBreaks];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    DROP TABLE [DoctorTimeBlocks];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    DROP TABLE [ScheduleWeekDays];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    DECLARE @var37 sysname;
    SELECT @var37 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DoctorSchedulingSettings]') AND [c].[name] = N'BufferAfterMinutes');
    IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [DoctorSchedulingSettings] DROP CONSTRAINT [' + @var37 + '];');
    ALTER TABLE [DoctorSchedulingSettings] DROP COLUMN [BufferAfterMinutes];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    DECLARE @var38 sysname;
    SELECT @var38 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DoctorSchedulingSettings]') AND [c].[name] = N'BufferBeforeMinutes');
    IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [DoctorSchedulingSettings] DROP CONSTRAINT [' + @var38 + '];');
    ALTER TABLE [DoctorSchedulingSettings] DROP COLUMN [BufferBeforeMinutes];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    DECLARE @var39 sysname;
    SELECT @var39 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DoctorSchedulingSettings]') AND [c].[name] = N'MaxBookingDaysAhead');
    IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [DoctorSchedulingSettings] DROP CONSTRAINT [' + @var39 + '];');
    ALTER TABLE [DoctorSchedulingSettings] DROP COLUMN [MaxBookingDaysAhead];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    DECLARE @var40 sysname;
    SELECT @var40 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DoctorSchedulingSettings]') AND [c].[name] = N'MinimumNoticeMinutes');
    IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [DoctorSchedulingSettings] DROP CONSTRAINT [' + @var40 + '];');
    ALTER TABLE [DoctorSchedulingSettings] DROP COLUMN [MinimumNoticeMinutes];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    DECLARE @var41 sysname;
    SELECT @var41 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DoctorSchedulingSettings]') AND [c].[name] = N'SlotIncrementMinutes');
    IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [DoctorSchedulingSettings] DROP CONSTRAINT [' + @var41 + '];');
    ALTER TABLE [DoctorSchedulingSettings] DROP COLUMN [SlotIncrementMinutes];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027034922_SimplifyDoctorScheduling'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027034922_SimplifyDoctorScheduling', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [Doctors] ADD [ClinicName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [Doctors] ADD [ProfessionalTitle] nvarchar(120) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [Doctors] ADD [YearsOfExperience] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [CoverPhotoUrl] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [FacebookUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Headline] nvarchar(160) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [InstagramUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Interests] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsProfilePublic] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    UPDATE u SET IsProfilePublic = 1 FROM AspNetUsers u INNER JOIN Doctors d ON d.Id = u.Id
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Languages] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [LinkedInUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Pronouns] nvarchar(40) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [TwitterUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [WebsiteUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [YouTubeUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    CREATE TABLE [UserCertificates] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(160) NOT NULL,
        [IssuingOrganization] nvarchar(160) NOT NULL,
        [IssueDate] date NOT NULL,
        [ExpiryDate] date NULL,
        [CredentialId] nvarchar(120) NULL,
        [CredentialUrl] nvarchar(500) NULL,
        [Description] nvarchar(1000) NULL,
        [ImageUrl] nvarchar(1000) NULL,
        CONSTRAINT [PK_UserCertificates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserCertificates_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    CREATE TABLE [UserQualifications] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Degree] nvarchar(160) NOT NULL,
        [Institution] nvarchar(200) NOT NULL,
        [FieldOfStudy] nvarchar(160) NULL,
        [StartYear] int NULL,
        [EndYear] int NULL,
        [Description] nvarchar(1000) NULL,
        CONSTRAINT [PK_UserQualifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserQualifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    CREATE INDEX [IX_UserCertificates_UserId] ON [UserCertificates] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    CREATE INDEX [IX_UserQualifications_UserId] ON [UserQualifications] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027055601_RichUserProfiles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027055601_RichUserProfiles', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027061830_ProfileContactInfo'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [ContactAddress] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027061830_ProfileContactInfo'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [ContactEmail] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027061830_ProfileContactInfo'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [ContactPhone] nvarchar(40) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027061830_ProfileContactInfo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027061830_ProfileContactInfo', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029134236_AddNotificationExtensions'
)
BEGIN
    ALTER TABLE [Notifications] ADD [DeduplicationKey] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029134236_AddNotificationExtensions'
)
BEGIN
    ALTER TABLE [Notifications] ADD [ReadAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029134236_AddNotificationExtensions'
)
BEGIN
    ALTER TABLE [Notifications] ADD [RelatedResourceId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029134236_AddNotificationExtensions'
)
BEGIN
    ALTER TABLE [Notifications] ADD [Title] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029134236_AddNotificationExtensions'
)
BEGIN
    CREATE INDEX [IX_Notifications_DeduplicationKey] ON [Notifications] ([DeduplicationKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029134236_AddNotificationExtensions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029134236_AddNotificationExtensions', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201638_AddChatRoomPrivacyAndScheduling'
)
BEGIN
    ALTER TABLE [ChatRooms] ADD [CoverImageUrl] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201638_AddChatRoomPrivacyAndScheduling'
)
BEGIN
    ALTER TABLE [ChatRooms] ADD [IsLocked] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201638_AddChatRoomPrivacyAndScheduling'
)
BEGIN
    ALTER TABLE [ChatRooms] ADD [PasswordHash] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201638_AddChatRoomPrivacyAndScheduling'
)
BEGIN
    ALTER TABLE [ChatRooms] ADD [ScheduledStartAtUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201638_AddChatRoomPrivacyAndScheduling'
)
BEGIN
    CREATE INDEX [IX_ChatRooms_IsArchived_IsLocked_LastMessageAtUtc] ON [ChatRooms] ([IsArchived], [IsLocked], [LastMessageAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201638_AddChatRoomPrivacyAndScheduling'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_ChatRooms_ScheduledStartAtUtc] ON [ChatRooms] ([ScheduledStartAtUtc]) WHERE [ScheduledStartAtUtc] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201638_AddChatRoomPrivacyAndScheduling'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029201638_AddChatRoomPrivacyAndScheduling', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201956_AddPatientNotesAndFollowUps'
)
BEGIN
    CREATE TABLE [FollowUps] (
        [Id] int NOT NULL IDENTITY,
        [DoctorId] nvarchar(450) NOT NULL,
        [PatientId] nvarchar(450) NOT NULL,
        [AppointmentId] int NULL,
        [Reason] nvarchar(200) NOT NULL,
        [Status] int NOT NULL,
        [DueAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ResolvedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_FollowUps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FollowUps_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]),
        CONSTRAINT [FK_FollowUps_AspNetUsers_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_FollowUps_AspNetUsers_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201956_AddPatientNotesAndFollowUps'
)
BEGIN
    CREATE TABLE [PatientNotes] (
        [Id] int NOT NULL IDENTITY,
        [DoctorId] nvarchar(450) NOT NULL,
        [PatientId] nvarchar(450) NOT NULL,
        [AppointmentId] int NULL,
        [Title] nvarchar(120) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PatientNotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PatientNotes_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]),
        CONSTRAINT [FK_PatientNotes_AspNetUsers_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_PatientNotes_AspNetUsers_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201956_AddPatientNotesAndFollowUps'
)
BEGIN
    CREATE INDEX [IX_FollowUps_AppointmentId] ON [FollowUps] ([AppointmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201956_AddPatientNotesAndFollowUps'
)
BEGIN
    CREATE INDEX [IX_FollowUps_DoctorId_Status_DueAtUtc] ON [FollowUps] ([DoctorId], [Status], [DueAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201956_AddPatientNotesAndFollowUps'
)
BEGIN
    CREATE INDEX [IX_FollowUps_PatientId_Status] ON [FollowUps] ([PatientId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201956_AddPatientNotesAndFollowUps'
)
BEGIN
    CREATE INDEX [IX_PatientNotes_AppointmentId] ON [PatientNotes] ([AppointmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201956_AddPatientNotesAndFollowUps'
)
BEGIN
    CREATE INDEX [IX_PatientNotes_DoctorId_CreatedAtUtc] ON [PatientNotes] ([DoctorId], [CreatedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201956_AddPatientNotesAndFollowUps'
)
BEGIN
    CREATE INDEX [IX_PatientNotes_PatientId_CreatedAtUtc] ON [PatientNotes] ([PatientId], [CreatedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029201956_AddPatientNotesAndFollowUps'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029201956_AddPatientNotesAndFollowUps', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029231725_AddPostAudienceAndMedia'
)
BEGIN
    ALTER TABLE [Posts] ADD [MediaJson] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029231725_AddPostAudienceAndMedia'
)
BEGIN
    ALTER TABLE [Posts] ADD [Visibility] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029231725_AddPostAudienceAndMedia'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029231725_AddPostAudienceAndMedia', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030031641_AddChatRoomReadTracking'
)
BEGIN
    ALTER TABLE [ChatRoomMembers] ADD [LastReadAtUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030031641_AddChatRoomReadTracking'
)
BEGIN
    UPDATE [ChatRoomMembers] SET [LastReadAtUtc] = [JoinedAtUtc] WHERE [LastReadAtUtc] IS NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030031641_AddChatRoomReadTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030031641_AddChatRoomReadTracking', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184444_AddPostDraftsGroupsAndMediaLedger'
)
BEGIN
    ALTER TABLE [Posts] ADD [IsDraft] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184444_AddPostDraftsGroupsAndMediaLedger'
)
BEGIN
    ALTER TABLE [Posts] ADD [TargetChatRoomId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184444_AddPostDraftsGroupsAndMediaLedger'
)
BEGIN
    CREATE TABLE [PostMediaAssets] (
        [Id] int NOT NULL IDENTITY,
        [Url] nvarchar(1000) NOT NULL,
        [PublicId] nvarchar(500) NOT NULL,
        [MediaType] nvarchar(16) NOT NULL,
        [OwnerId] nvarchar(450) NOT NULL,
        [PostId] int NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_PostMediaAssets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PostMediaAssets_AspNetUsers_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_PostMediaAssets_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184444_AddPostDraftsGroupsAndMediaLedger'
)
BEGIN
    CREATE INDEX [IX_Posts_IsDraft_PostedOn] ON [Posts] ([IsDraft], [PostedOn]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184444_AddPostDraftsGroupsAndMediaLedger'
)
BEGIN
    CREATE INDEX [IX_Posts_TargetChatRoomId] ON [Posts] ([TargetChatRoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184444_AddPostDraftsGroupsAndMediaLedger'
)
BEGIN
    CREATE INDEX [IX_Posts_Visibility_TargetChatRoomId] ON [Posts] ([Visibility], [TargetChatRoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184444_AddPostDraftsGroupsAndMediaLedger'
)
BEGIN
    CREATE INDEX [IX_PostMediaAssets_OwnerId_PostId_CreatedAtUtc] ON [PostMediaAssets] ([OwnerId], [PostId], [CreatedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184444_AddPostDraftsGroupsAndMediaLedger'
)
BEGIN
    CREATE INDEX [IX_PostMediaAssets_PostId] ON [PostMediaAssets] ([PostId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184444_AddPostDraftsGroupsAndMediaLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PostMediaAssets_Url] ON [PostMediaAssets] ([Url]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184444_AddPostDraftsGroupsAndMediaLedger'
)
BEGIN
    ALTER TABLE [Posts] ADD CONSTRAINT [FK_Posts_ChatRooms_TargetChatRoomId] FOREIGN KEY ([TargetChatRoomId]) REFERENCES [ChatRooms] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030184444_AddPostDraftsGroupsAndMediaLedger'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030184444_AddPostDraftsGroupsAndMediaLedger', N'8.0.0');
END;
GO

COMMIT;
GO

