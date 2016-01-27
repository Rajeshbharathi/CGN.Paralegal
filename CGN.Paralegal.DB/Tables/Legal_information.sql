CREATE TABLE [dbo].[Legal_information]
(
	[Legal_information_id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Legal_information_name] VARCHAR(50) NULL, 
    [Blogs] VARCHAR(MAX) NULL, 
    [Created_by] VARCHAR(50) NULL, 
    [Created_at] DATETIME NULL
)
