
CREATE PROCEDURE GetFeedByRBSE
	@RBSE char(9)
AS

SELECT
	[CaseFeed].[ID],
	[CaseFeed].[RBSE],
	[CaseFeed].[YearFrom],
	[CaseFeed].[YearTo],
	[CaseFeed].[RationType],
	[luRationType].[Description] AS RationDescription,
	[CaseFeed].[SupplierID],
	ISNULL([luSupplier].[Name], '')  AS SupplierName,
	[CaseFeed].[RationName],
	[CaseFeed].[IsPrePurchase],
	[CaseFeed].[RowStamp]

FROM
	[CaseFeed] LEFT JOIN [luSupplier] ON [CaseFeed].[SupplierID] = [luSupplier].[ID] 
	INNER JOIN [luRationType] ON [CaseFeed].[RationType] = [luRationType].[Code]
WHERE
	RBSE = @RBSE

RETURN
