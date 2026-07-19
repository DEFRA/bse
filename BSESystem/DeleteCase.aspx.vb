Partial Class DeleteCase
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlRBSE As RBSE

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
        VLAHeader1.PageTitle = "Delete Case"
        ctlRBSE.SetDefaultButton(btnLookUp)
        EnableControls()
        btnOK.Attributes.Add("onClick", "javascript:return confirm('Click OK to delete case');")
    End Sub

#Region "Event Handlers"

    Private Sub btnLookUp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLookUp.Click
        Session.Abandon()
        If LoadCaseDetails() Then
            ctlRBSE.SetValidMark(True)
            If VLATablesEmpty() Then
                btnOK.Enabled = True
                ctlRBSE.SetValidMark(True)
            Else
                btnOK.Enabled = False
                'ctlRBSE.SetValidMark(False, "Cannot find a case with this RBSE")
            End If
        Else
            btnOK.Enabled = False
            ctlRBSE.SetValidMark(False, "Cannot find a case with this RBSE")
        End If
    End Sub

    Private Sub btnOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOK.Click
        Dim sRBSE As String = Replace(ctlRBSE.RBSE, "/", "")
        Dim objCase As New BSELib.clsCase()

        If Not objCase.DeleteCase(Session.Item(SessionVars.SV_HeaderUserID), sRBSE) Then
            clsAppError.DisplayError("Error deleting Case")
        End If

        sRBSE = FormatRBSE(sRBSE)

        Response.Redirect("MaintenanceConfirmation.aspx?title=Case Deleted&message=The Case " & sRBSE & " was deleted")
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Response.Redirect("Home.aspx")
    End Sub

#End Region

#Region "Permissions"

    Private Sub EnableControls()
        VLAHeader1.GetUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "DEFRA Viewer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "DEFRA Data Entry" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "DEFRA Maintenance" Then
            ' Do Nothing
        ElseIf sGroupName = "VLA Data Entry" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "VLA Maintenance" Then
            ' Do Nothing
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

#End Region

#Region "Load Case Details"

    Private Function LoadCaseDetails() As Boolean
        Try
            Dim sRBSE As String = ctlRBSE.RBSE
            Dim objCase As New BSELib.clsCase()
            Dim iNumberOfCases As Int32

            Session(SessionVars.SV_RBSENumber) = sRBSE
            If Trim(sRBSE) <> "" Then
                GetCaseDetailsFromDatabase(sRBSE, Session)
                GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)

                If Not objCase.GetNumberOfCasesByCPHH(Session.Item(SessionVars.SV_CPHHNumber), iNumberOfCases) Then
                    clsAppError.DisplayError("GetNumberOfCasesByCPHH returned False")
                End If
                lblNumberOfCasesValue.Text = Convert.ToString(iNumberOfCases)
                If iNumberOfCases = 1 Then
                    lblFarmMessage.Visible = True
                End If

                If LoadDetailsFromSession() Then Return True
            End If
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Case Details.", ex)
            Return False
        End Try
    End Function

    Private Function LoadDetailsFromSession() As Boolean
        Dim dsCaseData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        Dim dsFarmData As DataSet = Session.Item(SessionVars.SV_FarmDetails)

        If Not (dsCaseData Is Nothing) Then
            If dsCaseData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then
                With dsCaseData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                    lblCPHHValue.Text = FormatCPHH(.Item("CPHH").ToString())
                End With
            End If

            If Not (dsFarmData Is Nothing) Then
                If dsFarmData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Count <> 0 Then
                    With dsFarmData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                        lblOwnerNameValue.Text = .Item("OwnerName").ToString()
                        lblAddress1Value.Text = .Item("Address1").ToString()
                        lblAddress2Value.Text = .Item("Address2").ToString()
                        lblAddress3Value.Text = .Item("Address3").ToString()
                        lblPostcodeValue.Text = .Item("Postcode").ToString()
                    End With

                    Return True
                End If
            End If
        End If
        lblCPHHValue.Text = ""
        lblOwnerNameValue.Text = ""
        lblAddress1Value.Text = ""
        lblAddress2Value.Text = ""
        lblAddress3Value.Text = ""
        lblPostcodeValue.Text = ""

        Return False
    End Function

    Private Function VLATablesEmpty() As Boolean
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        lblVLAMessage.Visible = False

        If dsData.Tables(BSELib.clsCase.FEED_TABLE).Rows.Count <> 0 Then
            lblVLAMessage.Visible = True
            Return False
        End If
        If dsData.Tables(BSELib.clsCase.CLINICAL_TABLE).Rows.Count <> 0 Then
            lblVLAMessage.Visible = True
            Return False
        End If
        If dsData.Tables(BSELib.clsCase.RELATION_TABLE).Rows.Count <> 0 Then
            lblVLAMessage.Visible = True
            Return False
        End If
        If dsData.Tables(BSELib.clsCase.OTHER_OWNER_TABLE).Rows.Count <> 0 Then
            lblVLAMessage.Visible = True
            Return False
        End If
        If dsData.Tables(BSELib.clsCase.CLINICAL_VISIT_TABLE).Rows.Count <> 0 Then
            lblVLAMessage.Visible = True
            Return False
        End If

        Dim sRBSE As String = Replace(ctlRBSE.RBSE, "/", "")

        GetBatchNumbersFromDatabase(sRBSE, Session)

        Dim dtBatchData As DataTable = Session.Item(SessionVars.SV_BatchNumbersTable)
        If Not dtBatchData Is Nothing Then
            If dtBatchData.Rows.Count <> 0 Then
                lblVLAMessage.Visible = True
                Return False
            End If
        End If
        Return True
    End Function
#End Region

End Class
