Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace libDataAccess
    '==================================================================================
    ' Class     : clsDataAccess
    ' Desc      : This class is used to access and perform operations on a DataProvider.
    '             It will either use the SQLServer dedicated SQL commands for extra
    '             performance on a SQLServer DB or use OleDB for generic access to
    '             various DataProviders.
    '             It will execute operations on stored procedures, direct to table or a
    '             SQL statement.
    '             All setting up of connections, dataadapters and commands is handled
    '             internally.
    ' Notes     : This class has Shared (static) methods which means the class does not
    '             need to be instantiated to be used. The Shared private data will
    '             hence only be retrieved once improving performance.
    '             This class does not support MTS or transactions inclusively.
    '----------------------------------------------------------------------------------
    ' Attributes: [Private]  m_blnUseSQL           - Used to use wither SQL or OleDB objects
    '             [Private]  m_strConnection       - Connection to the database
    '----------------------------------------------------------------------------------
    ' Methods   : [Public]  Initialise          - Initialises private data
    '             [Public]  ExecuteNonQuery     - Executes a command without return data
    '             [Public]  ExecuteQuery        - Executes a command that returns data
    '             [Public]  FillDataSet         - Executes a Fill DataSet on a DataAdapter
    '             [Public]  UpdateDataSet       - Executes an Update on a DataAdapter
    '             [Private] CreateConnection    - Creates either an OleDB or SQL connection
    '             [Private] CreateAdapter       - Creates either an OleDB or SQL adapter
    '             [Private] SetupCommand        - Creates either an OleDB or SQL command
    '----------------------------------------------------------------------------------
    ' Author    : PSH
    ' Created   : 23 June 2003
    '==================================================================================
    ' Change Log
    '
    ' Date      Author  Comments
    ' ----      ------  --------
    '==================================================================================

    Public Class DataAccess

#Region "Attributes"
        Private Shared m_strConnection As String = ""
#End Region

#Region "Initialisation"

        ' Initialisation of class
        Private Shared Sub Initialise()

            If m_strConnection = "" Then
                SyncLock GetType(DataAccess)
                    ' load the connection info from the config file
                    m_strConnection = System.Configuration.ConfigurationSettings. _
                                        AppSettings("DBConnectionString")
                End SyncLock
            End If

        End Sub

#End Region

