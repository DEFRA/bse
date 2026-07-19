Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient

Public Class FarmUpdateException : Inherits ApplicationException

    Public Sub New(ByVal message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(ByVal message As String, ByVal inner As Exception)
        MyBase.New(message, inner)
    End Sub

End Class

Public Class clsFarm

    ' Farm Entry Dataset Tables
    Public Const FARM_TABLE As Integer = 0
    Public Const RELATED_FARMS_TABLE As Integer = 1
    Public Const HERDSIZE_TABLE As Integer = 2

    Public Function FarmInDatabase(ByVal sCPHH As String) As Boolean
        Dim objInParamList As New ParameterList()

        Try

            objInParamList.QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)

            Dim dtData As New DataTable()

            FillDataTable("GetFarmByCPHH", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If dtData Is Nothing Then
                Return False
            Else
                If dtData.Rows.Count = 0 Then
                    Return False
                Else
                    Return True
                End If

            End If

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return True
        End Try
    End Function

    Public Function ChangeCPHH(ByVal iUserID As Integer, ByRef sOldCPHH As String, ByVal sNewCPHH As String) As Boolean

        Dim objFarmParamList As New libDataAccess.libDataAccess.ParameterList()

        With objFarmParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("OldCPHH", DbtType.dbtString, sOldCPHH)
            .QuickAddInputParam("NewCPHH", DbtType.dbtString, sNewCPHH)
            .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
        End With

        Try
            TBCultureDA.ExecuteNonQuery("ChangeCPHH", CommandType.StoredProcedure, objFarmParamList)
            Dim iReturnValue As Integer = CInt(objFarmParamList("RETURN_VALUE").Value)

            Select Case iReturnValue
                Case 1
                    Throw New Exception("Farm does not exist in the Farm table")
                Case 2
                    Throw New Exception("Error adding new row to Farm table")
                Case 3
                    Throw New Exception("Error updating the Farm Historical table")
                Case 4
                    Throw New Exception("Error updating the Farm Relation table")
                Case 5
                    Throw New Exception("Error updating the Herdsize table")
                Case 6
                    Throw New Exception("Error updating the Case table")
                Case 7
                    Throw New Exception("Error updating the OtherOwner table")
                Case 8
                    Throw New Exception("Error deleting a row from the Farm table")
            End Select

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

    Public Function GetNumberOfConfirmedCases(ByVal sCPHH As String, _
                              ByRef sConfirmedCases As String) As Boolean

        Dim objInParamList As New ParameterList()

        Try

            objInParamList.QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)

            Dim dtData As New DataTable()

            FillDataTable("GetNumberOfConfirmedCases", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If Not (dtData Is Nothing) Then
                If (dtData.Rows.Count() > 0) Then
                    sConfirmedCases = dtData.Rows(0)(0).ToString()

                    dtData = Nothing
                End If
            End If

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

    Public Function GetCountyParishForAuthority(ByVal sCPHH As String, _
                                ByRef sParishName As String, _
                                ByRef sCounty As String, _
                                ByRef sADNSRegionID As String, _
                                ByRef sAuthorityID As String, _
                                ByRef sAuthorityCountyID As String) As Boolean

        Dim objInParamList As New ParameterList()

        Try

            objInParamList.QuickAddInputParam("County", DbtType.dbtString, Left$(sCPHH, 2))
            objInParamList.QuickAddInputParam("Parish", DbtType.dbtString, Mid$(sCPHH, 3, 3))

            Dim dtData As New DataTable()

            FillDataTable("GetParishByCountyParish", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If Not (dtData Is Nothing) Then
                If (dtData.Rows.Count() > 0) Then
                    'County = dtData.Rows(0)(0)
                    'Parish = dtData.Rows(0)(1)
                    sParishName = CStr(GetRowColumnData(dtData.Rows(0)(2)))
                    sADNSRegionID = CStr(GetRowColumnData(dtData.Rows(0)(3)))
                    sAuthorityID = CStr(GetRowColumnData(dtData.Rows(0)(4)))
                    sAuthorityCountyID = CStr(GetRowColumnData(dtData.Rows(0)(5)))
                    sCounty = CStr(GetRowColumnData(dtData.Rows(0)(6)))

                    dtData = Nothing
                End If
            End If

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function


    Public Function GetVetnetDetails(ByVal sCPHH As String, _
                              ByRef sHerdmark As String, _
                              ByRef sNumericHerdmark As String) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)

            Dim dtData As New DataTable()

            FillDataTable("GetVetnetDetailsByCPHH", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If Not (dtData Is Nothing) Then
                If (dtData.Rows.Count() > 0) Then
                    'sCPHH = dtData.Rows(0)(0)
                    sHerdmark = CStr(GetRowColumnData(dtData.Rows(0)(1)))
                    sNumericHerdmark = CStr(GetRowColumnData(dtData.Rows(0)(2)))
                    dtData = Nothing
                End If
            End If

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try
    End Function

    Public Function GetFarmDetails(ByVal sCPHH As String, ByRef dsData As DataSet) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)

            FillDataSet("GetFarmDetailsByCPHH", _
                          CommandType.StoredProcedure, _
                          dsData, _
                          objInParamList)

            SetPrimaryKey(dsData.Tables(FARM_TABLE), "CPHH", False)
            SetPrimaryKey(dsData.Tables(RELATED_FARMS_TABLE), "ID", True)
            SetPrimaryKey(dsData.Tables(HERDSIZE_TABLE), "ID", True)
            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try

    End Function

    Public Function GetHerdSizeByCPHH(ByVal sCPHH As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)

            FillDataTable("GetHerdSizeByCPHH", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try

    End Function

    Public Function GetFarmsByCPH(ByVal sCPH As String, ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("CPH", DbtType.dbtString, sCPH)

            FillDataTable("GetFarmsByCPH", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try

    End Function

    Public Function GetByCPHH(ByVal sCPHH As String, _
                                  ByRef bIsNonGBFarm As Boolean, _
                                  ByRef sOwnerName As String, _
                                  ByRef sAddress1 As String, _
                                  ByRef sAddress2 As String, _
                                  ByRef sAddress3 As String, _
                                  ByRef sPostcode As String, _
                                  ByRef sParish As String, _
                                  ByRef sDistrict As String, _
                                  ByRef sCounty As String, _
                                  ByRef sCorrespondenceAddress1 As String, _
                                  ByRef sCorrespondenceAddress2 As String, _
                                  ByRef sCorrespondenceAddress3 As String, _
                                  ByRef sCorrespondencePostcode As String, _
                                  ByRef sMapReference As String, _
                                  ByRef sHerdmark1 As String, _
                                  ByRef sHerdmark2 As String, _
                                  ByRef sHerdmark3 As String, _
                                  ByRef sNumericHerdmark1 As String, _
                                  ByRef sNumericHerdmark2 As String, _
                                  ByRef sAHO As String, _
                                  ByRef sHerdType As String, _
                                  ByRef sPedigree As String, _
                                  ByRef bIsDealer As Boolean, _
                                  ByRef RowStamp As Long) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)

            Dim dtData As New DataTable()

            FillDataTable("GetFarmByCPHH", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If Not (dtData Is Nothing) Then
                If (dtData.Rows.Count() > 0) Then
                    'sCPHH = dtData.Rows(0)(0)
                    bIsNonGBFarm = CBool(GetRowColumnData(dtData.Rows(0)(1)))
                    sOwnerName = CStr(GetRowColumnData(dtData.Rows(0)(2)))
                    sAddress1 = CStr(GetRowColumnData(dtData.Rows(0)(3)))
                    sAddress2 = CStr(GetRowColumnData(dtData.Rows(0)(4)))
                    sAddress3 = CStr(GetRowColumnData(dtData.Rows(0)(5)))
                    sPostcode = CStr(GetRowColumnData(dtData.Rows(0)(6)))
                    sParish = CStr(GetRowColumnData(dtData.Rows(0)(7)))
                    sDistrict = CStr(GetRowColumnData(dtData.Rows(0)(8)))
                    sCounty = CStr(GetRowColumnData(dtData.Rows(0)(9)))
                    sCorrespondenceAddress1 = CStr(GetRowColumnData(dtData.Rows(0)(10)))
                    sCorrespondenceAddress2 = CStr(GetRowColumnData(dtData.Rows(0)(11)))
                    sCorrespondenceAddress3 = CStr(GetRowColumnData(dtData.Rows(0)(12)))
                    sCorrespondencePostcode = CStr(GetRowColumnData(dtData.Rows(0)(13)))
                    sMapReference = CStr(GetRowColumnData(dtData.Rows(0)(14)))
                    sHerdmark1 = CStr(GetRowColumnData(dtData.Rows(0)(15)))
                    sHerdmark2 = CStr(GetRowColumnData(dtData.Rows(0)(16)))
                    sHerdmark3 = CStr(GetRowColumnData(dtData.Rows(0)(17)))
                    sNumericHerdmark1 = CStr(GetRowColumnData(dtData.Rows(0)(18)))
                    sNumericHerdmark2 = CStr(GetRowColumnData(dtData.Rows(0)(19)))
                    sAHO = CStr(GetRowColumnData(dtData.Rows(0)(20)))
                    sHerdType = CStr(GetRowColumnData(dtData.Rows(0)(21)))
                    sPedigree = CStr(GetRowColumnData(dtData.Rows(0)(22)))
                    bIsDealer = CBool(GetRowColumnData(dtData.Rows(0)(23)))
                    'RowStamp = GetRowColumnData(dtData.Rows(0)(24))
                    RowStamp = Nothing

                    dtData = Nothing
                End If
            End If

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try

    End Function

    Public Function GetRelatedFarm(ByVal sCPHH As String, _
                                  ByRef dtData As DataTable) As Boolean

        Dim objInParamList As New ParameterList()

        Try
            objInParamList.QuickAddInputParam("CPHH", DbtType.dbtString, sCPHH)

            FillDataTable("GetRelatedFarm", _
                          CommandType.StoredProcedure, _
                          dtData, _
                          objInParamList)

            If Not (dtData Is Nothing) Then
                Return True
            Else
                Return False
            End If

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsSubmissionObject)
            Return False
        End Try

    End Function

    Private Function GetRowColumnData(ByRef objValue As Object) As Object
        If (IsDBNull(objValue)) Then
            Return Nothing
        Else
            Return objValue
        End If
    End Function

#Region "Update Farm Details"

    Friend Sub UpdateFarmDetails(ByVal iUserID As Integer, ByRef dsFarm As DataSet, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)
        Dim clsDataCheck As New BSELib.clsDataCheck()

        Try
            If clsDataCheck.DataTableHasChanges(dsFarm.Tables(FARM_TABLE)) Then
                UpdateFarmRecord(iUserID, dsFarm.Tables(FARM_TABLE).Rows(0), objDBConn, objDBTran, objErrorList)
            End If

            If clsDataCheck.DataTableHasChanges(dsFarm.Tables(RELATED_FARMS_TABLE)) Then
                UpdateRelatedFarms(dsFarm.Tables(RELATED_FARMS_TABLE), objDBConn, objDBTran, objErrorList)
            End If

            If clsDataCheck.DataTableHasChanges(dsFarm.Tables(HERDSIZE_TABLE)) Then
                UpdateHerdSizeRecords(dsFarm.Tables(HERDSIZE_TABLE), objDBConn, objDBTran, objErrorList)
            End If

        Catch exFarmUpdate As FarmUpdateException
            objErrorList.Add(exFarmUpdate.Message)
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            objErrorList.Add("An application error caused the farm update to fail")
        End Try

    End Sub

    Private Sub UpdateFarmRecord(ByVal iUserID As Integer, ByRef drFarmRow As DataRow, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        Dim objFarmParamList As New libDataAccess.libDataAccess.ParameterList()

        With objFarmParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("CPHH", DbtType.dbtString, drFarmRow.Item("CPHH"))
            .QuickAddInputParam("OwnerName", DbtType.dbtString, drFarmRow.Item("OwnerName"))
            .QuickAddInputParam("Address1", DbtType.dbtString, drFarmRow.Item("Address1"))
            .QuickAddInputParam("Address2", DbtType.dbtString, drFarmRow.Item("Address2"))
            .QuickAddInputParam("Address3", DbtType.dbtString, drFarmRow.Item("Address3"))
            .QuickAddInputParam("Postcode", DbtType.dbtString, drFarmRow.Item("Postcode"))
            .QuickAddInputParam("Parish", DbtType.dbtString, drFarmRow.Item("Parish"))
            .QuickAddInputParam("District", DbtType.dbtString, drFarmRow.Item("District"))
            .QuickAddInputParam("County", DbtType.dbtString, drFarmRow.Item("County"))
            .QuickAddInputParam("CorrespondenceAddress1", DbtType.dbtString, drFarmRow.Item("CorrespondenceAddress1"))
            .QuickAddInputParam("CorrespondenceAddress2", DbtType.dbtString, drFarmRow.Item("CorrespondenceAddress2"))
            .QuickAddInputParam("CorrespondenceAddress3", DbtType.dbtString, drFarmRow.Item("CorrespondenceAddress3"))
            .QuickAddInputParam("CorrespondencePostcode", DbtType.dbtString, drFarmRow.Item("CorrespondencePostcode"))
            .QuickAddInputParam("MapReference", DbtType.dbtString, drFarmRow.Item("MapReference"))
            .QuickAddInputParam("Herdmark1", DbtType.dbtString, drFarmRow.Item("Herdmark1"))
            .QuickAddInputParam("Herdmark2", DbtType.dbtString, drFarmRow.Item("Herdmark2"))
            .QuickAddInputParam("Herdmark3", DbtType.dbtString, drFarmRow.Item("Herdmark3"))
            .QuickAddInputParam("NumericHerdmark1", DbtType.dbtString, drFarmRow.Item("NumericHerdmark1"))
            .QuickAddInputParam("NumericHerdmark2", DbtType.dbtString, drFarmRow.Item("NumericHerdmark2"))
            .QuickAddInputParam("AHO", DbtType.dbtString, drFarmRow.Item("AHO"))
            .QuickAddInputParam("HerdType", DbtType.dbtString, drFarmRow.Item("HerdType"))
            .QuickAddInputParam("PedigreeType", DbtType.dbtString, drFarmRow.Item("PedigreeType"))
            .QuickAddInputParam("IsDealer", DbtType.dbtBoolean, drFarmRow.Item("IsDealer"))
            .QuickAddInputParam("ADNSRegionID", DbtType.dbtInteger, drFarmRow.Item("ADNSRegionID"))
            If drFarmRow.RowState = DataRowState.Modified Then
                .QuickAddInputParam("RowStamp", DbtType.dbtBinary, drFarmRow.Item("RowStamp"))
            End If
            .QuickAddInputParam("UserID", DbtType.dbtInteger, iUserID)
        End With

        Select Case drFarmRow.RowState

            'create a farm record
        Case DataRowState.Added
                Try
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "AddFarm", CommandType.StoredProcedure, objFarmParamList)
                Catch ex As Exception
                    clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
                    Throw New FarmUpdateException(ex.Message, ex.InnerException)
                End Try
                Dim iReturnValue As Integer = CInt(objFarmParamList("RETURN_VALUE").Value)
                Select Case iReturnValue
                    Case 1
                        Throw New FarmUpdateException("Failed to insert into the audit log")
                    Case 2
                        Throw New FarmUpdateException("Failed to insert into the Farm table")
                    Case 3
                        Throw New FarmUpdateException("A farm with CPHH" & drFarmRow.Item("CPHH").ToString() & " has already been created by another user")
                End Select

                'update an existing farm record
            Case DataRowState.Modified
                Try
                    TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditFarm", CommandType.StoredProcedure, objFarmParamList)
                Catch ex As Exception
                    clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
                    Throw New FarmUpdateException(ex.Message, ex.InnerException)
                End Try
                Dim iReturnValue As Integer = CInt(objFarmParamList("RETURN_VALUE").Value)
                Select Case iReturnValue
                    Case 1
                        Throw New FarmUpdateException("The farm with CPHH " & drFarmRow.Item("CPHH").ToString() & " has been deleted by another user")
                    Case 2
                        Throw New FarmUpdateException("Failed to insert into the audit log")
                    Case 3
                        objErrorList.Add("The farm record with CPHH " & drFarmRow.Item("CPHH").ToString() & " has been modified by another user")
                    Case 4
                        Throw New FarmUpdateException("Failed to update the Farm table")
                End Select
        End Select
    End Sub

    Private Sub UpdateRelatedFarms(ByRef dtRelatedFarm As DataTable, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        'update the related farms records
        Dim objRelatedFarmsParamList As New libDataAccess.libDataAccess.UpdateParameterList()

        With objRelatedFarmsParamList
            .AddInsertParam("RETURN_VALUE", DbtType.dbtInteger, daDirection:=ParameterDirection.ReturnValue)
            .AddInsertParam("CPHH", DbtType.dbtString)
            .AddInsertParam("RelatedCPHH", DbtType.dbtString)

            .AddUpdateParam("RETURN_VALUE", DbtType.dbtInteger, daDirection:=ParameterDirection.ReturnValue)
            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("RelatedCPHH", DbtType.dbtString)
            .AddUpdateParam("RowStamp", DbtType.dbtBinary)

            .AddDeleteParam("RETURN_VALUE", DbtType.dbtInteger, daDirection:=ParameterDirection.ReturnValue)
            .AddDeleteParam("ID", DbtType.dbtInteger)
            .AddDeleteParam("RowStamp", DbtType.dbtBinary)
        End With
        TBCultureDA.OptimisticUpdateDataTable(objDBConn, objDBTran, AddressOf OnRelatedFarmRowUpdated, "", "AddFarmRelation", "EditFarmRelation", "DeleteFarmRelation", _
                                    CommandType.StoredProcedure, _
                                    dtRelatedFarm, objRelatedFarmsParamList)

        AddRowErrorsToList("related farm", "RelatedCPHH", dtRelatedFarm, objErrorList)

    End Sub

    Private Sub OnRelatedFarmRowUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        'Dim iReturnValue As Integer = args.Command.Parameters("@RETURN_VALUE").Value

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Data was changed by another user"
                args.Status = UpdateStatus.SkipCurrentRow
            End If
        End If
    End Sub
    Private Sub UpdateHerdSizeRecords(ByRef dtHerdSize As DataTable, ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByRef objErrorList As ArrayList)

        'update the herd size records
        Dim objHerdSizeParamList As New libDataAccess.libDataAccess.UpdateParameterList()

        With objHerdSizeParamList
            .AddInsertParam("CPHH", DbtType.dbtString)
            .AddInsertParam("HerdYear", DbtType.dbtInteger)
            .AddInsertParam("TotalSize", DbtType.dbtInteger)
            .AddInsertParam("Lactation1Size", DbtType.dbtInteger)
            .AddInsertParam("Lactation2Size", DbtType.dbtInteger)
            .AddInsertParam("Lactation3Size", DbtType.dbtInteger)
            .AddInsertParam("Lactation4Size", DbtType.dbtInteger)
            .AddInsertParam("Lactation5Size", DbtType.dbtInteger)
            .AddInsertParam("Lactation6Size", DbtType.dbtInteger)
            .AddInsertParam("Lactation7Size", DbtType.dbtInteger)
            .AddInsertParam("Lactation8Size", DbtType.dbtInteger)
            .AddInsertParam("Lactation9Size", DbtType.dbtInteger)
            .AddInsertParam("Lactation10Size", DbtType.dbtInteger)
            .AddInsertParam("Lactation10PlusSize", DbtType.dbtInteger)

            .AddUpdateParam("ID", DbtType.dbtInteger)
            .AddUpdateParam("HerdYear", DbtType.dbtInteger)
            .AddUpdateParam("TotalSize", DbtType.dbtInteger)
            .AddUpdateParam("Lactation1Size", DbtType.dbtInteger)
            .AddUpdateParam("Lactation2Size", DbtType.dbtInteger)
            .AddUpdateParam("Lactation3Size", DbtType.dbtInteger)
            .AddUpdateParam("Lactation4Size", DbtType.dbtInteger)
            .AddUpdateParam("Lactation5Size", DbtType.dbtInteger)
            .AddUpdateParam("Lactation6Size", DbtType.dbtInteger)
            .AddUpdateParam("Lactation7Size", DbtType.dbtInteger)
            .AddUpdateParam("Lactation8Size", DbtType.dbtInteger)
            .AddUpdateParam("Lactation9Size", DbtType.dbtInteger)
            .AddUpdateParam("Lactation10Size", DbtType.dbtInteger)
            .AddUpdateParam("Lactation10PlusSize", DbtType.dbtInteger)
            .AddUpdateParam("RowStamp", DbtType.dbtBinary)

            .AddDeleteParam("ID", DbtType.dbtInteger)
            .AddDeleteParam("RowStamp", DbtType.dbtBinary)

        End With
        TBCultureDA.OptimisticUpdateDataTable(objDBConn, objDBTran, AddressOf OnHerdSizeRowUpdated, "", "AddHerdSize", "EditHerdSize", "DeleteHerdSize", _
                                    CommandType.StoredProcedure, _
                                    dtHerdSize, objHerdSizeParamList)

        AddRowErrorsToList("herd size", "Year", dtHerdSize, objErrorList)

    End Sub

    Private Sub OnHerdSizeRowUpdated(ByVal sender As Object, ByVal args As SqlRowUpdatedEventArgs)

        'Dim iReturnValue As Integer = args.Command.Parameters("@RETURN_VALUE").Value

        If args.Status = UpdateStatus.ErrorsOccurred Then
            args.Row.RowError = args.Errors.Message
            args.Status = UpdateStatus.SkipCurrentRow
        Else
            If args.RecordsAffected = 0 Then
                args.Row.RowError = "Data was changed by another user"
                args.Status = UpdateStatus.SkipCurrentRow
            End If
        End If

    End Sub

    Private Sub AddRowErrorsToList(ByVal sTableName As String, ByVal sReportColumn As String, ByRef dtData As DataTable, ByRef objErrorList As ArrayList)

        Dim drData As DataRow
        For Each drData In dtData.Rows
            If drData.HasErrors Then
                Dim objMessage As New System.Text.StringBuilder()
                objMessage.Append("Failed to ")
                Select Case drData.RowState
                    Case DataRowState.Added
                        objMessage.Append("add ")
                    Case DataRowState.Modified
                        objMessage.Append("update ")
                    Case DataRowState.Deleted
                        objMessage.Append("delete ")
                End Select
                objMessage.Append(sTableName)
                objMessage.Append(" with ")
                objMessage.Append(sReportColumn)
                objMessage.Append(" """)
                objMessage.Append(drData.Item(sReportColumn))
                objMessage.Append(""" :")
                objMessage.Append(drData.RowError)

                objErrorList.Add(objMessage.ToString())
            End If
        Next

    End Sub
#End Region

End Class
