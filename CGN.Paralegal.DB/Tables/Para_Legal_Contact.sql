CREATE TABLE [dbo].[Para_Legal_Contact]
(
	[Para_legal_Contact_id] INT NOT NULL PRIMARY KEY, 
    [Para_legal_id] INT NULL, 
    [Email_id] VARCHAR(50) NULL, 
    [Phone] INT NULL, 
    [Fax] VARCHAR(50) NULL, 
    [Address] VARCHAR(50) NULL, 
    [Website] VARCHAR(50) NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL, 
    CONSTRAINT [FK_Para_Legal_Contact] FOREIGN KEY ([Para_legal_id]) REFERENCES [Para_legal]([Para_legal_id])
)
