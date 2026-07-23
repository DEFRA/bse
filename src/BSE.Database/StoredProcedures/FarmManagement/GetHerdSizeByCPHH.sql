
CREATE PROCEDURE GetHerdSizeByCPHH 
	@CPHH char(11)
AS

SELECT
	[ID],
	[CPHH],
	[HerdYear],
	[TotalSize],
	[Lactation1Size],
	[Lactation2Size],
	[Lactation3Size],
	[Lactation4Size],
	[Lactation5Size],
	[Lactation6Size],
	[Lactation7Size],
	[Lactation8Size],
	[Lactation9Size],
	[Lactation10Size],
	[Lactation10PlusSize],
	[RowStamp]

FROM
	[HerdSize]
WHERE
	CPHH = @CPHH

RETURN
