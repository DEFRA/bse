
CREATE PROCEDURE AddluValuationAge
    @Code char(1),
    @Description varchar(50)
AS
	INSERT INTO luValuationAge
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN
