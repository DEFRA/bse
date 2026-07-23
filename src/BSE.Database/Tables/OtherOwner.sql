CREATE TABLE [dbo].[OtherOwner] (
    [ID]       INT           IDENTITY (1, 1) NOT NULL,
    [RBSE]     CHAR (9)      NOT NULL,
    [Type]     VARCHAR (10)  NOT NULL,
    [Name]     VARCHAR (150) NULL,
    [CPHH]     CHAR (11)     NULL,
    [RowStamp] ROWVERSION    NOT NULL,
    CONSTRAINT [PK_OtherOwner] PRIMARY KEY NONCLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_OtherOwner_CPHH] CHECK ([CPHH] like '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [CK_OtherOwner_NameOrCPHHRequired] CHECK (((not([Name] is null and [CPHH] is null)))),
    CONSTRAINT [CK_OtherOwner_RBSE] CHECK ([RBSE] like '[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [FK_OtherOwner_Case] FOREIGN KEY ([RBSE]) REFERENCES [dbo].[Case] ([RBSE]),
    CONSTRAINT [FK_OtherOwner_luOwnerType] FOREIGN KEY ([Type]) REFERENCES [dbo].[luOwnerType] ([Code])
);


GO
CREATE CLUSTERED INDEX [IX_OtherOwner_RBSE]
    ON [dbo].[OtherOwner]([RBSE] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_OtherOwner_CPHH]
    ON [dbo].[OtherOwner]([CPHH] ASC) WITH (FILLFACTOR = 90);

