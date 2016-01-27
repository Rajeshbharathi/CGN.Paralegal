CREATE TABLE [dbo].[Cases]
(
	[Case_id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Case_description] VARCHAR(50) NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL
)
