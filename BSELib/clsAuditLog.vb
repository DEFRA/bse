Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA

Public Class clsAuditLog

    Public Shared Function GetCaseAuditLogReport(ByVal sRBSE As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)
            End With

            FillDataTable("GetAuditLogByCase", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Shared Function GetDailyAuditLogReport(ByVal dDate As Date, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("LogDate", DbtType.dbtDateTime, dDate)
            End With

            FillDataTable("GetAuditLogByDate", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Shared Function GetFarmAuditLogReport(ByVal sCPHH As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)
            End With

            FillDataTable("GetAuditLogByFarm", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Shared Function GetUserAuditLogReport(ByVal iUserID As Integer, ByVal dStartDate As Date, ByVal dEndDate As Date, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("StartDate", DbtType.dbtDateTime, dStartDate)
                .QuickAddInputParam("EndDate", DbtType.dbtDateTime, dEndDate)
                .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
            End With

            FillDataTable("GetAuditLogByUser", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Shared Function GetNewFarmsAuditLogReport(ByVal dStartDate As Date, ByVal dEndDate As Date, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("StartDate", DbtType.dbtDateTime, dStartDate)
                .QuickAddInputParam("EndDate", DbtType.dbtDateTime, dEndDate)
            End With

            FillDataTable("GetAuditLogNewFarms", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Shared Function GetCPHHChangesAuditLogReport(ByVal dStartDate As Date, ByVal dEndDate As Date, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("StartDate", DbtType.dbtDateTime, dStartDate)
                .QuickAddInputParam("EndDate", DbtType.dbtDateTime, dEndDate)
            End With

            FillDataTable("GetAuditLogCPHHChanges", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Shared Function GetRBSEChangesAuditLogReport(ByVal dStartDate As Date, ByVal dEndDate As Date, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("StartDate", DbtType.dbtDateTime, dStartDate)
                .QuickAddInputParam("EndDate", DbtType.dbtDateTime, dEndDate)
            End With

            FillDataTable("GetAuditLogRBSEChanges", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Shared Function GetCaseMovesAuditLogReport(ByVal dStartDate As Date, ByVal dEndDate As Date, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .QuickAddInputParam("StartDate", DbtType.dbtDateTime, dStartDate)
                .QuickAddInputParam("EndDate", DbtType.dbtDateTime, dEndDate)
            End With

            FillDataTable("GetAuditLogCaseMoves", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function
End Class
