CREATE TABLE [dbo].[Paralegal_Comment](
	[CommentID] [bigint] IDENTITY(1,1) NOT NULL,
	[Para_legal_id] INT NOT NULL,
	[CommentText] [varchar](max) NULL,
	[CreatedBy] [varchar](50) NULL,
	[CreatedDate] [datetime] NULL,
	[LastUpdatedBy] [varchar](50) NULL,
	[LastUpdatedDate] [datetime] NULL, 
    CONSTRAINT [FK_Paralegal_Comment_Para_legal] FOREIGN KEY (Para_legal_id) REFERENCES [Para_legal](Para_legal_id)
) 