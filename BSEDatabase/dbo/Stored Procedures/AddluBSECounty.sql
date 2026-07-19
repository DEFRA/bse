
CREATE PROCEDURE AddluBSECounty
	@IDColumn char(2),
	@Code varchar(15),
	@Description varchar(50),
	@BSERegionID int

AS
	INSERT INTO luBSECounty
        		([ID], [Code], [Description], [BSERegionID])
	VALUES
		(@IDColumn, @Code, @Description, @BSERegionID)
        
RETURN
