
CREATE PROCEDURE AddluBreed
	@Code varchar(20),
	@FullName varchar(50),
	@AmalgamatedName varchar(50)

AS
	INSERT INTO luBreed
        		([Code], [FullName], [AmalgamatedName])
	VALUES
		(@Code, @FullName, @AmalgamatedName)
        
RETURN
