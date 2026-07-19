Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace libDataAccess

    '=======================================================================================
    ' Class     : UpdateParameterList Inherits ArrayList
    ' Desc      : Holds list of parameters that can be passed to a public clsDataAccess
    '             method.
    ' Note      : Would have been nice to derive from DictionaryBase so that we would not
    '             have to implement Item(string) methiod and could store key with parameter
    '             but DictionaryBase stores objects sorted , whereas we need to be able to
    '             access the parameters in the order added aswell as via key name
    '---------------------------------------------------------------------------------------
    ' Attributes: None
    '---------------------------------------------------------------------------------------
    ' Methods   : [Public] AddParameter - Creates update parameter for data & adds to list
    '             [Public] Item         - Allows retrieval of parameter by key name
    '---------------------------------------------------------------------------------------
    ' Author    : PSH
    ' Created   : 27 June 2003
    '=======================================================================================
    ' Change Log
    '
    ' Date      Author  Comments
    ' ----      ------  --------
    '=======================================================================================
    Public Class UpdateParameterList
        Inherits ArrayList

        ' Create new update parameter object and adds it the the array list
        Public Sub AddParameter(ByVal strKey As String, _
                                ByVal statementType As StatementType, _
                                ByVal dbType As DbtType, _
                                ByVal strParamName As String, _
                                Optional ByVal strColumn As String = "", _
                                Optional ByVal intSize As Integer = 0, _
                                Optional ByVal objValue As Object = Nothing, _
                                Optional ByVal daDirection As ParameterDirection = ParameterDirection.Input, _
                                Optional ByVal drvSourceVersion As DataRowVersion = DataRowVersion.Default)

            Dim clsParameter As New UpdateParameter(strKey, statementType, dbType, strParamName, strColumn, intSize, objValue, daDirection, drvSourceVersion)

            Add(clsParameter)

        End Sub

        ' short-cut functions for creating update, delete and insert parameters
        ' that have the same column and parameter names.  The key for the
        ' parameter will be set to the parameter name prepended with "Update_",
        ' "Insert_" or "Delete_" as appropriate.
        Public Sub AddUpdateParam(ByVal sParam As String, _
                                ByVal dbType As DbtType, _
                                Optional ByVal objValue As Object = Nothing, _
                                Optional ByVal daDirection As ParameterDirection = ParameterDirection.Input, _
                                Optional ByVal iSize As Integer = 0, _
                                Optional ByVal drvSourceVersion As DataRowVersion = DataRowVersion.Default)

            Dim sColumnName As String

            If drvSourceVersion = DataRowVersion.Original Then
                sColumnName = Mid$(sParam, 10) ' strip off "Original_"
            Else
                sColumnName = sParam
            End If

            AddParameter("Update_" & sParam, _
                         StatementType.stUpdate, _
                         dbType, _
                         "@" & sParam, _
                         sColumnName, _
                         iSize, _
                         objValue, _
                         daDirection, _
                            drvSourceVersion)
        End Sub
        Public Sub AddInsertParam(ByVal sParam As String, _
                                  ByVal dbType As DbtType, _
                                  Optional ByVal objValue As Object = Nothing, _
                                  Optional ByVal daDirection As ParameterDirection = ParameterDirection.Input, _
                                  Optional ByVal iSize As Integer = 0, _
                                Optional ByVal drvSourceVersion As DataRowVersion = DataRowVersion.Default)
            Dim sColumnName As String

            If drvSourceVersion = DataRowVersion.Original Then
                sColumnName = Mid$(sParam, 9) ' strip off "Original_"
            Else
                sColumnName = sParam
            End If

            AddParameter("Insert_" & sParam, _
                                     StatementType.stInsert, _
                                     dbType, _
                                     "@" & sParam, _
                                     sColumnName, _
                                     iSize, _
                                     objValue, _
                                     daDirection, _
                                        drvSourceVersion)
        End Sub
        Public Sub AddDeleteParam(ByVal sParam As String, _
                                  ByVal dbType As DbtType, _
                                  Optional ByVal objValue As Object = Nothing, _
                                   Optional ByVal daDirection As ParameterDirection = ParameterDirection.Input, _
                                Optional ByVal drvSourceVersion As DataRowVersion = DataRowVersion.Default)
            Dim sColumnName As String

            If drvSourceVersion = DataRowVersion.Original Then
                sColumnName = Mid$(sParam, 9) ' strip off "Original_"
            Else
                sColumnName = sParam
            End If

            AddParameter("Delete_" & sParam, _
                                                StatementType.stDelete, _
                                                dbType, _
                                                "@" & sParam, _
                                                sColumnName, _
                                                0, _
                                                objValue, _
                                                daDirection, _
                                                   drvSourceVersion)
        End Sub


        ' Allows parameters to be retrieved via their key name
        Default Public Overloads Property Item(ByVal strKey As String) As UpdateParameter
            Get
                Dim blnFound As Boolean = False
                Dim objParameter As UpdateParameter = Nothing
                Dim objFoundParameter As UpdateParameter = Nothing

                For Each objParameter In Me

                    If (objParameter.GetKey() = strKey) Then

                        objFoundParameter = objParameter
                        Exit For

                    End If

                Next

                Return objFoundParameter

            End Get
            Set(ByVal Value As UpdateParameter)

            End Set
        End Property

        Public Sub AddToAdapter(ByRef objAdapter As SqlDataAdapter)

            Dim objParameter As UpdateParameter = Nothing


            For Each objParameter In Me

                Select Case objParameter.GetStatementType()
                    Case StatementType.stSelect
                        objParameter.AddToCommand(objAdapter.SelectCommand)
                    Case StatementType.stInsert
                        objParameter.AddToCommand(objAdapter.InsertCommand)
                    Case StatementType.stUpdate
                        objParameter.AddToCommand(objAdapter.UpdateCommand)
                    Case StatementType.stDelete
                        objParameter.AddToCommand(objAdapter.DeleteCommand)
                End Select

            Next

        End Sub

        Public Sub GetFromAdapter(ByRef objAdapter As SqlDataAdapter)

            Dim objParameter As UpdateParameter = Nothing


            For Each objParameter In Me

                Select Case objParameter.GetStatementType()
                    Case StatementType.stSelect
                        objParameter.GetFromCommand(objAdapter.SelectCommand)
                    Case StatementType.stInsert
                        objParameter.GetFromCommand(objAdapter.InsertCommand)
                    Case StatementType.stUpdate
                        objParameter.GetFromCommand(objAdapter.UpdateCommand)
                    Case StatementType.stDelete
                        objParameter.GetFromCommand(objAdapter.DeleteCommand)

                End Select

            Next

        End Sub


    End Class


End Namespace

