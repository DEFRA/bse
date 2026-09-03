CREATE TABLE [dbo].[luBreed] (
    [Code]            VARCHAR (20) NOT NULL,
    [FullName]        VARCHAR (50) NOT NULL,
    [AmalgamatedName] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luBreed] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

