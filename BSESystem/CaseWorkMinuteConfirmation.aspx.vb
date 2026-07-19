Public Partial Class CaseWorkMinuteConfirmation
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Me.Load

        If IsNothing(Session(SessionVars.SV_RBSENumber)) Then
            Response.Redirect("SessionError.aspx")
        End If

        VLAHeader1.PageTitle = "Casework Minute Confirmation"

            If Not IsPostBack Then
            LoadCaseDetails()
            End If
        EnableControls()

    End Sub

    Private Sub EnableControls()
        If Request.QueryString("minute") = "AnnexC" Or Request.QueryString("minute") = "AnnexD" Then
            lblOutstandingForms.Visible = True
            chkHomebred.Visible = True
            chkBreeder.Visible = True
            chkPurchaser.Visible = True
            chkVendor.Visible = True
            chkSummarySheet.Visible = True
            chkAllPaperwork.Visible = True
        End If
    End Sub

#Region "Event Handlers"

    Private Sub btnPrintMemo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnPrintMemo.Click
        If Not Page.IsClientScriptBlockRegistered("ReportRedirect") Then

            If HttpContext.Current.Request.Browser.JavaScript Then
                Dim sJScript As New System.Text.StringBuilder()
                Dim sRBSE As String = Replace(Session.Item(SessionVars.SV_RBSENumber), "/", "")
                Dim sRedirectURL As String

                Select Case Request.QueryString("minute")
                    Case "ActiveMemo"
                        sRedirectURL = "ActiveMemo.aspx"
                    Case "ActiveMemoFS"
                        sRedirectURL = "ActiveMemoFallenStock.aspx"
                    Case "AnnexA"
                        sRedirectURL = "AnnexA.aspx"
                    Case "AnnexB"
                        sRedirectURL = "AnnexB.aspx"
                    Case "AnnexC"
                        If IsNoOutstandingPaperworkSelected() Then
                            lblError.Visible = True
                            Exit Sub
                        End If
                        UpdateSession()
                        sRedirectURL = "AnnexC.aspx"
                    Case "AnnexD"
                        If IsNoOutstandingPaperworkSelected() Then
                            lblError.Visible = True
                            Exit Sub
                        End If
                        UpdateSession()
                        sRedirectURL = "AnnexD.aspx"
                End Select

                sJScript.Append("<script language=""JavaScript"">")
                sJScript.Append("<!-- " + vbCrLf)
                sJScript.Append("window.open (""" & sRedirectURL & "?Print=Print"");") '7
                sJScript.Append("// -->")
                sJScript.Append("</script>")

                Page.RegisterClientScriptBlock("ReportRedirect", sJScript.ToString())
            End If
        End If
    End Sub

    Protected Sub btnDownload_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDownload.Click
        Select Case Request.QueryString("minute")
            Case "ActiveMemo"
                Response.Redirect("ActiveMemo.aspx")
            Case "ActiveMemoFS"
                Response.Redirect("ActiveMemoFallenStock.aspx")
            Case "AnnexA"
                Response.Redirect("AnnexA.aspx")
            Case "AnnexB"
                Response.Redirect("AnnexB.aspx")
            Case "AnnexC"
                If IsNoOutstandingPaperworkSelected() Then
                    lblError.Visible = True
                Else
                    UpdateSession()
                    Response.Redirect("AnnexC.aspx")
                End If
            Case "AnnexD"
                If IsNoOutstandingPaperworkSelected() Then
                    lblError.Visible = True
                Else
                    UpdateSession()
                    Response.Redirect("AnnexD.aspx")
                End If
        End Select
    End Sub

    Private Sub btnHome_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnHome.Click
        Response.Redirect("Home.aspx")
    End Sub

    Protected Sub btnBack_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBack.Click
        Response.Redirect("CaseWorkEntry.aspx")
    End Sub

#End Region

