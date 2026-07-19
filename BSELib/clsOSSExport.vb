Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient

Public Class clsOSSExport

    Public Shared Function CreateBSE1bBatch(ByRef iBatchID As Integer, _
                                            ByRef iBatchYear As Integer, _
                                            ByRef iBatchNumber As Integer) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
                .AddParameter("BatchID", DbtType.dbtInteger, "@BatchID", daDirection:=ParameterDirection.Output)
                .AddParameter("BatchYear", DbtType.dbtInteger, "@BatchYear", daDirection:=ParameterDirection.Output)
                .AddParameter("BatchNumber", DbtType.dbtInteger, "@BatchNumber", daDirection:=ParameterDirection.Output)
            End With

            ExecuteNonQuery("AddBatchNumber1989", _
                          CommandType.StoredProcedure, _
                          objInParamList)

            iBatchID = CInt(objInParamList("BatchID").Value)
            iBatchYear = CInt(objInParamList("BatchYear").Value)
            iBatchNumber = CInt(objInParamList("BatchNumber").Value)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Function GetOSSExportDetailsByRBSE(ByRef sRBSE As String, ByRef sCPHH As String, ByRef sOwnerName As String, ByRef sAddress1 As String) As Boolean

        Dim objInParamList As New ParameterList()
        Dim dtData As New DataTable()

        Try
            objInParamList.QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)

            FillDataTable("GetOSSExportByRBSE", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If Not (dtData Is Nothing) Then
                If (dtData.Rows.Count() > 0) Then
                    sRBSE = dtData.Rows(0)(0).ToString()
                    sCPHH = dtData.Rows(0)(1).ToString()
                    sOwnerName = dtData.Rows(0)(2).ToString()
                    sAddress1 = dtData.Rows(0)(3).ToString()
                End If
            End If

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

    Public Function GetCaseByBatchID(ByVal iBatchID As Integer, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("BatchID", DbtType.dbtInteger, iBatchID)

            FillDataTable("GetCaseByBatchID", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try

    End Function

End Class
