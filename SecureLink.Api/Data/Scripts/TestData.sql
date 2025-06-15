USE [SecureLink];
GO

-- Vložení dummy dat do tabulky SecureLinkSettings
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
