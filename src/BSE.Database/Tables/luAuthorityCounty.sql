CREATE TABLE [dbo].[luAuthorityCounty] (
    [ID]                INT          NOT NULL,
    [County]            VARCHAR (50) NOT NULL,
    [AuthorityRegionID] INT          NOT NULL,
    CONSTRAINT [PK_luAuthorityCounty] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_luAuthorityCounty_luAuthorityRegion] FOREIGN KEY ([AuthorityRegionID]) REFERENCES [dbo].[luAuthorityRegion] ([ID])
);

