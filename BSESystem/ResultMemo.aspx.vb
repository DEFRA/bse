Partial Class ResultMemo
    Inherits System.Web.UI.Page

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

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load, Me.Load

        If Request.QueryString("RBSE") = "" Then
            Response.Redirect("SessionError.aspx")
        End If

        Headers()
        If Not LoadCaseDetails() Then
            Throw New Exception("Unable to generate Memorandum")
        End If
    End Sub

    Private Sub Headers()
        Dim sRBSE As String = Request.QueryString.Get("RBSE")
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
                .AddHeader("Content-Disposition", "attachment; filename=""memo" & sRBSE & ".doc""")
            End With
        End If
        Me.EnableViewState = False
    End Sub


    Private Function LoadCaseDetails() As Boolean
        Try
            Dim sRBSE As String = Request.QueryString.Get("RBSE")
            Dim objCase As New BSELib.clsCase()
            Dim dtData As DataTable

            If Not (objCase.GetFinalResultByRBSE(sRBSE, dtData)) Then
                Throw New Exception("Case.GetFinalResultByRBSE returned False")
            End If

            If dtData.Rows.Count <> 0 Then
                With dtData.Rows(0)
                    lblTo2.Text = .Item("AHOName").ToString()
                    lblCaseRef.Text = "Case Ref: " & Right$(FormatRBSE(.Item("RBSE").ToString()), 8)
                    lblCPHH.Text = "CPHH: " & FormatCPHH(.Item("CPHH").ToString())
                    lblReportDate.Text = "Date of report: " & FormatDate(.Item("FinalResultDate").ToString())
                    lblAnimalIDValue.Text = .Item("Eartag").ToString()
                    If .Item("FinalResult").ToString() = "Pos" Then
                        lblConfirm.Text = "Disease is CONFIRMED on the premises"
                        lblDBSERef.Text = "DBSE Ref: " & FormatDBSE(.Item("DBSE").ToString())
                    Else
                        lblConfirm.Text = "Disease is NOT CONFIRMED on the premises"
                        lblDBSERef.Visible = False
                    End If
                    lblAHO.Text = "AHO: " & .Item("AHOCodeName").ToString()
                    lblName.Text = .Item("OwnerName").ToString()
                    lblAddress1Value.Text = .Item("Address1").ToString()
                    lblAddress2Value.Text = .Item("Address2").ToString()
                    lblAddress3Value.Text = .Item("Address3").ToString()
                    lblPostcodeValue.Text = .Item("Postcode").ToString()
                    lblParishValue.Text = .Item("Parish").ToString()
                    lblDistrictValue.Text = .Item("District").ToString()
                    lblCountyValue.Text = .Item("County").ToString()
                    If .Item("AlternateDiagnosis").ToString() = "" Then
                        lblAlternateResult.Visible = False
                        tblAlternateResultDetails.Visible = False
                    Else
                        lblAlternateResult.Visible = True
                        tblAlternateResultDetails.Visible = True
                        lblDateOfBirth.Text = FormatDate(.Item("BirthDate").ToString())
                        lblAlternateDiagnosis.Text = .Item("AlternateDiagnosis").ToString()
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

End Class
