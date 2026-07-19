Imports libDataAccess.libDataAccess
Imports libDataAccess.libDataAccess.TBCultureDA
Imports System.Data.SqlClient

Public Class clsADNSReport

    Private mdtADNSReport As DataTable

    Private msMissingCases As String
    Private mdtADNSSummary As DataTable
    Private msStartADNSReference As String
    Private msEndADNSReference As String

    Private msFromEmailAddress As String
    Private msToEmailAddress As String
    Private msSMTPHost As String
    Private miSMTPPort As Integer
    Private msSubject As String
    Private msEmailBody As String

    Private msArea As String

#Region "Shared Methods"


    'shared method to generate an empty datatable in the required
    'ADNS format.  To be used by the Northern Ireland case, where
    'the user builds the table themselves
    Public Shared Function CreateADNSReportTable() As DataTable

        Dim dtADNSReport As New DataTable()

        With dtADNSReport
            .Columns.Add("ID", System.Type.GetType("System.Int32"))
            .Columns("ID").AutoIncrement = True
            .Columns("ID").AutoIncrementSeed = 1
            .Columns("ID").AutoIncrementStep = 1

            .PrimaryKey = New DataColumn() {.Columns("ID")}

            .Columns.Add("RBSE", System.Type.GetType("System.String"))
            .Columns.Add("ADNSYear", System.Type.GetType("System.Int32"))
            .Columns.Add("ADNSNumber", System.Type.GetType("System.Int32"))
            .Columns.Add("ADNSRegionID", System.Type.GetType("System.Int32"))
            .Columns.Add("ADNSRegionName", System.Type.GetType("System.String"))
            .Columns.Add("ConfirmationDate", System.Type.GetType("System.DateTime"))
            .Columns.Add("ADNSReference", System.Type.GetType("System.String"))
            .Columns("ADNSReference").Expression = "ADNSYear + '/' + SUBSTRING('0000' + ADNSNumber, LEN(CONVERT(ADNSNumber, 'System.String')), 5)"
        End With

        Return dtADNSReport

    End Function

    'shared method to get the last ADNS reference used for a given area
    Public Shared Function GetLastADNSReference(ByVal area As String) As IDictionary
        Dim objInParamList As New ParameterList()
        Dim dsData As DataSet

        Try
            With objInParamList
                .QuickAddInputParam("Area", DbtType.dbtString, area)
            End With
            FillDataSet("GetLastADNSReferenceByArea", _
                        CommandType.StoredProcedure, _
                        dsData, _
                        objInParamList)

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw ex
        End Try

        Dim lastADNSReference As New Hashtable

        Dim table As DataTable = dsData.Tables(0)

        If Not table.Rows.Count = 0 Then
            Dim thisRow As DataRow = table.Rows(0)
            For Each thisColumn As DataColumn In table.Columns
                lastADNSReference.Add(thisColumn.ColumnName, thisRow.Item(thisColumn))
            Next
        End If

        Return lastADNSReference

    End Function


#End Region

