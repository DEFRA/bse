Partial Class ADNSExportNI
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlConfirmationDate As CalendarDate
    Protected WithEvents ctlADNSReference As ADNSReference
    Protected WithEvents ADNSPager As DataGridPager
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
        VLAHeader1.PageTitle = "ADNS Export : Northern Ireland Cases"
        SetEnterKeys()
        ADNSPager.SetGrid(grdADNS)
        SummaryPager.SetGrid(grdADNSSummary)
        If Not IsPostBack Then
            litEmailDiv.Visible = False
            litSummaryDiv.Visible = False
            litReportDiv.Visible = False
            grdADNSSummary.Visible = False
            ctlConfirmationDate.DateField = CStr(Now())
            PreparePage()

            'Preset fields to last ADNS reference 
            Dim LastADNSReference As IDictionary
            LastADNSReference = BSELib.clsADNSReport.GetLastADNSReference("NI")
            If Not LastADNSReference.Count = 0 Then
                ctlADNSReference.ADNSYear = CInt(LastADNSReference("LastADNSReferenceYear"))
                ctlADNSReference.ADNSNumber = CInt(LastADNSReference("LastADNSReferenceNumber")) + 1

                txtEmailReference.Text = "DBSE" & ctlADNSReference.ADNSReference.Substring(5)
            End If

        End If
        EnableControls()
    End Sub

    Private Sub SetEnterKeys()

        SetTextboxControlOnEnter(txtEmailReference, ctlADNSReference.FirstClientID)
        ctlADNSReference.SetControlOnEnter(txtRegion.ClientID)
        SetTextboxControlOnEnter(txtRegion, ctlConfirmationDate.FirstClientID)
        ctlConfirmationDate.SetControlOnEnter(btnAddToGrid.ClientID)

    End Sub

    Private Sub PreparePage()
        Dim dtData As New DataTable()

        dtData = BSELib.clsADNSReport.CreateADNSReportTable()

        Session.Item(SessionVars.SV_ADNSExportNITable) = dtData
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_ADNSExportNIView) = dvData

        With grdADNS
            .DataSource = dtData
            .DataKeyField = "ID"
            .CurrentPageIndex = 0
            .SelectedIndex = -1
            .EditItemIndex = -1
            .DataBind()
        End With

        With ADNSPager
            .DataTableSessionID = SessionVars.SV_ADNSExportNITable
            .DataViewSessionID = SessionVars.SV_ADNSExportNIView
            .PageLinkCount = 10
            .AllowAddNew = False
            .AllowEdit = False
            .AllowDelete = True
            .Refresh()
        End With
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
        lblEmailError.Visible = False
        lblNoDataError.Visible = False

        If txtEmailReference.Text = "" Then
            lblEmailError.Visible = True
            Exit Sub
        End If

        Dim dtData As DataTable
        dtData = CType(Session(SessionVars.SV_ADNSExportNITable), DataTable)
        If dtData.Rows.Count = 0 Then
            lblNoDataError.Visible = True
            Exit Sub
        End If

        Dim objADNS As BSELib.clsADNSReport
        objADNS = New BSELib.clsADNSReport(txtEmailReference.Text, dtData)
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

        Dim dtSummaryData As New DataTable()
        dtSummaryData = objADNS.SummaryCases
        Session.Item(SessionVars.SV_ADNSSummaryTable) = dtSummaryData
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_ADNSSummaryView) = dvData
        With grdADNSSummary
            .DataSource = dtSummaryData
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

    End Sub

    Private Sub btnSendEmail_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSendEmail.Click

        Dim objADNS As BSELib.clsADNSReport
        objADNS = CType(Session(SessionVars.SV_ADNSObject), BSELib.clsADNSReport)
        objADNS.EmailBody = txtBody.Text.Trim()
        objADNS.SendEmail(False, CStr(Session(SessionVars.SV_HeaderUserEmail)))
        Session.Remove(SessionVars.SV_ADNSObject)

        Response.Redirect("MaintenanceConfirmation.aspx?title=ADNS Export Complete&message=The message has been successfully sent.  You should receive a confirmation e-mail shortly.")
    End Sub

    Private Sub btnAddToGrid_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddToGrid.Click

        ctlADNSReference.Mandatory = True
        ctlConfirmationDate.Mandatory = True
        Dim dConfirmationDate As Date
        Dim bErrorsOccurred As Boolean = False

        'hide the report sections of the page
        litEmailDiv.Visible = False
        litSummaryDiv.Visible = False
        litReportDiv.Visible = False
        grdADNSSummary.Visible = False

        ctlADNSReference.SetValidMark(True)

        'display an error if no region was provided
        lblRegionError.Visible = False
        If txtRegion.Text = "" Then
            lblRegionError.Visible = True
            bErrorsOccurred = True
        End If

        If ctlADNSReference.Validate() Then

            If ctlADNSReference.ADNSNumber < 50000 OrElse ctlADNSReference.ADNSNumber > 59999 Then
                ctlADNSReference.SetValidMark(False, "Northern Ireland cases must have an ADNS Number beginning with a '5'")
                bErrorsOccurred = True
            End If
        Else
            bErrorsOccurred = True
        End If

        If Not ctlConfirmationDate.Validate(dConfirmationDate) Then
            bErrorsOccurred = True
        End If

        If bErrorsOccurred Then
            Exit Sub
        End If

        'add row to the datatable with the specified data
        Dim dtADNSReport As DataTable = Session.Item(SessionVars.SV_ADNSExportNITable)
        Dim drCurrentRow As DataRow = dtADNSReport.NewRow()
        With drCurrentRow
            .Item("ADNSYear") = ctlADNSReference.ADNSYear
            .Item("ADNSNumber") = ctlADNSReference.ADNSNumber
            .Item("ADNSRegionID") = txtRegion.Text
            .Item("ConfirmationDate") = CDate(ctlConfirmationDate.DateField)
        End With
        dtADNSReport.Rows.Add(drCurrentRow)

        Session.Item(SessionVars.SV_ADNSExportNITable) = dtADNSReport

        'rebind the grid to the datatable
        grdADNS.DataSource = dtADNSReport
        grdADNS.DataBind()
        ADNSPager.Refresh()

        'increment the input fields
        ctlADNSReference.ADNSNumber = ctlADNSReference.ADNSNumber + 1
        txtRegion.Text = ""
        ctlConfirmationDate.DateField = CStr(Now())

    End Sub

#End Region

End Class
