Partial Class ADNSExportCI
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlConfirmationDate As CalendarDate
    Protected WithEvents ctlADNSReference As ADNSReference
    Protected WithEvents SummaryPager As DataGridPager

#Region " Web Form Designer Generated Code "

    'This call is required by the Web Form Designer.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

    End Sub

    Private Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
        'CODEGEN: This method call is required by the Web Form Designer
        'Do not modify it using the code editor.
        InitializeComponent()
    End Sub

#End Region

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        VLAHeader1.PageTitle = "ADNS Export : Channel Island Cases"
        SetEnterKeys()
        SummaryPager.SetGrid(grdADNSSummary)
        If Not IsPostBack Then
            litEmailDiv.Visible = False
            litSummaryDiv.Visible = False
            litReportDiv.Visible = False
            grdADNSSummary.Visible = False
            ctlConfirmationDate.DateField = CStr(Now())

            'Preset fields to last ADNS reference 
            Dim LastADNSReference As IDictionary
            LastADNSReference = BSELib.clsADNSReport.GetLastADNSReference("CI")
            If Not LastADNSReference.Count = 0 Then
                ctlADNSReference.ADNSYear = CInt(LastADNSReference("LastADNSReferenceYear"))
                ctlADNSReference.ADNSNumber = CInt(LastADNSReference("LastADNSReferenceNumber")) + 1

                txtEmailReference.Text = "DBSE" & ctlADNSReference.ADNSReference.Substring(5)
            End If

        End If
        ctlConfirmationDate.Mandatory = True
        ctlADNSReference.Mandatory = True
        EnableControls()
    End Sub

    Private Sub SetEnterKeys()

        SetTextboxControlOnEnter(txtEmailReference, ctlADNSReference.FirstClientID)
        ctlADNSReference.SetControlOnEnter(txtJerseyCases.ClientID)
        SetTextboxControlOnEnter(txtJerseyCases, txtGuernseyCases.ClientID)
        SetTextboxControlOnEnter(txtGuernseyCases, txtIsleOfManCases.ClientID)
        SetTextboxControlOnEnter(txtIsleOfManCases, ctlConfirmationDate.FirstClientID)
        ctlConfirmationDate.SetControlOnEnter(btnGenerateReport.ClientID)

    End Sub

#Region "Permissions"

    Private Sub EnableControls()
        VLAHeader1.GetUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "DEFRA Viewer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "DEFRA Data Entry" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "DEFRA Maintenance" Then
            ' Do Nothing
        ElseIf sGroupName = "VLA Data Entry" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "VLA Maintenance" Then
            ' Do Nothing
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnGenerateReport_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGenerateReport.Click
        Dim dConfirmationDate As Date

        If ctlADNSReference.Validate() AndAlso ctlConfirmationDate.Validate(dConfirmationDate) Then
            Dim objADNS As BSELib.clsADNSReport
            objADNS = New BSELib.clsADNSReport(txtEmailReference.Text, ctlADNSReference.ADNSYear, ctlADNSReference.ADNSNumber, CInt(txtJerseyCases.Text), CInt(txtGuernseyCases.Text), CInt(txtIsleOfManCases.Text), CDate(ctlConfirmationDate.DateField))
            litReportDiv.Visible = True
            grdADNSSummary.Visible = True
            litSummaryDiv.Visible = True
            litEmailDiv.Visible = True
            With objADNS
                lblStartADNSReferenceValue.Text = .StartADNSReference
                lblEndADNSReferenceValue.Text = .EndADNSReference
                txtFromAddress.Text = .FromEmailAddress
                txtToAddress.Text = .ToEmailAddress
                txtSubject.Text = .Subject
                txtBody.Text = .EmailBody
            End With

            Dim dtData As New DataTable()
            dtData = objADNS.SummaryCases
            Session.Item(SessionVars.SV_ADNSSummaryTable) = dtData
            Dim dvData As DataView = dtData.DefaultView
            Session.Item(SessionVars.SV_ADNSSummaryView) = dvData
            With grdADNSSummary
                .DataSource = dtData
                .DataKeyField = "ID"
                .CurrentPageIndex = 0
                .SelectedIndex = -1
                .EditItemIndex = -1
                .DataBind()
            End With

            With SummaryPager
                .DataTableSessionID = SessionVars.SV_ADNSSummaryTable
                .DataViewSessionID = SessionVars.SV_ADNSSummaryView
                .PageLinkCount = 10
                .AllowAddNew = False
                .AllowEdit = False
                .AllowDelete = False
                .Refresh()
            End With

            Session(SessionVars.SV_ADNSObject) = objADNS
        End If
    End Sub

    Private Sub btnSendEmail_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSendEmail.Click

        Dim objADNS As BSELib.clsADNSReport
        objADNS = CType(Session(SessionVars.SV_ADNSObject), BSELib.clsADNSReport)
        objADNS.EmailBody = txtBody.Text.Trim()
        objADNS.SendEmail(False, CStr(Session(SessionVars.SV_HeaderUserEmail)))
        Session.Remove(SessionVars.SV_ADNSObject)

        Response.Redirect("MaintenanceConfirmation.aspx?title=ADNS Export Complete&message=The message has been successfully sent.  You should receive a confirmation e-mail shortly.")
    End Sub

#End Region


    
End Class
