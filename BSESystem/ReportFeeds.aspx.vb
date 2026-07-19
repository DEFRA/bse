Partial Class ReportFeeds
    Inherits System.Web.UI.Page
    Protected WithEvents lblDateEntered As System.Web.UI.WebControls.Label

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
            .AddHeader("Content-Disposition", "attachment; filename=""FeedsReport.doc""")
        End With
        Me.EnableViewState = False
    End Sub

    Private Function LoadDetails() As Boolean
        Try
            Dim sHoleBatchNumber As String
            Dim sBatchYear As String
            Dim sBatchNumber As String
            Dim dtFeeds As DataTable
            Dim dtCase As DataTable
            Dim dtData As New DataSet()
            Dim objBatch As New BSELib.clsBatch()
            Dim dtNow As DateTime

            sHoleBatchNumber = Session.Item(SessionVars.SV_PrintBatchNumber)

            SplitBatchNumber(sHoleBatchNumber, sBatchYear, sBatchNumber)

            If Not objBatch.GetDetailsForBatch(sBatchYear, sBatchNumber, dtFeeds, "GetFeedsByBatchID") Then
                Throw New Exception("Batch.GetDetailsForBatch returned False")
            End If

            If Not objBatch.GetDetailsForBatch(sBatchYear, sBatchNumber, dtCase, "GetCPHHRBSEForBatchID") Then
                Throw New Exception("Batch.GetDetailsForBatch returned False")
            End If

            'Add the datatables to a dataset
            dtData.Tables.Add(dtCase)
            dtData.Tables.Add(dtFeeds)

            'Create a relation between the cases related to this batch and the feeds related to this
            'batch.
            'dtData table 0 = dtCase
            'dtData table 1 = dtFeeds
            dtData.Relations.Add("CaseFeedsRelation", _
                                 dtData.Tables(0).Columns("RBSE"), _
                                 dtData.Tables(1).Columns("RBSE"))

            lblDateRun.Text = dtNow.Now
            lblBatchNumber.Text = sHoleBatchNumber
            If Not dtCase Is Nothing And Not dtFeeds Is Nothing Then
                lblNumberInBatch.Text = dtCase.Rows.Count.ToString
            End If

            'Bind the Parent repeater to the cases related to the batch
            'The Feeds related to the particular case are bound to the childrepeater, handled in
            'the aspx file.
            parentRepeater.DataSource = dtData.Tables(0)
            parentRepeater.DataBind()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Batch Feeds Details.", ex)
            Return False
        End Try
    End Function
End Class
