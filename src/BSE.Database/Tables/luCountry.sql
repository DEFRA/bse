CREATE TABLE [dbo].[luCountry] (
    [ID]          INT          NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luCountry] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90)
);

