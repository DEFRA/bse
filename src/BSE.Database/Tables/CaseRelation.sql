CREATE TABLE [dbo].[CaseRelation] (
    [ID]              INT          IDENTITY (1, 1) NOT NULL,
    [RBSE]            CHAR (9)     NOT NULL,
    [RelationType]    VARCHAR (11) NOT NULL,
    [RelationRBSE]    CHAR (9)     NULL,
    [Sex]             CHAR (1)     NULL,
    [BirthDay]        TINYINT      NULL,
    [BirthMonth]      TINYINT      NULL,
    [BirthYear]       SMALLINT     NULL,
    [RelationFate]    VARCHAR (12) NULL,
    [LeftDate]        DATETIME     NULL,
    [EartagHerdmark]  VARCHAR (8)  NULL,
    [Eartag]          VARCHAR (40) NULL,
    [Sire]            VARCHAR (80) NULL,
    [RowStamp]        ROWVERSION   NOT NULL,
    [EartagCountry]   VARCHAR (4)  NULL,
    [IsoFormatEartag] BIT          CONSTRAINT [DF_CaseRelation_IsoFormatEartag] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_CaseRelation] PRIMARY KEY NONCLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_CaseRelation_BirthDay] CHECK ([BirthDay] is null or [BirthMonth] is not null and ([BirthDay] >= 1 and [BirthDay] <= 31)),
    CONSTRAINT [CK_CaseRelation_BirthMonth] CHECK ([BirthMonth] is null or [BirthYear] is not null and ([BirthMonth] >= 1 and [BirthMonth] <= 12)),
    CONSTRAINT [CK_CaseRelation_BirthYear] CHECK ([BirthYear] <= datepart(year,getdate())),
    CONSTRAINT [CK_CaseRelation_LeftDate] CHECK ([LeftDate] <= getdate()),
    CONSTRAINT [CK_CaseRelation_RBSE] CHECK ([RBSE] like '[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [CK_CaseRelation_RelationRBSE] CHECK ([RelationRBSE] like '[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [CK_CaseRelation_RelationRBSEPresent] CHECK ([RelationRBSE] is null or [RelationRBSE] is not null and [Sex] is null and [BirthDay] is null and [BirthMonth] is null and [BirthYear] is null and [RelationFate] is null and [LeftDate] is null and [EartagHerdmark] is null and [Eartag] is null and [EartagCountry] is null and [Sire] is null),
    CONSTRAINT [FK_CaseRelation_Case] FOREIGN KEY ([RBSE]) REFERENCES [dbo].[Case] ([RBSE]),
    CONSTRAINT [FK_CaseRelation_Case1] FOREIGN KEY ([RelationRBSE]) REFERENCES [dbo].[Case] ([RBSE]),
    CONSTRAINT [FK_CaseRelation_luRelationFate] FOREIGN KEY ([RelationFate]) REFERENCES [dbo].[luRelationFate] ([Code]),
    CONSTRAINT [FK_CaseRelation_luRelationType] FOREIGN KEY ([RelationType]) REFERENCES [dbo].[luRelationType] ([Code]),
    CONSTRAINT [FK_CaseRelation_luSex] FOREIGN KEY ([Sex]) REFERENCES [dbo].[luSex] ([Code])
);


GO
CREATE CLUSTERED INDEX [IX_CaseRelation_RBSE]
    ON [dbo].[CaseRelation]([RBSE] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_CaseRelation_RelationRBSE]
    ON [dbo].[CaseRelation]([RelationRBSE] ASC) WITH (FILLFACTOR = 90);

