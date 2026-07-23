
CREATE PROCEDURE [dbo].[AddluCaseType]
    @Code varchar(2),
    @Description varchar(50)
AS
	INSERT INTO luCaseType
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN

