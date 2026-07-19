Public Partial Class ActiveMemo
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
                .AddHeader("Content-Disposition", "attachment; filename=""activememo" & sRBSE & ".doc""")
            End With
        End If
        Me.EnableViewState = False
    End Sub

    Private Function LoadCaseDetails() As Boolean
        Try
            Dim sRBSE As String = Session.Item(SessionVars.SV_RBSENumber)
            Dim objCase As New BSELib.clsCase()
            Dim dtData As DataTable

            If Not (objCase.GetMinuteDetails(sRBSE, "ActiveMemo", dtData)) Then
                Throw New Exception("Case.GetMinuteDetails returned False")
            End If

            If dtData.Rows.Count <> 0 Then
                With dtData.Rows(0)
                    lblRBSE.Text = "RBSE: " & FormatRBSE(.Item("RBSE").ToString())
                    lblAHONumber.Text = "AHO: " & .Item("AHONumber").ToString()
                    lblCPHH.Text = "CPHH: " & FormatCPHH(.Item("CPHH").ToString())
                    lblDate.Text = "Date of Report: " & FormatDate(.Item("ActiveMemoDate").ToString())
                    lblCaseSurvey.Text = .Item("SurveyDescription").ToString()
                    lblFarmOwner.Text = .Item("OwnerName").ToString()
                    lblEartag.Text = BuildEartag(.Item("EartagCountry").ToString(), .Item("EartagHerdmark").ToString(), .Item("Eartag").ToString())
                    lblBirthDate.Text = FormatDate(.Item("BirthDate").ToString())
                    lblCaseSlaughterDate.Text = FormatDate(.Item("SlaughterDate").ToString())
                End With

                Return True
            End If
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Case Details.", ex)
            Return False
        End Try
    End Function

    Private Function BuildEartag(ByVal country As String, ByVal herdmark As String, ByVal eartag As String) As String

        Dim result As String = String.Empty

        If country.Length > 0 Then
            result = country
        End If

        If herdmark.Length > 0 Then
            If result.Length > 0 Then
                result &= " "
            End If
            result &= herdmark
        End If

        If eartag.Length > 0 Then
            If result.Length > 0 Then
                result &= " "
            End If
            result &= eartag
        End If

        Return result

    End Function

    Protected Overrides Sub Render(ByVal writer As HtmlTextWriter)

        Dim pageContent As String = String.Empty

        ' get the HTML of the page and put it in the pageContent variable
        Using mem As New System.IO.MemoryStream()
            Dim twr As New System.IO.StreamWriter(mem)
            Using myWriter As New HtmlTextWriter(twr)
                MyBase.Render(myWriter)

                myWriter.Flush()
            End Using

            Using strmRdr As New System.IO.StreamReader(mem)
                strmRdr.BaseStream.Position = 0
                pageContent = strmRdr.ReadToEnd()
            End Using
        End Using

        'strip out the viewstate
        Dim startVS As Integer = pageContent.IndexOf("<input type=""hidden""", 0)
        Dim endVS As Integer = pageContent.IndexOf(">", startVS + "<input type=""hidden""".Length)
        Dim subVS As String = pageContent.Substring(startVS, endVS - startVS + 1)
        pageContent = pageContent.Replace(subVS, String.Empty)

        'write out the page content minus the viewstate
        writer.Write(pageContent)

    End Sub
End Class