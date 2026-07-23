
CREATE PROCEDURE GetXYReferenceByPrefixCode
	@Code char(2)
AS

SELECT
	[Code],
	CASE LEN([XCoordPrefix])
		WHEN 1 THEN '0' + [XCoordPrefix]
		ELSE [XCoordPrefix]
	END AS  XCoordPrefix,
	CASE LEN([YCoordPrefix])
		WHEN 1 THEN '0' + [YCoordPrefix]
		ELSE [YCoordPrefix]
	END AS  YCoordPrefix

FROM
	luMapReference
WHERE
	[Code] = @Code

RETURN
