/****** Object:  StoredProcedure [dbo].[GetBatchIDForBatch]    Script Date: 18/09/2026 16:33:57 ******/
SET ANSI_NULLS OFF
GO
 
SET QUOTED_IDENTIFIER ON
GO
 

-- Idempotent: DROP + CREATE ensures no conflicts on re-deployment
IF OBJECT_ID('[dbo].[GetBatchIDForBatch]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[GetBatchIDForBatch];
GO

CREATE OR ALTER PROCEDURE [dbo].[GetBatchIDForBatch]
	@BatchYear smallint,
	@BatchNumber int,
	@BatchID int OUTPUT AS

	SELECT
		@BatchID = [BatchID]
	FROM
		[Batch]
	WHERE
		([BatchYear] = @BatchYear OR ([BatchYear] IS NULL AND @BatchYear IS NULL)) AND
		[BatchNumber] = @BatchNumber 
GO
