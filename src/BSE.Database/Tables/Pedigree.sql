CREATE TABLE [dbo].[Pedigree] (
    [ID]              INT          IDENTITY (1, 1) NOT NULL,
    [RBSE]            CHAR (9)     NULL,
    [Eartag]          VARCHAR (15) NULL,
    [Name]            VARCHAR (80) NULL,
    [SireID]          INT          NULL,
    [DamID]           INT          NULL,
    [Sex]             CHAR (1)     NULL,
    [Herdbook]        VARCHAR (15) NULL,
    [AlternativeName] VARCHAR (50) NULL,
    [BirthDay]        TINYINT      NULL,
    [BirthMonth]      TINYINT      NULL,
    [BirthYear]       SMALLINT     NULL,
    [RowStamp]        ROWVERSION   NOT NULL,
    CONSTRAINT [PK_Pedigree] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_Pedigree_BirthDay] CHECK ([BirthDay] is null or [BirthMonth] is not null and ([BirthDay] >= 1 and [BirthDay] <= 31)),
    CONSTRAINT [CK_Pedigree_BirthMonth] CHECK ([BirthMonth] is null or [BirthYear] is not null and ([BirthMonth] >= 1 and [BirthMonth] <= 12)),
    CONSTRAINT [CK_Pedigree_BirthYear] CHECK ([BirthYear] < datepart(year,getdate())),
    CONSTRAINT [CK_Pedigree_RBSE] CHECK ([RBSE] like '[0-9][0-9][0123456789X][0123456789X][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [CK_Pedigree_RBSEPresent] CHECK ([RBSE] is null or [RBSE] is not null and [Sex] is null and [BirthDay] is null and [BirthMonth] is null and [BirthYear] is null),
    CONSTRAINT [FK_Pedigree_Case] FOREIGN KEY ([RBSE]) REFERENCES [dbo].[Case] ([RBSE]),
    CONSTRAINT [FK_Pedigree_luSex] FOREIGN KEY ([Sex]) REFERENCES [dbo].[luSex] ([Code]),
    CONSTRAINT [FK_Pedigree_Pedigree] FOREIGN KEY ([SireID]) REFERENCES [dbo].[Pedigree] ([ID]),
    CONSTRAINT [FK_Pedigree_Pedigree1] FOREIGN KEY ([DamID]) REFERENCES [dbo].[Pedigree] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Pedigree_RBSE]
    ON [dbo].[Pedigree]([RBSE] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_Pedigree_SireID]
    ON [dbo].[Pedigree]([SireID] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_Pedigree_DamID]
    ON [dbo].[Pedigree]([DamID] ASC) WITH (FILLFACTOR = 90);


GO
CREATE NONCLUSTERED INDEX [IX_Pedigree_Eartag]
    ON [dbo].[Pedigree]([Eartag] ASC) WITH (FILLFACTOR = 90);

