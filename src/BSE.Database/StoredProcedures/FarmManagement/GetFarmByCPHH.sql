
CREATE PROCEDURE GetFarmByCPHH 
	@CPHH char(11)
AS

SELECT
	[CPHH],
	[IsNonGBFarm],
	[OwnerName],
	[Address1],
	[Address2],
	[Address3],
	[Postcode],
	[Parish],
	[District],
	[County],
	[CorrespondenceAddress1],
	[CorrespondenceAddress2],
	[CorrespondenceAddress3],
	[CorrespondencePostcode],
	[MapReference],
	[Herdmark1],
	[Herdmark2],
	[Herdmark3],
	[NumericHerdmark1],
	[NumericHerdmark2],
	[AHO],
	[HerdType],
	[PedigreeType],
	[IsDealer],
	[Farm].[ADNSRegionID],
	[luADNSRegion].[AuthorityID],
	[luAuthority].[AuthorityCountyID],
	[RowStamp]
FROM
	[Farm] LEFT JOIN [luADNSRegion] ON [Farm].[ADNSRegionID] = [luADNSRegion].[ID]
	LEFT JOIN [luAuthority] ON [luADNSRegion].[AuthorityID] = [luAuthority].[ID]
WHERE
	CPHH = @CPHH

RETURN
