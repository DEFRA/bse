Partial Class ReportCaseFarm
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
            .AddHeader("Content-Disposition", "attachment; filename=""FarmCaseReport.doc""")
        End With
        Me.EnableViewState = False
    End Sub

    Private Function LoadDetails() As Boolean
        Try
            Dim sWholeBatchNumber As String
            Dim sBatchYear As String
            Dim sBatchNumber As String
            Dim dtHerd As DataTable
            Dim dtCase As DataTable
            Dim dtPreviousOwner As DataTable
            Dim dsData As New DataSet()
            Dim objBatch As New BSELib.clsBatch()
            Dim dtNow As DateTime

            sWholeBatchNumber = Session.Item(SessionVars.SV_PrintBatchNumber)

            SplitBatchNumber(sWholeBatchNumber, sBatchYear, sBatchNumber)

            If Not objBatch.GetDetailsForBatch(sBatchYear, sBatchNumber, dtCase, "GetCaseFarmByBatchID") Then
                Throw New Exception("Batch.GetDetailsForBatch returned False")
            End If

            If Not objBatch.GetDetailsForBatch(sBatchYear, sBatchNumber, dtHerd, "GetHerdDetailByBatchID") Then
                Throw New Exception("Batch.GetDetailsForBatch returned False")
            End If

            If Not objBatch.GetDetailsForBatch(sBatchYear, sBatchNumber, dtPreviousOwner, "GetPreviousOwnerByBatchID") Then
                Throw New Exception("Batch.GetDetailsForBatch returned False")
            End If

            'Add the datatables to a dataset
            dsData.Tables.Add(dtCase)
            dsData.Tables.Add(dtHerd)
            dsData.Tables.Add(dtPreviousOwner)

            'Create a relation between the cases related to this batch and the herd related to this
            'batch.
            'dsData table 0 = dtCase
            'dsData table 1 = dtHerd
            dsData.Relations.Add("FarmHerdRelation", _
                                  dsData.Tables(0).Columns("RBSE"), _
                                  dsData.Tables(1).Columns("RBSE"))
            dsData.Relations.Add("CasePreviousOwnerRelation", _
                                  dsData.Tables(0).Columns("RBSE"), _
                                  dsData.Tables(2).Columns("RBSE"))

            lblDateRun.Text = dtNow.Now
            lblBatchNumber.Text = sWholeBatchNumber
            If Not dtCase Is Nothing And Not dtHerd Is Nothing Then
                lblNumberInBatch.Text = dtCase.Rows.Count.ToString
            End If

            'Bind the Parent repeater to the cases related to the batch
            parentRepeater.DataSource = dsData.Tables(0)
            parentRepeater.DataBind()

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Case & Farm Details.", ex)
            Return False
        End Try
    End Function

End Class
