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
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [RoomTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Capacity] int NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        CONSTRAINT [PK_RoomTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RoomType_Capacity] CHECK ([Capacity] BETWEEN 1 AND 20)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [Services] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Unit] nvarchar(30) NOT NULL,
        [UnitPrice] decimal(18,0) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Services] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Service_Price] CHECK ([UnitPrice] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [GuestProfiles] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Phone] nvarchar(30) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [Notes] nvarchar(2000) NOT NULL,
        CONSTRAINT [PK_GuestProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GuestProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [Rooms] (
        [Id] int NOT NULL IDENTITY,
        [RoomNumber] nvarchar(20) NOT NULL,
        [RoomTypeId] int NOT NULL,
        [NightlyRate] decimal(18,0) NOT NULL,
        [ImagePath] nvarchar(500) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Amenities] nvarchar(1000) NOT NULL,
        [HousekeepingStatus] int NOT NULL,
        [OperationalStatus] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Room_Rate] CHECK ([NightlyRate] > 0),
        CONSTRAINT [FK_Rooms_RoomTypes_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [RoomTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [Reservations] (
        [Id] int NOT NULL IDENTITY,
        [ConfirmationCode] nvarchar(30) NOT NULL,
        [GuestProfileId] int NOT NULL,
        [RoomId] int NOT NULL,
        [ArrivalDate] date NOT NULL,
        [DepartureDate] date NOT NULL,
        [GuestCount] int NOT NULL,
        [NightlyRateSnapshot] decimal(18,0) NOT NULL,
        [Status] int NOT NULL,
        [Source] int NOT NULL,
        [CreatedByUserId] nvarchar(450) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CheckInAt] datetimeoffset NULL,
        [CheckOutAt] datetimeoffset NULL,
        [Notes] nvarchar(2000) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Reservations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Reservation_Dates] CHECK ([ArrivalDate] < [DepartureDate]),
        CONSTRAINT [CK_Reservation_Values] CHECK ([GuestCount] > 0 AND [NightlyRateSnapshot] > 0),
        CONSTRAINT [FK_Reservations_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reservations_GuestProfiles_GuestProfileId] FOREIGN KEY ([GuestProfileId]) REFERENCES [GuestProfiles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reservations_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [Folios] (
        [Id] int NOT NULL IDENTITY,
        [ReservationId] int NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ClosedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Folios] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Folios_Reservations_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [Reservations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [FolioItems] (
        [Id] int NOT NULL IDENTITY,
        [FolioId] int NOT NULL,
        [Kind] int NOT NULL,
        [ServiceId] int NULL,
        [DescriptionSnapshot] nvarchar(300) NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPriceSnapshot] decimal(18,0) NOT NULL,
        CONSTRAINT [PK_FolioItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_FolioItem_Kind] CHECK (([Kind] = 0 AND [ServiceId] IS NULL) OR ([Kind] = 1 AND [ServiceId] IS NOT NULL)),
        CONSTRAINT [CK_FolioItem_Values] CHECK ([Quantity] > 0 AND [UnitPriceSnapshot] >= 0),
        CONSTRAINT [FK_FolioItems_Folios_FolioId] FOREIGN KEY ([FolioId]) REFERENCES [Folios] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FolioItems_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [FolioId] int NOT NULL,
        [Amount] decimal(18,0) NOT NULL,
        [Method] int NOT NULL,
        [PaidAt] datetimeoffset NOT NULL,
        [ReceivedByUserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Payment_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_Payments_AspNetUsers_ReceivedByUserId] FOREIGN KEY ([ReceivedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Folios_FolioId] FOREIGN KEY ([FolioId]) REFERENCES [Folios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_FolioItems_FolioId] ON [FolioItems] ([FolioId]) WHERE [Kind] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE INDEX [IX_FolioItems_ServiceId] ON [FolioItems] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Folios_ReservationId] ON [Folios] ([ReservationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_GuestProfiles_UserId] ON [GuestProfiles] ([UserId]) WHERE [UserId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_FolioId] ON [Payments] ([FolioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE INDEX [IX_Payments_ReceivedByUserId] ON [Payments] ([ReceivedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Reservations_ConfirmationCode] ON [Reservations] ([ConfirmationCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE INDEX [IX_Reservations_CreatedByUserId] ON [Reservations] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE INDEX [IX_Reservations_GuestProfileId] ON [Reservations] ([GuestProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Reservations_RoomId] ON [Reservations] ([RoomId]) WHERE [Status] = 2');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE INDEX [IX_Reservations_RoomId_Status_ArrivalDate_DepartureDate] ON [Reservations] ([RoomId], [Status], [ArrivalDate], [DepartureDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Rooms_RoomNumber] ON [Rooms] ([RoomNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    CREATE INDEX [IX_Rooms_RoomTypeId] ON [Rooms] ([RoomTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013172545_InitialHotel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013172545_InitialHotel', N'10.0.11');
END;

COMMIT;
GO

