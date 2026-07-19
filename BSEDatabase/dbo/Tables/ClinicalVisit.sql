CREATE TABLE [dbo].[ClinicalVisit] (
    [ID]        INT        IDENTITY (1, 1) NOT NULL,
    [RBSE]      CHAR (9)   NOT NULL,
    [VisitDate] DATETIME   NOT NULL,
    [RowStamp]  ROWVERSION NOT NULL,
    CONSTRAINT [PK_ClinicalVisit] PRIMARY KEY NONCLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_ClinicalVisit_RBSE] CHECK ([RBSE] like '[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [FK_ClinicalVisit_Case] FOREIGN KEY ([RBSE]) REFERENCES [dbo].[Case] ([RBSE])
);


GO
CREATE CLUSTERED INDEX [IX_ClinicalVisit_RBSE]
    ON [dbo].[ClinicalVisit]([RBSE] ASC) WITH (FILLFACTOR = 90);

