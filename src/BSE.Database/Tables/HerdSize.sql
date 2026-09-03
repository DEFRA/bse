CREATE TABLE [dbo].[HerdSize] (
    [ID]                  INT        IDENTITY (1, 1) NOT NULL,
    [CPHH]                CHAR (11)  NOT NULL,
    [HerdYear]            SMALLINT   NOT NULL,
    [TotalSize]           SMALLINT   NOT NULL,
    [Lactation1Size]      SMALLINT   NULL,
    [Lactation2Size]      SMALLINT   NULL,
    [Lactation3Size]      SMALLINT   NULL,
    [Lactation4Size]      SMALLINT   NULL,
    [Lactation5Size]      SMALLINT   NULL,
    [Lactation6Size]      SMALLINT   NULL,
    [Lactation7Size]      SMALLINT   NULL,
    [Lactation8Size]      SMALLINT   NULL,
    [Lactation9Size]      SMALLINT   NULL,
    [Lactation10Size]     SMALLINT   NULL,
    [Lactation10PlusSize] SMALLINT   NULL,
    [RowStamp]            ROWVERSION NOT NULL,
    CONSTRAINT [PK_HerdSize] PRIMARY KEY NONCLUSTERED ([ID] ASC) WITH (FILLFACTOR = 90),
    CONSTRAINT [CK_HerdSize_CPHH] CHECK ([CPHH] like '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [CK_HerdSize_HerdYear] CHECK ([HerdYear] >= 1975 and [HerdYear] <= datepart(year,getdate())),
    CONSTRAINT [CK_HerdSize_LactationNullable] CHECK ([Lactation1Size] is null and [Lactation2Size] is null and [Lactation3Size] is null and [Lactation4Size] is null and [Lactation5Size] is null and [Lactation6Size] is null and [Lactation7Size] is null and [Lactation8Size] is null and [Lactation9Size] is null and [Lactation10Size] is null and [Lactation10PlusSize] is null or [Lactation1Size] is not null and [Lactation2Size] is not null and [Lactation3Size] is not null and [Lactation4Size] is not null and [Lactation5Size] is not null and [Lactation6Size] is not null and [Lactation7Size] is not null and [Lactation8Size] is not null and [Lactation9Size] is not null and [Lactation10Size] is not null and [Lactation10PlusSize] is not null),
    CONSTRAINT [CK_HerdSize_TotalSize] CHECK ([TotalSize] >= 1 and [TotalSize] <= 2000),
    CONSTRAINT [FK_HerdSize_Farm] FOREIGN KEY ([CPHH]) REFERENCES [dbo].[Farm] ([CPHH])
);


GO
CREATE CLUSTERED INDEX [IX_HerdSize_CPHH]
    ON [dbo].[HerdSize]([ID] ASC) WITH (FILLFACTOR = 90);

