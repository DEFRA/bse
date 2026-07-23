
CREATE PROCEDURE GetBABByRBSE
	@RBSE char(9)
AS

SELECT
	[RBSE],
	[NatalCPHH],
	[Notes],
	[TracedName],
	[TracedAddress1],
	[TracedAddress2],
	[TracedAddress3],
	[TracedPostcode],
	[FeedRisk],
	[HorizontalRisk],
	[MaternalRisk],
	[RowStamp]
FROM
	[CaseBAB]
WHERE
	RBSE = @RBSE

RETURN
