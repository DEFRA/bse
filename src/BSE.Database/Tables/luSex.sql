CREATE TABLE [dbo].[luSex] (
    [Code]        CHAR (1)     NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luSex] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

