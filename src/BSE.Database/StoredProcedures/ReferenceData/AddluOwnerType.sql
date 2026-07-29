
CREATE PROCEDURE AddluOwnerType
    @Code varchar(10),
    @Description varchar(50)
AS
	INSERT INTO luOwnerType
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
