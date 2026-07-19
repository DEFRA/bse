
CREATE PROCEDURE GetRelatedFarm
	@CPHH char(11)
AS

SELECT
	[ID],
	[FarmRelation].[CPHH],
	[RelatedCPHH],
	ISNULL([OwnerName] + ', ' + [Address1], 'BSE Free') AS [Status],
	[FarmRelation].[RowStamp]
FROM
	[FarmRelation] LEFT JOIN [Farm] ON [FarmRelation].[RelatedCPHH] = [Farm].[CPHH]
WHERE
	[FarmRelation].[CPHH] = @CPHH

RETURN
