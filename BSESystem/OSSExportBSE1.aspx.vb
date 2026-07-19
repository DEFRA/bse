Partial Class OSSExportBSE1
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlBatchNumber As BatchNumber

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
        VLAHeader1.PageTitle = "OSS Export : BSE1"
        ctlBatchNumber.SetDefaultButton(btnDownload)
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

#Region "Event Handlers"

    Private Sub btnDownload_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDownload.Click
        Dim iBatchID As Integer
        Dim avExportData(2) As Object

        If ctlBatchNumber.Validate(iBatchID) Then
            If Trim(ctlBatchNumber.BatchNumber) <> "" And Len(ctlBatchNumber.BatchNumber) >= 5 Then
                avExportData(0) = iBatchID
                avExportData(1) = Left(ctlBatchNumber.BatchNumber, 4)
                avExportData(2) = ctlBatchNumber.BatchNumber
                avExportData(2) = Right(avExportData(2), (Len(avExportData(2)) - InStr(avExportData(2), "/")))

                Session.Item(SessionVars.SV_OSSExportDataBSE1) = avExportData

                If Len(ctlBatchNumber.BatchNumber) > 8 Then
                    lblTruncateError.Visible = True
                Else
                    lblTruncateError.Visible = False
                End If

                OpenDownloadPopup()
            End If
        End If
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

    Private Sub btnHome_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnHome.Click
        Response.Redirect("OSSExportMenu.aspx")
    End Sub

#End Region

End Class
