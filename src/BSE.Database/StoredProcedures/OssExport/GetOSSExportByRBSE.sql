
CREATE PROCEDURE GetOSSExportByRBSE
	@RBSE char(9)
AS

SELECT
	LEFT([Case].[RBSE], 2) + '/' + SUBSTRING([Case].[RBSE], 3, 2) + '/' + RIGHT([Case].[RBSE], 5) AS [RBSE],
	LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
	[Farm].[OwnerName],
	[Farm].[Address1]
FROM
	[Case] LEFT JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]
WHERE
	[Case].[RBSE] = @RBSE

RETURN
