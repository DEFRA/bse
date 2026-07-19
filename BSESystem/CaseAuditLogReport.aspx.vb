Partial Class CaseAuditLogReport
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ResultsPager As DataGridPager

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

        If IsNothing(Session(SessionVars.SV_RBSENumber)) Then
            Response.Redirect("SessionError.aspx")
        End If

        VLAHeader1.PageTitle = "Case Audit Log Report"
        ResultsPager.SetGrid(grdResults)
        If Not IsPostBack Then
            GetReport()
        End If

    End Sub

    Private Sub GetReport()

        Dim objAuditLog As BSELib.clsAuditLog
        Dim dtResultsData As DataTable
        Dim dvResultsData As DataView

        Dim sRBSE As String = Session(SessionVars.SV_RBSENumber)
        lblRBSE.Text = "RBSE: " & sRBSE
        If objAuditLog.GetCaseAuditLogReport(Replace(sRBSE, "/", ""), dtResultsData) Then

            dtResultsData.TableName = "caseauditlog"
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

#Region "Permissions"

    Private Sub EnableControls()

        VLAHeader1.GetUserDetails()
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)


        If sGroupName <> "DEFRA Viewer" And sGroupName <> "DEFRA Data Entry" And sGroupName <> "DEFRA Maintenance" And sGroupName <> "VLA Data Entry" And sGroupName <> "VLA Maintenance" Then
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub
#End Region

    Private Sub btnReturn_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnReturn.Click
        Dim sPage As String

        sPage = Request.QueryString.Get("page") & ".aspx"
        Response.Redirect(sPage)
    End Sub

End Class
