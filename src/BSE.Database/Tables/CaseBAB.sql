CREATE TABLE [dbo].[CaseBAB] (
    [RBSE]           CHAR (9)      NOT NULL,
    [NatalCPHH]      CHAR (11)     NULL,
    [Notes]          VARCHAR (500) NULL,
    [TracedName]     VARCHAR (30)  NULL,
    [TracedAddress1] VARCHAR (30)  NULL,
    [TracedAddress2] VARCHAR (30)  NULL,
    [TracedAddress3] VARCHAR (30)  NULL,
    [TracedPostcode] VARCHAR (10)  NULL,
    [FeedRisk]       VARCHAR (10)  NULL,
    [HorizontalRisk] VARCHAR (10)  NULL,
    [MaternalRisk]   VARCHAR (10)  NULL,
    [RowStamp]       ROWVERSION    NOT NULL,
    CONSTRAINT [PK_BABCase] PRIMARY KEY CLUSTERED ([RBSE] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_CaseBAB_NatalCPHH] CHECK ([NatalCPHH] like '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [CK_CaseBAB_RBSE] CHECK ([RBSE] like '[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [FK_BABCase_luFeedRisk] FOREIGN KEY ([FeedRisk]) REFERENCES [dbo].[luFeedRisk] ([Code]),
    CONSTRAINT [FK_BABCase_luHorizontalRisk] FOREIGN KEY ([HorizontalRisk]) REFERENCES [dbo].[luHorizontalRisk] ([Code]),
    CONSTRAINT [FK_BABCase_luMaternalRisk] FOREIGN KEY ([MaternalRisk]) REFERENCES [dbo].[luMaternalRisk] ([Code]),
    CONSTRAINT [FK_CaseBAB_Case] FOREIGN KEY ([RBSE]) REFERENCES [dbo].[Case] ([RBSE])
);

