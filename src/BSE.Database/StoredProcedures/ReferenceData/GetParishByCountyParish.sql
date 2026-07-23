
CREATE PROCEDURE GetParishByCountyParish 
	@County char(2),
	@Parish char(3)
AS

	SELECT
		[luParish].[County],
		[luParish].[Parish],
		[luParish].[Name],
		[luParish].[ADNSRegionID],
		[luADNSRegion].[AuthorityID],
		[luAuthority].[AuthorityCountyID],
		[luParish].[BSECounty]		
	FROM
		[luParish] LEFT JOIN [luADNSRegion] ON [luParish].[ADNSRegionID] = [luADNSRegion].[ID]
		LEFT JOIN [luAuthority] ON [luADNSRegion].[AuthorityID] = [luAuthority].[ID]
	WHERE
		[luParish].[County] = @County AND [luParish].[Parish] = @Parish
	
	RETURN
