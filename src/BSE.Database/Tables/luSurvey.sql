CREATE TABLE [dbo].[luSurvey] (
    [Code]        VARCHAR (4)  NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_luSurvey] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

