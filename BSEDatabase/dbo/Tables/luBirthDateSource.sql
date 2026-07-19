CREATE TABLE [dbo].[luBirthDateSource] (
    [Code]        VARCHAR (5)  NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luBirthDateSource] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

