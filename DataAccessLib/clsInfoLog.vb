Imports System
Imports System.IO           ' Log to File support
Imports System.Diagnostics  ' Log to event viewer support

Imports libDataAccess.libDataAccess

Namespace libDataAccess

#Region "Enums"

    Public Enum LogType
        ltInformation = 1
        ltError = 2
        ltWarning = 3
        ltCritical = 4
    End Enum


#End Region

    '=======================================================================================
    ' Class     : InfoLog
    ' Desc      : The class performs the basics for information logging to file/database
    '             and to the event viewer. To log entries to file and event viewer you can
    '             instantiate this class and use it. However to log to database you must
    '             inherit this class and override the LogToDatabase Method with specific
    '             code relevant to your database table etc.
    '             All Methods are overridable so that you can modify the logging procedure
    '             to various locations and possibly add new logging destinations.
    '---------------------------------------------------------------------------------------
    ' Attributes: [Protected] m_strUser    - User name using module
    '             [Protected] m_strCompany - Company name to use for event viewer
    '             [Protected] m_strProduct - Product name to use for event viewer
    '             [Protected] m_strLogFile - Path/Name of log file
    '---------------------------------------------------------------------------------------
    ' Methods   : [Public]    New (Constructor) - Initialises private data
    '             [Public]    LogEntry          - Log an entry to various locations
    '             [Protected] LogToFile         - Log the entry to file
    '             [Protected] LogToDatabase     - Log the entry to database
    '             [Protected] LogToEventViewer  - Log the entry to event viewer
    '---------------------------------------------------------------------------------------
    ' Author    : PSH
    ' Created   : 01 July 2003
    '=======================================================================================
    ' Change Log
    '
    ' Date      Author  Comments
    ' ----      ------  --------
    '=======================================================================================
    Public Class InfoLog

        Const c_strCompanyName As String = "VLA"
        Const c_strProductName As String = "BSESystem"

