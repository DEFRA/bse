Partial Class FinalResultEntry
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlRetrospectiveDate As CalendarDate
    Protected WithEvents ctlRBSE As RBSE
    Protected WithEvents TestsPager As DataGridPager

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
        VLAHeader1.PageTitle = "Final Result Entry"
        TestsPager.SetGrid(grdTests)
        'SetEnterKeys()
        If Not IsPostBack Then
            LoadLookupLists()
        End If
        EnableControls()
    End Sub

    Private Sub SetEnterKeys()

        ctlRBSE.SetDefaultButton(btnLookup)
        ctlRetrospectiveDate.SetControlOnEnter(txtComments.ClientID)
        SetTextboxControlOnEnter(txtComments, btnSave.ClientID)

    End Sub

#Region "Lookup Population"

    Private Sub LoadLookupLists()

        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(7)
            If Not (objDataTable Is Nothing) Then
                ddlTestType.DataSource = objDataTable
                ddlTestType.DataValueField = "Code"
                ddlTestType.DataTextField = "Description"
                ddlTestType.DataBind()
                Common.AddItemToDropDownList(ddlTestType)
            End If

            objDataTable = objLookup.GetLookupData(8)
            If Not (objDataTable Is Nothing) Then
                ddlFinalResult.DataSource = objDataTable
                ddlFinalResult.DataValueField = "Code"
                ddlFinalResult.DataTextField = "Description"
                ddlFinalResult.DataBind()
                Common.AddItemToDropDownList(ddlFinalResult)

                ddlResult.DataSource = objDataTable
                ddlResult.DataValueField = "Code"
                ddlResult.DataTextField = "Description"
                ddlResult.DataBind()
                Common.AddItemToDropDownList(ddlResult)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve lookup data", ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnDownload_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnDownload.Click
        If Not Page.IsClientScriptBlockRegistered("ReportRedirect") Then

            If HttpContext.Current.Request.Browser.JavaScript Then
                Dim sJScript As New System.Text.StringBuilder()

                sJScript.Append("<script language=""JavaScript"">")
                sJScript.Append("<!-- " + vbCrLf)
                sJScript.Append("window.open (""Redirect.aspx?page=ResultMemo.aspx?RBSE=" & ctlRBSE.UnformattedRBSE)
                sJScript.Append(""",""relationspopupwindow"",""top=100,left=100,width=100,height=50,buttons=no,scrollbars=yes,location=no,menubar=no,resizable=yes,status=no,directories=no,toolbar=no"");" + vbCrLf)
                sJScript.Append("// -->")
                sJScript.Append("</script>")

                Page.RegisterClientScriptBlock("ReportRedirect", sJScript.ToString())
            End If
        End If
    End Sub

    Private Sub btnPrintMemo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPrintMemo.Click
        If Not Page.IsClientScriptBlockRegistered("ReportRedirect") Then

            If HttpContext.Current.Request.Browser.JavaScript Then
                Dim sJScript As New System.Text.StringBuilder()

                sJScript.Append("<script language=""JavaScript"">")
                sJScript.Append("<!-- " + vbCrLf)
                sJScript.Append("window.open (""ResultMemo.aspx?RBSE=" & ctlRBSE.UnformattedRBSE & "&Print=Print"");") '7
                sJScript.Append("// -->")
                sJScript.Append("</script>")

                Page.RegisterClientScriptBlock("ReportRedirect", sJScript.ToString())
            End If
        End If
    End Sub

    Private Sub ddlFinalResult_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlFinalResult.SelectedIndexChanged
        lblPositiveWarning.Visible = False
        lblDBSECleared.Visible = False
        lblResultsNotMatching.Visible = False

        If ddlFinalResult.SelectedItem.Value = "Pos" Then
            If Not ResultsContainPositive() Then
                lblResultsNotMatching.Visible = True
                lblResultsNotMatching.ToolTip = "You have chosen a Positive result but none of the Test results have returned Positive."
            End If
            If chkPaperworkComplete.Checked = False Then
                lblPositiveWarning.Visible = True
            End If
        ElseIf ddlFinalResult.SelectedItem.Value = "Neg" Then
            If ResultsContainPositive() Then
                lblResultsNotMatching.Visible = True
                lblResultsNotMatching.ToolTip = "You have chosen a Negative result but there is a Positive test result."
            End If
        End If
        lblFinalResultDateValue.Text = CStr(Day(Now())) & "/" & CStr(Month(Now())) & "/" & CStr(Year(Now()))
        btnDownload.Enabled = False
        btnPrintMemo.Enabled = False
        If lblDBSEValue.Text <> "TBC" And lblDBSEValue.Text <> "" Then
            lblDBSECleared.Visible = True
        End If
        lblDBSEValue.Text = "TBC"
        CheckControlState()
    End Sub

    Private Function ResultsContainPositive() As Boolean

        Dim dtData As DataTable = Session.Item(SessionVars.SV_FinalResultTestTable)
        Dim dr As DataRow
        For Each dr In dtData.Rows
            If dr.Item("TestResult").ToString() = "Pos" Then
                Return True
            End If
        Next
        Return False

    End Function

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub btnLookup_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLookup.Click
        Dim sRBSE As String = Replace(ctlRBSE.RBSE, "/", "")
        Session.Item(SessionVars.SV_RBSENumber) = sRBSE
        lblDBSECleared.Visible = False
        lblResultsNotMatching.Visible = False
        lblPositiveWarning.Visible = False

        If sRBSE <> "" Then
            If LoadCaseDetails() Then
                ctlRBSE.SetValidMark(True)
            Else
                EmptyFields()
                ctlRBSE.SetValidMark(False, "This RBSE could not be found in the database")
            End If
        Else
            EmptyFields()
            btnDownload.Enabled = False
            btnPrintMemo.Enabled = False
            ctlRBSE.SetValidMark(False, "Please enter an RBSE")
        End If
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        If UpdateFinalResult() Then
            ' Save session details to database
            Response.Redirect("FinalResultConfirmation.aspx")
        End If
    End Sub

    Protected Sub txtLabComment_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtLabComment.TextChanged
        CheckControlState()
    End Sub

#End Region

#Region "Permissions"

    Private Sub EnableControls()
        VLAHeader1.GetUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "DEFRA Viewer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "DEFRA Data Entry" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "DEFRA Maintenance" Then
            CheckControlState()
        ElseIf sGroupName = "VLA Data Entry" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "VLA Maintenance" Then
            CheckControlState()
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

    Private Sub CheckControlState()
        If ctlRBSE.RBSE <> "" Then
            ddlFinalResult.Enabled = True
            ddlTestType.Enabled = True
            txtComments.Enabled = True
            ddlResult.Enabled = True
            ctlRetrospectiveDate.Enabled = True

            If ddlFinalResult.SelectedItem.Value <> "" Then
                btnSave.Enabled = True
            Else
                btnSave.Enabled = False
            End If

            'If ddlFinalResult.SelectedItem.Value = "Neg" Then
            '    txtAlternateDiagnosis.Enabled = True
            'Else
            '    txtAlternateDiagnosis.Enabled = False
            'End If
            txtLabComment.Enabled = True
        Else
            ddlFinalResult.Enabled = False
            ddlTestType.Enabled = False
            txtComments.Enabled = False
            ddlResult.Enabled = False
            ctlRetrospectiveDate.Enabled = False
            DisableTestsGrid()
            btnSave.Enabled = False
            txtLabComment.Enabled = False
        End If
    End Sub

#End Region

#Region "Handle Data"

    Private Sub EmptyFields()
        lblEarTagValue.Text = ""
        lblDateOfBirthValue.Text = ""
        lblCPHHValue.Text = ""
        chkPurchaserBSE1Received.Checked = False
        chkBreederBSE1Received.Checked = False
        chkVendor1BSE1Received.Checked = False
        chkHomebredBSE1Received.Checked = False
        chkSummarySheetReceived.Checked = False
        chkPaperworkComplete.Checked = False
        SelectItemInDropDownList(ddlFinalResult, "")
        lblFinalResultDateValue.Text = ""
        lblDBSEValue.Text = ""
        SelectItemInDropDownList(ddlTestType, "")
        SelectItemInDropDownList(ddlResult, "")
        ctlRetrospectiveDate.DateField = ""
        txtComments.Text = ""
        lblNameValue.Text = ""
        lblAddressValue.Text = ""
        DisableTestsGrid()
    End Sub

    Private Function LoadCaseDetails() As Boolean
        Try
            Dim sRBSE As String = Session.Item(SessionVars.SV_RBSENumber)
            Dim objCase As New BSELib.clsCase()
            Dim dtData As DataTable
            Dim dtTestData As DataTable

            If Not (objCase.GetFinalResultByRBSE(sRBSE, dtData)) Then
                Throw New Exception("Case.GetFinalResultByRBSE returned False")
            End If

            Session.Item(SessionVars.SV_FinalResultTable) = dtData

            If Not (objCase.GetTestDataByRBSE(sRBSE, dtTestData)) Then
                Throw New Exception("Case.GetTestDataByRBSE returned False")
            End If

            Session.Item(SessionVars.SV_FinalResultTestTable) = dtTestData

            If dtData.Rows.Count <> 0 Then
                With dtData.Rows(0)
                    lblEarTagValue.Text = .Item("Eartag").ToString()
                    lblDateOfBirthValue.Text = FormatDate(.Item("BirthDate").ToString())
                    lblCPHHValue.Text = FormatCPHH(.Item("CPHH").ToString())
                    chkPurchaserBSE1Received.Checked = GetRowColumnData(.Item("IsPurchaserBSE1Received"))
                    chkBreederBSE1Received.Checked = GetRowColumnData(.Item("IsBreederBSE1Received"))
                    chkVendor1BSE1Received.Checked = GetRowColumnData(.Item("IsVendor1BSE1Received"))
                    chkHomebredBSE1Received.Checked = GetRowColumnData(.Item("IsHomebredBSE1Received"))
                    chkSummarySheetReceived.Checked = GetRowColumnData(.Item("IsSummarySheetReceived"))
                    chkPaperworkComplete.Checked = GetRowColumnData(.Item("IsPaperworkComplete"))

                    lblPurchaserBSE1ReceivedDate.Text = FormatDate(.Item("PurchaserBSE1ReceivedDate").ToString())
                    lblBreederBSE1ReceivedDate.Text = FormatDate(.Item("BreederBSE1ReceivedDate").ToString())
                    lblVendor1BSE1ReceivedDate.Text = FormatDate(.Item("Vendor1BSE1ReceivedDate").ToString())
                    lblHomebredBSE1ReceivedDate.Text = FormatDate(.Item("HomebredBSE1ReceivedDate").ToString())
                    lblSummarySheetReceivedDate.Text = FormatDate(.Item("SummarySheetReceivedDate").ToString())
                    lblPaperworkCompleteDate.Text = FormatDate(.Item("PaperworkCompleteDate").ToString())

                    If IsDBNull(.Item("FinalResult")) Then
                        lblFinalResultDateValue.Text = CStr(Day(Now())) & "/" & CStr(Month(Now())) & "/" & CStr(Year(Now()))
                        lblDBSEValue.Text = "TBC"
                        btnDownload.Enabled = False
                        btnPrintMemo.Enabled = False
                    Else
                        SelectItemInDropDownList(ddlFinalResult, .Item("FinalResult").ToString())
                        lblFinalResultDateValue.Text = FormatDate(.Item("FinalResultDate").ToString())
                        lblDBSEValue.Text = FormatDBSE(.Item("DBSE").ToString())

                        'txtAlternateDiagnosis.Text = .Item("AlternateDiagnosis").ToString()
                        txtLabComment.Text = .Item("LabComment").ToString()
                        'If .Item("FinalResult").ToString = "Neg" Then
                        '    txtAlternateDiagnosis.Enabled = True
                        'Else
                        '    txtAlternateDiagnosis.Enabled = False
                        'End If
                        txtLabComment.Enabled = True
                        btnDownload.Enabled = True
                        btnPrintMemo.Enabled = True
                    End If


                    lblCaseTypeValue.Text = .Item("CaseType").ToString()
                    SelectItemInDropDownList(ddlTestType, .Item("RetrospectiveTestType").ToString())
                    SelectItemInDropDownList(ddlResult, .Item("RetrospectiveResult").ToString())
                    ctlRetrospectiveDate.DateField = FormatDate(.Item("RetrospectiveResultDate").ToString())
                    txtComments.Text = .Item("RetrospectiveComment").ToString()
                    lblNameValue.Text = .Item("OwnerName").ToString()
                    lblAddressValue.Text = .Item("Address1").ToString()
                End With

                EnableTestsGrid()

                Return True
            End If
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Case Details.", ex)
            Return False
        End Try
    End Function

    Private Function UpdateFinalResult() As Boolean
        Try
            Dim objcase As New BSELib.clsCase()
            Dim dtData As DataTable = Session.Item(SessionVars.SV_FinalResultTable)

            Dim sRBSE As String = Replace(ctlRBSE.RBSE, "/", "")
            Dim sFinalResult As String = GetSelectedItemFromDropDownList(ddlFinalResult)
            If sFinalResult = "" Then Return False
            Dim dFinalResultDate As Date = FormatEmptyString(lblFinalResultDateValue.Text)
            Dim sRetrospectiveTestType As String = GetSelectedItemFromDropDownList(ddlTestType)
            Dim sRetrospectiveResult As String = GetSelectedItemFromDropDownList(ddlResult)
            Dim sRetrospectiveResultDate As String = ctlRetrospectiveDate.DateField
            Dim sRetrospectiveComment As String = txtComments.Text
            Dim sAlternateDiagnosis As String = ""
            Dim bRowstamp() As Byte = dtData.Rows(0)("Rowstamp")
            Dim iUserID As Integer = Session.Item(SessionVars.SV_HeaderUserID)
            Dim sDBSE As String
            Dim sLabComment As String = txtLabComment.Text

            If Not objcase.FinalResultEntry(sRBSE, _
                                        sFinalResult, _
                                        dFinalResultDate, _
                                        sRetrospectiveTestType, _
                                        sRetrospectiveResult, _
                                        sRetrospectiveResultDate, _
                                        sRetrospectiveComment, _
                                        bRowstamp, _
                                        iUserID, _
                                        sDBSE, _
                                        sAlternateDiagnosis, _
                                        sLabComment) Then
                clsAppError.DisplayError("Failed to Save Final Result Details.  FinalResultEntry returned False.")
            End If

            Return True

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Save Final Result Details.", ex)
            Return False
        End Try
    End Function
#End Region

#Region "Handle Grids"

    Private Sub DisableTestsGrid()
        grdTests.DataSource = New DataTable()
        grdTests.DataBind()
        TestsPager.AllowAddNew = False
        TestsPager.PageLinkCount = 0
        TestsPager.Refresh()
    End Sub

    Private Sub EnableTestsGrid()
        Dim dtData As DataTable = Session.Item(SessionVars.SV_FinalResultTestTable)
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_FinalResultTestView) = dvData

        grdTests.DataSource = dtData
        grdTests.DataKeyField = "ID"
        grdTests.CurrentPageIndex = 0
        grdTests.SelectedIndex = -1
        grdTests.EditItemIndex = -1
        grdTests.DataBind()

        TestsPager.DataTableSessionID = SessionVars.SV_FinalResultTestTable
        TestsPager.DataViewSessionID = SessionVars.SV_FinalResultTestView
        TestsPager.PageLinkCount = 10
        TestsPager.Refresh()
    End Sub

#End Region

End Class
