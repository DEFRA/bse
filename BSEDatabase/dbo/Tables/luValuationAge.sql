CREATE TABLE [dbo].[luValuationAge] (
    [Code]        CHAR (1)     NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luValuationAge] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

