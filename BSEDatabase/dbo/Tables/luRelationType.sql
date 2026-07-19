CREATE TABLE [dbo].[luRelationType] (
    [Code]        VARCHAR (11) NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luRelationType] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