#Region "Constructors"

    'constructor for GB cases
    Public Sub New(ByVal sEmailReference As String, ByVal iADNSYear As Integer, ByVal iADNSNumber As Integer)

        Dim objInParamList As New ParameterList()
        Dim dsData As DataSet

        msArea = "GB"

        Try
            With objInParamList
                .QuickAddInputParam("ADNSYear", DbtType.dbtInteger, iADNSYear)
                .QuickAddInputParam("StartADNSNumber", DbtType.dbtInteger, iADNSNumber)
            End With

            FillDataSet("GetADNSCasesForGB", _
                          CommandType.StoredProcedure, _
                          dsData, _
                          objInParamList)

        Catch ex As Exception
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw ex
        End Try

        mdtADNSReport = dsData.Tables(0)

        msMissingCases = GetMissingCases(dsData.Tables(1))
        mdtADNSSummary = GetSummaryCases()
        InitializeEmail(sEmailReference)

    End Sub

    'constructor for Channel Island cases
    Public Sub New(ByVal sEmailReference As String, ByVal iADNSYear As Integer, ByVal iADNSNumber As Integer, ByVal iJerseyCases As Integer, ByVal iGuernseyCases As Integer, ByVal iIsleOfManCases As Integer, ByVal dConfirmationDate As Date)

        Dim iCurrentADNSNumber As Integer = iADNSNumber
        Dim drCurrentRow As DataRow

        msArea = "CI"

        mdtADNSReport = clsADNSReport.CreateADNSReportTable()

        If iJerseyCases > 0 Then
            For iCurrentADNSNumber = iADNSNumber To iADNSNumber + iJerseyCases - 1
                drCurrentRow = mdtADNSReport.NewRow()
                With drCurrentRow
                    .Item("ADNSYear") = iADNSYear
                    .Item("ADNSNumber") = iCurrentADNSNumber
                    .Item("ADNSRegionID") = 6200 'Jersey region code
                    .Item("ADNSRegionName") = "Jersey"
                    .Item("ConfirmationDate") = dConfirmationDate
                End With
                mdtADNSReport.Rows.Add(drCurrentRow)
            Next iCurrentADNSNumber
        End If

        If iGuernseyCases > 0 Then
            For iCurrentADNSNumber = iADNSNumber + iJerseyCases To iADNSNumber + iJerseyCases + iGuernseyCases - 1
                drCurrentRow = mdtADNSReport.NewRow()
                With drCurrentRow
                    .Item("ADNSYear") = iADNSYear
                    .Item("ADNSNumber") = iCurrentADNSNumber
                    .Item("ADNSRegionID") = 6100 'Guernsey region code
                    .Item("ADNSRegionName") = "Guernsey"
                    .Item("ConfirmationDate") = dConfirmationDate
                End With
                mdtADNSReport.Rows.Add(drCurrentRow)
            Next iCurrentADNSNumber
        End If

        If iIsleOfManCases > 0 Then
            For iCurrentADNSNumber = iADNSNumber + iJerseyCases + iGuernseyCases To iADNSNumber + iJerseyCases + iGuernseyCases + iIsleOfManCases - 1
                drCurrentRow = mdtADNSReport.NewRow()
                With drCurrentRow
                    .Item("ADNSYear") = iADNSYear
                    .Item("ADNSNumber") = iCurrentADNSNumber
                    .Item("ADNSRegionID") = 6300 'Isle of Man region code
                    .Item("ADNSRegionName") = "Isle of Man"
                    .Item("ConfirmationDate") = dConfirmationDate
                End With
                mdtADNSReport.Rows.Add(drCurrentRow)
            Next iCurrentADNSNumber
        End If

        mdtADNSSummary = GetSummaryCases()
        InitializeEmail(sEmailReference)

    End Sub

    'constructor for Northern Ireland cases
    Public Sub New(ByVal sEmailReference As String, ByRef dtADNSReport As DataTable)

        msArea = "NI"

        mdtADNSReport = dtADNSReport

        mdtADNSSummary = GetSummaryCases()
        InitializeEmail(sEmailReference)

    End Sub

#End Region

#Region "Report Data Generation"

    Private Function GetMissingCases(ByRef dtMissingCases As DataTable) As String
        Dim sCases As New System.Text.StringBuilder()

        If dtMissingCases.Rows.Count > 0 Then
            Dim drCurrentRow As DataRow

            For Each drCurrentRow In dtMissingCases.Rows
                If sCases.Length > 0 Then
                    sCases.Append(", ")
                End If
                sCases.Append(CStr(drCurrentRow("RBSE")))
            Next
        End If

        Return sCases.ToString()

    End Function

    Private Function GetSummaryCases() As DataTable

        Dim dtSummary As New DataTable()

        With dtSummary
            .Columns.Add("ID", System.Type.GetType("System.Int32"))
            .Columns("ID").AutoIncrement = True
            .Columns("ID").AutoIncrementSeed = 1
            .Columns("ID").AutoIncrementStep = 1

            .PrimaryKey = New DataColumn() {.Columns("ID")}

            .Columns.Add("ADNSRegionID", System.Type.GetType("System.Int32"))
            .Columns.Add("ADNSRegionName", System.Type.GetType("System.String"))
            .Columns.Add("CasesCount", System.Type.GetType("System.Int32"))
        End With

        Dim drCurrentRow As DataRow
        Dim drSummaryRow() As DataRow

        For Each drCurrentRow In mdtADNSReport.Rows
            drSummaryRow = dtSummary.Select("ADNSRegionID = " & CStr(drCurrentRow("ADNSRegionID")))
            If drSummaryRow.Length = 1 Then
                drSummaryRow(0)("CasesCount") = CInt(drSummaryRow(0)("CasesCount")) + 1
            Else
                Dim drNewSummaryRow As DataRow = dtSummary.NewRow()
                drNewSummaryRow("ADNSRegionID") = drCurrentRow("ADNSRegionID")
                drNewSummaryRow("ADNSRegionName") = drCurrentRow("ADNSRegionName")
                drNewSummaryRow("CasesCount") = 1
                dtSummary.Rows.Add(drNewSummaryRow)
            End If
        Next

        Return dtSummary
    End Function