#Region "Public Methods"

        '=======================================================================================
        ' Sub       : ExecuteNonQuery
        ' Desc      : Executes a given operation on a DataProvider that does not return any data.
        '
        '             Method accepts sqlText which can either be:
        '             - name of a stored procedure, or
        '             - name of a table, or
        '             - a select sql statement
        '
        '             Also, has optional parameters array as input to the query.
        '             It will return the number of records that have been affected or -1 if an 
        '             error has occured.
        ' Note      : It will use a SQLDataAdapter if m_blnUseSQL is true or an OleDBDataAdapter
        '             if m_blnUseSQL is false
        '---------------------------------------------------------------------------------------
        ' Arguments : strSQLtext      [IN] [String]      - stored proc name/table/sql statement
        '             cmdType         [IN] [CommandType] - stored proc, table direct, SQL text
        '             [varParameters] [IN] [Parameter]]  - array of input parameters
        '             [lngCmdTimeout] [IN] [Long]]       - timeout to be used for operation
        '
        ' Returns   : [Integer] - Number of rows returned or -1 if error
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

            Dim objCommand As SqlCommand
            Dim objConnection As SqlConnection


            Try
                ' Check Private static data is setup
                Initialise()

                ' Create connection
                CreateConnection(objConnection)

                ' Setup command
                SetupCommand(objCommand, objConnection, strSQLtext, cmdType, lngCmdTimeout)

                ' Setup parameters if any
                'SetupCommandParameters(objCommand, objParameterList)
                If Not objParameterList Is Nothing Then
                    objParameterList.AddToCommand(objCommand)
                End If

                ' Open the connection
                objConnection.Open()

                ' Execute non query command
                objCommand.ExecuteNonQuery()

                ' Get any output/return parameter data
                If Not objParameterList Is Nothing Then
                    objParameterList.GetFromCommand(objCommand)
                End If

            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                Throw errTIE.InnerException
            Catch errSE As SystemException
                Throw errSE
            Catch err As Exception
                Throw err
            Finally
                ' Close connection
                If Not (objConnection Is Nothing) Then
                    objConnection.Close()
                End If

                ' Cleanup
                objCommand = Nothing
                objConnection = Nothing
            End Try

        End Sub

        ' overload of ExecuteNonQuery that allows callers to pass their
        ' own database and transaction - to allow client objects to handle
        ' their own transaction processing
        Public Shared Sub ExecuteNonQuery(ByRef objConnection As SqlConnection, _
                                          ByRef objTransaction As SqlTransaction, _
                                          ByVal strSQLtext As String, _
                                          ByVal cmdType As CommandType, _
                                          Optional ByRef objParameterList As ParameterList = Nothing, _
                                          Optional ByVal lngCmdTimeout As Integer = 30)

            Dim objCommand As SqlCommand

            Try
                ' Setup command
                SetupCommand(objCommand, objConnection, strSQLtext, cmdType, lngCmdTimeout)
                objCommand.Transaction = objTransaction

                ' Setup parameters if any
                'SetupCommandParameters(objCommand, objParameterList)
                If Not objParameterList Is Nothing Then
                    objParameterList.AddToCommand(objCommand)
                End If

                ' Execute non query command
                objCommand.ExecuteNonQuery()

                ' Get any output/return parameter data
                If Not objParameterList Is Nothing Then
                    objParameterList.GetFromCommand(objCommand)
                End If

            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                Throw errTIE.InnerException
            Catch errSE As SystemException
                Throw errSE
            Catch err As Exception
                Throw err
            End Try
        End Sub

        '=======================================================================================
        ' Sub       : ExecuteQuery
        ' Desc      : Executes a given operation on a DataProvider that returns some data, which
        '             is known already and whose parameters will have been passed in as the
        '             outputParamList, which will be filled with values by this method.
        '
        '             Method accepts sqlText which can either be:
        '             - name of a stored procedure, or
        '             - name of a table, or
        '             - a select sql statement
        '
        '             Also, has optional parameters array as input to the query.
        '             It will NOT return more than one record of information so should be used
        '             to return data where there is only one record - otherwise use FillDataSet.
        ' Note      : It will use a SQLDataAdapter if m_blnUseSQL is true or an OleDBDataAdapter
        '             if m_blnUseSQL is false
        '---------------------------------------------------------------------------------------
        ' Arguments : strSQLtext       [IN]  [String]           - stored proc name/table/sql statement
        '             cmdType          [IN]  [CommandType]      - stored proc, table direct, SQL text
        '             varOutParamList  [OUT] [clsParameterList] - array of input parameters
        '             [varInParamList] [IN]  [clsParameterList] - array of input parameters
        '             [lngCmdTimeout]  [IN]  [Long]             - timeout to be used for operation
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

            Dim objReader As SqlDataReader
            Dim objCommand As SqlCommand
            Dim objConnection As SqlConnection


            Try
                ' Check Private static data is setup
                Initialise()

                ' Create connection
                CreateConnection(objConnection)

                ' Setup command
                SetupCommand(objCommand, objConnection, strSQLtext, cmdType, lngCmdTimeout)

                ' Setup parameters if any
                'SetupCommandParameters(objCommand, objInParamList)
                If Not objInParamList Is Nothing Then
                    objInParamList.AddToCommand(objCommand)
                End If

                ' Open the connection
                objConnection.Open()

                ' Execute reader command
                ' We will use Sequential Access so that we can support large binary objects
                objReader = objCommand.ExecuteReader(CommandBehavior.SequentialAccess)

                ' Get any output/return parameter data
                If Not objInParamList Is Nothing Then
                    objInParamList.GetFromCommand(objCommand)
                End If

                ' Get Reader Data
                'GetOutputParameters(objReader, objOutParamList)
                If (objReader.Read()) Then
                    objOutParamList.GetFromReader(objReader)
                End If

            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                Throw errTIE.InnerException
            Catch errSE As SystemException
                Throw errSE
            Catch err As Exception
                Throw err
            Finally
                ' Close Reader
                If Not (objReader Is Nothing) Then
                    objReader.Close()
                End If

                ' Close connection
                If Not (objConnection Is Nothing) Then
                    objConnection.Close()
                End If

                ' Cleanup
                objReader = Nothing
                objCommand = Nothing
                objConnection = Nothing
            End Try

        End Sub

        '=======================================================================================
        ' Function  : FillDataTable
        ' Desc      : Executes a Fill operation on a DataAdapter on the database passed in
        '             to the constructor.
        '             Method accepts sqlText which can either be:
        '             - name of a stored procedure, or
        '             - name of a table, or
        '             - a select sql statement
        '
        '             Also, has optional parameters array as input to the query.
        '             It will return a dataset of the result.
        ' Note      : It will use a SQLDataAdapter if m_blnUseSQL is true or an OleDBDataAdapter
        '             if m_blnUseSQL is false
        '---------------------------------------------------------------------------------------
        ' Arguments : strSQLtext      [IN]  [String]      - stored proc name/table/sql statement
        '             cmdType         [IN]  [CommandType] - stored proc, table direct, SQL text
        '             dtData          [OUT] [DataTable]   - populated datatable will be returned
        '             [varParameters] [IN]  [Parameter]]  - array of input parameters
        '             [lngCmdTimeout] [IN]  [Long]]       - timeout to be used for operation
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

            Dim objAdapter As SqlDataAdapter
            Dim objConnection As SqlConnection


            Try
                ' Check Private static data is setup
                Initialise()

                ' Create connection
                CreateConnection(objConnection)

                ' Create data adapter
                CreateAdapter(objAdapter)

                ' Setup command
                SetupCommand(objAdapter.SelectCommand, objConnection, strSQLtext, cmdType, lngCmdTimeout)

                ' Setup parameters if any
                'SetupCommandParameters(objAdapter.SelectCommand, objParameterList)
                If Not objParameterList Is Nothing Then
                    objParameterList.AddToCommand(objAdapter.SelectCommand)
                End If

                ' Open connection
                objConnection.Open()

                ' Create new dataset
                dtData = New DataTable()

                ' Fill the datatable with database records and handle any exceptions
                objAdapter.Fill(dtData)

                ' Get any output/return parameter data
                If Not objParameterList Is Nothing Then
                    objParameterList.GetFromCommand(objAdapter.SelectCommand)
                End If

            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                Throw errTIE.InnerException
            Catch errSE As SystemException
                Throw errSE
            Catch err As Exception
                Throw err
            Finally
                ' Close connection
                If Not (objConnection Is Nothing) Then
                    objConnection.Close()
                End If

                ' Cleanup
                objAdapter = Nothing
                objConnection = Nothing
            End Try

        End Sub

        Public Shared Sub FillDataSet(ByVal strSQLtext As String, _
                                             ByVal cmdType As CommandType, _
                                             ByRef dsData As DataSet, _
                                             Optional ByRef objParameterList As ParameterList = Nothing, _
                                             Optional ByVal lngCmdTimeout As Integer = 30)

            Dim objAdapter As SqlDataAdapter
            Dim objConnection As SqlConnection


            Try
                ' Check Private static data is setup
                Initialise()

                ' Create connection
                CreateConnection(objConnection)

                ' Create data adapter
                CreateAdapter(objAdapter)

                ' Setup command
                SetupCommand(objAdapter.SelectCommand, objConnection, strSQLtext, cmdType, lngCmdTimeout)

                ' Setup parameters if any
                'SetupCommandParameters(objAdapter.SelectCommand, objParameterList)
                If Not objParameterList Is Nothing Then
                    objParameterList.AddToCommand(objAdapter.SelectCommand)
                End If

                ' Open connection
                objConnection.Open()

                ' Create new dataset
                dsData = New DataSet()

                ' Fill the datatable with database records and handle any exceptions
                objAdapter.Fill(dsData)

                ' Get any output/return parameter data
                If Not objParameterList Is Nothing Then
                    objParameterList.GetFromCommand(objAdapter.SelectCommand)
                End If

            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                Throw errTIE.InnerException
            Catch errSE As SystemException
                Throw errSE
            Catch err As Exception
                Throw err
            Finally
                ' Close connection
                If Not (objConnection Is Nothing) Then
                    objConnection.Close()
                End If

                ' Cleanup
                objAdapter = Nothing
                objConnection = Nothing
            End Try

        End Sub

        '=======================================================================================
        ' Sub       : UpdateDataTable
        ' Desc      : Executes an Update operation on a DataAdapter on the database passed in
        '             to the constructor.
        '
        '             Method accepts sqlText for Select, Insert, Update and Delete operations
        '             which can either be:
        '             - name of a stored procedure, or
        '             - name of a table, or
        '             - a select sql statement
        '
        '             Also, has optional parameters array as input to Select, Insert, Update 
        '             Delete commands.
        '             It will use the dataset provided as the modified dataset with which to
        '             update the database with
        ' Note      : It will use a SQLDataAdapter if m_blnUseSQL is true or an OleDBDataAdapter
        '             if m_blnUseSQL is false
        '---------------------------------------------------------------------------------------
        ' Arguments : strSelSQLtext      [IN] [String]      - stored proc name/table/sql statement
        '             strInsSQLtext      [IN] [String]      - stored proc name/table/sql statement
        '             strUpdSQLtext      [IN] [String]      - stored proc name/table/sql statement
        '             strDelSQLtext      [IN] [String]      - stored proc name/table/sql statement
        '             cmdType            [IN] [CommandType] - stored proc, table direct, SQL text
        '             dtData             [IN] [DataTable]   - populated datatable used to update DB
        '             [objParameterList] [IN] [clsUpdateParameterList]  - array of input parameters
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

            Dim objAdapter As SqlDataAdapter
            Dim objConnection As SqlConnection
            Dim objTransaction As SqlTransaction = Nothing

            Try
                ' Check Private static data is setup
                Initialise()

                ' Create connection
                CreateConnection(objConnection)

                ' Create data adapter
                CreateAdapter(objAdapter)

                ' Setup commands
                SetupCommand(objAdapter.SelectCommand, objConnection, strSelSQLtext, cmdType, lngCmdTimeout)
                SetupCommand(objAdapter.InsertCommand, objConnection, strInsSQLtext, cmdType, lngCmdTimeout)
                SetupCommand(objAdapter.UpdateCommand, objConnection, strUpdSQLtext, cmdType, lngCmdTimeout)
                SetupCommand(objAdapter.DeleteCommand, objConnection, strDelSQLtext, cmdType, lngCmdTimeout)

                ' Setup parameters if any
                'SetupAdapterParameters(objAdapter, objParameterList)
                If Not objParameterList Is Nothing Then
                    objParameterList.AddToAdapter(objAdapter)
                End If

                ' Open connection
                objConnection.Open()

                ' begin transaction
                objTransaction = objConnection.BeginTransaction()
                SetAdapterTransaction(objAdapter, objTransaction)

                ' Update the database with the dataset changes and handle any exceptions
                objAdapter.Update(dtData)

                objTransaction.Commit()
                objTransaction = Nothing

                ' Get any output/return parameter data
                If Not objParameterList Is Nothing Then
                    objParameterList.GetFromAdapter(objAdapter)
                End If

            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                If Not objTransaction Is Nothing Then
                    objTransaction.Rollback()
                End If
                Throw errTIE.InnerException
            Catch errSE As SystemException
                If Not objTransaction Is Nothing Then
                    objTransaction.Rollback()
                End If
                Throw errSE
            Catch err As Exception
                If Not objTransaction Is Nothing Then
                    objTransaction.Rollback()
                End If
                Throw err
            Finally
                ' Close connection
                If Not (objConnection Is Nothing) Then
                    objConnection.Close()
                End If

                ' Cleanup
                objAdapter = Nothing
                objConnection = Nothing

            End Try

        End Sub

        'added for optimistic updates of datatable data
        'only works with SQL connection
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
            Dim objAdapter As SqlDataAdapter
            Try

                ' Create data adapter
                CreateAdapter(objAdapter)

                ' Setup commands
                SetupCommand(objAdapter.SelectCommand, objConnection, strSelSQLtext, cmdType, lngCmdTimeout)
                SetupCommand(objAdapter.InsertCommand, objConnection, strInsSQLtext, cmdType, lngCmdTimeout)
                SetupCommand(objAdapter.UpdateCommand, objConnection, strUpdSQLtext, cmdType, lngCmdTimeout)
                SetupCommand(objAdapter.DeleteCommand, objConnection, strDelSQLtext, cmdType, lngCmdTimeout)

                ' Setup parameters if any
                'SetupAdapterParameters(objAdapter, objParameterList)
                If Not objParameterList Is Nothing Then
                    objParameterList.AddToAdapter(objAdapter)
                End If

                SetAdapterTransaction(objAdapter, objTransaction)

                objAdapter.ContinueUpdateOnError = True
                AddHandler objAdapter.RowUpdated, objUpdateEventHandler

                ' Update the database with the dataset changes and handle any exceptions
                objAdapter.Update(dtData)

                ' Get any output/return parameter data
                If Not objParameterList Is Nothing Then
                    objParameterList.GetFromAdapter(objAdapter)
                End If

            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                Throw errTIE.InnerException
            Catch errSE As SystemException
                Throw errSE
            Catch err As Exception
                Throw err
            Finally
                ' Cleanup
                RemoveHandler objAdapter.RowUpdated, objUpdateEventHandler
                objAdapter = Nothing
            End Try

        End Sub

        ' returns an open database connection object
        Public Shared Function OpenConnection() As SqlConnection
            Dim objConnection As SqlConnection = Nothing
            Try
                ' Check Private static data is setup
                Initialise()

                ' Create connection
                CreateConnection(objConnection)

                ' Open connection
                objConnection.Open()

                Return objConnection

            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                If Not objConnection Is Nothing Then
                    objConnection.Close()
                End If
                Throw errTIE.InnerException
            Catch err As Exception
                If Not objConnection Is Nothing Then
                    objConnection.Close()
                End If
                Throw err
            End Try
        End Function

        ' Closes a database connection
        Public Shared Sub CloseConnection(ByRef objConnection As SqlConnection)
            Try
                objConnection.Close()
            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                If Not objConnection Is Nothing Then
                    objConnection.Close()
                End If
                Throw errTIE.InnerException
            Catch err As Exception
                If Not objConnection Is Nothing Then
                    objConnection.Close()
                End If
                Throw err
            End Try
        End Sub

        ' Starts a transaction
        Public Shared Function BeginTransaction(ByRef objConnection As SqlConnection) As SqlTransaction
            Try
                Return objConnection.BeginTransaction()
            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                Throw errTIE.InnerException
            Catch err As Exception
                Throw err
            End Try
        End Function
        ' Commits a transaction
        Public Shared Sub CommitTransaction(ByRef objTransaction As SqlTransaction)
            Try
                objTransaction.Commit()
            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                Throw errTIE.InnerException
            Catch err As Exception
                Throw err
            End Try
        End Sub
        ' Rolls back a transaction
        Public Shared Sub RollbackTransaction(ByRef objTransaction As SqlTransaction)
            Try
                objTransaction.Rollback()
            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                Throw errTIE.InnerException
            Catch err As Exception
                Throw err
            End Try
        End Sub

        ' Updates a data table using the passed connection and transaction
        ' - intended for client objects that need to perform their own 
        ' transactioning
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
                Dim objAdapter As SqlDataAdapter
                ' Create data adapter
                CreateAdapter(objAdapter)

                ' Setup commands
                SetupCommand(objAdapter.SelectCommand, objConnection, strSelSQLtext, cmdType, lngCmdTimeout)
                SetupCommand(objAdapter.InsertCommand, objConnection, strInsSQLtext, cmdType, lngCmdTimeout)
                SetupCommand(objAdapter.UpdateCommand, objConnection, strUpdSQLtext, cmdType, lngCmdTimeout)
                SetupCommand(objAdapter.DeleteCommand, objConnection, strDelSQLtext, cmdType, lngCmdTimeout)

                ' Setup parameters if any
                'SetupAdapterParameters(objAdapter, objParameterList)
                If Not objParameterList Is Nothing Then
                    objParameterList.AddToAdapter(objAdapter)
                End If

                SetAdapterTransaction(objAdapter, objTransaction)

                ' Update the database with the dataset changes and handle any exceptions
                objAdapter.Update(dtData)

                ' Get any output/return parameter data
                If Not objParameterList Is Nothing Then
                    objParameterList.GetFromAdapter(objAdapter)
                End If

            Catch errTIE As System.Reflection.TargetInvocationException
                ' Inner exception has the actual error
                Throw errTIE.InnerException
            Catch errSE As SystemException
                Throw errSE
            Catch err As Exception
                Throw err
            End Try
        End Sub

