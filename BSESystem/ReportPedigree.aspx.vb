Partial Class ReportPedigree
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

        If IsNothing(Session(SessionVars.SV_PrintBatchNumber)) Then
            Response.Redirect("SessionError.aspx")
        End If

        Headers()
        LoadDetails()
    End Sub

    Private Sub Headers()
        With Response
            .Clear()
            .Charset = ""
            .ContentType = "application/vnd.ms-word"
            .AddHeader("Content-Disposition", "attachment; filename=""PedigreeReport.doc""")
        End With
        Me.EnableViewState = False
    End Sub

    Private Function LoadDetails() As Boolean
        Try
            Dim sHoleBatchNumber As String
            Dim sBatchYear As String
            Dim sBatchNumber As String
            Dim dtData As DataTable
            Dim objBatch As New BSELib.clsBatch()
            Dim dtNow As DateTime

            sHoleBatchNumber = Session.Item(SessionVars.SV_PrintBatchNumber)

            SplitBatchNumber(sHoleBatchNumber, sBatchYear, sBatchNumber)

            If Not objBatch.GetDetailsForBatch(sBatchYear, sBatchNumber, dtData, "GetDamSireDetailsByBatchID") Then
                Throw New Exception("Batch.GetDetailsForBatch returned False")
            End If

            lblDateRun.Text = dtNow.Now
            lblBatchNumber.Text = sHoleBatchNumber
            If Not dtData Is Nothing Then
                lblNumberInBatch.Text = dtData.Rows.Count.ToString
            End If

            'Bind the Parent repeater to the cases related to the batch
            parentRepeater.DataSource = dtData
            parentRepeater.DataBind()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Batch Pedigree Details.", ex)
            Return False
        End Try
    End Function
End Class
