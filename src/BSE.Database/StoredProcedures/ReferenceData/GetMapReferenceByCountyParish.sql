
CREATE PROCEDURE GetMapReferenceByCountyParish
	@County char(2),
	@Parish char(3)
AS

SELECT
	'0' + [luMapReference].[XCoordPrefix] + SUBSTRING([luParishMapReference].[MapReference1], 3, 2) AS XReference1,
	CASE LEN([luMapReference].[YCoordPrefix])
		WHEN 1 THEN '0' + [luMapReference].[YCoordPrefix]
		ELSE [luMapReference].[YCoordPrefix]
	END  + SUBSTRING([luParishMapReference].[MapReference1], 5, 2) AS YReference1,
	'0' + [luMapReference].[XCoordPrefix] + SUBSTRING([luParishMapReference].[MapReference2], 3, 2) AS XReference2,
	CASE LEN([luMapReference].[YCoordPrefix])
		WHEN 1 THEN '0' + [luMapReference].[YCoordPrefix]
		ELSE [luMapReference].[YCoordPrefix]
	END  + SUBSTRING([luParishMapReference].[MapReference2], 5, 2) AS YReference2

FROM
	[luParishMapReference] INNER JOIN [luMapReference] ON LEFT([luParishMapReference].[MapReference1], 2) = [luMapReference].[Code] 
WHERE
	County = @County AND Parish = @Parish

	

RETURN
