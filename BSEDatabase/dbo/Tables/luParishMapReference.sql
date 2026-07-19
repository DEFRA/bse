CREATE TABLE [dbo].[luParishMapReference] (
    [ID]            INT      NOT NULL,
    [County]        CHAR (2) NOT NULL,
    [Parish]        CHAR (3) NOT NULL,
    [MapReference1] CHAR (6) NOT NULL,
    [MapReference2] CHAR (6) NULL,
    CONSTRAINT [PK_luParishMapReference] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [FK_luParishMapReference_luParish] FOREIGN KEY ([County], [Parish]) REFERENCES [dbo].[luParish] ([County], [Parish])
);