#Region "Handle Data"

    Private Function LoadCaseDetails() As Boolean
        Try
            Dim sRBSE As String = Session.Item(SessionVars.SV_RBSENumber)
            Dim objCase As New BSELib.clsCase()
            Dim dtData As DataTable

            If Not (objCase.GetCaseWorkEntryByRBSE(sRBSE, dtData)) Then
                Throw New Exception("Case.GetCaseWorkEntryByRBSE returned False")
            End If

            lblRBSE.Text = "RBSE: " & FormatRBSE(sRBSE)

            If dtData.Rows.Count <> 0 Then

                Select Case Request.QueryString("minute")
                    Case "ActiveMemo"
                        lblMinute.Text = "Minute: Active Memo"
                        lblDate.Text = "Date: " & FormatDate(dtData.Rows(0).Item("ActiveMemoDate"))
                    Case "ActiveMemoFS"
                        lblMinute.Text = "Minute: Active Memo"
                        lblDate.Text = "Date: " & FormatDate(dtData.Rows(0).Item("ActiveMemoDate"))
                    Case "AnnexA"
                        lblMinute.Text = "Minute: Annex A"
                        lblDate.Text = "Date: " & FormatDate(dtData.Rows(0).Item("AnnexADate"))
                    Case "AnnexB"
                        lblMinute.Text = "Minute: Annex B"
                        lblDate.Text = "Date: " & FormatDate(dtData.Rows(0).Item("AnnexBDate"))
                    Case "AnnexC"
                        lblMinute.Text = "Minute: Annex C"
                        lblDate.Text = "Date: " & FormatDate(dtData.Rows(0).Item("AnnexCDate"))
                    Case "AnnexD"
                        lblMinute.Text = "Minute: Annex D"
                        lblDate.Text = "Date: " & FormatDate(dtData.Rows(0).Item("AnnexDDate"))
                End Select
            End If
            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Case Details.", ex)
            Return False
        End Try
    End Function

    Private Function IsNoOutstandingPaperworkSelected() As Boolean
        If Not chkHomebred.Checked And _
           Not chkBreeder.Checked And _
           Not chkPurchaser.Checked And _
           Not chkVendor.Checked And _
           Not chkSummarySheet.Checked And _
           Not chkAllPaperwork.Checked Then

            Return True
        Else
            Return False
        End If

    End Function

    Private Sub UpdateSession()
        Session.Item(SessionVars.SV_HomebredFormOutstanding) = chkHomebred.Checked
        Session.Item(SessionVars.SV_BreederFormOutstanding) = chkBreeder.Checked
        Session.Item(SessionVars.SV_PurchaserFormOutstanding) = chkPurchaser.Checked
        Session.Item(SessionVars.SV_VendorFormOutstanding) = chkVendor.Checked
        Session.Item(SessionVars.SV_SummarySheetOutstanding) = chkSummarySheet.Checked
        Session.Item(SessionVars.SV_AllPaperworkOutstanding) = chkAllPaperwork.Checked
    End Sub

#End Region

    Protected Sub chkHomebred_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkHomebred.CheckedChanged
        If chkHomebred.Checked = True Then lblError.Visible = False
    End Sub

    Protected Sub chkBreeder_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkBreeder.CheckedChanged
        If chkBreeder.Checked = True Then lblError.Visible = False
    End Sub

    Protected Sub chkPurchaser_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkPurchaser.CheckedChanged
        If chkPurchaser.Checked = True Then lblError.Visible = False
    End Sub

    Protected Sub chkVendor_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkVendor.CheckedChanged
        If chkVendor.Checked = True Then lblError.Visible = False
    End Sub

    Protected Sub chkSummarySheet_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkSummarySheet.CheckedChanged
        If chkSummarySheet.Checked = True Then lblError.Visible = False
    End Sub

    Protected Sub chkAllPaperwork_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkAllPaperwork.CheckedChanged
        If chkAllPaperwork.Checked = True Then lblError.Visible = False
    End Sub
End Class