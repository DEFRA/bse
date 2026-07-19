Imports System

Namespace libDataAccess

#Region "Enums"

    Public Enum StatementType
        stSelect = 1
        stInsert = 2
        stUpdate = 3
        stDelete = 4
    End Enum

#End Region

    '=======================================================================================
    ' Class     : UpdateParameter
    ' Desc      : Holds information for a parameter that will be bound to a data command
    '             object at some point.
    '---------------------------------------------------------------------------------------
    ' Attributes: 
    ' [Private] m_ctType    [CommandType] - Select/Insert/Update/Delete command
    ' 
    '---------------------------------------------------------------------------------------
    ' Methods   : [Public] New            - Initialises parameter with property values
    '             [Public] GetCommandType - Get value of parameters command type
    '---------------------------------------------------------------------------------------
    ' Author    : PSH
    ' Created   : 27 June 2003
    '=======================================================================================
    ' Change Log
    '
    ' Date      Author  Comments
    ' ----      ------  --------
    '=======================================================================================
    Public Class UpdateParameter
        Inherits Parameter

        Private m_StatementType As StatementType

        ' Initialise Parameter's attribute data
        Public Sub New(ByVal strKey As String, _
                       ByVal statementType As StatementType, _
                       ByVal dbType As DbtType, _
                       ByVal strName As String, _
                       Optional ByVal strColumn As String = "", _
                       Optional ByVal intSize As Integer = 0, _
                       Optional ByVal objValue As Object = Nothing, _
                       Optional ByVal pdDirection As ParameterDirection = ParameterDirection.Input, _
                       Optional ByVal drvSrcVer As DataRowVersion = DataRowVersion.Default)

            MyBase.New(strKey, dbType, strName, strColumn, intSize, objValue, pdDirection, drvSrcVer)

            m_StatementType = statementType

        End Sub

        Public Function GetStatementType() As StatementType
            Return m_StatementType
        End Function

    End Class

End Namespace

