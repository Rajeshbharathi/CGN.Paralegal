CREATE TABLE [dbo].[Para_Legal_Law_Area]
(
	[Para_Legal_Law_Area_id] INT NOT NULL PRIMARY KEY, 
    [Para_legal_id] INT NULL, 
    [Law_id] INT NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL, 
    CONSTRAINT [FK_Para_Legal] FOREIGN KEY ([Para_legal_id]) REFERENCES [Para_legal]([Para_legal_id]), 
    CONSTRAINT [FK_Para_Legal_Law_Area] FOREIGN KEY ([Law_id]) REFERENCES [Area_Of_law]([Law_id])
)
