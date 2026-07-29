CREATE TABLE [dbo].[CaseTest] (
    [ID]         INT          IDENTITY (1, 1) NOT NULL,
    [RBSE]       CHAR (9)     NOT NULL,
    [TestType]   VARCHAR (10) NOT NULL,
    [TestResult] VARCHAR (5)  NOT NULL,
    [RowStamp]   ROWVERSION   NOT NULL,
    CONSTRAINT [PK_CaseTest] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_CaseTest_RBSE] CHECK ([RBSE] like '[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [FK_CaseTest_Case] FOREIGN KEY ([RBSE]) REFERENCES [dbo].[Case] ([RBSE]),
    CONSTRAINT [FK_CaseTest_luTestResult] FOREIGN KEY ([TestResult]) REFERENCES [dbo].[luTestResult] ([Code]),
    CONSTRAINT [FK_CaseTest_luTestType] FOREIGN KEY ([TestType]) REFERENCES [dbo].[luTestType] ([Code])
);


GO
CREATE NONCLUSTERED INDEX [IX_CaseTest_RBSE]
    ON [dbo].[CaseTest]([RBSE] ASC) WITH (FILLFACTOR = 90);

