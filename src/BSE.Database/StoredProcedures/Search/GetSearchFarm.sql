


CREATE PROCEDURE GetSearchFarm
	@CPHH varchar(11) = '',
	@OwnerName varchar(100) = '',
	@Address varchar(160) = '',
	@County varchar(15) = '',
	@Herdmark varchar(8) = '',
	@NumericHerdmark varchar(6) = '',
	@IsDealer bit,
	@AHO varchar(2) = '',
	@IncludeNonGBFarms bit = 0 AS

	SET NOCOUNT ON

	IF @Herdmark = '' BEGIN
		SET @Herdmark = '%'
	END

	IF @NumericHerdmark = '' BEGIN
		SET @NumericHerdmark = '%'
	END

	SELECT
		LEFT([Farm].[CPHH], 2) + '/' + SUBSTRING([Farm].[CPHH], 3, 3) + '/' + SUBSTRING([Farm].[CPHH], 6, 4) + '/' + RIGHT([Farm].[CPHH], 2) AS [CPHH],
		[OwnerName],
		ISNULL([Address1] + ' ', '') + ISNULL([Address2] + ' ', '') + ISNULL([Address3] + ' ', '') + ISNULL([Postcode], '') AS [Address],
		[luBSECounty].[Description] AS [County],
		ISNULL([Herdmark1] + ' ', '') + ISNULL([Herdmark2] + ' ', '') + ISNULL([Herdmark3], '') AS [Herdmark],
		ISNULL([NumericHerdmark1] + ' ', '') + ISNULL([NumericHerdmark2], '') AS [NumericHerdmark],
		[MapReference],
		[luAHO].[Name] AS [AHO],
		[luHerdType].[Description] AS [HerdType],
		ISNULL([CorrespondenceAddress1] + ' ', '') + ISNULL([CorrespondenceAddress2] + ' ', '') + ISNULL([CorrespondenceAddress3] + ' ', '') + ISNULL([CorrespondencePostcode], '') AS [CorrespondenceAddress],
		[CasesCount],
		ISNULL([ConfirmedCasesCount],0) AS [ConfirmedCasesCount]
	FROM
		[Farm] INNER JOIN [luBSECounty] ON [Farm].[County] = [luBSECounty].[Code]
		LEFT JOIN [luAHO] ON [Farm].[AHO] = [luAHO].[Code]
		LEFT JOIN [luHerdType] ON [Farm].[HerdType] = [luHerdType].[Code]
		INNER JOIN
			(
			SELECT
				CPHH,
				COUNT(*) AS [CasesCount]
			FROM
				[Case]
			GROUP BY
				CPHH) CaseAggregate ON [Farm].CPHH = [CaseAggregate].[CPHH]
		 LEFT JOIN
			(
			SELECT
				CPHH,
				COUNT(*) AS [ConfirmedCasesCount]
			FROM
				[Case]
			WHERE
				[FinalResult] = 'Pos'
			GROUP BY
				CPHH) CaseAggregate2 ON [Farm].CPHH = [CaseAggregate2].[CPHH]
	WHERE
		[Farm].[CPHH] LIKE @CPHH + '%' AND
		[OwnerName] LIKE '%' + @OwnerName + '%' AND
		(ISNULL([Address1] + ' ', '') + ISNULL([Address2] + ' ', '') + ISNULL([Address3] + ' ', '') + ISNULL([Postcode], '') LIKE '%' + @Address + '%' OR
		ISNULL([CorrespondenceAddress1] + ' ', '') + ISNULL([CorrespondenceAddress2] + ' ', '') + ISNULL([CorrespondenceAddress3] + ' ', '') + ISNULL([CorrespondencePostcode], '') LIKE '%' + @Address + '%') AND
		[Farm].[County] LIKE @County + '%' AND
		(ISNULL([Herdmark1],'') LIKE @Herdmark OR ISNULL([Herdmark2],'') LIKE @Herdmark OR ISNULL([Herdmark3],'') LIKE @Herdmark ) AND
       		(ISNULL([NumericHerdmark1], '') LIKE @NumericHerdmark OR ISNULL([NumericHerdmark2], '') LIKE @NumericHerdmark) AND
		ISNULL([IsDealer],3) BETWEEN ISNULL(@IsDealer, 0) AND ISNULL(@IsDealer,3) AND
		ISNULL([Farm].[AHO], '') LIKE @AHO + '%' AND
		([Farm].[IsNonGBFarm] = 0 OR [Farm].[IsNonGBFarm] = @IncludeNonGBFarms)
	ORDER BY
		[Farm].[CPHH]

	SET NOCOUNT OFF
