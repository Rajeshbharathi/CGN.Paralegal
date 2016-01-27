CREATE TABLE [dbo].[Area_of_Law]
(
	[Law_id] INT NOT NULL Identity(1,1) PRIMARY KEY , 
    [Law_name] VARCHAR(50) NULL, 
    [Created by] VARCHAR(50) NULL, 
    [Created at] DATETIME NULL
)
