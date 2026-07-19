Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace libDataAccess

    '=======================================================================================
    ' Class     : ParameterList Inherits ArrayList
    ' Desc      : Holds list of parameters that can be passed to a public clsDataAccess
    '             method.
    ' Note      : Would have been nice to derive from DictionaryBase so that we would not
    '             have to implement Item(string) methiod and could store key with parameter
    '             but DictionaryBase stores objects sorted , whereas we need to be able to
    '             access the parameters in the order added aswell as via key name
    '---------------------------------------------------------------------------------------
    ' Attributes: None
    '---------------------------------------------------------------------------------------
    ' Methods   : [Public] AddParameter       - Creates parameter for data & adds to list
    '             [Public] Item               - Allows retrieval of parameter by key name
    '---------------------------------------------------------------------------------------
    ' Author    : PSH
    ' Created   : 27 June 2003
    '=======================================================================================
    ' Change Log
    '
    ' Date      Author  Comments
    ' ----      ------  --------
    '=======================================================================================
    Public Class ParameterList
        Inherits ArrayList

        ' Create new parameter object and adds it the the array list
        Public Sub AddParameter(ByVal strKey As String, _
                                ByVal dbType As DbtType, _
                                ByVal strParamName As String, _
                                Optional ByVal strColumn As String = "", _
                                Optional ByVal intSize As Integer = 0, _
                                Optional ByVal objValue As Object = Nothing, _
                                Optional ByVal daDirection As ParameterDirection = ParameterDirection.Input, _
                                Optional ByVal drvSourceVersion As DataRowVersion = DataRowVersion.Default)

            Dim clsParameter As New Parameter(strKey, dbType, strParamName, strColumn, intSize, objValue, daDirection, drvSourceVersion)

            Add(clsParameter)

        End Sub

        ' short-cut function to add an input parameter whose ID, column name
        ' and parameter name are all the same; and set a value at the same
        ' time.
        Public Sub QuickAddInputParam(ByVal sField As String, _
                                      ByVal dbType As DbtType, _
                                      Optional ByVal Value As Object = Nothing)

            AddParameter(sField, dbType, "@" & sField, sField)
            If Not Value Is Nothing Then
                Item(sField).Value = Value
            End If
        End Sub

        ' short-cut function to add an output parameter with the same ID and
        ' column name but no parameter name - i.e. a value returned in a 
        ' resultset.
        Public Sub QuickAddResultsetParam(ByVal sField As String, _
                                          ByVal dbType As DbtType)
            AddParameter(sField, dbType, "", sField, , "", ParameterDirection.Output)
        End Sub

        ' Allows parameters to be retrieved via their key name
        Default Public Overloads Property Item(ByVal strKey As String) As Parameter
            Get
                Dim blnFound As Boolean = False
                Dim objParameter As Parameter = Nothing
                Dim objFoundParameter As Parameter = Nothing

                For Each objParameter In Me

                    If (objParameter.GetKey() = strKey) Then

                        objFoundParameter = objParameter
                        Exit For

                    End If

                Next

                Return objFoundParameter

            End Get
            Set(ByVal Value As Parameter)

            End Set
        End Property

        Public Sub AddToCommand(ByRef objCommand As SqlCommand)

            Dim objParameter As Parameter = Nothing


            For Each objParameter In Me

                objParameter.AddToCommand(objCommand)

            Next

        End Sub

        Public Sub GetFromCommand(ByRef objCommand As SqlCommand)

            Dim objParameter As Parameter = Nothing


            For Each objParameter In Me

                objParameter.GetFromCommand(objCommand)

            Next

        End Sub

        Public Sub GetFromReader(ByRef objReader As SqlDataReader)

            Dim intParam As Integer = 0
            Dim objParameter As Parameter = Nothing

            For Each objParameter In Me

                objParameter.GetFromReader(intParam, objReader)

                intParam = intParam + 1

            Next

        End Sub

    End Class


End Namespace
