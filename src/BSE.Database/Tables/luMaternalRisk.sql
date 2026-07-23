CREATE TABLE [dbo].[luMaternalRisk] (
    [Code]        VARCHAR (10) NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luMaternalRisk] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

