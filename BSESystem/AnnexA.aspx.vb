Public Partial Class AnnexA
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Headers()
        If Not LoadCaseDetails() Then
            Throw New Exception("Unable to generate Memorandum")
        End If
    End Sub

    Private Sub Headers()
        Dim sRBSE As String = Session.Item(SessionVars.SV_RBSENumber)
        Dim sPrint As String = Request.QueryString.Get("Print")

        If sPrint = "Print" Then
            If Not Page.IsClientScriptBlockRegistered("PrintRedirect") Then

                If HttpContext.Current.Request.Browser.JavaScript Then
                    Dim sJScript As New System.Text.StringBuilder()

                    sJScript.Append("<script language=""JavaScript"">")
                    sJScript.Append("<!-- " + vbCrLf)
                    sJScript.Append("if (window.print)")
                    sJScript.Append("window.print();")
                    sJScript.Append("// -->")
                    sJScript.Append("</script>")

                    Page.RegisterClientScriptBlock("PrintRedirect", sJScript.ToString())
                End If
            End If
        Else
            With Response
                .Clear()
                .Charset = ""
                .ContentType = "application/vnd.ms-word"
                .AddHeader("Content-Disposition", "attachment; filename=""annexa" & sRBSE & ".doc""")
            End With
        End If
        Me.EnableViewState = False
    End Sub

    Private Function LoadCaseDetails() As Boolean
        Try
            Dim sRBSE As String = Session.Item(SessionVars.SV_RBSENumber)
            Dim objCase As New BSELib.clsCase()
            Dim dtData As DataTable

            If Not (objCase.GetMinuteDetails(sRBSE, "AnnexA", dtData)) Then
                Throw New Exception("Case.GetMinuteDetails returned False")
            End If

            If dtData.Rows.Count <> 0 Then
                With dtData.Rows(0)
                    lblRBSE.Text = FormatRBSE(.Item("RBSE").ToString())
                    lblDate.Text = FormatDate(.Item("AnnexADate").ToString())
                    lblAHOName.Text = .Item("AHOName").ToString()
                End With

                Return True
            End If
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Case Details.", ex)
            Return False
        End Try
    End Function

End Class