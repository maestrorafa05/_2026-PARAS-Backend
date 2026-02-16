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
CREATE TABLE [Rooms] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Location] nvarchar(200) NULL,
    [Capacity] int NOT NULL,
    [Facilities] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id])
);

CREATE TABLE [Loans] (
    [Id] uniqueidentifier NOT NULL,
    [RoomId] uniqueidentifier NOT NULL,
    [NamaPeminjam] nvarchar(100) NOT NULL,
    [NIM] nvarchar(20) NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Loans] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Loans_EndTime_After_StartTime] CHECK ([EndTime] > [StartTime]),
    CONSTRAINT [FK_Loans_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoanStatusHistories] (
    [Id] uniqueidentifier NOT NULL,
    [LoanId] uniqueidentifier NOT NULL,
    [FromStatus] int NOT NULL,
    [ToStatus] int NOT NULL,
    [ChangedBy] nvarchar(100) NULL,
    [Comment] nvarchar(300) NULL,
    [ChangedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_LoanStatusHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoanStatusHistories_Loans_LoanId] FOREIGN KEY ([LoanId]) REFERENCES [Loans] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Loans_RoomId_StartTime_EndTime] ON [Loans] ([RoomId], [StartTime], [EndTime]);

CREATE INDEX [IX_LoanStatusHistories_LoanId_ChangedAt] ON [LoanStatusHistories] ([LoanId], [ChangedAt]);

CREATE UNIQUE INDEX [IX_Rooms_Code] ON [Rooms] ([Code]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260215040511_InitalCreate', N'10.0.3');

COMMIT;
GO

