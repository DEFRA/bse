CREATE TABLE [dbo].[luBSEForm] (
    [Code]        CHAR (2)     NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luBSEForm] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

