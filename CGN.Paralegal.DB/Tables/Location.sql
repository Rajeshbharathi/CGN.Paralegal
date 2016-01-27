CREATE TABLE [dbo].[Location]
(
	[Location_id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Location_name] VARCHAR(50) NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL
)
