set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


CREATE PROCEDURE [dbo].[AddluRegionalLab]
    @Code char(4),
    @Description varchar(50)
AS
	INSERT INTO luRegionalLab
        		([Code], [Description])
	VALUES
		(@Code, @Description)
        
RETURN

 