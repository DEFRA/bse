CREATE TABLE [dbo].[CaseFeed] (
    [ID]            INT          IDENTITY (1, 1) NOT NULL,
    [RBSE]          CHAR (9)     NOT NULL,
    [YearFrom]      SMALLINT     NOT NULL,
    [YearTo]        SMALLINT     NOT NULL,
    [RationType]    VARCHAR (2)  NOT NULL,
    [SupplierID]    INT          NULL,
    [RationName]    VARCHAR (60) NULL,
    [IsPrePurchase] BIT          NOT NULL,
    [RowStamp]      ROWVERSION   NOT NULL,
    CONSTRAINT [PK_CaseFeed] PRIMARY KEY NONCLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_CaseFeed_RBSE] CHECK ([RBSE] like '[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [CK_CaseFeed_YearTo] CHECK ([YearTo] >= [YearFrom]),
    CONSTRAINT [FK_CaseFeed_Case] FOREIGN KEY ([RBSE]) REFERENCES [dbo].[Case] ([RBSE]),
    CONSTRAINT [FK_CaseFeed_luRationType] FOREIGN KEY ([RationType]) REFERENCES [dbo].[luRationType] ([Code]),
    CONSTRAINT [FK_CaseFeed_luSupplier] FOREIGN KEY ([SupplierID]) REFERENCES [dbo].[luSupplier] ([ID])
);


GO
CREATE CLUSTERED INDEX [IX_CaseFeed_RBSE]
    ON [dbo].[CaseFeed]([RBSE] ASC) WITH (FILLFACTOR = 90);

