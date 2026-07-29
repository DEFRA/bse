CREATE TABLE [dbo].[lnkBatchCase] (
    [BatchID]  INT         NOT NULL,
    [RBSE]     CHAR (9)    NOT NULL,
    [Document] VARCHAR (5) NOT NULL,
    CONSTRAINT [PK_lnkBatchCase] PRIMARY KEY CLUSTERED ([BatchID] ASC, [RBSE] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_lnkBatchCase_RBSE] CHECK ([RBSE] like '[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [FK_lnkBatchCase_Batch] FOREIGN KEY ([BatchID]) REFERENCES [dbo].[Batch] ([BatchID]),
    CONSTRAINT [FK_lnkBatchCase_Case] FOREIGN KEY ([RBSE]) REFERENCES [dbo].[Case] ([RBSE]),
    CONSTRAINT [FK_lnkBatchCase_luDocumentType] FOREIGN KEY ([Document]) REFERENCES [dbo].[luDocumentType] ([Code])
);


GO
CREATE NONCLUSTERED INDEX [IX_lnkBatchCase_RBSE]
    ON [dbo].[lnkBatchCase]([RBSE] ASC) WITH (FILLFACTOR = 90);

