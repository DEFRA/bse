Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA

Public Class clsUser

    Public Function GetUserGroups() As DataTable

        Dim dtData As New DataTable()

        Try
            FillDataTable("GetluUserGroup", _
                          CommandType.StoredProcedure, _
                          dtData)

            Return dtData

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try
    End Function

    Public Function SaveUsers(ByRef dtData As DataTable) As Boolean
        Try
            Dim params As New libDataAccess.libDataAccess.UpdateParameterList()

            With params
                ' update parameters
                .AddUpdateParam("ID", DbtType.dbtInteger)
                .AddUpdateParam("NTLogin", DbtType.dbtString)
                .AddUpdateParam("Name", DbtType.dbtString)
                .AddUpdateParam("Email", DbtType.dbtString)
                .AddUpdateParam("UserGroup", DbtType.dbtInteger)
                .AddUpdateParam("IsActive", DbtType.dbtBoolean)

                ' insert parameters
                .AddInsertParam("NTLogin", DbtType.dbtString)
                .AddInsertParam("Name", DbtType.dbtString)
                .AddInsertParam("Email", DbtType.dbtString)
                .AddInsertParam("UserGroup", DbtType.dbtInteger)
                .AddInsertParam("IsActive", DbtType.dbtBoolean)
                .AddParameter("@ID", StatementType.stInsert, DbtType.dbtInteger, "@ID", daDirection:=ParameterDirection.Output, intsize:=7)

                ' delete parameters
                '.AddDeleteParam
            End With

            TBCultureDA.UpdateDataTable("", "AddUser", "EditUser", "CannotDeleteUser", _
                                        CommandType.StoredProcedure, _
                                        dtData, params)

            Return True

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return False
        End Try
    End Function

    Public Function GetUsers() As DataTable

        Dim dtData As New DataTable()

        Try
            FillDataTable("GetUsers", _
                          CommandType.StoredProcedure, _
                          dtData)

            SetPrimaryKey(dtData, "ID", True)

            Return dtData
        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsLookupData)
            Return Nothing
        End Try

    End Function

    Public Function GetGroupForUser(ByVal sNTUserID As String, ByRef GroupCode As Long, ByRef GroupName As String) As Boolean
        Try
            Dim ParamsIn As New ParameterList()
            Dim ParamsOut As New ParameterList()

            ParamsIn.QuickAddInputParam("NTUserID", DbtType.dbtString, sNTUserID)

            ParamsOut.QuickAddResultsetParam("UserGroup", DbtType.dbtInteger)
            ParamsOut.QuickAddResultsetParam("Name", DbtType.dbtString)

            ExecuteQuery("GetGroupForUser", _
                         CommandType.StoredProcedure, _
                         ParamsOut, _
                         ParamsIn)

            GroupCode = Convert.ToInt32(ParamsOut.Item("UserGroup").Value)
            GroupName = Convert.ToString(ParamsOut.Item("Name").Value)

            Return True

        Catch ex As Exception
            'clsLog.LogException(ex, clsLog.LogSource.lsUserObject)
            Return False
        End Try
    End Function

    Public Function GetUserByNTLogin(ByVal NTLogin As String, ByRef UserID As Integer, ByRef Name As String, ByRef GroupCode As Integer, ByRef GroupName As String, ByRef Email As String) As Boolean
        Try
            Dim ParamsIn As New ParameterList()
            Dim ParamsOut As New ParameterList()

            ParamsIn.QuickAddInputParam("NTLogin", DbtType.dbtString, NTLogin)

            ParamsOut.QuickAddResultsetParam("ID", DbtType.dbtInteger)
            ParamsOut.QuickAddResultsetParam("Name", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("UserGroup", DbtType.dbtInteger)
            ParamsOut.QuickAddResultsetParam("GroupName", DbtType.dbtString)
            ParamsOut.QuickAddResultsetParam("Email", DbtType.dbtString)

            ExecuteQuery("GetUserByNTLogin", _
                         CommandType.StoredProcedure, _
                         ParamsOut, _
                         ParamsIn)

            UserID = CInt(ParamsOut.Item("ID").Value)
            Name = Convert.ToString(ParamsOut.Item("Name").Value)
            GroupCode = Convert.ToInt32(ParamsOut.Item("UserGroup").Value)
            GroupName = Convert.ToString(ParamsOut.Item("GroupName").Value)
            Email = Convert.ToString(ParamsOut.Item("Email").Value)

            Return True

        Catch ex As Exception
            'clsLog.LogException(ex, clsLog.LogSource.lsUserObject)
            Return False
        End Try
    End Function
End Class
