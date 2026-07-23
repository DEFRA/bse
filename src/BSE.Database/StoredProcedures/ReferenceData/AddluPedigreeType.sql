
CREATE PROCEDURE AddluPedigreeType
    @Code char(2),
    @Description varchar(50)
AS
	INSERT INTO luPedigreeType
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
