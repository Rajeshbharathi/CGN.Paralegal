CREATE TABLE [dbo].[Case_Area_of_law]
(
	[Case_area_of_law_id] INT NOT NULL PRIMARY KEY, 
    [case_id] INT NULL, 
    [law_id] INT NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL, 
    CONSTRAINT [FK_Case_Area_of_law] FOREIGN KEY ([case_id]) REFERENCES [Cases]([case_id]), 
    CONSTRAINT [FK_Case_Area_of_law_location] FOREIGN KEY ([law_id]) REFERENCES [Area_of_law]([law_id])
)
