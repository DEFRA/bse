Partial Class OSSExportBSE1Download
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

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        If IsNothing(Session(SessionVars.SV_OSSExportDataBSE1)) Then
            Response.Redirect("SessionError.aspx")
        End If

        Dim avExportData(2) As Object

        avExportData = Session.Item(SessionVars.SV_OSSExportDataBSE1)

        If avExportData Is Nothing Then
            Response.Redirect("Home.aspx")
        End If

        Headers(CStr(avExportData(1)), CStr(avExportData(2)))

        If Not LoadDetails(avExportData(0), CStr(avExportData(1)), CStr(avExportData(2))) Then
            clsAppError.DisplayError("Unable to generate Export")
        End If
    End Sub

#Region "Private Methods"

    Private Sub Headers(ByVal sBatchYear As String, ByVal sBatchNumber As String)
        With Response
            .Clear()
            .Charset = ""
            .ContentType = "text/plain"
            Dim sFilename As String = "EPBSE_" & Right(sBatchYear, 2) & "." & Right("000000" & sBatchNumber, 3)
            .AddHeader("Content-Disposition", "attachment; filename=" & sFilename) 'Hiya
        End With
        Me.EnableViewState = False
    End Sub

    Private Function LoadDetails(ByRef iBatchID As Integer, ByRef sBatchYear As String, ByRef sBatchNumber As String) As Boolean

        Try
            Dim objOSSExport As New BSELib.clsOSSExport()
            Dim dtData As DataTable
            Dim iCount As Int32
            Dim sString As String = Right(sBatchYear, 2) & Right("000000" & sBatchNumber, 3)

            If Not objOSSExport.GetCaseByBatchID(iBatchID, dtData) Then
                Throw New Exception("Failed to get Details")
            End If

            If Not dtData Is Nothing Then
                For iCount = 0 To dtData.Rows.Count - 1
                    Response.Write("|" & sString & "|")
                    Response.Write(dtData.Rows(iCount)("RBSE").ToString() & "|")
                    Response.Write(dtData.Rows(iCount)("CPHH").ToString() & "|" & vbCrLf)
                Next
            End If
            Return True

        Catch ex As Exception
            clsAppError.DisplayError("Failed to get Details", ex)
            Return False
        End Try
    End Function

#End Region

End Class
