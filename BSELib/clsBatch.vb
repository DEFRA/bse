Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient

Public Class BatchLinkException : Inherits ApplicationException

    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
    End Sub

End Class

Public Class clsBatch

    Public Shared Function CreateBatchNumberLink(ByVal iBatchID As Integer, ByVal sRBSE As String, ByVal sDocument As String, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
                .QuickAddInputParam("BatchID", DbtType.dbtInteger, iBatchID)
                .QuickAddInputParam("RBSE", DbtType.dbtString, sRBSE)
                .QuickAddInputParam("Document", DbtType.dbtString, sDocument)
            End With

            If (objDBConn Is Nothing) Then
                ExecuteNonQuery("AddBatchNumberLink", _
                              CommandType.StoredProcedure, _
                              objInParamList)
            Else
                ExecuteNonQuery(objDBConn, objDBTran, "AddBatchNumberLink", _
                              CommandType.StoredProcedure, _
                              objInParamList)
            End If

            Dim iReturnValue As Integer = CInt(objInParamList("RETURN_VALUE").Value)

            Select Case iReturnValue
                Case 1
                    Throw New ApplicationException("BatchID does not exist in Batch Table")
                Case 2
                    'Throw New BatchLinkException("Link already exists")
                Case 3
                    Throw New ApplicationException("Could not create link")
            End Select

            Return True

        Catch ex As BatchLinkException
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw ex
        End Try

    End Function

    Public Shared Function GetBatchIDForBatch(ByVal sBatchYear As String, _
                                             ByVal iBatchNumber As Integer, _
                                             ByRef iBatchID As Integer) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            With objInParamList
                .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
                If sBatchYear = "" Then
                    .QuickAddInputParam("BatchYear", DbtType.dbtSmallInt, DBNull.Value)
                Else
                    .QuickAddInputParam("BatchYear", DbtType.dbtSmallInt, CInt(sBatchYear))
                End If
                .QuickAddInputParam("BatchNumber", DbtType.dbtInteger, iBatchNumber)
                .AddParameter("BatchID", DbtType.dbtInteger, "@BatchID", daDirection:=ParameterDirection.Output)
            End With

            ExecuteNonQuery("GetBatchIDForBatch", _
                          CommandType.StoredProcedure, _
                          objInParamList)

            iBatchID = CInt(objInParamList("BatchID").Value)
            Return (iBatchID > 0)

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try

    End Function

    Public Shared Function CreateBatchNumber(ByRef iBatchID As Integer, _
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

            ExecuteNonQuery("AddBatchNumber", _
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

    Public Shared Function GetDetailsForBatch(ByVal sBatchYear As String, _
                                          ByVal sBatchNumber As String, _
                                          ByRef dtData As DataTable, _
                                          ByVal sStoredProcedureName As String) As Boolean

        Dim iBatchID As Integer
        Dim objInParamList As New ParameterList()

        Try
            If GetBatchIDForBatch(sBatchYear, CInt(sBatchNumber), iBatchID) Then

                objInParamList.QuickAddInputParam("BatchID", DbtType.dbtInteger, iBatchID)

                TBCultureDA.FillDataTable(sStoredProcedureName, _
                                          CommandType.StoredProcedure, _
                                          dtData, _
                                          objInParamList)

                Return True
            End If
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Return False
        End Try
    End Function

    Public Shared Function GetLatestBatchNumbers() As DataTable

        Dim dtData As DataTable
        Try

            TBCultureDA.FillDataTable("GetLatestBatchNumbers", _
                                          CommandType.StoredProcedure, _
                                          dtData)

            Return dtData

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw ex
        End Try
    End Function
End Class