#End Region

#Region "Email Data Generation"

    Private Sub InitializeEmail(ByVal sEmailReference As String)

        msFromEmailAddress = System.Configuration.ConfigurationSettings. _
                                        AppSettings("ADNSEmailFromAddress")

        msToEmailAddress = System.Configuration.ConfigurationSettings. _
                                        AppSettings("ADNSEmailToAddress")

        msSMTPHost = System.Configuration.ConfigurationSettings. _
                                AppSettings("SMTPHost")

        miSMTPPort = CInt(System.Configuration.ConfigurationSettings. _
                                        AppSettings("SMTPPort"))

        msSubject = "t=DATA;r=" & sEmailReference

        If mdtADNSReport.Rows.Count > 0 Then
            msEmailBody = CreateEmailBody()

            msStartADNSReference = CStr(mdtADNSReport.Compute("MIN(ADNSReference)", ""))
            msEndADNSReference = CStr(mdtADNSReport.Compute("MAX(ADNSReference)", ""))
        End If

    End Sub


    Private Function CreateEmailBody() As String

        Dim sEmail As System.Text.StringBuilder = New System.Text.StringBuilder()

        Dim drCurrentRow As DataRow
        With sEmail
            For Each drCurrentRow In mdtADNSReport.Rows

                .Append("<I>CVETUNK1")
                .Append(vbCrLf)
                .Append("<C>104<V>30")
                .Append(vbCrLf)
                .Append("<C>110<V>")
                .Append(drCurrentRow.Item("ADNSYear"))
                .Append(" / ")
                .Append(Right$("0000" & CStr(drCurrentRow.Item("ADNSNumber")), 5))
                .Append(vbCrLf)
                .Append("<C>112<V>")
                .Append(Right$("0000" & CStr(drCurrentRow.Item("ADNSRegionID")), 5))
                .Append(vbCrLf)
                .Append("<C>117<V>2")
                .Append(vbCrLf)
                .Append("<C>140<V>")
                Dim dConfirmationDate As Date = CDate(drCurrentRow.Item("ConfirmationDate"))
                .Append(Right$("0" + CStr(dConfirmationDate.Day), 2))
                .Append(Right$("0" + CStr(dConfirmationDate.Month), 2))
                .Append(Right$(CStr(dConfirmationDate.Year), 2))
                .Append(vbCrLf)
            Next
        End With

        Return sEmail.ToString()

    End Function
#End Region

