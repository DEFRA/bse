Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Web

Public Class LookupData

    Public Function ListEditableLookups() As DataTable

        Try
            Dim dtData As New DataTable()
            TBCultureDA.FillDataTable("GetEditableLookups", _
                                      CommandType.StoredProcedure, _
                                      dtData)
            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Public Function GetNonGBCounty() As DataTable

        Dim dtData As New DataTable()

        Try
            FillDataTable("GetNonGBCounty", _
                          CommandType.StoredProcedure, _
                          dtData)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Public Function GetBSERegionID() As DataTable

        Dim dtData As New DataTable()

        Try
            FillDataTable("GetluBSERegion", _
                          CommandType.StoredProcedure, _
                          dtData)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Public Function GetLookupData(ByVal TableID As Integer) As DataTable

        Dim dtData As New DataTable()

        Try
            Dim sProcName As String = GetSelectProc(TableID)
            If sProcName = "" Then
                Throw New Exception("The look-up table Select procedure could not " _
                                    & "be found for table ID " & TableID.ToString)
            End If

            FillDataTable(sProcName, _
                          CommandType.StoredProcedure, _
                          dtData)

            ' set the ID field as the primary key - this should be the same for
            ' all lookup tables
            SetPrimaryKey(dtData, "ID", True)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Private Function GetLookupDataTable(ByVal strStoredProc As String) As DataTable

        Dim dtData As New DataTable()

        Try
            FillDataTable(strStoredProc, _
                          CommandType.StoredProcedure, _
                          dtData)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Private Function GetSelectProc(ByVal ID As Integer) As String
        Dim sSelect As String
        Dim sUpdate As String
        Dim sInsert As String
        Dim sDelete As String
        GetStoredProcedures(ID, sSelect, sUpdate, sInsert, sDelete)
        Return sSelect
    End Function

    Private Sub GetStoredProcedures(ByVal ID As Integer, _
                                    ByRef sSelectProc As String, _
                                    ByRef sUpdateProc As String, _
                                    ByRef sInsertProc As String, _
                                    ByRef sDeleteProc As String)

        Dim ParamsIn As New ParameterList()
        Dim ParamsOut As New ParameterList()

        ParamsIn.QuickAddInputParam("ID", DbtType.dbtInteger, ID)

        ParamsOut.AddParameter("SelectStoredProcedure", DbtType.dbtString, "", "SelectStoredProcedure", , "", ParameterDirection.Output)
        ParamsOut.AddParameter("UpdateStoredProcedure", DbtType.dbtString, "", "UpdateStoredProcedure", , "", ParameterDirection.Output)
        ParamsOut.AddParameter("InsertStoredProcedure", DbtType.dbtString, "", "InsertStoredProcedure", , "", ParameterDirection.Output)
        ParamsOut.AddParameter("DeleteStoredProcedure", DbtType.dbtString, "", "DeleteStoredProcedure", , "", ParameterDirection.Output)

        ExecuteQuery("GetEditableLookupProcs", _
                     CommandType.StoredProcedure, _
                     ParamsOut, _
                     ParamsIn)

        sSelectProc = CStr(ParamsOut.Item("SelectStoredProcedure").Value)
        sUpdateProc = CStr(ParamsOut.Item("UpdateStoredProcedure").Value)
        sInsertProc = CStr(ParamsOut.Item("InsertStoredProcedure").Value)
        sDeleteProc = CStr(ParamsOut.Item("DeleteStoredProcedure").Value)

    End Sub

    Public Function SaveSupplier(ByVal TableID As Integer, _
                                    ByRef dt As DataTable) As Boolean
        Try
            Dim sSelect As String
            Dim sUpdate As String
            Dim sInsert As String
            Dim sDelete As String
            GetStoredProcedures(TableID, sSelect, sUpdate, sInsert, sDelete)

            Dim params As New libDataAccess.libDataAccess.UpdateParameterList()

            ' update parameters
            params.AddUpdateParam("ID", DbtType.dbtInteger)
            params.AddUpdateParam("Name", DbtType.dbtString)
            params.AddUpdateParam("Details", DbtType.dbtString)

            ' insert parameters
            params.AddInsertParam("Name", DbtType.dbtString)
            params.AddInsertParam("Details", DbtType.dbtString)
            params.AddInsertParam("ID", DbtType.dbtInteger, , ParameterDirection.Output)

            ' delete parameters
            params.AddDeleteParam("ID", DbtType.dbtInteger)

            TBCultureDA.UpdateDataTable("", sInsert, sUpdate, sDelete, _
                                        CommandType.StoredProcedure, _
                                        dt, params)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return False
        End Try
    End Function

    Public Function SaveLookupData(ByVal TableID As Integer, _
                                    ByRef dt As DataTable) As Boolean
        Try
            Dim sSelect As String
            Dim sUpdate As String
            Dim sInsert As String
            Dim sDelete As String
            GetStoredProcedures(TableID, sSelect, sUpdate, sInsert, sDelete)

            Dim params As New libDataAccess.libDataAccess.UpdateParameterList()

            Select Case TableID
                Case 16     ' LOOKUP_AHO
                    BuildParamListAHO(params, dt)
                Case 13     ' LOOKUP_BREED
                    BuildParamListBreed(params, dt)
                Case 23     ' LOOKUP_BSE_COUNTY
                    BuildParamListBSECounty(params, dt)
                Case 19     ' LOOKUP_RELATION_FATE
                    BuildParamListRelationFate(params, dt)
                Case 17     ' LOOKUP_SUPPLIER
                    BuildParamListSupplier(params, dt)
                Case 7      ' LOOKUP_TEST_TYPE
                    BuildParamListTestType(params, dt)
                Case 27 'LOOKUP_TSE_TESTING_SITE
                    BuildParamListTSETestingSite(params, dt)
                Case 28 'LOOKUP_AHRO
                    BuildParamListAHRO(params, dt)
                Case Else
                    BuildParamListCommon(params, dt)
            End Select


            TBCultureDA.UpdateDataTable("", sInsert, sUpdate, sDelete, _
                                        CommandType.StoredProcedure, _
                                        dt, params)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return False
        End Try
    End Function

    Private Sub BuildParamListCommon(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("Original_Code", Parameter.ToDbtType(dt.Columns("Code").DataType), drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddUpdateParam("Description", DbtType.dbtString)

        ' insert parameters
        params.AddInsertParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddInsertParam("Description", DbtType.dbtString)

        ' delete parameters
        params.AddDeleteParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
    End Sub

    Private Sub BuildParamListAHO(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("Original_Code", DbtType.dbtString, drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", DbtType.dbtString)
        params.AddUpdateParam("Name", DbtType.dbtString)
        params.AddUpdateParam("BSERegionID", DbtType.dbtInteger)

        ' insert parameters
        params.AddInsertParam("Code", DbtType.dbtString)
        params.AddInsertParam("Name", DbtType.dbtString)
        params.AddInsertParam("BSERegionID", DbtType.dbtInteger)

        ' delete parameters
        params.AddDeleteParam("Code", DbtType.dbtString)
    End Sub

    Private Sub BuildParamListTSETestingSite(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("Original_CPH", DbtType.dbtString, drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Name", DbtType.dbtString)
        params.AddUpdateParam("Address", DbtType.dbtString)
        params.AddUpdateParam("CPH", DbtType.dbtString)
        params.AddUpdateParam("AHO", DbtType.dbtString)

        ' insert parameters
        params.AddInsertParam("Name", DbtType.dbtString)
        params.AddInsertParam("Address", DbtType.dbtString)
        params.AddInsertParam("CPH", DbtType.dbtString)
        params.AddInsertParam("AHO", DbtType.dbtString)

        ' delete parameters
        params.AddDeleteParam("CPH", DbtType.dbtString)
    End Sub
    Private Sub BuildParamListBreed(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("Original_Code", DbtType.dbtString, drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", DbtType.dbtString)
        params.AddUpdateParam("FullName", DbtType.dbtString)
        params.AddUpdateParam("AmalgamatedName", DbtType.dbtString)

        ' insert parameters
        params.AddInsertParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddInsertParam("FullName", DbtType.dbtString)
        params.AddInsertParam("AmalgamatedName", DbtType.dbtString)

        ' delete parameters
        params.AddDeleteParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
    End Sub

    Private Sub BuildParamListBSECounty(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("IDColumn", DbtType.dbtString)
        params.AddUpdateParam("Original_Code", DbtType.dbtString, drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", DbtType.dbtString)
        params.AddUpdateParam("Description", DbtType.dbtString)
        params.AddUpdateParam("BSERegionID", DbtType.dbtInteger)

        ' insert parameters
        params.AddInsertParam("IDColumn", DbtType.dbtString)
        params.AddInsertParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddInsertParam("Description", DbtType.dbtString)
        params.AddInsertParam("BSERegionID", DbtType.dbtInteger)

        ' delete parameters
        params.AddDeleteParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
    End Sub

    Private Sub BuildParamListRelationFate(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("Original_Code", Parameter.ToDbtType(dt.Columns("Code").DataType), drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddUpdateParam("Description", DbtType.dbtString)
        params.AddUpdateParam("IsActive", DbtType.dbtBoolean)

        ' insert parameters
        params.AddInsertParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddInsertParam("Description", DbtType.dbtString)
        params.AddInsertParam("IsActive", DbtType.dbtBoolean)

        ' delete parameters
        params.AddDeleteParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
    End Sub

    Private Sub BuildParamListSupplier(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("ID", DbtType.dbtInteger)
        params.AddUpdateParam("Name", DbtType.dbtString)
        params.AddUpdateParam("Details", DbtType.dbtString)

        ' insert parameters
        params.AddInsertParam("Name", DbtType.dbtString)
        params.AddInsertParam("Details", DbtType.dbtString)
        params.AddInsertParam("ID", DbtType.dbtInteger, , ParameterDirection.Output)

        ' delete parameters
        params.AddDeleteParam("ID", DbtType.dbtInteger)
    End Sub

    Private Sub BuildParamListAHRO(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("ID", DbtType.dbtInteger)
        params.AddUpdateParam("Name", DbtType.dbtString)

        ' insert parameters
        params.AddInsertParam("Name", DbtType.dbtString)
        params.AddInsertParam("ID", DbtType.dbtInteger, , ParameterDirection.Output)

        ' delete parameters
        params.AddDeleteParam("Name", DbtType.dbtString)
    End Sub

    Private Sub BuildParamListTestType(ByRef params As libDataAccess.libDataAccess.UpdateParameterList, ByRef dt As DataTable)
        ' update parameters
        params.AddUpdateParam("Original_Code", Parameter.ToDbtType(dt.Columns("Code").DataType), drvSourceVersion:=DataRowVersion.Original)
        params.AddUpdateParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddUpdateParam("Description", DbtType.dbtString)
        params.AddUpdateParam("IsActive", DbtType.dbtBoolean)

        ' insert parameters
        params.AddInsertParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
        params.AddInsertParam("Description", DbtType.dbtString)
        params.AddInsertParam("IsActive", DbtType.dbtBoolean)

        ' delete parameters
        params.AddDeleteParam("Code", Parameter.ToDbtType(dt.Columns("Code").DataType))
    End Sub

    Public Function GetAuthorityCounties() As DataTable

        Dim dtData As New DataTable()

        Try
            FillDataTable("GetluAuthorityCountyAll", _
                          CommandType.StoredProcedure, _
                          dtData)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Throw ex
        End Try

    End Function

    Public Function GetAuthorities(ByVal viAuthorityCountyID As Integer) As DataTable

        Dim dtData As New DataTable()
        Dim objParamList As New ParameterList()

        objParamList.QuickAddInputParam("AuthorityCountyID", DbtType.dbtInteger, viAuthorityCountyID)
        Try
            FillDataTable("GetluAuthorityByAuthorityCounty", _
                          CommandType.StoredProcedure, _
                          dtData, objParamList)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Throw ex
        End Try

    End Function

    Public Function GetADNSRegions(ByVal viAuthorityID As Integer) As DataTable

        Dim dtData As New DataTable()
        Dim objParamList As New ParameterList()

        objParamList.QuickAddInputParam("AuthorityID", DbtType.dbtInteger, viAuthorityID)
        Try
            FillDataTable("GetluADNSRegionByAuthority", _
                          CommandType.StoredProcedure, _
                          dtData, objParamList)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Throw ex
        End Try

    End Function

    Public Function GetAHOCode() As DataTable

        Dim dtData As New DataTable()

        Try
            FillDataTable("GetluAHOCode", _
                          CommandType.StoredProcedure, _
                          dtData)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Public Function GetAutoCompleteData() As DataView
        Try
            Dim view As DataView = DirectCast(HttpContext.Current.Cache("suggestTSETestingSites"), DataView)
            If view Is Nothing Then
                Dim dtData As New DataTable()
                FillDataTable("GetluTseTestingSite", _
                               CommandType.StoredProcedure, _
                               dtData)
                view = dtData.DefaultView
                HttpContext.Current.Cache("suggestTSETestingSites") = view
            End If
            Return view
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
        End Try
    End Function

End Class

