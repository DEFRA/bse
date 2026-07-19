Partial Class SearchCPHH
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ResultsPager As DataGridPager
    Protected WithEvents ctlCPHH As CPHH

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
        VLAHeader1.PageTitle = "List Of Cases For A Given Holding/Herdmark"
        SetEnterKeys()
        ResultsPager.SetGrid(grdResults)
        ctlCPHH.SetEnableCPH(True)
        If Not IsPostBack Then
            grdResults.Visible = False
            ResultsPager.Visible = False
        End If
    End Sub

    Private Sub SetEnterKeys()

        ctlCPHH.SetControlOnEnter(txtHerdmark.ClientID)
        SetTextboxControlOnEnter(txtHerdmark, txtNumericHerdmark.ClientID)
        SetTextboxControlOnEnter(txtNumericHerdmark, chkIncludeNonGBCases.ClientID)

    End Sub

    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click

        lblResultError.Visible = False

        If ctlCPHH.CPHH = "" AndAlso txtHerdmark.Text = "" AndAlso txtNumericHerdmark.Text = "" Then
            lblError.Visible = True
            grdResults.Visible = False
            ResultsPager.Visible = False
            hlbExcel.Visible = False
            Exit Sub
        End If

        Dim objSearch As BSELib.clsSearch
        Dim dtResultsData As DataTable
        Dim dvResultsData As DataView

        Try
            objSearch.GetCaseByCPHHResults(ctlCPHH.CPHH, _
                                                    txtHerdmark.Text, _
                                                    txtNumericHerdmark.Text, _
                                                    chkIncludeNonGBCases.Checked, _
                                                    dtResultsData)

            dtResultsData.TableName = "casebycphhsearchresults"
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
