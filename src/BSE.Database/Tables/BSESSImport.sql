CREATE TABLE [dbo].[BSESSImport] (
    [RBSE]                   CHAR (9)      NOT NULL,
    [UnformattedRBSE]        VARCHAR (10)  NULL,
    [AnimalIID]              DECIMAL (18)  NOT NULL,
    [Eartag]                 VARCHAR (20)  NULL,
    [BirthDate]              DATETIME      NULL,
    [TestGroupDerivedSurvey] VARCHAR (4)   NULL,
    [TestGroupName]          VARCHAR (50)  NULL,
    [FinalResultID]          TINYINT       NULL,
    [FinalResultName]        VARCHAR (25)  NULL,
    [NotificationDate]       SMALLDATETIME NULL,
    [BarCode]                VARCHAR (20)  NULL,
    CONSTRAINT [PK_BSESSImport] PRIMARY KEY CLUSTERED ([RBSE] ASC) WITH (FILLFACTOR = 75)
);


GO
CREATE NONCLUSTERED INDEX [IX_BSESSImport_NotificationDate]
    ON [dbo].[BSESSImport]([NotificationDate] ASC) WITH (FILLFACTOR = 75);


GO
CREATE NONCLUSTERED INDEX [IX_BSESSImport_Eartag]
    ON [dbo].[BSESSImport]([Eartag] ASC) WITH (FILLFACTOR = 75);


GO
CREATE NONCLUSTERED INDEX [IX_BSESSImport_TestGroupDerivedSurvey]
    ON [dbo].[BSESSImport]([TestGroupDerivedSurvey] ASC) WITH (FILLFACTOR = 75);

