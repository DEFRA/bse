
CREATE VIEW dbo.COUNTY
AS
SELECT     dbo.luBSECounty.Code AS County, UPPER(dbo.luBSERegion.Name) AS Region, dbo.luBSERegion.SortOrder AS Region_Order, 
                      UPPER(dbo.luCountry.Description) AS Country, dbo.luBSECounty.Description AS CountyProper, dbo.luBSERegion.Name AS RegionProper
FROM         dbo.luBSECounty INNER JOIN
                      dbo.luBSERegion ON dbo.luBSECounty.BSERegionID = dbo.luBSERegion.ID INNER JOIN
                      dbo.luCountry ON dbo.luBSERegion.CountryID = dbo.luCountry.ID

