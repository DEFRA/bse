Partial Class MoveCase
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlRBSE As RBSE
    Protected WithEvents chkNonGBCase As System.Web.UI.WebControls.CheckBox
    Protected WithEvents ctlCPHH As CPHH

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
        VLAHeader1.PageTitle = "Move Case"
        SetEnterKeys()
        If Not IsPostBack Then
            ctlCPHH.Enabled = False
        End If
        EnableControls()
        btnOK.Attributes.Add("onClick", "javascript:return confirm('Click OK to confirm change');")
    End Sub

    Private Sub SetEnterKeys()

        ctlRBSE.SetDefaultButton(btnLookUp)
        ctlCPHH.SetDefaultButton(btnCheck)

    End Sub

#Region "Event Handlers"

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub btnLookUp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLookUp.Click
        If LoadCaseDetails() Then
            btnCheck.Enabled = True
            ctlCPHH.Enabled = True
            ctlRBSE.SetValidMark(True)
        Else
            btnCheck.Enabled = False
            ctlCPHH.Enabled = False
            ctlRBSE.SetValidMark(False, "A case with this RBSE was not found")
            lblOwnerNameValue.Text = ""
            lblAddress1Value.Text = ""
            lblAddress2Value.Text = ""
            lblAddress3Value.Text = ""
            lblPostcodeValue.Text = ""
            lblCPHHValue.Text = ""
            lblEartagValue.Text = ""
        End If
    End Sub

    Private Sub btnCheck_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCheck.Click

        If ctlCPHH.Validate() Then
            If LoadNewFarmDetails() Then
                btnOK.Enabled = True
                If Replace(ctlCPHH.CPHH, "/", "") = Replace(lblCPHHValue.Text, "/", "") Then
                    btnOK.Enabled = False
                    ctlCPHH.SetValidMark(False, "The CPHH you have entered is the same as the existing one.")
                Else
                    btnOK.Enabled = True
                    ctlCPHH.SetValidMark(True)
                End If
            Else
                Dim sNewCPHH As String = Replace(ctlCPHH.CPHH, "/", "")
                Dim sOldCPHH As String = Replace(lblCPHHValue.Text, "/", "")
                If sNewCPHH.Substring(0, 9) = sOldCPHH.Substring(0, 9) Then
                    btnCreateNewFarm.Visible = True
                End If
            End If
        Else
            btnOK.Enabled = False

            lblNewNameValue.Text = ""
            lblNewAddress1Value.Text = ""
            lblNewAddress2Value.Text = ""
            lblNewAddress3Value.Text = ""
            lblNewPostcodeValue.Text = ""
        End If
    End Sub

    Private Sub btnOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOK.Click
        Dim sRBSE As String = Replace(ctlRBSE.RBSE, "/", "")
        Dim sNewCPHH As String = ctlCPHH.CPHH
        Dim objCase As New BSELib.clsCase()

        If Not objCase.MoveCase(Session.Item(SessionVars.SV_HeaderUserID), sRBSE, sNewCPHH) Then
            clsAppError.DisplayError("Error moving Case")
        End If

        sRBSE = FormatRBSE(sRBSE)
        sNewCPHH = FormatCPHH(sNewCPHH)

        Response.Redirect("MaintenanceConfirmation.aspx?title=Case Move Complete&message=The Case " & sRBSE & " was moved to " & sNewCPHH)
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

#Region "Load New Farm Details"
    Private Function LoadNewFarmDetails() As Boolean

        Dim objFarm As New BSELib.clsFarm()
        Dim dsData As DataSet
        Dim dsOldCaseDetails As DataSet

        Try
            Dim sCPHH As String = ctlCPHH.CPHH
            If Trim(sCPHH) <> "" Then

                If Not (objFarm.GetFarmDetails(sCPHH, dsData)) Then
                    Throw New Exception("Case.GetFarmDetails returned False")
                End If

                If Not (dsData Is Nothing) Then
                    If dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Count <> 0 Then
                        With dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                            lblNewNameValue.Text = .Item("OwnerName").ToString()
                            lblNewAddress1Value.Text = .Item("Address1").ToString()
                            lblNewAddress2Value.Text = .Item("Address2").ToString()
                            lblNewAddress3Value.Text = .Item("Address3").ToString()
                            lblNewPostcodeValue.Text = .Item("Postcode").ToString()
                            dsOldCaseDetails = Session.Item(SessionVars.SV_CaseDetails)
                            If GetRowColumnData(.Item("IsNonGBFarm")) Then
                                If Not GetRowColumnData(dsOldCaseDetails.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("IsNonGBCase")) Then
                                    ctlCPHH.SetValidMark(False, "You have entered a GB Case and a Non-GB Farm")
                                    Return False
                                End If
                            Else
                                If GetRowColumnData(dsOldCaseDetails.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("IsNonGBCase")) Then
                                    ctlCPHH.SetValidMark(False, "You have entered a Non-GB Case and a GB Farm")
                                    Return False
                                End If
                            End If
                        End With
                        ctlCPHH.SetValidMark(True)
                        Return True
                    End If
                End If
            End If
            ctlCPHH.SetValidMark(False, "A farm with this CPHH was not found")
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load New Farm Details.", ex)
            Return False
        End Try
    End Function
#End Region

#Region "Load Case Details"

    Private Function LoadCaseDetails() As Boolean

        Dim dsData As DataSet
        Dim iNumberOfCases As Int32
        Dim objCase As New BSELib.clsCase()

        Try
            Dim sRBSE As String = ctlRBSE.RBSE
            Session(SessionVars.SV_RBSENumber) = sRBSE
            If Trim(sRBSE) <> "" Then
                Session.Remove(SessionVars.SV_CaseDetails)
                Session.Remove(SessionVars.SV_FarmDetails)

                GetCaseDetailsFromDatabase(sRBSE, Session)
                GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)

                If Not objCase.GetNumberOfCasesByCPHH(Session.Item(SessionVars.SV_CPHHNumber), iNumberOfCases) Then
                    clsAppError.DisplayError("GetNumberOfCasesByCPHH returned False")
                End If
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
            End If
        End If
        Return False
    End Function
#End Region

    Private Sub btnCreateNewFarm_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCreateNewFarm.Click
        Session(SessionVars.SV_NewCPHHNumber) = Replace(ctlCPHH.CPHH, "/", "")
        Response.Redirect("MoveCaseNewFarm.aspx")
    End Sub
End Class
