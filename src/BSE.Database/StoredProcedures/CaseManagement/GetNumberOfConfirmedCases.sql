
CREATE PROCEDURE GetNumberOfConfirmedCases
	@CPHH char(11)
AS

SELECT
	COUNT(*)
FROM
	[Case]
WHERE
	[CPHH] = @CPHH AND [FinalResult] = 'Pos'

RETURN
