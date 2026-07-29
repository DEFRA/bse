CREATE TABLE [dbo].[CaseHistorical] (
    [RBSE]                  CHAR (9)     NOT NULL,
    [SireDamIdentity]       VARCHAR (30) NULL,
    [VICReference]          VARCHAR (20) NULL,
    [CVLReference]          VARCHAR (20) NULL,
    [OldCaseID]             VARCHAR (30) NULL,
    [Herdbook]              DECIMAL (12) NULL,
    [OffspringBirthDate]    DATETIME     NULL,
    [OffspringSex]          CHAR (1)     NULL,
    [OffspringSexTwin]      CHAR (1)     NULL,
    [OffspringFate]         VARCHAR (4)  NULL,
    [OffspringFateTwin]     VARCHAR (4)  NULL,
    [OffspringFateDate]     DATETIME     NULL,
    [OffspringfateDateTwin] DATETIME     NULL,
    [IsOffspringCalved]     BIT          NULL,
    CONSTRAINT [PK_CaseHistorical] PRIMARY KEY CLUSTERED ([RBSE] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_CaseHistorical_RBSE] CHECK ([RBSE] like '[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [FK_CaseHistorical_Case] FOREIGN KEY ([RBSE]) REFERENCES [dbo].[Case] ([RBSE]),
    CONSTRAINT [FK_CaseHistorical_luSex] FOREIGN KEY ([OffspringSex]) REFERENCES [dbo].[luSex] ([Code]),
    CONSTRAINT [FK_CaseHistorical_luSex1] FOREIGN KEY ([OffspringSexTwin]) REFERENCES [dbo].[luSex] ([Code])
);