#Region "Public Methods"

        '=======================================================================================
        ' Sub       : LogToFile
        ' Desc      : This method attempts to log the entry to the file path supplied in the
        '             constructor.
        ' Notes     : If this method fails we do not log it.
        '---------------------------------------------------------------------------------------
        ' Arguments : ltType         [IN]   [LogType] - log type - Info/Warning/Error etc
        '             strSource      [IN]   [String]  - name of source of log entry
        '             strDescription [IN]   [String]  - main text description of log entry
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 01 July 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Public Shared Sub LogToFile(ByVal ltType As LogType, _
                                    ByVal strDescription As String, _
                                    Optional ByVal strUser As String = "", _
                                    Optional ByVal strSource As String = "", _
                                    Optional ByVal strFile As String = "")

            ' Check we have a log file path setup
            If (strFile = "") Then
                Return
            End If

            Dim dtNow As DateTime
            Dim swWriter As StreamWriter
            Dim strLogText As String


            Try
                ' Attempt to open/create logFile for writing
                swWriter = New StreamWriter(strFile, True)
                swWriter.AutoFlush = True

                ' Get Date/Time to log
                dtNow = DateTime.Now

                strLogText = dtNow.ToString()

                Select Case (ltType)
                    Case LogType.ltInformation
                        strLogText += " Information "
                    Case LogType.ltError
                        strLogText += " Error       "
                    Case LogType.ltWarning
                        strLogText += " Warning     "
                    Case LogType.ltCritical
                        strLogText += " Critical    "
                    Case Else
                        strLogText += " Unknown     "
                End Select

                strLogText += strUser + " "
                strLogText += strSource + " "
                strLogText += strDescription

                swWriter.WriteLine(strLogText)

                swWriter.Close()
            Catch err As SystemException
                Throw New LogInfoException("An exception occured in LogToFile.")
            End Try

        End Sub

        '=======================================================================================
        ' Sub       : LogToDatabase
        ' Desc      : This method is merely a place holder method so that a deriving class 
        '             just has to override this method and the LogEntry process will still
        '             process an entry internally.
        '             This class could have been made 'MustInherit' because this method needs
        '             to be added before logging to a database can be used. However, because
        '             the other two logging destinations do not need specific code I thought
        '             it correct to make this class be instaniatable for basic logging facilities.
        ' Notes     : If this method fails we do not log it.
        '---------------------------------------------------------------------------------------
        ' Arguments : ltType         [IN]   [LogType] - log type - Info/Warning/Error etc
        '             strSource      [IN]   [String]  - name of source of log entry
        '             strDescription [IN]   [String]  - main text description of log entry
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 01 July 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        'Public Shared Sub LogToDatabase(ByVal ltType As LogType, _
        '                                ByVal strDescription As String, _
        '                                Optional ByVal strUser As String = "", _
        '                                Optional ByVal intSource As Integer = 0, _
        '                                Optional ByVal strSource As String = "")

        '    Dim objParams As ParameterList


        '    Try
        '        objParams = New ParameterList()

        '        If Not (objParams Is Nothing) Then
        '            objParams.AddParameter("ReturnCode", DbtType.dbtInteger, "@Error", , , , ParameterDirection.ReturnValue)
        '            objParams.AddParameter("UserID", DbtType.dbtString, "@UserID", "UserID", 20)
        '            objParams.AddParameter("Type", DbtType.dbtTinyInt, "@Type", "Type")
        '            objParams.AddParameter("Source", DbtType.dbtTinyInt, "@Source", "Source")
        '            objParams.AddParameter("Description", DbtType.dbtString, "@Description", "Description", 4000)

        '            objParams.Item("UserID").Value = strUser

        '            If (ltType = LogType.ltInformation) Then
        '                objParams.Item("Type").Value = 1
        '            Else
        '                objParams.Item("Type").Value = 2
        '            End If

        '            objParams.Item("Source").Value = intSource

        '            Dim strText As String

        '            ' If nothing specified for strSource then just use strDescription directly
        '            If (strSource = "") Then
        '                strText = strDescription
        '            Else
        '                strText = "Source: " + strSource + " - Description: " + strDescription
        '            End If

        '            objParams.Item("Description").Value = strText

        '            DataAccess.ExecuteNonQuery("AddLog", CommandType.StoredProcedure, objParams)
        '        End If

        '    Catch ex As Exception
        '        Throw New LogInfoException("An exception occured in LogToDatabase.", ex)
        '    End Try

        'End Sub

        '=======================================================================================
        ' Sub       : LogToEventViewer
        ' Desc      : This method attempts to log the entry to the event viewer under the
        '             company and product name supplid to the constructor.
        '             A check will be made to see if the company/product source exists before
        '             using it - if not it will be created.
        ' Notes     : If this method fails we do not log it.
        '---------------------------------------------------------------------------------------
        ' Arguments : ltType         [IN]   [LogType] - log type - Info/Warning/Error etc
        '             strSource      [IN]   [String]  - name of source of log entry
        '             strDescription [IN]   [String]  - main text description of log entry
        '---------------------------------------------------------------------------------------
        ' Author    : PSH
        ' Created   : 01 July 2003
        '=======================================================================================
        ' Change Log
        '
        ' Date      Author  Comments
        ' ----      ------  --------
        '=======================================================================================
        Public Shared Sub LogToEventViewer(ByVal ltType As LogType, _
                                           ByVal strSource As String, _
                                           ByVal strDescription As String)

            ' Create the source, if it does not already exist.
            If Not EventLog.SourceExists(c_strCompanyName) Then
                EventLog.CreateEventSource(c_strCompanyName, c_strProductName)
            End If

            ' Create an EventLog instance and assign its source.
            Dim elEntry As New EventLog()

            Dim eltType As EventLogEntryType

            Select Case (ltType)
                Case LogType.ltInformation
                    eltType = EventLogEntryType.Information
                Case LogType.ltWarning
                    eltType = EventLogEntryType.Warning
                Case Else
                    eltType = EventLogEntryType.Error
            End Select

            Dim strText As String
            strText.Format("Source: %s\nDescription: %s", strSource, strDescription)

            ' Write an informational entry to the event log.    
            elEntry.WriteEntry(c_strCompanyName, strDescription, eltType)

            elEntry.Close()

        End Sub

#End Region

    End Class

    Public Class LogInfoException
        Inherits System.Exception

        Public Sub New(ByVal strMessage As String)
            MyBase.New(strMessage)
        End Sub

        Public Sub New(ByVal strMessage As String, ByVal InnerException As Exception)
            MyBase.New(strMessage, InnerException)
        End Sub

    End Class

End Namespace
