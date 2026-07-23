CREATE TABLE [dbo].[luCounty] (
    [County]    CHAR (2)     NOT NULL,
    [Name]      VARCHAR (50) NOT NULL,
    [CountryID] INT          NOT NULL,
    CONSTRAINT [PK_luCountyCode] PRIMARY KEY CLUSTERED ([County] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_luCounty_luCountry] FOREIGN KEY ([CountryID]) REFERENCES [dbo].[luCountry] ([ID])
);

