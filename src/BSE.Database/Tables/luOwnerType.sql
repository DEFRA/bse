CREATE TABLE [dbo].[luOwnerType] (
    [Code]        VARCHAR (10) NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luOwnerType] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

