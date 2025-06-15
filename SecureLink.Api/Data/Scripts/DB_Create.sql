/****************************************************
 * Vytvoření databáze SecureLink a přidruženého loginu
 ****************************************************/

IF DB_ID('SecureLink') IS NOT NULL
BEGIN
    ALTER DATABASE [SecureLink] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [SecureLink];
END
GO

-- Vytvoření databáze
CREATE DATABASE [SecureLink];
GO

-- Použití databáze SecureLink
USE [SecureLink];
GO

-- Vytvoření SQL loginu a databázového uživatele
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'SecureLinkLogin')
BEGIN
    CREATE LOGIN SecureLinkLogin WITH PASSWORD = 'SfNE9Wvz8fkhBxYR8Fl';
END
GO

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'SecureLinkLogin')
BEGIN
    CREATE USER SecureLinkLogin FOR LOGIN SecureLinkLogin;
    ALTER ROLE db_owner ADD MEMBER SecureLinkLogin;
END
GO


-- Tabulka UserTypes
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserTypes](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](50) NOT NULL,
    [Description] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
    [Id] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF,
       ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
    [Name] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF,
       ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- Tabulka Users
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Username] [nvarchar](128) NOT NULL,
    [PasswordHash] [nvarchar](256) NOT NULL,
    [UserType] [int] NOT NULL,
    [CreatedAt] [datetime] NOT NULL,
    [LastLoginAt] [datetime] NULL,
    [IsBlocked] [bit] NOT NULL,
    [FailedAttempts] [int] NOT NULL,
    [DatabaseName] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF,
       IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON,
       ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Users] ADD CONSTRAINT [DF_Users_CreatedAt] DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Users] ADD DEFAULT ((0)) FOR [IsBlocked]
GO
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [DF_Users_FailedAttempts] DEFAULT ((0)) FOR [FailedAttempts]
GO

ALTER TABLE [dbo].[Users] WITH CHECK ADD CONSTRAINT [FK_Users_UserTypes] FOREIGN KEY([UserType])
REFERENCES [dbo].[UserTypes] ([Id])
GO

ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_UserTypes]
GO

-- Tabulka LoginAttempts
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LoginAttempts](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Username] [nvarchar](100) NOT NULL,
    [AttemptTime] [datetime] NOT NULL,
    [IpAddress] [nvarchar](50) NULL,
    [ClientIPAddress] [nvarchar](50) NULL,
    [Success] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
    [Id] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF,
       IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON,
       ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[LoginAttempts] ADD CONSTRAINT [DF_LoginAttempts_Success] DEFAULT ((0)) FOR [Success]
GO

-- Tabulka KeyAssigments
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[KeyAssigments](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Username] [nvarchar](100) NOT NULL,
    [IpAddress] [nvarchar](45) NULL,
    [KeyUsed] [nvarchar](256) NOT NULL,
    [Nonce] [varchar](200) NULL,
    [CreatedAt] [datetime] NULL,
 CONSTRAINT [PK_KeyAssigments] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF,
       IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON,
       ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[KeyAssigments] ADD CONSTRAINT [DF_KeyAssigments_CreatedAt] DEFAULT (getdate()) FOR [CreatedAt]
GO

-- Tabulka JwtKeys
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JwtKeys](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [KeyValue] [nvarchar](256) NOT NULL,
    [KeyType] [int] NOT NULL,
    [IsActive] [bit] NOT NULL,
    [ValidFrom] [datetime] NOT NULL,
    [ExpiresAt] [datetime] NOT NULL,
 CONSTRAINT [PK_JwtKeys] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF,
       IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON,
       ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[JwtKeys] ADD CONSTRAINT [DF_JwtKeys_IsActive] DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[JwtKeys] ADD CONSTRAINT [DF_JwtKeys_ValidFrom] DEFAULT (getdate()) FOR [ValidFrom]
GO

-- Tabulka BlockedIPs
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BlockedIPs](
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [IPAddress] [nvarchar](45) NOT NULL,
    [Blocked] [datetime] NOT NULL,
    [BlockedUntil] [datetime] NOT NULL,
 CONSTRAINT [PK_BlockedIPs] PRIMARY KEY CLUSTERED 
(
    [ID] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF,
       IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON,
       ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[BlockedIPs] ADD CONSTRAINT [DF_BlockedIPs_Blocked] DEFAULT (getdate()) FOR [Blocked]
GO


-- Tabulka SecureLinkSettings: konfigurace šifrovaného odkazu
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SecureLinkSettings](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [EncryptedKey] [nvarchar](256) NOT NULL,
    [Message] [nvarchar](500) NOT NULL,
    [ExpirationDate] [datetime] NOT NULL,
    [ShowCommentBox] [bit] NOT NULL DEFAULT (0),
    [Processed] [bit] NOT NULL DEFAULT (0),
    [DatabaseName] [nvarchar](200) NOT NULL,
    CONSTRAINT [PK_SecureLinkSettings] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    ) WITH (
        PAD_INDEX = OFF, 
        STATISTICS_NORECOMPUTE = OFF,
        IGNORE_DUP_KEY = OFF, 
        ALLOW_ROW_LOCKS = ON,
        ALLOW_PAGE_LOCKS = ON, 
        OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
    ) ON [PRIMARY]
) ON [PRIMARY]
GO


