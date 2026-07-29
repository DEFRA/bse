

CREATE PROCEDURE [dbo].[GetLastADNSReferenceByArea]
	@Area char(2)
	AS

	SELECT
		[LastEmailReference],
		[LastADNSReferenceYear],
		[LastADNSReferenceNumber]
	FROM
		[ADNSArea]
	WHERE
		(Area = @Area)
