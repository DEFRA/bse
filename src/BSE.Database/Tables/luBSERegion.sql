CREATE TABLE [dbo].[luBSERegion] (
    [ID]        INT          NOT NULL,
    [SortOrder] SMALLINT     NOT NULL,
    [Name]      VARCHAR (50) NOT NULL,
    [CountryID] INT          NOT NULL,
    CONSTRAINT [PK_luBSERegion] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_luBSERegion_luCountry] FOREIGN KEY ([CountryID]) REFERENCES [dbo].[luCountry] ([ID])
);

