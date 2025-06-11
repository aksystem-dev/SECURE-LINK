USE [SecureLink];
GO

-- 1) Vložení dat do tabulky UserTypes
INSERT INTO [dbo].[UserTypes] ([Name], [Description])
VALUES 
  ('Admin', 'Full access to the system'),
  ('Guest', 'Limited access'),
  ('Reader', 'Read-only access'),
  ('Writer', 'Write and read access');
GO

-- 2) Vložení uživatele do tabulky Users => heslo: test
  insert into Users (Username, PasswordHash, UserType, CreatedAt, DatabaseName)
  values ('EmailSMSGate', '$2a$11$N0LQB/FDkBSubBAA0alX6ehs0R1IhCB9wE81d.4TWvc.b8KshQg9q', 1, GETDATE(), 'DefaultConnection')
GO

-- 3) Vložení dummy dat do tabulky SecureLinkSettings
INSERT INTO [dbo].[SecureLinkSettings] ([EncryptedKey], [Message], [ExpirationDate], [DatabaseName])
VALUES 
  ('dummyEncryptedKey1', 'This is a secure link message.', DATEADD(DAY, 30, GETDATE()), 'DefaultConnection');
GO

-- 4) Vložení dummy dat do tabulky ActionOptions
-- Předpokládáme, že první vložený záznam do SecureLinkSettings má Id = 1.
INSERT INTO [dbo].[ActionOptions] ([SecureLinkSettingsId], [ActionType], [ButtonText], [SqlCommand])
VALUES 
  (1, 1, 'Confirm', 'UPDATE SomeTable SET Status = ''Confirmed'' WHERE Id = 1'),
  (1, 2, 'Reject', 'UPDATE SomeTable SET Status = ''Rejected'' WHERE Id = 1');
GO