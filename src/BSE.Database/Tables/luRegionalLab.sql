CREATE TABLE [dbo].[luRegionalLab] (
    [Code]        CHAR (4)     NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luRegionalLab] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

