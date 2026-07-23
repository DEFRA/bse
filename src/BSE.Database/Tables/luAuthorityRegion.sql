CREATE TABLE [dbo].[luAuthorityRegion] (
    [ID]        INT          NOT NULL,
    [Region]    VARCHAR (50) NOT NULL,
    [CountryID] INT          NOT NULL,
    CONSTRAINT [PK_luAuthorityRegion] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_luAuthorityRegion_luCountry] FOREIGN KEY ([CountryID]) REFERENCES [dbo].[luCountry] ([ID])
);

