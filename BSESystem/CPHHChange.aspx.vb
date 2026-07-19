Partial Class CPHHChange
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents ctlOldCPHH As CPHH
    Protected WithEvents ctlNewCPHH As CPHH

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
        VLAHeader1.PageTitle = "CPHH Change"
        SetEnterKeys()
        If Not IsPostBack Then
            ctlNewCPHH.Enabled = False
        End If
        EnableControls()
        btnOK.Attributes.Add("onClick", "javascript:return confirm('Click OK to confirm change');")
    End Sub

    Private Sub SetEnterKeys()

        ctlOldCPHH.SetDefaultButton(btnLookUp)
        ctlNewCPHH.SetControlOnEnter(btnOK.ClientID)

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
        If LoadFarmDetails() Then
            btnOK.Enabled = True
            ctlNewCPHH.Enabled = True
            ctlOldCPHH.SetValidMark(True)
        Else
            btnOK.Enabled = False
            ctlNewCPHH.Enabled = False
            ctlOldCPHH.SetValidMark(False, "A farm with this CPHH was not found")
            lblAddress1Value.Text = ""
            lblAddress2Value.Text = ""
            lblAddress3Value.Text = ""
            lblPostcodeValue.Text = ""
            lblConfirmedCasesValue.Text = ""
            lblOwnerNameValue.Text = ""
        End If
    End Sub

    Private Sub btnOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOK.Click
        Dim sOldCPHH As String = ctlOldCPHH.CPHH
        Dim sNewCPHH As String = ctlNewCPHH.CPHH
        Dim objFarm As New BSELib.clsFarm()
        Dim dsData As DataSet = Nothing

        If sNewCPHH = "" Then
            ctlNewCPHH.SetValidMark(False, "You must enter a new CPHH")
        Else
            If sOldCPHH = sNewCPHH Then
                ctlNewCPHH.SetValidMark(False, "The Old and New CPHHs are the same")
            Else
                ctlNewCPHH.SetValidMark(True)

                If objFarm.FarmInDatabase(Replace(sNewCPHH, "/", "")) Then
                    ctlNewCPHH.SetValidMark(False, "This Farm already exists in the database.")
                Else
                    ctlNewCPHH.SetValidMark(True)
                    If Not objFarm.ChangeCPHH(Session.Item(SessionVars.SV_HeaderUserID), sOldCPHH, sNewCPHH) Then
                        clsAppError.DisplayError("Error updating the CPHH")
                    End If
                    sOldCPHH = FormatCPHH(sOldCPHH)
                    sNewCPHH = FormatCPHH(sNewCPHH)

                    Response.Redirect("MaintenanceConfirmation.aspx?title=CPHH Change Complete&message=The CPHH " & sOldCPHH & " was changed to " & sNewCPHH & ".  Location information for the farm, including County, AHO and ADNS Region, have not been changed.  Please check and amend them if necessary.")
                End If
            End If
        End If
    End Sub

#End Region

#Region "Load Case Details"

    Private Function LoadFarmDetails() As Boolean

        Dim dsData As DataSet

        Try
            Dim sCPHH As String = ctlOldCPHH.CPHH
            If Trim(sCPHH) <> "" Then
                Session.Item(SessionVars.SV_CPHHNumber) = sCPHH
                GetFarmDetailsFromDatabase(sCPHH, Session)

                dsData = Session.Item(SessionVars.SV_FarmDetails)
                If Not (dsData Is Nothing) Then
                    If dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Count <> 0 Then
                        With dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                            lblOwnerNameValue.Text = .Item("OwnerName").ToString()
                            lblAddress1Value.Text = .Item("Address1").ToString()
                            lblAddress2Value.Text = .Item("Address2").ToString()
                            lblAddress3Value.Text = .Item("Address3").ToString()
                            lblPostcodeValue.Text = .Item("Postcode").ToString()
                        End With

                        DisplayNumberOfConfirmedCases(sCPHH)
                        Return True
                    End If
                End If
            End If
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Farm Details.", ex)
            Return False
        End Try
    End Function

    Private Sub DisplayNumberOfConfirmedCases(ByVal sCPHH As String)
        Dim objFarm As New BSELib.clsFarm()
        Dim sConfirmedCases As String

        If Not objFarm.GetNumberOfConfirmedCases(sCPHH, sConfirmedCases) Then
            Throw New Exception("Farm.GetNumberOfConfirmedCases returned False")
        End If
        lblConfirmedCasesValue.Text = sConfirmedCases
    End Sub

#End Region

End Class
