CREATE TABLE [dbo].[Para_legal]
(
	[Para_legal_id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Para_legal_name] VARCHAR(50) NULL, 
    [Short desc] VARCHAR(50) NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL
)
