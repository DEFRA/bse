CREATE TABLE [dbo].[luRelationFate] (
    [Code]        VARCHAR (12) NOT NULL,
    [Description] VARCHAR (50) NOT NULL,
    [IsActive]    BIT          CONSTRAINT [DF_luRelationFate_IsActive] DEFAULT (1) NOT NULL,
    CONSTRAINT [PK_luRelationFate] PRIMARY KEY CLUSTERED ([Code] ASC) WITH (FILLFACTOR = 90)
);

