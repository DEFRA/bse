Public Partial Class AnnexD
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
                .AddHeader("Content-Disposition", "attachment; filename=""annexd" & sRBSE & ".doc""")
            End With
        End If
        Me.EnableViewState = False
    End Sub

    Private Function LoadCaseDetails() As Boolean
        Try
            Dim sRBSE As String = Session.Item(SessionVars.SV_RBSENumber)
            Dim objCase As New BSELib.clsCase()
            Dim dtData As DataTable
            Dim dtGridData As New DataTable

            If Not (objCase.GetMinuteDetails(sRBSE, "AnnexD", dtData)) Then
                Throw New Exception("Case.GetMinuteDetails returned False")
            End If

            If dtData.Rows.Count <> 0 Then
                With dtData.Rows(0)
                    lblDate.Text = FormatDate(.Item("AnnexDDate").ToString())
                    lblAnnexCDate.Text = FormatDate(.Item("AnnexCDate").ToString())
                    lblAHOName1.Text = .Item("AHOName").ToString()
                    lblAHOName2.Text = .Item("AHOName").ToString()
                End With
            Else
                Return False
            End If

            dtGridData.Columns.Add("RBSE", System.Type.GetType("System.String"))
            dtGridData.Columns.Add("FormType", System.Type.GetType("System.String"))

            If Session.Item(SessionVars.SV_HomebredFormOutstanding) = True Then
                Dim drRow As DataRow = dtGridData.NewRow()
                drRow(0) = sRBSE
                drRow(1) = "BSE 1 - Homebred"
                dtGridData.Rows.Add(drRow)
                dtGridData.AcceptChanges()
            End If
            If Session.Item(SessionVars.SV_BreederFormOutstanding) = True Then
                Dim drRow As DataRow = dtGridData.NewRow()
                drRow(0) = sRBSE
                drRow(1) = "BSE 1 - Breeder"
                dtGridData.Rows.Add(drRow)
                dtGridData.AcceptChanges()
            End If
            If Session.Item(SessionVars.SV_PurchaserFormOutstanding) = True Then
                Dim drRow As DataRow = dtGridData.NewRow()
                drRow(0) = sRBSE
                drRow(1) = "BSE 1 - Purchaser"
                dtGridData.Rows.Add(drRow)
                dtGridData.AcceptChanges()
            End If
            If Session.Item(SessionVars.SV_VendorFormOutstanding) = True Then
                Dim drRow As DataRow = dtGridData.NewRow()
                drRow(0) = sRBSE
                drRow(1) = "BSE 1 - Vendor"
                dtGridData.Rows.Add(drRow)
                dtGridData.AcceptChanges()
            End If
            If Session.Item(SessionVars.SV_SummarySheetOutstanding) = True Then
                Dim drRow As DataRow = dtGridData.NewRow()
                drRow(0) = sRBSE
                drRow(1) = "Summary Sheet"
                dtGridData.Rows.Add(drRow)
                dtGridData.AcceptChanges()
            End If
            If Session.Item(SessionVars.SV_AllPaperworkOutstanding) = True Then
                Dim drRow As DataRow = dtGridData.NewRow()
                drRow(0) = sRBSE
                drRow(1) = "All Paperwork"
                dtGridData.Rows.Add(drRow)
                dtGridData.AcceptChanges()
            End If

            rptRepeater.DataSource = dtGridData
            rptRepeater.DataBind()

            Session.Remove(SessionVars.SV_HomebredFormOutstanding)
            Session.Remove(SessionVars.SV_BreederFormOutstanding)
            Session.Remove(SessionVars.SV_PurchaserFormOutstanding)
            Session.Remove(SessionVars.SV_VendorFormOutstanding)
            Session.Remove(SessionVars.SV_SummarySheetOutstanding)
            Session.Remove(SessionVars.SV_AllPaperworkOutstanding)

            Return True

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Case Details.", ex)
            Return False
        End Try
    End Function

End Class