-- Tabulka ActionOptions: definice akcí (tlačítek) v rámci odkazu
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ActionOptions](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [SecureLinkSettingsId] [int] NOT NULL,
    [ActionType] [int] NOT NULL,
    [ButtonText] [nvarchar](100) NOT NULL,
    [SqlCommand] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_ActionOptions] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF,
       IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON,
       ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ActionOptions] WITH CHECK ADD CONSTRAINT [FK_ActionOptions_SecureLinkSettings] FOREIGN KEY([SecureLinkSettingsId])
REFERENCES [dbo].[SecureLinkSettings] ([Id])
GO

ALTER TABLE [dbo].[ActionOptions] CHECK CONSTRAINT [FK_ActionOptions_SecureLinkSettings]
GO

-- Tabulka SecureLinkLogs: logování akcí v rámci odkazu
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SecureLinkLogs] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,  -- Unikátní ID záznamu
    [EncryptedKey] NVARCHAR(500) NOT NULL,        -- Šifrovaný klíč, který se validuje
    [RequestType] NVARCHAR(50) NOT NULL,          -- Typ požadavku ('Validate' / 'Confirm')
    [ClientIPAddress] NVARCHAR(50) NULL,          -- IP adresa klienta
    [Timestamp] DATETIME NOT NULL DEFAULT GETDATE(), -- Čas požadavku
    [IsSuccess] BIT NOT NULL DEFAULT 0,           -- Indikace, zda byla operace úspěšná
    [Message] NVARCHAR(1000) NULL                 -- Volitelná zpráva (chyba nebo výsledek)
)
GO

-- Tabulka FailedValidations: záznamy o neúspěšných validacích
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FailedValidations] (
    [Id]            INT IDENTITY(1,1) PRIMARY KEY,
    [ClientIPAddress] NVARCHAR(50) NOT NULL UNIQUE, -- Unikátní záznam pro každou IP
    [FailedCount]   INT NOT NULL DEFAULT 1,         -- Počet neúspěšných pokusů
    [LastAttempt]   DATETIME NOT NULL DEFAULT GETDATE(), -- Poslední neúspěšný pokus
    [Reason]        NVARCHAR(255) NULL              -- Volitelná zpráva s důvodem
);
GO


-- Tabulka BlockedIPs: záznamy o zablokovaných IP adresách
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BlockedIPs](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[IPAddress] [nvarchar](45) NOT NULL,
	[Blocked] [datetime] NOT NULL,
	[BlockedUntil] [datetime] NOT NULL,
 CONSTRAINT [PK_BlockedIPs] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[BlockedIPs] ADD  CONSTRAINT [DF_BlockedIPs_Blocked]  DEFAULT (getdate()) FOR [Blocked]
GO

-- Vytvoreni dat v tabulce UserTypes
INSERT INTO [dbo].[UserTypes] ([Name], [Description])
VALUES 
  ('Admin', 'Full access to the system'),
  ('Guest', 'Limited access'),
  ('Reader', 'Read-only access'),
  ('Writer', 'Write and read access');
GO

-- Vložení uživatele do tabulky Users => heslo: 57EoB8Vwrn4lewRoxcil9BP4yACLek43HSEc0O
  insert into Users (Username, PasswordHash, UserType, CreatedAt, DatabaseName)
  values ('EmailSMSGate', '$2a$11$N0LQB/FDkBSubBAA0alX6ehs0R1IhCB9wE81d.4TWvc.b8KshQg9q', 1, GETDATE(), 'DefaultConnection')
GO

-- Optional: Audit DB uživatele pro SecureLink

USE [SecureLink]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SecureUserAuditLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EventTime] [datetime] NULL,
	[EventData] [xml] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[SecureUserAuditLog] ADD  DEFAULT (getdate()) FOR [EventTime]
GO

USE [master];
GO

-- Vytvoření nového triggeru
CREATE TRIGGER trg_LogAlterLogin_SecureLink
ON ALL SERVER
FOR ALTER_LOGIN
AS
BEGIN
    DECLARE @login NVARCHAR(255);
    SET @login = EVENTDATA().value('(/EVENT_INSTANCE/ObjectName)[1]', 'NVARCHAR(255)');

    IF @login = 'SecureLinkLogin'
    BEGIN
        INSERT INTO [SecureLink].[dbo].[SecureUserAuditLog] (EventData)
        VALUES (EVENTDATA());
    END
END;
GO
