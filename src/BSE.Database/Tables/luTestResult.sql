CREATE TABLE [dbo].[luTestResult] (
    [Code]        VARCHAR (5)  NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luTestResult] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

