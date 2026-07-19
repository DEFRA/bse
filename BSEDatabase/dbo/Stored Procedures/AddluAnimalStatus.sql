
CREATE PROCEDURE AddluAnimalStatus
    @Code varchar(9),
    @Description varchar(50)
AS
	INSERT INTO luAnimalStatus
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
