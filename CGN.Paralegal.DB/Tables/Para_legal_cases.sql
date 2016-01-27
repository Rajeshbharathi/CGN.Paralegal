CREATE TABLE [dbo].[Para_legal_cases]
(
	[Para_legal_case_id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Para_legal_id] INT NULL, 
    [case_id] INT NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL, 
    CONSTRAINT [FK_Para_legal_details] FOREIGN KEY ([Para_legal_id]) REFERENCES [Para_legal]([Para_legal_id]), 
    CONSTRAINT [FK_Para_legal_cases] FOREIGN KEY ([case_id]) REFERENCES [Cases]([case_id])
)
