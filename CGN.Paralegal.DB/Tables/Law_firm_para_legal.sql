CREATE TABLE [dbo].[Law_firm_para_legal]
(
	[Law_firm_para_legal_id] INT NOT NULL PRIMARY KEY, 
    [Law_firm_id] INT NULL, 
    [Para_legal_id] INT NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL, 
    CONSTRAINT [FK_Law_firm_para_legal_ID] FOREIGN KEY ([Para_legal_id]) REFERENCES [Para_legal]([Para_legal_id]), 
    CONSTRAINT [FK_Law_firm_para_legal] FOREIGN KEY ([Law_firm_id]) REFERENCES [Law_firm]([Law_firm_id])
)
