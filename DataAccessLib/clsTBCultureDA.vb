Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace libDataAccess

    Public Class StoredProcException
        Inherits System.Exception

        Public Sub New(ByVal strMessage As String)
            MyBase.New(strMessage)
        End Sub

    End Class

    Public Class TBCultureDA

        Private Shared c_intDALibraryError As Integer = 3
        Private Shared c_intStoredProcError As Integer = 1


#Region "Public Methods"

        '=======================================================================================
        ' Sub       : ExecuteNonQuery
        ' Desc      : Executes DataAccess.ExecuteNonQuery() and handles exceptions by logging
        '             any to the database. It then throws the exception for the calling class
        '             to be able to deal with.
        '---------------------------------------------------------------------------------------
        ' Arguments : strSQLtext         [IN] [String]      - stored proc name/table/sql statement
        '             cmdType            [IN] [CommandType] - stored proc, table direct, SQL text
        '             [objParameterList] [IN] [ParameterList]] - array of input parameters
        '             [lngCmdTimeout]    [IN] [Long]]       - timeout to be used for operation
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 23 June 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Public Shared Sub ExecuteNonQuery(ByVal strSQLtext As String, _
                                          ByVal cmdType As CommandType, _
                                          Optional ByRef objParameterList As ParameterList = Nothing, _
                                          Optional ByVal lngCmdTimeout As Integer = 30)

            Try
                DataAccess.ExecuteNonQuery(strSQLtext, cmdType, objParameterList, lngCmdTimeout)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            End Try

        End Sub

        ' Executes an action query using the passed connection and transaction
        Public Shared Sub ExecuteNonQuery(ByRef objConnection As SqlConnection, _
                                          ByRef objTransaction As SqlTransaction, _
                                          ByVal strSQLtext As String, _
                                          ByVal cmdType As CommandType, _
                                          Optional ByRef objParameterList As ParameterList = Nothing, _
                                          Optional ByVal lngCmdTimeout As Integer = 30)
            Try
                DataAccess.ExecuteNonQuery(objConnection, _
                                            objTransaction, _
                                            strSQLtext, _
                                            cmdType, _
                                            objParameterList, _
                                            lngCmdTimeout)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            End Try
        End Sub

        '=======================================================================================
        ' Sub       : ExecuteQuery
        ' Desc      : Executes DataAccess.ExecuteQuery() and handles exceptions by logging
        '             any to the database. It then throws the exception for the calling class
        '             to be able to deal with.
        '---------------------------------------------------------------------------------------
        ' Arguments : strSQLtext       [IN]  [String]        - stored proc name/table/sql statement
        '             cmdType          [IN]  [CommandType]   - stored proc, table direct, SQL text
        '             varOutParamList  [OUT] [ParameterList] - array of input parameters
        '             [varInParamList] [IN]  [ParameterList] - array of input parameters
        '             [lngCmdTimeout]  [IN]  [Long]          - timeout to be used for operation
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 23 June 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Public Shared Sub ExecuteQuery(ByVal strSQLtext As String, _
                                       ByVal cmdType As CommandType, _
                                       ByRef objOutParamList As ParameterList, _
                                       Optional ByRef objInParamList As ParameterList = Nothing, _
                                       Optional ByVal lngCmdTimeout As Integer = 30)

            Try
                DataAccess.ExecuteQuery(strSQLtext, cmdType, objOutParamList, objInParamList, lngCmdTimeout)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            Finally
                CheckSPError(objInParamList)
            End Try

        End Sub

        '=======================================================================================
        ' Function  : FillDataTable
        ' Desc      : Executes DataAccess.FillDataTable() and handles exceptions by logging
        '             any to the database. It then throws the exception for the calling class
        '             to be able to deal with.
        '---------------------------------------------------------------------------------------
        ' Arguments : strSQLtext         [IN]  [String]        - stored proc name/table/sql statement
        '             cmdType            [IN]  [CommandType]   - stored proc, table direct, SQL text
        '             dtData             [OUT] [DataTable]     - populated datatable will be returned
        '             [objParameterList] [IN]  [ParameterList] - array of input parameters
        '             [lngCmdTimeout]    [IN]  [Long]          - timeout to be used for operation
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 23 June 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Public Shared Sub FillDataTable(ByVal strSQLtext As String, _
                                        ByVal cmdType As CommandType, _
                                        ByRef dtData As DataTable, _
                                        Optional ByRef objParameterList As ParameterList = Nothing, _
                                        Optional ByVal lngCmdTimeout As Integer = 30)

            Try
                DataAccess.FillDataTable(strSQLtext, cmdType, dtData, objParameterList, lngCmdTimeout)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            Finally
                CheckSPError(objParameterList)
            End Try

        End Sub

        Public Shared Sub FillDataSet(ByVal strSQLtext As String, _
                                        ByVal cmdType As CommandType, _
                                        ByRef dsData As DataSet, _
                                        Optional ByRef objParameterList As ParameterList = Nothing, _
                                        Optional ByVal lngCmdTimeout As Integer = 30)

            Try
                DataAccess.FillDataSet(strSQLtext, cmdType, dsData, objParameterList, lngCmdTimeout)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            Finally
                CheckSPError(objParameterList)
            End Try

        End Sub


        '=======================================================================================
        ' Sub       : UpdateDataTable
        ' Desc      : Executes DataAccess.UpdateDataTable() and handles exceptions by logging
        '             any to the database. It then throws the exception for the calling class
        '             to be able to deal with.
        '---------------------------------------------------------------------------------------
        ' Arguments : strSelSQLtext      [IN] [String]      - stored proc name/table/sql statement
        '             strInsSQLtext      [IN] [String]      - stored proc name/table/sql statement
        '             strUpdSQLtext      [IN] [String]      - stored proc name/table/sql statement
        '             strDelSQLtext      [IN] [String]      - stored proc name/table/sql statement
        '             cmdType            [IN] [CommandType] - stored proc, table direct, SQL text
        '             dtData             [IN] [DataTable]   - populated datatable used to update DB
        '             [objParameterList] [IN] [UpdateParameterList]  - array of input parameters
        '             [lngCmdTimeout]    [IN] [Long]]       - timeout to be used for operation
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 23 June 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Public Shared Sub UpdateDataTable(ByVal strSelSQLtext As String, _
                                          ByVal strInsSQLtext As String, _
                                          ByVal strUpdSQLtext As String, _
                                          ByVal strDelSQLtext As String, _
                                          ByVal cmdType As CommandType, _
                                          ByRef dtData As DataTable, _
                                          Optional ByRef objParameterList As UpdateParameterList = Nothing, _
                                          Optional ByVal lngCmdTimeout As Integer = 30)

            Try
                DataAccess.UpdateDataTable(strSelSQLtext, strInsSQLtext, strUpdSQLtext, strDelSQLtext, cmdType, dtData, objParameterList, lngCmdTimeout)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            End Try

        End Sub

        Public Shared Sub OptimisticUpdateDataTable(ByRef objConnection As SqlConnection, _
                                          ByRef objTransaction As SqlTransaction, _
                                          ByRef objUpdateEventHandler As SqlRowUpdatedEventHandler, _
                                          ByVal strSelSQLtext As String, _
                                               ByVal strInsSQLtext As String, _
                                               ByVal strUpdSQLtext As String, _
                                               ByVal strDelSQLtext As String, _
                                               ByVal cmdType As CommandType, _
                                               ByRef dtData As DataTable, _
                                               Optional ByRef objParameterList As UpdateParameterList = Nothing, _
                                               Optional ByVal lngCmdTimeout As Integer = 30)
            Try
                DataAccess.OptimisticUpdateDataTable(objConnection, objTransaction, objUpdateEventHandler, strSelSQLtext, strInsSQLtext, strUpdSQLtext, strDelSQLtext, cmdType, dtData, objParameterList, lngCmdTimeout)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            End Try

        End Sub

        ' convenient function for setting a single primary key field in a 
        ' datatable.
        ' No errors are trapped so this will raise an excpetion if the
        ' caller specifies a field that doesn't exist in the DataTable.
        Public Shared Sub SetPrimaryKey(ByRef dt As DataTable, _
                                        ByVal sKeyField As String, _
                                        Optional ByVal bAutoIncrement As Boolean = False)

            Dim KeyCol As DataColumn = dt.Columns(sKeyField)
            If bAutoIncrement Then
                KeyCol.AutoIncrement = True
                KeyCol.AutoIncrementSeed = -1
                KeyCol.AutoIncrementStep = -1
            End If
            dt.PrimaryKey = New DataColumn() {KeyCol}

        End Sub

        ' methods for client objects that need to handle their own
        ' transactioning

        ' returns an open database connection object
        Public Shared Function OpenConnection() As SqlConnection
            Try
                Return DataAccess.OpenConnection()
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            End Try
        End Function
        ' Closes a database connection
        Public Shared Sub CloseConnection(ByRef objConnection As SqlConnection)
            Try
                DataAccess.CloseConnection(objConnection)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            End Try
        End Sub
        ' Starts a transaction
        Public Shared Function BeginTransaction(ByRef objConnection As SqlConnection) As SqlTransaction
            Try
                Return DataAccess.BeginTransaction(objConnection)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            End Try
        End Function
        ' Commits a transaction
        Public Shared Sub CommitTransaction(ByRef objTransaction As SqlTransaction)
            Try
                DataAccess.CommitTransaction(objTransaction)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            End Try
        End Sub
        ' Rolls back a transaction
        Public Shared Sub RollbackTransaction(ByRef objTransaction As SqlTransaction)
            Try
                DataAccess.RollbackTransaction(objTransaction)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            End Try
        End Sub
        ' Updates a data table using the passed connection and transaction
        Public Shared Sub UpdateDataTable(ByRef objConnection As SqlConnection, _
                                          ByRef objTransaction As SqlTransaction, _
                                          ByVal strSelSQLtext As String, _
                                          ByVal strInsSQLtext As String, _
                                          ByVal strUpdSQLtext As String, _
                                          ByVal strDelSQLtext As String, _
                                          ByVal cmdType As CommandType, _
                                          ByRef dtData As DataTable, _
                                          Optional ByRef objParameterList As UpdateParameterList = Nothing, _
                                          Optional ByVal lngCmdTimeout As Integer = 30)
            Try
                DataAccess.UpdateDataTable(objConnection, _
                                           objTransaction, _
                                           strSelSQLtext, _
                                           strInsSQLtext, _
                                           strUpdSQLtext, _
                                           strDelSQLtext, _
                                           cmdType, _
                                           dtData, _
                                           objParameterList, _
                                           lngCmdTimeout)
            Catch err As Exception
                InfoLog.LogToEventViewer(LogType.ltError, err.Source, err.Message)
                Throw err
            End Try
        End Sub

