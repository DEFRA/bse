Partial Class BSESSCheckByDate

    Inherits System.Web.UI.Page

    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ResultsPager As DataGridPager
    Protected WithEvents ctlStartDate As CalendarDate
    Protected WithEvents ctlEndDate As CalendarDate

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
        VLAHeader1.PageTitle = "BSESS Check : Cases Within A Date Range"
        SetEnterKeys()
        ResultsPager.SetGrid(grdResults)
        ctlStartDate.Mandatory = True
        ctlEndDate.Mandatory = True

        If Not IsPostBack Then
            grdResults.Visible = False
            ResultsPager.Visible = False
            hlbExcel.Visible = False
        End If
        EnableControls()
    End Sub

    Private Sub SetEnterKeys()

        ctlStartDate.SetControlOnEnter(ctlEndDate.FirstClientID)
        ctlEndDate.SetControlOnEnter(btnSearch.ClientID)

    End Sub

    Private Sub EnableControls()

        VLAHeader1.GetUserDetails()
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)


        If sGroupName <> "DEFRA Viewer" And sGroupName <> "DEFRA Data Entry" And sGroupName <> "DEFRA Maintenance" And sGroupName <> "VLA Data Entry" And sGroupName <> "VLA Maintenance" Then
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click

        If Not IsDateRangeValid(ctlStartDate, ctlEndDate, "notification date") Then
            grdResults.Visible = False
            ResultsPager.Visible = False
            hlbExcel.Visible = False
            Exit Sub
        End If

        Dim objBSESS As BSELib.clsBSESS
        Dim dtResultsData As DataTable
        Dim dvResultsData As DataView

        If objBSESS.GetBSESSCheckByDate(CDate(ctlStartDate.DateField), _
                                                CDate(ctlEndDate.DateField), _
                                                dtResultsData) Then

            dtResultsData.TableName = "bsesscheckbydateresults"
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
        End If

    End Sub

End Class
