CREATE TABLE [dbo].[luMapReference] (
    [Code]         CHAR (2)    NOT NULL,
    [XCoordPrefix] CHAR (1)    NOT NULL,
    [YCoordPrefix] VARCHAR (2) NOT NULL,
    CONSTRAINT [PK_luMapReference] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

