CREATE TABLE [dbo].[Law_firm]
(
	[Law_firm_id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Type] VARCHAR(50) NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL
)
