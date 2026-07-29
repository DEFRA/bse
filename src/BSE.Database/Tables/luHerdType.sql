CREATE TABLE [dbo].[luHerdType] (
    [Code]        CHAR (1)     NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luHerdType] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

