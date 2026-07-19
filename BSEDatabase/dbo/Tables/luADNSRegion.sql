CREATE TABLE [dbo].[luADNSRegion] (
    [ID]          INT          NOT NULL,
    [Name]        VARCHAR (50) NOT NULL,
    [AuthorityID] INT          NOT NULL,
    CONSTRAINT [PK_luIDES] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_luADNSRegion_luAuthority] FOREIGN KEY ([AuthorityID]) REFERENCES [dbo].[luAuthority] ([ID])
);

