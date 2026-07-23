
CREATE PROCEDURE GetFarmsByCPH 
	@CPH varchar(11)
AS

SELECT
	LEFT([CPHH], 2) + '/' + SUBSTRING([CPHH], 3, 3) + '/' + SUBSTRING([CPHH], 6, 4) + '/' + RIGHT([CPHH], 2) AS [CPHH],
	[OwnerName],
	[Address1]

FROM
	[Farm]
WHERE
	(CPHH LIKE @CPH + '%')

RETURN
