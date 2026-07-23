CREATE TABLE [dbo].[luParish] (
    [County]       CHAR (2)     NOT NULL,
    [Parish]       CHAR (3)     NOT NULL,
    [Name]         VARCHAR (50) NULL,
    [ADNSRegionID] INT          NULL,
    [BSECounty]    VARCHAR (15) NULL,
    CONSTRAINT [PK_luParish] PRIMARY KEY CLUSTERED ([County] ASC, [Parish] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_luParish_luADNSRegion] FOREIGN KEY ([ADNSRegionID]) REFERENCES [dbo].[luADNSRegion] ([ID]),
    CONSTRAINT [FK_luParish_luBSECounty] FOREIGN KEY ([BSECounty]) REFERENCES [dbo].[luBSECounty] ([Code]),
    CONSTRAINT [FK_luParish_luCounty] FOREIGN KEY ([County]) REFERENCES [dbo].[luCounty] ([County])
);


GO
CREATE NONCLUSTERED INDEX [IX_luParish_ADNSRegionID]
    ON [dbo].[luParish]([ADNSRegionID] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_luParish_BSECounty]
    ON [dbo].[luParish]([BSECounty] ASC) WITH (FILLFACTOR = 90);

