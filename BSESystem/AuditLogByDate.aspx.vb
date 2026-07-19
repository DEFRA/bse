Partial Class AuditLogByDate
    Inherits System.Web.UI.Page

    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ResultsPager As DataGridPager
    Protected WithEvents ctlLogDate As CalendarDate

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
        VLAHeader1.PageTitle = "Daily Audit Log Report"
        ctlLogDate.SetDefaultButton(btnSearch)
        ResultsPager.SetGrid(grdResults)
        If Not IsPostBack Then
            hlbExcel.Visible = False
            grdResults.Visible = False
            ResultsPager.Visible = False
        End If
    End Sub



    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click

        ctlLogDate.Mandatory = True

        Dim dDate As Date
        If Not ctlLogDate.Validate(dDate) Then
            grdResults.Visible = False
            ResultsPager.Visible = False
            hlbExcel.Visible = False
            Exit Sub
        End If

        Dim objAuditLog As BSELib.clsAuditLog
        Dim dtResultsData As DataTable
        Dim dvResultsData As DataView

        If objAuditLog.GetDailyAuditLogReport(dDate, dtResultsData) Then

            dtResultsData.TableName = "dailyauditlogresults"
            dvResultsData = dtResultsData.DefaultView

            Session.Item(SessionVars.SV_ExcelExport) = dtResultsData
            Session.Item(SessionVars.SV_ExcelExportView) = dvResultsData

            With grdResults
                .Visible = True
                .DataSource = dtResultsData
                .DataKeyField = "ID"
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
        End If

    End Sub
End Class