#End Region

#Region "Private Methods"

            '=======================================================================================
            ' Sub       : CreateConnection
            ' Desc      : Creates the input parameter as either a SQLConnection or OleDBConnection
            '             depending on the value of m_blnUseSQL.
            '---------------------------------------------------------------------------------------
            ' Arguments : objConnection [IN/OUT] [IDbConnection] - interface to create connection for
            '---------------------------------------------------------------------------------------
            ' Author    : PSH
            ' Created   : 23 June 2003
            '=======================================================================================
            ' Change Log
            '
            ' Date      Author  Comments
            ' ----      ------  --------
            '=======================================================================================
        Private Shared Sub CreateConnection(ByRef objConnection As SqlConnection)

            objConnection = New SqlClient.SqlConnection(m_strConnection)

        End Sub

        '=======================================================================================
        ' Sub       : CreateAdapter
        ' Desc      : Creates the input parameter as either a SQLDataAdapter or OleDBDataAdapter
        '             depending on the value of m_blnUseSQL.
        '---------------------------------------------------------------------------------------
        ' Arguments : objAdapter [IN/OUT] [IDbDataAdapter] - interface to create adapter for
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 23 June 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Private Shared Sub CreateAdapter(ByRef objAdapter As SqlDataAdapter)

            objAdapter = New SqlClient.SqlDataAdapter()

        End Sub

        '=======================================================================================
        ' Sub       : SetupCommand
        ' Desc      : Creates the input parameter objCommand as either a SQLCommand or an
        '             OleDBCommand depending on the value of m_blnUseSQL.
        '---------------------------------------------------------------------------------------
        ' Arguments : objCommand    [IN/OUT] [IDbCommand]   - interface of cammand object
        '             objConnection [IN/OUT] [IDbConnection]- interface of connection object
        '             strSQLtext    [IN/OUT] [String]       - stored proc name/table/sql statement
        '             cmdType       [IN/OUT] [CommandType]  - type either StoredProc/Table/SQL
        '             lngTimeout    [IN/OUT] [Integer]      - command timeout amount
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 23 June 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Private Shared Sub SetupCommand(ByRef objCommand As SqlCommand, _
                                 ByRef objConnection As SqlConnection, _
                                 ByVal strSQLtext As String, _
                                 ByVal cmdType As CommandType, _
                                 ByVal lngTimeout As Integer)

            objCommand = New SqlClient.SqlCommand()

            With objCommand
                .Connection = objConnection
                .CommandType = cmdType
                .CommandText = strSQLtext
                .CommandTimeout = lngTimeout
            End With

        End Sub

        Private Shared Sub SetAdapterTransaction(ByRef objAdapter As SqlDataAdapter, _
                                                 ByRef objTransaction As SqlTransaction)


            If Not objAdapter.SelectCommand Is Nothing Then
                objAdapter.SelectCommand.Transaction = objTransaction
            End If
            If Not objAdapter.UpdateCommand Is Nothing Then
                objAdapter.UpdateCommand.Transaction = objTransaction
            End If
            If Not objAdapter.InsertCommand Is Nothing Then
                objAdapter.InsertCommand.Transaction = objTransaction
            End If
            If Not objAdapter.DeleteCommand Is Nothing Then
                objAdapter.DeleteCommand.Transaction = objTransaction
            End If

        End Sub

#End Region

    End Class

End Namespace
