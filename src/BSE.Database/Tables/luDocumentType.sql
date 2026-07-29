CREATE TABLE [dbo].[luDocumentType] (
    [Code]        VARCHAR (5)  NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luDocumentType] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

