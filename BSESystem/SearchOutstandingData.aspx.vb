Partial Class SearchOutstandingData
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ResultsPager As DataGridPager
    Protected WithEvents ctlFormADateFrom As CalendarDate
    Protected WithEvents ctlFormADateTo As CalendarDate

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
        VLAHeader1.PageTitle = "Outstanding Data Search"
        SetEnterKeys()
        ResultsPager.SetGrid(grdResults)
        If Not IsPostBack Then
            grdResults.Visible = False
            ResultsPager.Visible = False
        End If
    End Sub

    Private Sub SetEnterKeys()

        ctlFormADateFrom.SetControlOnEnter(ctlFormADateTo.FirstClientID)
        ctlFormADateTo.SetControlOnEnter(chkIncludeNonGBCases.ClientID)

    End Sub



    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click

        lblResultError.Visible = False

        If Not optOutstandingResults.Checked _
            AndAlso Not optFate.Checked _
            AndAlso Not optBSE1s.Checked Then

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

        Dim objSearch As BSELib.clsSearch
        Dim dtResultsData As DataTable
        Dim dvResultsData As DataView

        Dim iOutstandingDataType As BSELib.clsSearch.OutstandingDataType

        If optOutstandingResults.Checked Then
            iOutstandingDataType = BSELib.clsSearch.OutstandingDataType.OutstandingResults
        End If

        If optFate.Checked Then
            iOutstandingDataType = BSELib.clsSearch.OutstandingDataType.OutstandingFates
        End If

        If optBSE1s.Checked Then
            iOutstandingDataType = BSELib.clsSearch.OutstandingDataType.OutstandingBSE1s
        End If

        Try
            objSearch.GetOutstandingDataResults(iOutstandingDataType, _
                                                    ctlFormADateFrom.DateField, _
                                                    ctlFormADateTo.DateField, _
                                                    chkIncludeNonGBCases.Checked, _
                                                    dtResultsData)

            dtResultsData.TableName = "outstandingdatasearchresults"

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

    Private Sub btnSearchMenu_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearchMenu.Click
        Response.Redirect("SearchMenu.aspx")
    End Sub
End Class
