

CREATE PROCEDURE AddluUserGroup
    @ID int,
    @Name varchar(50)
AS
	INSERT INTO luUserGroup
        		([ID], [Name])
	VALUES
		(@ID, @Name)
        
RETURN

