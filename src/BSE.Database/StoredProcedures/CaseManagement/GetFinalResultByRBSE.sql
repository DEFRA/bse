

CREATE PROCEDURE [dbo].[GetFinalResultByRBSE]
	@RBSE char(9)
AS

	SELECT
		[Case].[RBSE],
		[Case].[CPHH],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		[Case].[BSE1ReceivedDate],
		[Case].[Fate],
		[Case].[IsPurchaserBSE1Received],
		[Case].[IsBreederBSE1Received],
		[Case].[IsVendor1BSE1Received],
		[Case].[IsHomebredBSE1Received],
		[Case].[IsSummarySheetReceived],
		[Case].[IsPaperworkComplete],
		[Case].[FinalResultDate],
		[Case].[FinalResult],
		[Case].[DBSE],
		[Case].[BirthDate],
		[Case].[RetrospectiveTestType],
		[Case].[RetrospectiveResult],
		[Case].[RetrospectiveResultDate],
		[Case].[RetrospectiveComment],
		[Case].[Rowstamp],
		[Case].[LabComment],
		[Case].[AlternateDiagnosis],
		[Farm].[OwnerName],
		[Farm].[Address1],
		[Farm].[Address2],
		[Farm].[Address3],
		[Farm].[Postcode],
		[Farm].[Parish],
		[Farm].[District],
		[Farm].[County],
		[Farm].[AHO],
		[luCaseType].[Description] As CaseType,
		[luAHO].[Name] AS AHOName,
		[luAHO].[Code] + ' ' + [luAHO].[Name] AS AHOCodeName,
		[luCounty].[Name] AS CountyName,
		[CaseWork].[PurchaserBSE1ReceivedDate],
		[CaseWork].[BreederBSE1ReceivedDate],
		[CaseWork].[Vendor1BSE1ReceivedDate],
		[CaseWork].[HomebredBSE1ReceivedDate],
		[CaseWork].[SummarySheetReceivedDate],
		[CaseWork].[PaperworkCompleteDate]
	FROM
		[Case] 
		LEFT JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]
		LEFT JOIN [luCaseType] ON [Case].[CaseType] = [luCaseType].[Code]
		LEFT JOIN [luAHO] ON [Farm].[AHO] = [luAHO].[Code]
		LEFT JOIN [luCounty] ON [Farm].[County] = [luCounty].[County]
		LEFT JOIN [CaseWork] ON [Case].[RBSE] = [CaseWork].[RBSE]
	WHERE
		[Case].[RBSE] = @RBSE
	
	RETURN




