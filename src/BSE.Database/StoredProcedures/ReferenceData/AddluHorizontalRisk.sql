
CREATE PROCEDURE AddluHorizontalRisk
    @Code varchar(10),
    @Description varchar(50)
AS
	INSERT INTO luHorizontalRisk
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
