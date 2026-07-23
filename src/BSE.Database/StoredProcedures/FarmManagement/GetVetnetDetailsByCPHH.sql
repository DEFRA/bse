
CREATE PROCEDURE GetVetnetDetailsByCPHH 
	@CPHH char(11)
AS

SELECT
	[CPHH],
	[Herdmark],
	[NumericHerdmark]

FROM
	[VetnetHerdmark]
WHERE
	CPHH = @CPHH

RETURN