#Region "Email Sending"

    Public Sub SendEmail(ByVal bSaveADNSData As Boolean, ByVal sUserEmail As String)

        Dim objDBConn As SqlConnection = Nothing
        Dim objDBTran As SqlTransaction = Nothing
        Dim dSentDate As Date = Now()
        Try
            'open a database connection and begin a transaction
            objDBConn = TBCultureDA.OpenConnection()
            objDBTran = TBCultureDA.BeginTransaction(objDBConn)

            If bSaveADNSData Then
                Dim drCurrentRow As DataRow
                For Each drCurrentRow In mdtADNSReport.Rows
                    UpdateCaseWithADNS(objDBConn, objDBTran, dSentDate, drCurrentRow)
                Next
            End If

            EditLastADNSReference(objDBConn, objDBTran)

            Dim objSMTP As System.Net.Mail.SmtpClient = New System.Net.Mail.SmtpClient(msSMTPHost, miSMTPPort)
            'send email to user
            objSMTP.Send(msFromEmailAddress, sUserEmail, msSubject, msEmailBody)
            'send email to Brussels
            objSMTP.Send(msFromEmailAddress, msToEmailAddress, msSubject, msEmailBody)

            'commit the database transaction
            TBCultureDA.CommitTransaction(objDBTran)

        Catch ex As Exception
            If Not objDBTran Is Nothing Then
                TBCultureDA.RollbackTransaction(objDBTran)
            End If
            clsLog.LogException(ex, clsLog.LogSource.lsStoredProcedure)
            Throw ex
        Finally
            If Not objDBConn Is Nothing Then
                TBCultureDA.CloseConnection(objDBConn)
            End If
        End Try

    End Sub

    Private Sub UpdateCaseWithADNS(ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction, ByVal dSentDate As Date, ByRef drData As DataRow)

        Dim objParamList As New libDataAccess.libDataAccess.ParameterList()

        With objParamList
            .AddParameter("RETURN_VALUE", DbtType.dbtInteger, "RETURN_VALUE", daDirection:=ParameterDirection.ReturnValue)
            .QuickAddInputParam("RBSE", DbtType.dbtString, drData("RBSE"))
            .QuickAddInputParam("SentDate", DbtType.dbtDateTime, dSentDate)
            .QuickAddInputParam("ADNSRegionID", DbtType.dbtInteger, drData("ADNSRegionID"))
            .QuickAddInputParam("ADNSYear", DbtType.dbtSmallInt, drData("ADNSYear"))
            .QuickAddInputParam("ADNSNumber", DbtType.dbtInteger, drData("ADNSNumber"))
            .QuickAddInputParam("RowStamp", DbtType.dbtBinary, drData("RowStamp"))
        End With

        TBCultureDA.ExecuteNonQuery(objDBConn, objDBTran, "EditCaseADNS", CommandType.StoredProcedure, objParamList)
        Dim iReturnValue As Integer = CInt(objParamList("RETURN_VALUE").Value)
        Select Case iReturnValue
            Case 1
                Throw New CaseUpdateException("The case with RBSE " & CStr(drData("RBSE")) & " has had its details changed by another user")
            Case 2
                Throw New CaseUpdateException("The case with RBSE " & CStr(drData("RBSE")) & " has been reported to Brussels by another user")
            Case 3
                Throw New CaseUpdateException("The case with RBSE " & CStr(drData("RBSE")) & " has had its ADNS Region changed by another user")
            Case 4
                Throw New CaseUpdateException("The case with RBSE " & CStr(drData("RBSE")) & " has been assigned an ADNS Reference by another user")
            Case 5
                Throw New CaseUpdateException("Failed to update the case with RBSE " & CStr(drData("RBSE")) & " with ADNS information")
        End Select
    End Sub

    Private Sub EditLastADNSReference(ByRef objDBConn As SqlConnection, ByRef objDBTran As SqlTransaction)
        Dim objInParameterList As New ParameterList()
        With objInParameterList
            .QuickAddInputParam("Area", DbtType.dbtString, msArea)
            .QuickAddInputParam("EmailReference", DbtType.dbtString, Right(msSubject, 9))
            .QuickAddInputParam("ADNSReferenceYear", DbtType.dbtSmallInt, msEndADNSReference.Substring(0, 4))
            .QuickAddInputParam("ADNSReferenceNumber", DbtType.dbtInteger, Right(msEndADNSReference, 5))

        End With
        ExecuteNonQuery(objDBConn, objDBTran, "EditLastADNSReference", CommandType.StoredProcedure, objInParameterList)
    End Sub

#End Region

#Region "Public Properties"

    Public ReadOnly Property MissingCases() As String
        Get
            Return msMissingCases
        End Get
    End Property

    Public ReadOnly Property FromEmailAddress() As String
        Get
            Return msFromEmailAddress
        End Get
    End Property

    Public ReadOnly Property ToEmailAddress() As String
        Get
            Return msToEmailAddress
        End Get
    End Property

    Public ReadOnly Property Subject() As String
        Get
            Return msSubject
        End Get
    End Property

    Public Property EmailBody() As String
        Get
            Return msEmailBody
        End Get
        Set(ByVal value As String)
            msEmailBody = value
        End Set
    End Property

    Public ReadOnly Property StartADNSReference() As String
        Get
            Return msStartADNSReference
        End Get
    End Property

    Public ReadOnly Property EndADNSReference() As String
        Get
            Return msEndADNSReference
        End Get
    End Property

    Public ReadOnly Property SummaryCases() As DataTable
        Get
            Return mdtADNSSummary
        End Get
    End Property
#End Region

End Class
