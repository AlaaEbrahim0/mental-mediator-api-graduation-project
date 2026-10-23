using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncSomeeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AspNetUsers: profile fields the community and doctor UIs read.
            // Biography and City were moved off Doctors by
            // ExpandDoctorProfileCommunity, which the hosted database never
            // applied, so those columns are still on Doctors there.
            migrationBuilder.Sql(@"
                IF COL_LENGTH('AspNetUsers', 'Biography') IS NULL
                    ALTER TABLE [AspNetUsers] ADD [Biography] nvarchar(max) NULL;
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('AspNetUsers', 'City') IS NULL
                    ALTER TABLE [AspNetUsers] ADD [City] nvarchar(100) NULL;
            ");

            // Posts: the new post attributes introduced by
            // ExpandDoctorProfileCommunity. These are the columns whose absence
            // made GET /api/posts return 500 - the query selected them
            // unconditionally.
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Posts', 'Category') IS NULL
                    ALTER TABLE [Posts] ADD [Category] nvarchar(100) NULL;
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('Posts', 'Tags') IS NULL
                    ALTER TABLE [Posts] ADD [Tags] nvarchar(1000) NULL;
            ");

            // PostLikes: the table backing the like feature. Guarded so a
            // database that already has it is left untouched.
            migrationBuilder.Sql(@"
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
            ");

            // Foreign keys are added separately so a partially-created table
            // from an earlier attempt is still completed correctly.
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[PostLikes]', N'U') IS NOT NULL
                   AND OBJECT_ID(N'[FK_PostLikes_Posts_PostId]', N'F') IS NULL
                BEGIN
                    ALTER TABLE [PostLikes] WITH NOCHECK ADD
                        CONSTRAINT [FK_PostLikes_Posts_PostId] FOREIGN KEY ([PostId])
                        REFERENCES [Posts] ([Id]) ON DELETE CASCADE;
                END
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[PostLikes]', N'U') IS NOT NULL
                   AND OBJECT_ID(N'[FK_PostLikes_AspNetUsers_UserId]', N'F') IS NULL
                BEGIN
                    ALTER TABLE [PostLikes] WITH NOCHECK ADD
                        CONSTRAINT [FK_PostLikes_AspNetUsers_UserId] FOREIGN KEY ([UserId])
                        REFERENCES [AspNetUsers] ([Id]);
                END
            ");

            // One like per user per post.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = N'IX_PostLikes_UserId_PostId'
                                 AND object_id = OBJECT_ID(N'[PostLikes]'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_PostLikes_UserId_PostId]
                        ON [PostLikes] ([UserId], [PostId]);
                END
            ");

            // Copy the doctor profile values that still live on Doctors into
            // the columns the model now expects on AspNetUsers. Only legacy
            // databases that never ran ExpandDoctorProfileCommunity have
            // those columns on Doctors; on a database where all migrations
            // applied in order they were already moved, and the copy must
            // not run (Invalid column name otherwise).
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Doctors', 'Biography') IS NOT NULL
                   AND COL_LENGTH('AspNetUsers', 'Biography') IS NOT NULL
                BEGIN
                    UPDATE u
                    SET u.Biography = d.Biography,
                        u.City      = d.City
                    FROM [AspNetUsers] u
                    INNER JOIN [Doctors] d ON d.Id = u.Id
                    WHERE (u.Biography IS NULL AND d.Biography IS NOT NULL)
                       OR (u.City IS NULL AND d.City IS NOT NULL);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. This migration only adds nullable columns
            // and one table, so a rollback would be destructive for no
            // benefit; the added columns are safe to leave in place.
        }
    }
}