#End Region

#Region "Private Method"

        '=======================================================================================
        ' Function  : CheckSPError
        ' Desc      : This method is called by the TBCulture business object after executing a 
        '             database command. This method will check through the parameter results
        '             to check for 'ReturnValue' parameters. Each 'ReturnValue' parameter gets
        '             its value object passed to CheckReturnParameter() which will log an error
        '             if the value is not equal to 0.
        '---------------------------------------------------------------------------------------
        ' Arguments : objParamList [IN] [clsParameterList] - list of parameters used in query
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 01 July 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Private Shared Sub CheckSPError(ByVal objParameterList As ArrayList)

            Dim objParameter As Parameter = Nothing


            ' Check for a parameter with direction 'ReturnValue'
            If Not objParameterList Is Nothing Then
                For Each objParameter In objParameterList
                    If (objParameter.GetDirection = ParameterDirection.ReturnValue) Then
                        ' Will throw exception if a stored proc failed
                        CheckReturnParameter(objParameter.Value())
                    End If
                Next
            End If

        End Sub

        '=======================================================================================
        ' Function  : CheckReturnParameter
        ' Desc      : This method will check if the value passed in is not equal to 0. If not 
        '             (StoredProc error) then call LogToDatabase() to write error to database.
        '---------------------------------------------------------------------------------------
        ' Arguments : objValue [IN] [Object] - Parameter value object which should be an int32
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 29 July 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Private Shared Sub CheckReturnParameter(ByVal objValue As Object)

            Dim strText As String
            Dim lgType As LogType
            Dim intValue As Integer

            Try
                intValue = Convert.ToInt32(objValue)

                If (intValue = 0) Then
                    Return
                End If

                If (intValue < 0) Then
                    lgType = LogType.ltInformation
                    strText = "No records were affected during transaction."
                ElseIf (intValue > 0) Then
                    lgType = LogType.ltError
                    strText = "Stored Procedure Error " + CStr(intValue)
                End If

                Throw New StoredProcException(strText)

            Catch ex As Exception
                Throw ex
            End Try

        End Sub

#End Region

    End Class

End Namespace
