
CREATE PROCEDURE GetPrefixCodeByXYReference
	@XCoordPrefix char(1),
	@YCoordPrefix varchar(2)
AS

SELECT
	[Code]
FROM
	luMapReference
WHERE
	[XCoordPrefix] = @XCoordPrefix and [YCoordPrefix] = @YCoordPrefix

RETURN
