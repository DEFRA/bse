
CREATE PROCEDURE CopyHerdSizeToExportTable AS

	IF EXISTS (SELECT * FROM dbo.sysobjects WHERE ID = object_id(N'[dbo].[expAge]') and OBJECTPROPERTY(id, N'IsUserTable') = 1) BEGIN
		DROP TABLE [expAge]
	END

	CREATE TABLE [dbo].[expAge] (
		[seq_a] [numeric](5, 0) NOT NULL ,
		[cphh] [char] (11) NOT NULL ,
		[year] [numeric](4, 0) NOT NULL ,
		[herd_size] [numeric](5, 0) NULL ,
		[lact_1] [numeric](5, 0) NULL ,
		[lact_2] [numeric](5, 0) NULL ,
		[lact_3] [numeric](5, 0) NULL ,
		[lact_4] [numeric](5, 0) NULL ,
		[lact_5] [numeric](5, 0) NULL ,
		[lact_6] [numeric](5, 0) NULL ,
		[lact_7] [numeric](5, 0) NULL ,
		[lact_8] [numeric](5, 0) NULL ,
		[lact_9] [numeric](5, 0) NULL ,
		[lact_10] [numeric](5, 0) NULL ,
		[lact_10_pl] [numeric](5, 0) NULL 
	) ON [PRIMARY]

	ALTER TABLE [dbo].[expAge] WITH NOCHECK ADD 
		CONSTRAINT [ag_pk_seq_a] PRIMARY KEY  CLUSTERED 
		(
			[seq_a]
		)  ON [PRIMARY] 

	CREATE  INDEX [cphh_ind_age] ON [dbo].[expAge]([cphh]) ON [PRIMARY]

	CREATE  INDEX [cphh_yr_ind_age] ON [dbo].[expAge]([cphh], [year]) ON [PRIMARY]

	INSERT INTO [expAge]
	SELECT
		[ID] AS seq_a, 
		[HerdSize].[CPHH] AS cphh,
		[HerdYear] AS [year],
		[TotalSize] AS herd_size,
		[Lactation1Size] AS lact_1,
		[Lactation2Size] AS lact_2,
		[Lactation3Size] AS lact_3,
		[Lactation4Size] AS lact_4,
		[Lactation5Size] AS lact_5,
		[Lactation6Size] AS lact_6,
		[Lactation7Size] AS lact_7,
		[Lactation8Size] AS lact_8, 
		[Lactation9Size] AS lact_9,
		[Lactation10Size] AS lact_10,
		[Lactation10PlusSize] AS lact_10_pl
	FROM
		[HerdSize] INNER JOIN [Farm] ON [HerdSize].[CPHH] = [Farm].[CPHH]
	WHERE
		[IsNonGBFarm] = 0
