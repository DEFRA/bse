Partial Class RBSEChange
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlOldRBSE As RBSE
    Protected WithEvents ctlNewRBSE As RBSE

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
        VLAHeader1.PageTitle = "RBSE Change"
        SetEnterKeys()
        If Not IsPostBack Then
            ctlNewRBSE.SetEnabled(False)
        End If
        EnableControls()
        btnOK.Attributes.Add("onClick", "javascript:return confirm('Click OK to confirm change');")
    End Sub

    Private Sub SetEnterKeys()

        ctlOldRBSE.SetDefaultButton(btnLookUp)
        ctlNewRBSE.SetControlOnEnter(btnOK.ClientID)

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

#Region "Event Handlers"

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub btnLookUp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLookUp.Click
        If LoadCaseDetails() Then
            btnOK.Enabled = True
            ctlNewRBSE.SetEnabled(True)
            ctlOldRBSE.SetValidMark(True)
        Else
            btnOK.Enabled = False
            ctlNewRBSE.SetEnabled(False)
            ctlOldRBSE.SetValidMark(False, "A case with this RBSE was not found")
        End If
    End Sub

    Private Sub btnOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOK.Click
        Dim sOldRBSE As String = Replace(ctlOldRBSE.RBSE, "/", "")
        Dim sNewRBSE As String = Replace(ctlNewRBSE.RBSE, "/", "")
        Dim objCase As New BSELib.clsCase()

        If ctlOldRBSE.RBSE = ctlNewRBSE.RBSE Then
            ctlNewRBSE.SetValidMark(False, "The Old and New RBSEs are the same")
        Else
            ctlNewRBSE.SetValidMark(True)
            If CaseInDatabase(sNewRBSE) Then
                ctlNewRBSE.SetValidMark(False, "This Case already exists in the Database.")
            Else
                ctlNewRBSE.SetValidMark(True)
                If Not objCase.ChangeRBSE(Session.Item(SessionVars.SV_HeaderUserID), sOldRBSE, sNewRBSE) Then
                    clsAppError.DisplayError("Error changing the RBSE")
                End If

                sOldRBSE = FormatRBSE(sOldRBSE)
                sNewRBSE = FormatRBSE(sNewRBSE)

                Response.Redirect("MaintenanceConfirmation.aspx?title=RBSE Change Complete&message=The RBSE " & sOldRBSE & " was changed to " & sNewRBSE)
            End If
        End If
    End Sub

    Private Function CaseInDatabase(ByVal sRBSE As String) As Boolean
        Dim objCase As New BSELib.clsCase()
        Dim dsData As New DataSet()

        sRBSE = Replace(sRBSE, "/", "")
        If Not (objCase.GetCaseDetails(sRBSE, dsData)) Then
            Throw New Exception("Case.GetCaseDetails returned False")
        End If

        If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count = 0 Then
            Return False
        Else
            Return True
        End If
    End Function

#End Region

#Region "Load Case Details"

    Private Function LoadCaseDetails() As Boolean

        Dim dsData As DataSet

        Try
            Dim sRBSE As String = ctlOldRBSE.RBSE
            Session(SessionVars.SV_RBSENumber) = sRBSE
            Session.Remove(SessionVars.SV_CPHHNumber)
            Session.Remove(SessionVars.SV_CaseDetails)
            Session.Remove(SessionVars.SV_FarmDetails)
            If Trim(sRBSE) <> "" Then
                GetCaseDetailsFromDatabase(sRBSE, Session)
                GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)
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
                    lblEartagValue.Text = .Item("EartagHerdmark").ToString() & " " & .Item("Eartag").ToString()
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
            Else
                lblOwnerNameValue.Text = ""
                lblAddress1Value.Text = ""
                lblAddress2Value.Text = ""
                lblAddress3Value.Text = ""
                lblPostcodeValue.Text = ""
            End If
        End If
        Return False
    End Function
#End Region

End Class
