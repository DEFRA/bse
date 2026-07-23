
CREATE PROCEDURE AddluHerdType
    @Code char(1),
    @Description varchar(50)
AS
	INSERT INTO luHerdType
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
