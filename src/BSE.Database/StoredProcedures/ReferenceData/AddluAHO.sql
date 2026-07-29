
CREATE PROCEDURE AddluAHO
    @Code char(2),
    @Name varchar(50),
    @BSERegionID int
AS
	INSERT INTO luAHO
        		([Code], [Name], [BSERegionID])
	VALUES
		(@Code, @Name, @BSERegionID)
        
RETURN
