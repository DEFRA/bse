Partial Class PrintBatch
    Inherits System.Web.UI.Page
    Protected WithEvents BatchNumber1 As BatchNumber
    Protected WithEvents VLAHeader1 As VLAHeader

    Private Enum ReportType As Integer
        eClinical = 1
        eFarmAndCase = 2
        eFeeds = 3
        eOffspring = 4
        ePedigree = 5
    End Enum
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
        VLAHeader1.PageTitle = "Print Batch"
        BatchNumber1.SetControlOnEnter(ddlReportType.ClientID)
        If Not IsPostBack Then

            If CStr(Session(SessionVars.SV_BatchNumber)) <> "" Then
                BatchNumber1.BatchNumber = CStr(Session(SessionVars.SV_BatchNumber))
                VLAHeader1.BatchNumber = BatchNumber1.BatchNumber
            End If
            LoadList()
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

#Region "EventHandlers"
    Private Sub btnDownload_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDownload.Click
        Dim sBatchNumber As String

        sBatchNumber = BatchNumber1.BatchNumber

        Dim iBatchID As Integer
        If Not BatchNumber1.Validate(iBatchID) Then
            Exit Sub
        End If

        If Not BatchNumber1.ValidateBlank() Then
            Exit Sub
        End If

        If sBatchNumber <> "" Then
            Session(SessionVars.SV_PrintBatchNumber) = sBatchNumber
        End If

        'Produce the necessary report based on the user choice from the report type pick list
        Select Case ddlReportType.SelectedItem.Value
            Case ReportType.eClinical
                'Response.Redirect("ReportClinical.aspx", False)
                OpenDownloadPopup("ReportClinical.aspx")
            Case ReportType.eFarmAndCase
                'Response.Redirect("ReportCaseFarm.aspx")
                OpenDownloadPopup("ReportCaseFarm.aspx")
            Case ReportType.eFeeds
                'Response.Redirect("ReportFeeds.aspx")
                OpenDownloadPopup("ReportFeeds.aspx")
            Case ReportType.eOffspring
                'Response.Redirect("ReportOffspring.aspx")
                OpenDownloadPopup("ReportOffspring.aspx")
            Case ReportType.ePedigree
                'Response.Redirect("ReportPedigree.aspx")
                OpenDownloadPopup("ReportPedigree.aspx")
        End Select
    End Sub

#End Region

#Region "Private Functions"
    Private Sub LoadList()

        AddItemToEndOfDropDownList(ddlReportType, "", "0")
        AddItemToEndOfDropDownList(ddlReportType, "Clinical", "1")
        AddItemToEndOfDropDownList(ddlReportType, "Farm and Case", "2")
        AddItemToEndOfDropDownList(ddlReportType, "Feeds", "3")
        AddItemToEndOfDropDownList(ddlReportType, "Offspring", "4")
        AddItemToEndOfDropDownList(ddlReportType, "Pedigree", "5")

    End Sub

    Private Sub OpenDownloadPopup(ByVal sReport As String)
        If Not Page.IsClientScriptBlockRegistered("ReportRedirect") Then

            If HttpContext.Current.Request.Browser.JavaScript Then
                Dim sJScript As New System.Text.StringBuilder()

                sJScript.Append("<script language=""JavaScript"">")
                sJScript.Append("<!-- " + vbCrLf)
                sJScript.Append("window.open (""Redirect.aspx?page=" & sReport)
                sJScript.Append(""",""relationspopupwindow"",""top=100,left=100,width=100,height=50,buttons=no,scrollbars=yes,location=no,menubar=no,resizable=yes,status=no,directories=no,toolbar=no"");" + vbCrLf)
                sJScript.Append("// -->")
                sJScript.Append("</script>")

                Page.RegisterClientScriptBlock("ReportRedirect", sJScript.ToString())
            End If
        End If

        'If Not Page.IsClientScriptBlockRegistered("ReportRedirect") Then

        '    If HttpContext.Current.Request.Browser.JavaScript Then
        '        Dim sJScript As New System.Text.StringBuilder()

        '        sJScript.Append("<script language=""JavaScript"">")
        '        sJScript.Append("<!-- " + vbCrLf)
        '        sJScript.Append("var w=window.open (""" & sReport & "")
        '        sJScript.Append(""",""reportspopupwindow"",""top=0,left=0,width=630,height=500,buttons=no,scrollbars=yes,location=no,menubar=no,resizable=yes,status=no,directories=no,toolbar=no"");" + vbCrLf)
        '        sJScript.Append("// -->")
        '        sJScript.Append("</script>")

        '        Page.RegisterClientScriptBlock("ReportRedirect", sJScript.ToString())
        '    End If
        'End If
    End Sub
#End Region

End Class
