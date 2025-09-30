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
CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(128) NOT NULL,
    [Slug] nvarchar(128) NOT NULL,
    [ParentId] int NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_Categories_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Categories] ([Id])
);

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

CREATE INDEX [IX_Categories_ParentId] ON [Categories] ([ParentId]);

CREATE INDEX [IX_Posts_CategoryId] ON [Posts] ([CategoryId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250707154537_InitialCreate', N'9.0.5');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250914143027_QuestionsAndAnswersTablesExist', N'9.0.5');


                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__Questions__PostI__52593CB8]') AND parent_object_id = OBJECT_ID(N'[dbo].[Questions]'))
                BEGIN
                    ALTER TABLE [Questions] DROP CONSTRAINT [FK__Questions__PostI__52593CB8]
                END

ALTER TABLE [Questions] ADD CONSTRAINT [FK_Questions_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250914154140_AddCascadeDeleteForQuestions', N'9.0.5');

CREATE TABLE [Explanations] (
    [Id] int NOT NULL IDENTITY,
    [Text] nvarchar(500) NOT NULL,
    [QuestionId] int NOT NULL,
    CONSTRAINT [PK_Explanations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Explanations_Questions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [Questions] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Explanations_QuestionId] ON [Explanations] ([QuestionId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250915184015_AddExplanationsTable', N'9.0.5');

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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250919165127_AddEventsTable', N'9.0.5');

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

CREATE UNIQUE INDEX [IX_RelatedPosts_PostId_RelatedPostId] ON [RelatedPosts] ([PostId], [RelatedPostId]);

CREATE INDEX [IX_RelatedPosts_RelatedPostId] ON [RelatedPosts] ([RelatedPostId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250928130607_AddRelatedPostsTable', N'9.0.5');

COMMIT;
GO

