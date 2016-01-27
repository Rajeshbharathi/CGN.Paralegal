CREATE TABLE [dbo].[Answer]
(
	[Answer_id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Answer] VARCHAR(MAX) NULL, 
    [Question_id] INT NULL, 
    [Legal_information_id] INT NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL, 
    [Last_Updated_by] VARCHAR(50) NULL, 
    [Last_Updated_at] DATETIME NULL, 
    CONSTRAINT [FK_Answer_Legal_info] FOREIGN KEY ([Legal_information_id]) REFERENCES [Legal_information]([Legal_information_id]), 
    CONSTRAINT [FK_Answer_Question] FOREIGN KEY ([Question_id]) REFERENCES [Question]([Question_id])
)
