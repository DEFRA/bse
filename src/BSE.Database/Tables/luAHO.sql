CREATE TABLE [dbo].[luAHO] (
    [Code]        CHAR (2)     NOT NULL,
    [Name]        VARCHAR (50) NOT NULL,
    [BSERegionID] INT          NOT NULL,
    CONSTRAINT [PK_luAHO] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_luAHO_luBSERegion] FOREIGN KEY ([BSERegionID]) REFERENCES [dbo].[luBSERegion] ([ID])
);

