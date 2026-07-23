
CREATE PROCEDURE AddluCaseFate
    @Code varchar(4),
    @Description varchar(50)
AS
	INSERT INTO luCaseFate
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
