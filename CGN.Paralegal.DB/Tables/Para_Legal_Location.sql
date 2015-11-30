CREATE TABLE [dbo].[Para_Legal_Location]
(
	[Para_Legal_Location_id] INT NOT NULL PRIMARY KEY, 
    [Para_legal_id] INT NULL, 
    [location_id] INT NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL, 
    CONSTRAINT [FK_Para_Legal_ID] FOREIGN KEY ([Para_legal_id]) REFERENCES [Para_legal]([Para_legal_id]), 
    CONSTRAINT [FK_Para_Legal_Location] FOREIGN KEY ([location_id]) REFERENCES [Location]([location_id])
)
