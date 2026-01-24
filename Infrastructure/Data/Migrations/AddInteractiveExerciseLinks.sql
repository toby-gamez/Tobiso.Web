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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250707154537_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(128) NOT NULL,
        [Slug] nvarchar(128) NOT NULL,
        [ParentId] int NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Categories_Categories_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Categories] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250707154537_InitialCreate'
)
BEGIN
    CREATE TABLE [Posts] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [FilePath] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CategoryId] int NULL,
        CONSTRAINT [PK_Posts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Posts_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250707154537_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Categories_ParentId] ON [Categories] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250707154537_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Posts_CategoryId] ON [Posts] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250707154537_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250707154537_InitialCreate', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250914143027_QuestionsAndAnswersTablesExist'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250914143027_QuestionsAndAnswersTablesExist', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250914154140_AddCascadeDeleteForQuestions'
)
BEGIN

                    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__Questions__PostI__52593CB8]') AND parent_object_id = OBJECT_ID(N'[dbo].[Questions]'))
                    BEGIN
                        ALTER TABLE [Questions] DROP CONSTRAINT [FK__Questions__PostI__52593CB8]
                    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250914154140_AddCascadeDeleteForQuestions'
)
BEGIN
    ALTER TABLE [Questions] ADD CONSTRAINT [FK_Questions_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250914154140_AddCascadeDeleteForQuestions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250914154140_AddCascadeDeleteForQuestions', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250915184015_AddExplanationsTable'
)
BEGIN
    CREATE TABLE [Explanations] (
        [Id] int NOT NULL IDENTITY,
        [Text] nvarchar(500) NOT NULL,
        [QuestionId] int NOT NULL,
        CONSTRAINT [PK_Explanations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Explanations_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250915184015_AddExplanationsTable'
)
BEGIN
    CREATE INDEX [IX_Explanations_QuestionId] ON [Explanations] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250915184015_AddExplanationsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250915184015_AddExplanationsTable', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250919165127_AddEventsTable'
)
BEGIN
    CREATE TABLE [Events] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [IsAllDay] bit NOT NULL,
        [Location] nvarchar(200) NULL,
        [Color] nvarchar(7) NOT NULL DEFAULT N'#007bff',
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
        [UpdatedAt] datetime2 NULL,
        [IsRecurring] bit NOT NULL,
        [RecurrencePattern] nvarchar(50) NULL,
        [RecurrenceEndDate] datetime2 NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250919165127_AddEventsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250919165127_AddEventsTable', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250928130607_AddRelatedPostsTable'
)
BEGIN
    CREATE TABLE [RelatedPosts] (
        [Id] int NOT NULL IDENTITY,
        [PostId] int NOT NULL,
        [RelatedPostId] int NOT NULL,
        [Text] nvarchar(500) NULL,
        CONSTRAINT [PK_RelatedPosts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RelatedPost_DifferentPosts] CHECK ([PostId] <> [RelatedPostId]),
        CONSTRAINT [FK_RelatedPosts_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RelatedPosts_Posts_RelatedPostId] FOREIGN KEY ([RelatedPostId]) REFERENCES [Posts] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250928130607_AddRelatedPostsTable'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RelatedPosts_PostId_RelatedPostId] ON [RelatedPosts] ([PostId], [RelatedPostId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250928130607_AddRelatedPostsTable'
)
BEGIN
    CREATE INDEX [IX_RelatedPosts_RelatedPostId] ON [RelatedPosts] ([RelatedPostId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250928130607_AddRelatedPostsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250928130607_AddRelatedPostsTable', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251207122019_AddAddendumTable'
)
BEGIN
    CREATE TABLE [Addendums] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Addendums] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251207122019_AddAddendumTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251207122019_AddAddendumTable', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260105164108_EditFix'
)
BEGIN
    EXEC sp_rename N'[Posts].[UpdatedAt]', N'LastFix', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260105164108_EditFix'
)
BEGIN
    ALTER TABLE [Posts] ADD [LastEdit] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260105164108_EditFix'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260105164108_EditFix', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121194202_AddFeedback'
)
BEGIN
    CREATE TABLE [Feedbacks] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsRead] bit NOT NULL,
        CONSTRAINT [PK_Feedbacks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121194202_AddFeedback'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260121194202_AddFeedback', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121200635_AddPlatformToFeedback'
)
BEGIN
    ALTER TABLE [Feedbacks] ADD [Platform] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121200635_AddPlatformToFeedback'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260121200635_AddPlatformToFeedback', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122142613_AddInteractiveExercise'
)
BEGIN
    CREATE TABLE [InteractiveExercises] (
        [Id] int NOT NULL IDENTITY,
        [PostId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [ConfigJson] nvarchar(max) NOT NULL,
        [SolutionJson] nvarchar(max) NOT NULL,
        [InstructionsMarkdown] nvarchar(max) NULL,
        [OrderIndex] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_InteractiveExercises] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InteractiveExercises_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122142613_AddInteractiveExercise'
)
BEGIN
    CREATE INDEX [IX_InteractiveExercises_PostId_OrderIndex] ON [InteractiveExercises] ([PostId], [OrderIndex]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260122142613_AddInteractiveExercise'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260122142613_AddInteractiveExercise', N'9.0.5');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124190543_AddInteractiveExerciseLinks'
)
BEGIN
    ALTER TABLE [InteractiveExercises] DROP CONSTRAINT [FK_InteractiveExercises_Posts_PostId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124190543_AddInteractiveExerciseLinks'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InteractiveExercises]') AND [c].[name] = N'PostId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [InteractiveExercises] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [InteractiveExercises] ALTER COLUMN [PostId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124190543_AddInteractiveExerciseLinks'
)
BEGIN
    CREATE TABLE [InteractiveExerciseCategories] (
        [InteractiveExerciseId] int NOT NULL,
        [CategoryId] int NOT NULL,
        CONSTRAINT [PK_InteractiveExerciseCategories] PRIMARY KEY ([InteractiveExerciseId], [CategoryId]),
        CONSTRAINT [FK_InteractiveExerciseCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InteractiveExerciseCategories_InteractiveExercises_InteractiveExerciseId] FOREIGN KEY ([InteractiveExerciseId]) REFERENCES [InteractiveExercises] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124190543_AddInteractiveExerciseLinks'
)
BEGIN
    CREATE TABLE [InteractiveExercisePosts] (
        [InteractiveExerciseId] int NOT NULL,
        [PostId] int NOT NULL,
        CONSTRAINT [PK_InteractiveExercisePosts] PRIMARY KEY ([InteractiveExerciseId], [PostId]),
        CONSTRAINT [FK_InteractiveExercisePosts_InteractiveExercises_InteractiveExerciseId] FOREIGN KEY ([InteractiveExerciseId]) REFERENCES [InteractiveExercises] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InteractiveExercisePosts_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124190543_AddInteractiveExerciseLinks'
)
BEGIN
    CREATE INDEX [IX_InteractiveExerciseCategories_CategoryId] ON [InteractiveExerciseCategories] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124190543_AddInteractiveExerciseLinks'
)
BEGIN
    CREATE INDEX [IX_InteractiveExercisePosts_PostId] ON [InteractiveExercisePosts] ([PostId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124190543_AddInteractiveExerciseLinks'
)
BEGIN
    ALTER TABLE [InteractiveExercises] ADD CONSTRAINT [FK_InteractiveExercises_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260124190543_AddInteractiveExerciseLinks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260124190543_AddInteractiveExerciseLinks', N'9.0.5');
END;

COMMIT;
GO

