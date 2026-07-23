CREATE TABLE [dbo].[luAnimalStatus] (
    [Code]        VARCHAR (9)  NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luAnimalStatus] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

