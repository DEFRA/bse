CREATE TABLE [dbo].[luPedigreeType] (
    [Code]        CHAR (2)     NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luPedigree] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

