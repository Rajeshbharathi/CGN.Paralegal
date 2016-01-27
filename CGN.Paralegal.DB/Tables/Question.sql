CREATE TABLE [dbo].[Question]
(
	[Question_id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Question] VARCHAR(200) NULL, 
    [Legal_information_id] INT NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL, 
    [Last_Updated_by] VARCHAR(50) NULL, 
    [Last_Updated_at] DATETIME NULL, 
    CONSTRAINT [FK_Question_Legal_info] FOREIGN KEY ([Legal_information_id]) REFERENCES [Legal_information]([Legal_information_id]) 
)
