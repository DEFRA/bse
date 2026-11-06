CREATE PROCEDURE [dbo].[HasCaseRemovalSince]
        @RBSE char(9),
        @Since datetime AS

        SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM [AuditLog]
                WHERE [TableName] = 'Case'
                  AND [LogDate] >= @Since
                  AND (
                        ([Reason] = 'Deletion' AND [RBSE] = @RBSE)
                        OR ([FieldName] = 'RBSE'
                            AND [BeforeValue] = LEFT(@RBSE, 2) + '/' + SUBSTRING(@RBSE, 3, 2) + '/' + RIGHT(@RBSE, 5))
                      )
        ) THEN 1 ELSE 0 END
