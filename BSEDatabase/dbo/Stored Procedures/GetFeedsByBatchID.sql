

CREATE PROCEDURE GetFeedsByBatchID 
	@BatchID int
AS
SELECT
	[CaseFeed].[RBSE],
	[CaseFeed].[YearFrom],
	[CaseFeed].[YearTo],
	[CaseFeed].[RationType],
	[CaseFeed].[RationName],
	CONVERT(char(1), CASE [CaseFeed].[IsPrePurchase] WHEN 1 THEN 'Y' ELSE 'N' END) AS [IsPrePurchase],
	[luSupplier].[Name],
	[Case].[CPHH]

FROM
	[Case] INNER JOIN [lnkBatchCase] ON [lnkBatchCase].[RBSE] = [Case].[RBSE]
	INNER JOIN [CaseFeed] ON [CaseFeed].[RBSE] = [Case].[RBSE]
	LEFT JOIN [luSupplier] ON [CaseFeed].[SupplierID] = [luSupplier].[ID]

WHERE 
	[lnkBatchCase].[BatchID] = @BatchID

ORDER BY 
	[CaseFeed].[RBSE]
