
CREATE PROCEDURE AddluRationType
    @Code varchar(2),
    @Description varchar(50)
AS
	INSERT INTO luRationType
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
