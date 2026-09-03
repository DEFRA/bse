
CREATE PROCEDURE GetEditableLookupProcs
    @ID int
AS
    SELECT 
	[SelectStoredProcedure], 
	[UpdateStoredProcedure], 
	[InsertStoredProcedure], 
	[DeleteStoredProcedure]
    FROM 
	EditableLookup 
    WHERE 
	[ID] = @ID
RETURN
