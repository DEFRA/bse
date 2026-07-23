

CREATE PROCEDURE GetADNSCasesForGB
	@ADNSYear int,
	@StartADNSNumber int AS

	DECLARE @ttblADNS TABLE
		(
		ID int IDENTITY(1, 1),
		RBSE char(9)
		)
	
	-- insert into the temporary table all cases that can be reported
	INSERT INTO @ttblADNS
		(
		RBSE
		)
	SELECT
		[RBSE]
	FROM
		[Case] INNER JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]
	WHERE
		[FinalResult] = 'Pos' AND [FinalResultDate] IS NOT NULL AND
		[EmailSentToADNSDate] IS NULL AND
		[IsNonGBCase] = 0 AND
		[Farm].[ADNSRegionID] IS NOT NULL -- eliminate cases that can't be reported.  This is so the ADNS reference isn't incremented for them

	-- return valid ADNS cases
	SELECT
		[ADNS].[ID],
		[Case].[RBSE],
		@ADNSYear AS [ADNSYear],
		[ADNS].[ID] + @StartADNSNumber -1 AS [ADNSNumber],		
		[Farm].[ADNSRegionID],
		[luADNSRegion].[Name] AS [ADNSRegionName],
		[FinalResultDate] AS [ConfirmationDate],
		CONVERT(varchar(4), @ADNSYear) + '/' + RIGHT('0000' + CONVERT(varchar(5), [ADNS].[ID] + @StartADNSNumber -1), 5) AS [ADNSReference],
		[Case].[RowStamp]
	FROM
		@ttblADNS [ADNS]  INNER JOIN [Case] ON [ADNS].[RBSE] = [Case].[RBSE]
		INNER JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]
		INNER JOIN [luADNSRegion] ON [Farm].[ADNSRegionID] = [luADNSRegion].[ID]
	ORDER BY [ADNS].[ID]

	-- and invalid ones (ones without an ADNS region)
	SELECT
		LEFT([RBSE], 2) + '/' + SUBSTRING([RBSE], 3, 2) + '/' + RIGHT([RBSE], 5) AS [RBSE]
	FROM
		[Case] INNER JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]
	WHERE
		[FinalResult] = 'Pos' AND [FinalResultDate] IS NOT NULL AND
		[EmailSentToADNSDate] IS NULL AND
		[IsNonGBCase] = 0 AND
		[Farm].[ADNSRegionID] IS  NULL
