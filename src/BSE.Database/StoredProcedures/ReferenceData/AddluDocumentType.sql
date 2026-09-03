
CREATE PROCEDURE AddluDocumentType
    @Code varchar(5),
    @Description varchar(50)
AS
	INSERT INTO luDocumentType
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
