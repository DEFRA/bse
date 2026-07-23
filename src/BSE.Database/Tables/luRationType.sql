CREATE TABLE [dbo].[luRationType] (
    [Code]        VARCHAR (2)  NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luRationType] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

