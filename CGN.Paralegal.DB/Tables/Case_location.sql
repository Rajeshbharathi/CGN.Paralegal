CREATE TABLE [dbo].[Case_location]
(
	[case_location_id] INT NOT NULL PRIMARY KEY, 
    [case_id] INT NULL, 
    [location_id] INT NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL, 
    CONSTRAINT [FK_Case_location_ID] FOREIGN KEY ([case_id]) REFERENCES [Cases]([case_id]), 
    CONSTRAINT [FK_Case_location] FOREIGN KEY ([location_id]) REFERENCES [Location]([location_id])
)
