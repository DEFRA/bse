
CREATE PROCEDURE AddluAnimalOrigin
    @Code char(1),
    @Description varchar(10)
AS
	INSERT INTO 
		luAnimalOrigin
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
