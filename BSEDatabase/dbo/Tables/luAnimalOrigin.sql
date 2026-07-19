CREATE TABLE [dbo].[luAnimalOrigin] (
    [Code]        CHAR (1)     NOT NULL,
    [Description] VARCHAR (10) NOT NULL,
    CONSTRAINT [PK_luAnimalOrigin] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

