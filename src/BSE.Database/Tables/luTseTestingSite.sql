CREATE TABLE [dbo].[luTseTestingSite] (
    [Name]    VARCHAR (50)  NOT NULL,
    [Address] VARCHAR (255) NOT NULL,
    [CPH]     CHAR (11)     NOT NULL,
    [AHO]     CHAR (2)      NOT NULL,
    CONSTRAINT [FK_luTseTestingSite_AHO] FOREIGN KEY ([AHO]) REFERENCES [dbo].[luAHO] ([Code])
);

