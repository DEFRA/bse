CREATE PROCEDURE AddBatchNumber1989
        @BatchID int OUTPUT,
        @BatchYear smallint OUTPUT,
        @BatchNumber int OUTPUT AS

        DECLARE
                @RowCount int,
                @ErrorCode int

        BEGIN TRANSACTION

        INSERT INTO [Batch]
                (
                [BatchYear],
                [BatchNumber]
                )
        SELECT
                1989,
                ISNULL(MAX([BatchNumber]),0) + 1  -- add 1 to the highest existing batch number for this year.  If this year has no batches, start with 1
        FROM
                [Batch]
        WHERE
                [BatchYear] =1989

        SELECT
                @ErrorCode = @@ERROR,
                @RowCount = @@ROWCOUNT,
                @BatchID = SCOPE_IDENTITY()

        IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
                ROLLBACK TRANSACTION
                RETURN 1
        END

        SELECT
                @BatchYear = [BatchYear],
                @BatchNumber = [BatchNumber]
        FROM
                [Batch]
        WHERE
                [BatchID] = @BatchID

        SELECT
                @ErrorCode = @@ERROR,
                @RowCount = @@ROWCOUNT

        IF @RowCount <> 1 OR @ErrorCode <> 0 BEGIN
                ROLLBACK TRANSACTION
                RETURN 2
        END

        COMMIT TRANSACTION
        RETURN 0
