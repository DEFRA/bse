Partial Class FinalResultConfirmation
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader

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

        VLAHeader1.PageTitle = "Final Result Confirmation"
        If Not IsPostBack Then
            LoadCaseDetails()
        End If
        EnableControls()
    End Sub

#Region "Event Handlers"

    Private Sub btnPrintMemo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPrintMemo.Click
        If Not Page.IsClientScriptBlockRegistered("ReportRedirect") Then

            If HttpContext.Current.Request.Browser.JavaScript Then
                Dim sJScript As New System.Text.StringBuilder()
                Dim sRBSE As String = Replace(Session.Item(SessionVars.SV_RBSENumber), "/", "")

                sJScript.Append("<script language=""JavaScript"">")
                sJScript.Append("<!-- " + vbCrLf)
                sJScript.Append("window.open (""ResultMemo.aspx?RBSE=" & sRBSE & "&Print=Print"");") '7
                sJScript.Append("// -->")
                sJScript.Append("</script>")

                Page.RegisterClientScriptBlock("ReportRedirect", sJScript.ToString())
            End If
        End If
    End Sub

    Private Sub btnFinalResult_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnFinalResult.Click
        Response.Redirect("FinalResultEntry.aspx")
    End Sub

    Private Sub btnHome_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnHome.Click
        Response.Redirect("Home.aspx")
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
            ' Do Nothing
        ElseIf sGroupName = "VLA Data Entry" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "VLA Maintenance" Then
            ' Do Nothing
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

#End Region

#Region "Handle Data"

    Private Function LoadCaseDetails() As Boolean
        Try
            Dim sRBSE As String = Session.Item(SessionVars.SV_RBSENumber)
            Dim objCase As New BSELib.clsCase()
            Dim dtData As DataTable

            If Not (objCase.GetFinalResultByRBSE(sRBSE, dtData)) Then
                Throw New Exception("Case.GetFinalResultByRBSE returned False")
            End If

            Session.Item(SessionVars.SV_FinalResultTable) = dtData

            If dtData.Rows.Count <> 0 Then
                With dtData.Rows(0)

                    lblRBSE.Text = "RBSE: " & FormatRBSE(.Item("RBSE").ToString())
                    If IsDBNull(.Item("FinalResult")) Then
                        lblDBSE.Text = "DBSE: "
                        hlDownload.Enabled = False
                        hlDownload.ImageUrl = "images\btndownloaddisabled.gif"
                    Else
                        lblDBSE.Text = "DBSE: " & FormatDBSE(.Item("DBSE").ToString())
                        hlDownload.Enabled = True
                        hlDownload.ImageUrl = "images\btndownload.gif"
                        hlDownload.NavigateUrl = "ResultMemo.aspx?RBSE=" & sRBSE
                    End If
                End With

                Return True
            End If
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Case Details.", ex)
            Return False
        End Try
    End Function

#End Region

End Class
