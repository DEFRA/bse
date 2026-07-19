Partial Class OSSExportBSE1b
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents BSE1bPager As DataGridPager
    Protected WithEvents ctlRBSE As RBSE

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
        VLAHeader1.PageTitle = "OSS Export : BSE1b"
        ctlRBSE.SetDefaultButton(btnAddToGrid)
        BSE1bPager.SetGrid(grdBSE1b)
        If Not IsPostBack Then
            PrepareGrid()
        End If
        EnableControls()
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
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "VLA Data Entry" Then
            ' Do Nothing
        ElseIf sGroupName = "VLA Maintenance" Then
            ' Do Nothing
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

#End Region

#Region "Prepare Grid"

    Private Sub PrepareGrid()
        Dim dtData As New DataTable()

        With dtData
            .Columns.Add("ID", System.Type.GetType("System.Int32"))
            .Columns("ID").AutoIncrement = True
            .Columns("ID").AutoIncrementSeed = 1
            .Columns("ID").AutoIncrementStep = 1

            Dim KeyCol As DataColumn = .Columns("ID")
            KeyCol.AutoIncrement = True
            KeyCol.AutoIncrementSeed = 1
            KeyCol.AutoIncrementStep = 1

            .PrimaryKey = New DataColumn() {KeyCol}

            .Columns.Add("RBSE", System.Type.GetType("System.String"))
            .Columns.Add("CPHH", System.Type.GetType("System.String"))
            .Columns.Add("OwnerName", System.Type.GetType("System.String"))
            .Columns.Add("Address1", System.Type.GetType("System.String"))
            .Columns.Add("BatchID", System.Type.GetType("System.Int32"))
        End With

        Session.Item(SessionVars.SV_OSSExportDataBSE1bTable) = dtData
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_OSSExportDataBSE1bView) = dvData

        grdBSE1b.DataSource = dtData
        grdBSE1b.DataKeyField = "ID"
        grdBSE1b.CurrentPageIndex = 0
        grdBSE1b.SelectedIndex = -1
        grdBSE1b.EditItemIndex = -1
        grdBSE1b.DataBind()

        BSE1bPager.DataTableSessionID = SessionVars.SV_OSSExportDataBSE1bTable
        BSE1bPager.DataViewSessionID = SessionVars.SV_OSSExportDataBSE1bView
        BSE1bPager.PageLinkCount = 10
        BSE1bPager.AllowAddNew = False
        BSE1bPager.AllowEdit = False
        BSE1bPager.AllowDelete = True
        BSE1bPager.Refresh()
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnAddToGrid_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddToGrid.Click
        Dim sRBSE As String
        Dim sCPHH As String
        Dim sOwnerName As String
        Dim sAddress1 As String
        Dim objOSSExport As New BSELib.clsOSSExport()

        If ctlRBSE.RBSE <> "" Then
            If ctlRBSE.Validate() Then
                sRBSE = Replace(ctlRBSE.RBSE, "/", "")
                If RBSEInTable(sRBSE) Then
                    ctlRBSE.SetValidMark(False, "The RBSE you entered is already in the table.")
                Else
                    ctlRBSE.SetValidMark(True)
                    If Not objOSSExport.GetOSSExportDetailsByRBSE(sRBSE, sCPHH, sOwnerName, sAddress1) Then
                        clsAppError.DisplayError("Failed to get Case Details")
                    End If

                    If sCPHH Is Nothing Then ' Only update the grid if we have data back from the database
                        ctlRBSE.SetValidMark(False, "The RBSE you entered was not in the database.")
                    Else
                        ctlRBSE.SetValidMark(True)
                        If lblBatchID.Text = "" Then
                            GenerateBatchNumber()
                        End If

                        Dim dtData As DataTable = Session.Item(SessionVars.SV_OSSExportDataBSE1bTable)
                        Dim drData As DataRow = dtData.NewRow()

                        drData("RBSE") = sRBSE
                        drData("CPHH") = sCPHH
                        drData("OwnerName") = sOwnerName
                        drData("Address1") = sAddress1
                        drData("BatchID") = CInt(lblBatchID.Text)

                        dtData.Rows.Add(drData)

                        Session.Item(SessionVars.SV_OSSExportDataBSE1bTable) = dtData

                        grdBSE1b.DataSource = dtData
                        grdBSE1b.DataBind()
                        BSE1bPager.Refresh()
                        btnExportToOSS.Enabled = True
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub btnExportToOSS_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnExportToOSS.Click
        btnAddToGrid.Enabled = False
        btnExportToOSS.Enabled = False
        CreateLinks()
        GenerateReport()
    End Sub

    Private Sub btnHome_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnHome.Click
        Response.Redirect("OSSExportMenu.aspx")
    End Sub

