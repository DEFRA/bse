CREATE TABLE [dbo].[luHorizontalRisk] (
    [Code]        VARCHAR (10) NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luHorizontalRisk] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

