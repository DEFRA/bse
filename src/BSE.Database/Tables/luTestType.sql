CREATE TABLE [dbo].[luTestType] (
    [Code]        VARCHAR (10) NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    [IsActive]    BIT          CONSTRAINT [DF_luTestType_IsActive] DEFAULT (1) NOT NULL,
    CONSTRAINT [PK_luTestType] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

