
CREATE PROCEDURE AddluADNSRegion
    @ID int,
    @Name varchar(50)
AS
	INSERT INTO luADNSRegion
        		([ID], [Name])
	VALUES
		(@ID, @Name)
        
RETURN