#End Region

#Region "Private Methods"

    Private Function RBSEInTable(ByVal sRBSE As String) As Boolean
        Dim dtData As DataTable = Session.Item(SessionVars.SV_OSSExportDataBSE1bTable)
        Dim iCount As Int32

        If Not dtData Is Nothing Then
            For iCount = 0 To dtData.Rows.Count - 1
                If Replace(sRBSE, "/", "") = Replace(dtData.Rows(iCount)("RBSE"), "/", "") Then
                    Return True
                End If
            Next 'iCount
        End If

        Return False
    End Function

    Private Sub GenerateBatchNumber()
        Dim objOSSExport As New BSELib.clsOSSExport()
        Dim iBatchID As Integer
        Dim iBatchYear As Integer
        Dim iBatchNumber As Integer

        If Not objOSSExport.CreateBSE1bBatch(iBatchID, iBatchYear, iBatchNumber) Then
            clsAppError.DisplayError("Failed to generate a new batch number")
        End If

        If iBatchNumber > 999 Then
            lblTruncatedError.Visible = True
        Else
            lblTruncatedError.Visible = False
        End If
        lblBatchNumberValue.Text = CStr(iBatchYear) & "/" & CStr(iBatchNumber)
        lblBatchID.Text = CStr(iBatchID)
    End Sub

    Private Sub GenerateReport()
        Dim avExportData(2) As Object
        Dim sBatchNumber As String = lblBatchNumberValue.Text

        avExportData(0) = CInt(lblBatchID.Text)
        avExportData(1) = Left(sBatchNumber, 4)
        avExportData(2) = Right(sBatchNumber, (Len(sBatchNumber) - InStr(sBatchNumber, "/")))

        Session.Item(SessionVars.SV_OSSExportDataBSE1) = avExportData

        OpenDownloadPopup()
    End Sub

    Private Sub OpenDownloadPopup()
        If Not Page.IsClientScriptBlockRegistered("ReportRedirect") Then

            If HttpContext.Current.Request.Browser.JavaScript Then
                Dim sJScript As New System.Text.StringBuilder()

                sJScript.Append("<script language=""JavaScript"">")
                sJScript.Append("<!-- " + vbCrLf)
                sJScript.Append("window.open (""Redirect.aspx?page=OSSExportBSE1Download.aspx")
                sJScript.Append(""",""relationspopupwindow"",""top=100,left=100,width=100,height=50,buttons=no,scrollbars=yes,location=no,menubar=no,resizable=yes,status=no,directories=no,toolbar=no"");" + vbCrLf)
                sJScript.Append("// -->")
                sJScript.Append("</script>")

                Page.RegisterClientScriptBlock("ReportRedirect", sJScript.ToString())
            End If
        End If
    End Sub

    Private Sub CreateLinks()
        Dim dtData As DataTable = Session.Item(SessionVars.SV_OSSExportDataBSE1bTable)
        Dim iCount As Integer
        Dim objBatch As New BSELib.clsBatch()
        Dim iBatchID As Integer = CInt(lblBatchID.Text)
        Dim sRBSE As String

        For iCount = 0 To dtData.Rows.Count - 1
            sRBSE = Replace(dtData.Rows(iCount)("RBSE").ToString(), "/", "")
            If Not objBatch.CreateBatchNumberLink(iBatchID, sRBSE, "BSE1b", Nothing, Nothing) Then
                clsAppError.DisplayError("Failed to create batch number link")
            End If
        Next
    End Sub

#End Region

End Class
