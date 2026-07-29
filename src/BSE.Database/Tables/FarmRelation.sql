CREATE TABLE [dbo].[FarmRelation] (
    [ID]          INT        IDENTITY (1, 1) NOT NULL,
    [CPHH]        CHAR (11)  NOT NULL,
    [RelatedCPHH] CHAR (11)  NOT NULL,
    [RowStamp]    ROWVERSION NOT NULL,
    CONSTRAINT [PK_FarmRelation] PRIMARY KEY NONCLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_FarmLink_CPHH] CHECK ([CPHH] like '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [CK_FarmLink_RelatedCPHH] CHECK ([RelatedCPHH] like '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [FK_FarmLink_Farm] FOREIGN KEY ([CPHH]) REFERENCES [dbo].[Farm] ([CPHH]),
    CONSTRAINT [UQ_FarmRelation] UNIQUE NONCLUSTERED ([CPHH] ASC, [RelatedCPHH] ASC) WITH (FILLFACTOR = 90)
);


GO
CREATE CLUSTERED INDEX [IX_FarmRelation_CPHH]
    ON [dbo].[FarmRelation]([CPHH] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_FarmRelation_RelatedCPHH]
    ON [dbo].[FarmRelation]([RelatedCPHH] ASC) WITH (FILLFACTOR = 90);

