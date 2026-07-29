CREATE TABLE [dbo].[luBSECounty] (
    [ID]          CHAR (2)     NULL,
    [Code]        VARCHAR (15) NOT NULL,
    [Description] VARCHAR (50) NULL,
    [BSERegionID] INT          NULL,
    CONSTRAINT [PK_luCounty] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_luCounty_ID] CHECK ([ID] like '[0-9][0-9]'),
    CONSTRAINT [FK_luBSECounty_luBSERegion] FOREIGN KEY ([BSERegionID]) REFERENCES [dbo].[luBSERegion] ([ID])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_luCounty_Code]
    ON [dbo].[luBSECounty]([Code] ASC) WITH (FILLFACTOR = 90);

