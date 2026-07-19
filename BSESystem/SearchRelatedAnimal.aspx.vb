Partial Class SearchRelatedAnimal
    Inherits System.Web.UI.Page
    Protected WithEvents ctlCaseRBSE As RBSE
    Protected WithEvents ctlRelationRBSE As RBSE
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
        VLAHeader1.PageTitle = "Related Animal Search"
        SetEnterKeys()
        ResultsPager.SetGrid(grdResults)
        If Not IsPostBack Then
            LoadLookupLists()
            grdResults.Visible = False
            ResultsPager.Visible = False
        End If
    End Sub

    Private Sub SetEnterKeys()

        ctlCaseRBSE.SetControlOnEnter(txtEartag.ClientID)
        SetTextboxControlOnEnter(txtEartag, ctlRelationRBSE.FirstClientID)
        ctlRelationRBSE.SetControlOnEnter(ddlRelationType.ClientID)

    End Sub
    Private Sub LoadLookupLists()

        Dim blnResult As Boolean
        Dim objRelationTypeTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objRelationTypeTable = objLookup.GetLookupData(LOOKUP_RELATION_TYPE)
            If Not (objRelationTypeTable Is Nothing) Then
                With ddlRelationType
                    .DataSource = objRelationTypeTable
                    .DataValueField = "Code"
                    .DataTextField = "Description"
                    .DataBind()
                End With
            End If

            AddItemToDropDownList(ddlRelationType, "Sire", "SIRE")
            AddItemToDropDownList(ddlRelationType, "Dam", "DAM")
            AddItemToDropDownList(ddlRelationType)

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Related Animal Search' drop down lists.", ex)
        End Try

    End Sub

    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click

        Dim objSearch As BSELib.clsSearch
        Dim dtResultsData As DataTable
        Dim dvResultsData As DataView

        lblResultError.Visible = False

        If ctlCaseRBSE.RBSE = "" AndAlso ctlRelationRBSE.RBSE = "" _
            AndAlso txtName.Text = "" _
            AndAlso txtEartag.Text = "" AndAlso GetSelectedItemFromDropDownList(ddlRelationType) = "" Then
            lblError.Visible = True
            grdResults.Visible = False
            ResultsPager.Visible = False
            hlbExcel.Visible = False
            Exit Sub
        End If

        Try
            objSearch.GetRelatedAnimalSearchResults(ctlCaseRBSE.UnformattedRBSE, _
                                                txtName.Text, _
                                                txtEartag.Text, _
                                                ctlRelationRBSE.UnformattedRBSE, _
                                                GetSelectedItemFromDropDownList(ddlRelationType), _
                                             dtResultsData)

            dtResultsData.TableName = "relatedanimalsearchresults"
            dvResultsData = dtResultsData.DefaultView

            Session.Item(SessionVars.SV_ExcelExport) = dtResultsData
            Session.Item(SessionVars.SV_ExcelExportView) = dvResultsData

            With grdResults
                .Visible = True
                .DataSource = dtResultsData
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
