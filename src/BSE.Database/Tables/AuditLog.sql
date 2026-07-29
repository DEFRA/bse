CREATE TABLE [dbo].[AuditLog] (
    [ID]          INT          IDENTITY (1, 1) NOT NULL,
    [TableName]   VARCHAR (50) NOT NULL,
    [FieldName]   VARCHAR (50) NULL,
    [LogDate]     DATETIME     CONSTRAINT [DF_AuditLog_LogDate] DEFAULT (getdate()) NOT NULL,
    [UserID]      INT          NOT NULL,
    [BeforeValue] VARCHAR (50) NULL,
    [AfterValue]  VARCHAR (50) NULL,
    [Reason]      VARCHAR (20) NOT NULL,
    [RBSE]        CHAR (9)     NULL,
    [CPHH]        CHAR (11)    NULL,
    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_AuditLog_CPHH] CHECK ([CPHH] like '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [CK_AuditLog_RBSE] CHECK ([RBSE] like '[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [FK_AuditLog_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_AuditLog_LogDate]
    ON [dbo].[AuditLog]([LogDate] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_AuditLog_UserID]
    ON [dbo].[AuditLog]([UserID] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_AuditLog_Reason]
    ON [dbo].[AuditLog]([Reason] ASC) WITH (FILLFACTOR = 90);

