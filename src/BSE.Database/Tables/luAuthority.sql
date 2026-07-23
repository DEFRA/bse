CREATE TABLE [dbo].[luAuthority] (
    [ID]                INT          NOT NULL,
    [Name]              VARCHAR (30) NOT NULL,
    [AuthorityCountyID] INT          NOT NULL,
    CONSTRAINT [PK_Authority] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_luAuthority_luAuthorityCounty] FOREIGN KEY ([AuthorityCountyID]) REFERENCES [dbo].[luAuthorityCounty] ([ID])
);

