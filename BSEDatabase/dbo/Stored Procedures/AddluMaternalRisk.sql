
CREATE PROCEDURE AddluMaternalRisk
    @Code varchar(10),
    @Description varchar(50)
AS
	INSERT INTO luMaternalRisk
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
