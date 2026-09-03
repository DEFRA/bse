
CREATE PROCEDURE CopyFarmToExportTable AS

	IF EXISTS (SELECT * FROM dbo.sysobjects WHERE ID = object_id(N'[dbo].[expFarm]') and OBJECTPROPERTY(id, N'IsUserTable') = 1) BEGIN
		DROP TABLE [expFarm]
	END

	CREATE TABLE [expFarm] (
		[cphh] [char] (11) NOT NULL ,
		[county] [varchar] (15) NULL ,
		[x_coord] [numeric](6, 0) NULL ,
		[y_coord] [numeric](7, 0) NULL ,
		[herd_mark] [char] (6) NULL ,
		[aho] [char] (2) NULL ,
		[pedigree] [char] (2) NULL ,
		[herd_type] [char] (1) NULL ,
		[dealer_flag] [char] (1) NULL ,
		[status] [varchar] (4) NULL 
	) ON [PRIMARY]

	ALTER TABLE [dbo].[expFarm] WITH NOCHECK ADD 
		CONSTRAINT [fm_pk_cphh] PRIMARY KEY  CLUSTERED 
		(
			[cphh]
		) WITH  FILLFACTOR = 90  ON [PRIMARY] 

	CREATE  INDEX [fm_herd_mark] ON [dbo].[expFarm]([herd_mark]) ON [PRIMARY]

	CREATE  INDEX [fm_county] ON [dbo].[expFarm]([county]) ON [PRIMARY]

	CREATE  INDEX [fm_status] ON [dbo].[expFarm]([status], [cphh]) ON [PRIMARY]

	INSERT INTO [expFarm]
	SELECT
		[Farm].[CPHH] AS cphh,
		[Farm].[County] as county,
		CASE WHEN luMapReference.XCoordPrefix IS NULL THEN NULL ELSE luMapReference.XCoordPrefix + SUBSTRING(MapReference,3,3) + '00' END AS x_coord,
		CASE WHEN luMapReference.YCoordPrefix IS NULL THEN NULL ELSE luMapReference.YCoordPrefix + SUBSTRING(MapReference,6,3) + '00'  END AS y_coord,
		CONVERT(char(6), [Farm].[Herdmark1]) AS herd_mark,
		[Farm].[AHO] as aho,
		[Farm].[PedigreeType] AS pedigree,
		[Farm].[HerdType] AS herd_type,
		(CASE WHEN IsDealer = 1 THEN 'D' END) AS dealer_flag,
		CASE WHEN PosCount > 0 THEN 'POS' WHEN PendCount > 0 THEN 'PEND' ELSE 'NEG' END AS status
	FROM
		[Farm] LEFT JOIN
                          		(
			SELECT
				[CPHH],
				COUNT(*) AS PosCount
                            	FROM
				[Case]
                            	WHERE
				[FinalResult] = 'Pos'
                            	GROUP BY
				CPHH
			) PosQuery ON [Farm].[CPHH] = PosQuery.CPHH
		 LEFT JOIN
                          		(
			SELECT
				[CPHH],
				COUNT(*)  AS PendCount
                            	FROM
				[Case]
                            	WHERE
				[FinalResult] IS NULL AND
				[Fate] != 'ALT' AND [Fate] != 'REC'
                            	GROUP BY
				[CPHH]
			) PendQuery ON [Farm].[CPHH] = PendQuery.CPHH
		LEFT JOIN [luMapReference] ON LEFT([Farm].[MapReference],2) = [luMapReference].[Code]
	WHERE
		[IsNonGBFarm] = 0
