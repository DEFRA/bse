
CREATE PROCEDURE GetOtherOwnerByRBSE
	@RBSE char(9)
AS

	SELECT
		[ID],
		[RBSE],
		[Type],
		[Name],
		[CPHH],
		[RowStamp]
	FROM
		[OtherOwner]
	WHERE
		RBSE = @RBSE

RETURN
