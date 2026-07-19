Partial Class SearchCase
    Inherits System.Web.UI.Page
    Protected WithEvents ctlRBSE As RBSE
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ResultsPager As DataGridPager
    Protected WithEvents ctlFormADateFrom As CalendarDate
    Protected WithEvents ctlFormADateTo As CalendarDate
    Protected WithEvents ctlFinalResultDateFrom As CalendarDate
    Protected WithEvents ctlFinalResultDateTo As CalendarDate
    Protected WithEvents ctlBirthDateFrom As CalendarDate
    Protected WithEvents ctlBirthDateTo As CalendarDate

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
        VLAHeader1.PageTitle = "Case Search"
        SetEnterKeys()
        ResultsPager.SetGrid(grdResults)
        If Not IsPostBack Then
            LoadLookupLists()
            grdResults.Visible = False
            ResultsPager.Visible = False
        End If
    End Sub

    Private Sub SetEnterKeys()

        ctlRBSE.SetControlOnEnter(txtEartag.ClientID)
        SetTextboxControlOnEnter(txtEartag, txtDBSE.ClientID)
        SetTextboxControlOnEnter(txtDBSE, ddlFate.ClientID)
        ctlFormADateFrom.SetControlOnEnter(ctlFormADateTo.FirstClientID)
        ctlFormADateTo.SetControlOnEnter(ctlFinalResultDateFrom.FirstClientID)
        ctlFinalResultDateFrom.SetControlOnEnter(ctlFinalResultDateTo.FirstClientID)
        ctlFinalResultDateTo.SetControlOnEnter(ctlBirthDateFrom.FirstClientID)
        ctlBirthDateFrom.SetControlOnEnter(ctlBirthDateTo.FirstClientID)
        ctlBirthDateTo.SetControlOnEnter(txtNotes.ClientID)
        SetTextboxControlOnEnter(txtNotes, btnSearch.ClientID)

    End Sub

    Private Sub LoadLookupLists()

        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_CASE_FATE)
            If Not (objDataTable Is Nothing) Then
                ddlFate.DataSource = objDataTable
                ddlFate.DataValueField = "Code"
                ddlFate.DataTextField = "Description"
                ddlFate.DataBind()
                Common.AddItemToDropDownList(ddlFate)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_SEX)
            If Not (objDataTable Is Nothing) Then
                ddlSex.DataSource = objDataTable
                ddlSex.DataValueField = "Code"
                ddlSex.DataTextField = "Description"
                ddlSex.DataBind()
                Common.AddItemToDropDownList(ddlSex)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_TEST_RESULT)
            If Not (objDataTable Is Nothing) Then
                ddlFinalResult.DataSource = objDataTable
                ddlFinalResult.DataValueField = "Code"
                ddlFinalResult.DataTextField = "Description"
                ddlFinalResult.DataBind()
                Common.AddItemToDropDownList(ddlFinalResult)
            End If

            objDataTable = objLookup.GetLookupData(10)
            If Not (objDataTable Is Nothing) Then
                ddlSurvey.DataSource = objDataTable
                ddlSurvey.DataValueField = "Code"
                ddlSurvey.DataTextField = "Description"
                ddlSurvey.DataBind()
                Common.AddItemToDropDownList(ddlSurvey)
            End If

            objDataTable = GetPassiveActiveTable
            If Not (objDataTable Is Nothing) Then
                ddlPassiveActive.DataSource = objDataTable
                ddlPassiveActive.DataValueField = "Code"
                ddlPassiveActive.DataTextField = "Description"
                ddlPassiveActive.DataBind()
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Case Search' drop down lists.", ex)
        End Try

    End Sub

    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click

        lblResultError.Visible = False

        If ctlRBSE.RBSE = "" _
            AndAlso txtEartag.Text = "" _
            AndAlso txtDBSE.Text = "" _
            AndAlso GetSelectedItemFromDropDownList(ddlFate) = "" _
            AndAlso GetSelectedItemFromDropDownList(ddlFinalResult) = "" _
            AndAlso GetSelectedItemFromDropDownList(ddlSex) = "" _
            AndAlso GetSelectedItemFromDropDownList(ddlSurvey) = "" _
            AndAlso ctlFormADateFrom.DateField = "" _
            AndAlso ctlFormADateTo.DateField = "" _
            AndAlso ctlFinalResultDateFrom.DateField = "" _
            AndAlso ctlFinalResultDateTo.DateField = "" _
            AndAlso ctlBirthDateFrom.DateField = "" _
            AndAlso ctlBirthDateTo.DateField = "" _
            AndAlso txtNotes.Text = "" _
            AndAlso Not chkIsImportedCase.Checked Then

            lblError.Visible = True
            grdResults.Visible = False
            ResultsPager.Visible = False
            hlbExcel.Visible = False
            Exit Sub
        End If

        If Not IsDateRangeValid(ctlFormADateFrom, ctlFormADateTo, "Form A Date") Then
            grdResults.Visible = False
            ResultsPager.Visible = False
            hlbExcel.Visible = False
            Exit Sub
        End If

        If Not IsDateRangeValid(ctlFinalResultDateFrom, ctlFinalResultDateTo, "Final Result Date") Then
            grdResults.Visible = False
            ResultsPager.Visible = False
            hlbExcel.Visible = False
            Exit Sub
        End If

        If Not IsDateRangeValid(ctlBirthDateFrom, ctlBirthDateTo, "Birth Date") Then
            grdResults.Visible = False
            ResultsPager.Visible = False
            hlbExcel.Visible = False
            Exit Sub
        End If

        Dim objSearch As BSELib.clsSearch
        Dim dtResultsData As DataTable
        Dim dvResultsData As DataView

        Try
            objSearch.GetCaseSearchResults(ctlRBSE.UnformattedRBSE, _
                                                    txtEartag.Text, _
                                                    Replace(txtDBSE.Text, "/", ""), _
                                                    GetSelectedItemFromDropDownList(ddlFate), _
                                                    GetSelectedItemFromDropDownList(ddlFinalResult), _
                                                    GetSelectedItemFromDropDownList(ddlSex), _
                                                    GetSelectedItemFromDropDownList(ddlSurvey), _
                                                    txtNotes.Text, _
                                                    ctlFormADateFrom.DateField, _
                                                    ctlFormADateTo.DateField, _
                                                    ctlFinalResultDateFrom.DateField, _
                                                    ctlFinalResultDateTo.DateField, _
                                                    ctlBirthDateFrom.DateField, _
                                                    ctlBirthDateTo.DateField, _
                                                    chkIncludeNonGBCases.Checked, _
                                                    GetSelectedItemFromDropDownList(ddlPassiveActive), _
                                                    chkIsImportedCase.Checked, _
                                                    dtResultsData)
            dtResultsData.TableName = "casesearchresults"
            dvResultsData = dtResultsData.DefaultView

            Session.Item(SessionVars.SV_ExcelExport) = dtResultsData
            Session.Item(SessionVars.SV_ExcelExportView) = dvResultsData

            With grdResults
                .Visible = True
                .DataSource = dtResultsData
                .DataKeyField = "RBSE"
                .CurrentPageIndex = 0
                .SelectedIndex = -1
                .EditItemIndex = -1
                .DataBind()
            End With

            With ResultsPager
                .Visible = True
                .DataTableSessionID = SessionVars.SV_ExcelExport
                .DataViewSessionID = SessionVars.SV_ExcelExportView
                .AllowAddNew = False
                .AllowDelete = False
                .AllowEdit = False
                .PageLinkCount = 10
                .Refresh()
            End With

            hlbExcel.Visible = True

        Catch ex As SqlClient.SqlException
            lblResultError.Visible = True
            lblResultError.Text = ex.Message
        End Try
    End Sub

    Private Function GetPassiveActiveTable() As DataTable
        Try
            Dim objDataTable As New DataTable("Passive/Active")
            Dim objDataRow As DataRow

            objDataTable.Columns.Add(New DataColumn("Code", GetType(String)))
            objDataTable.Columns.Add(New DataColumn("Description", GetType(String)))

            objDataRow = objDataTable.NewRow
            objDataRow("Code") = ""
            objDataRow("Description") = ""
            objDataTable.Rows.Add(objDataRow)

            objDataRow = objDataTable.NewRow
            objDataRow("Code") = "P"
            objDataRow("Description") = "Passive"
            objDataTable.Rows.Add(objDataRow)

            objDataRow = objDataTable.NewRow
            objDataRow("Code") = "A"
            objDataRow("Description") = "Active"
            objDataTable.Rows.Add(objDataRow)

            Return objDataTable

        Catch ex As Exception
            clsAppError.DisplayError("Failed to generate the Passive Active Table contents.", ex)
        End Try
    End Function

    Private Sub btnSearchMenu_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearchMenu.Click
        Response.Redirect("SearchMenu.aspx")
    End Sub
End Class
