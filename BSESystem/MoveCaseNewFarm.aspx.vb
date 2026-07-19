Partial Class MoveCaseNewFarm
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents MapReference1 As MapReference

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
        If IsNothing(Session(SessionVars.SV_RBSENumber)) Then
            Response.Redirect("SessionError.aspx")
        End If

        VLAHeader1.PageTitle = "Move Case"
        SetEnterKeys()
        'btnOK.Attributes.Add("onClick", "javascript:return confirm('Click OK to confirm change');")
        If Not IsPostBack Then
            LoadLookupLists()
            PopulateControls()
        End If
        EnableControls()
    End Sub

    Private Sub SetEnterKeys()
        'ctlRBSE.SetDefaultButton(btnLookUp)
        'ctlCPHH.SetDefaultButton(btnCheck)
    End Sub

#Region "Populate Lookup Lists"

    Private Sub LoadLookupLists()

        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_AHO)
            If Not (objDataTable Is Nothing) Then
                ddlAHO.DataSource = objDataTable
                ddlAHO.DataValueField = "Code"
                ddlAHO.DataTextField = "Name"
                ddlAHO.DataBind()
                Common.AddItemToDropDownList(ddlAHO)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_BSE_COUNTY)
            If Not (objDataTable Is Nothing) Then
                ddlCounty.DataSource = objDataTable
                ddlCounty.DataValueField = "Code"
                ddlCounty.DataTextField = "Description"
                ddlCounty.DataBind()
                Common.AddItemToDropDownList(ddlCounty)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_AHO)
            If Not (objDataTable Is Nothing) Then
                ddlAHO.DataSource = objDataTable
                ddlAHO.DataValueField = "Code"
                ddlAHO.DataTextField = "Name"
                ddlAHO.DataBind()
                Common.AddItemToDropDownList(ddlAHO)
            End If

            objDataTable = objLookup.GetAuthorityCounties()
            If Not (objDataTable Is Nothing) Then
                ddlAuthorityCounty.DataSource = objDataTable
                ddlAuthorityCounty.DataValueField = "ID"
                ddlAuthorityCounty.DataTextField = "County"
                ddlAuthorityCounty.DataBind()
                Common.AddItemToDropDownList(ddlAuthorityCounty)
            End If

            Common.AddItemToDropDownList(ddlLocalAuthority)
            Common.AddItemToDropDownList(ddlADNSRegion)

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Farm Tab' drop down lists.", ex)
        End Try

    End Sub

    Private Function LoadAuthorityLookup(ByVal vsAuthorityCountyID As String) As String

        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()
        Dim sAuthorityID As String = ""

        Try
            ddlLocalAuthority.Items.Clear()

            If vsAuthorityCountyID <> "" Then
                objDataTable = objLookup.GetAuthorities(CInt(vsAuthorityCountyID))
                If Not (objDataTable Is Nothing) Then
                    ddlLocalAuthority.DataSource = objDataTable
                    ddlLocalAuthority.DataValueField = "ID"
                    ddlLocalAuthority.DataTextField = "Name"
                    ddlLocalAuthority.DataBind()
                    If objDataTable.Rows.Count = 1 Then
                        sAuthorityID = objDataTable.Rows(0).Item("ID").ToString()
                    End If
                End If
            End If
            Common.AddItemToDropDownList(ddlLocalAuthority)
            If sAuthorityID <> "" Then
                SelectItemInDropDownList(ddlLocalAuthority, sAuthorityID)
            End If
            Return sAuthorityID

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Farm Tab' local authority drop down list.", ex)
        End Try
    End Function

    Private Function LoadADNSRegionLookup(ByVal vsAuthorityID As String) As String

        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()
        Dim sADNSRegionID As String = ""

        Try
            ddlADNSRegion.Items.Clear()

            If vsAuthorityID <> "" Then
                objDataTable = objLookup.GetADNSRegions(CInt(vsAuthorityID))
                If Not (objDataTable Is Nothing) Then
                    ddlADNSRegion.DataSource = objDataTable
                    ddlADNSRegion.DataValueField = "ID"
                    ddlADNSRegion.DataTextField = "Name"
                    ddlADNSRegion.DataBind()
                    If objDataTable.Rows.Count = 1 Then
                        sADNSRegionID = objDataTable.Rows(0).Item("ID").ToString()
                    End If
                End If
            End If
            AddItemToDropDownList(ddlADNSRegion)
            If sADNSRegionID <> "" Then
                SelectItemInDropDownList(ddlADNSRegion, sADNSRegionID)
            End If
            Return sADNSRegionID
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Farm Tab' ADNS Region drop down list.", ex)
        End Try
    End Function

#End Region

#Region "Event Handlers"

    Private Sub btnCancel_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Response.Redirect("Home.aspx")
    End Sub

    Private Sub btnUseAboveAddress_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUseAboveAddress.Click
        txtCorrespondenceAddress1.Text = txtAddress1.Text
        txtCorrespondenceAddress2.Text = txtAddress2.Text
        txtCorrespondenceAddress3.Text = txtAddress3.Text
        txtCorrespondencePostcode.Text = txtPostcode.Text
    End Sub

    Private Sub ddlAuthorityCounty_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlAuthorityCounty.SelectedIndexChanged

        Dim dsData As DataSet

        Try
            dsData = Session.Item(SessionVars.SV_FarmDetails)

            If Not dsData Is Nothing Then
                If dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Count <> 0 Then

                    With dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                        .Item("AuthorityCountyID") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlAuthorityCounty))
                        .Item("AuthorityID") = FormatEmptyString(LoadAuthorityLookup(.Item("AuthorityCountyID").ToString()))
                        .Item("ADNSRegionID") = FormatEmptyString(LoadADNSRegionLookup(.Item("AuthorityID").ToString()))
                    End With
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to change authority county value.", ex)
        End Try
    End Sub

    Private Sub ddlLocalAuthority_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlLocalAuthority.SelectedIndexChanged

        Dim dsData As DataSet

        Try
            dsData = Session.Item(SessionVars.SV_FarmDetails)

            If Not dsData Is Nothing Then
                If dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Count <> 0 Then

                    With dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                        .Item("AuthorityID") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlLocalAuthority))
                        .Item("ADNSRegionID") = FormatEmptyString(LoadADNSRegionLookup(.Item("AuthorityID").ToString()))
                    End With
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to change authority county value.", ex)
        End Try
    End Sub

    Private Sub btnOK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOK.Click

        Dim sRBSE As String
        Dim sNewCPHH As String

        Try
            sRBSE = Replace(Session(SessionVars.SV_RBSENumber), "/", "")
            sNewCPHH = Replace(Session(SessionVars.SV_NewCPHHNumber), "/", "")
            Dim objCase As New BSELib.clsCase()

            Dim sOwnerName As String = txtOwnerName.Text
            Dim sAddress1 As String = txtAddress1.Text
            Dim sAddress2 As String = txtAddress2.Text
            Dim sAddress3 As String = txtAddress3.Text
            Dim sPostCode As String = txtPostcode.Text
            Dim sParish As String = txtParish.Text
            Dim sDistrict As String = txtDistrict.Text
            Dim sCounty As String = GetSelectedItemFromDropDownList(ddlCounty)
            Dim sCorrespondenceAddress1 As String = txtCorrespondenceAddress1.Text
            Dim sCorrespondenceAddress2 As String = txtCorrespondenceAddress2.Text
            Dim sCorrespondenceAddress3 As String = txtCorrespondenceAddress3.Text
            Dim sCorrespondencePostcode As String = txtCorrespondencePostcode.Text
            Dim sMapReference As String = MapReference1.MapReference
            Dim sHerdmark1 As String = txtHerdmark1.Text
            Dim sHerdmark2 As String = txtHerdmark2.Text
            Dim sHerdmark3 As String = txtHerdmark3.Text
            Dim sNumericHerdmark1 As String = txtNumericHerdmark1.Text
            Dim sNumericHerdmark2 As String = txtNumericHerdmark2.Text
            Dim sAHO As String = GetSelectedItemFromDropDownList(ddlAHO)
            Dim iADNSRegionID As Integer = GetSelectedItemFromDropDownList(ddlADNSRegion)

            If CheckMandatoryFields(sOwnerName, sAddress1, sAHO, sParish, sCounty, iADNSRegionID) Then
                If Not objCase.MoveCaseNewFarm(Session.Item(SessionVars.SV_HeaderUserID), _
                                                sRBSE, _
                                                sNewCPHH, _
                                                sOwnerName, _
                                                sAddress1, _
                                                sAddress2, _
                                                sAddress3, _
                                                sPostCode, _
                                                sParish, _
                                                sDistrict, _
                                                sCounty, _
                                                sCorrespondenceAddress1, _
                                                sCorrespondenceAddress2, _
                                                sCorrespondenceAddress3, _
                                                sCorrespondencePostcode, _
                                                sMapReference, _
                                                sHerdmark1, _
                                                sHerdmark2, _
                                                sHerdmark3, _
                                                sNumericHerdmark1, _
                                                sNumericHerdmark2, _
                                                sAHO, _
                                                iADNSRegionID) Then

                    clsAppError.DisplayError("Error moving Case")
                    Exit Sub
                End If
            Else
                Exit Sub
            End If

            sRBSE = FormatRBSE(sRBSE)
            sNewCPHH = FormatCPHH(sNewCPHH)

        Catch ex As Exception
            clsAppError.DisplayError("Error moving the case to the new farm.", ex)
            Exit Sub
        End Try
        Response.Redirect("MaintenanceConfirmation.aspx?title=Case Move Complete&message=The Case " & sRBSE & " was moved to " & sNewCPHH)
    End Sub

    Private Function CheckMandatoryFields(ByVal sOwnerName As String, ByVal sAddress1 As String, ByVal sAHO As String, ByVal sParish As String, ByVal sCounty As String, ByVal iADNSRegionID As Integer) As Boolean
        Try
            If sOwnerName.Trim = "" Then
                clsAppError.DisplayError("You have not entered an Owner Name.")
                Return False
            ElseIf sAddress1.Trim = "" Then
                clsAppError.DisplayError("You have not entered the first line of the Address.")
                Return False
            ElseIf sAHO.Trim = "" Then
                clsAppError.DisplayError("You have not entered an Animal Health Office.")
                Return False
            ElseIf sParish.Trim = "" Then
                clsAppError.DisplayError("You have not entered a Parish.")
                Return False
            ElseIf sCounty.Trim = "" Then
                clsAppError.DisplayError("You have not entered a County.")
                Return False
            ElseIf iADNSRegionID = Nothing Then
                clsAppError.DisplayError("You have not entered an ADNS Region.")
                Return False
            End If
            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Error checking mandatory fields.", ex)
        End Try
    End Function

#End Region

#Region "Map Reference Handling"

    Private Sub MapReference1_MapReferenceChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles MapReference1.MapReferenceChanged
        ' Validate Map Reference to ensure that it is within the Parish.
        Dim sCounty As String
        Dim sParish As String
        Dim sCPHH As String
        Dim sMapReference As String = MapReference1.MapReference
        Dim objCase As New BSELib.clsCase()

        sCPHH = Session(SessionVars.SV_CPHHNumber)
        sCounty = Left$(sCPHH, 2)
        sParish = Mid$(sCPHH, 3, 3)
        If sMapReference <> "" Then
            If sCounty <> "" And sParish <> "" Then
                If objCase.ValidateMapReference(sCounty, sParish, sMapReference) Then
                    lblInvalidMapReference.Visible = False
                Else
                    lblInvalidMapReference.Visible = True
                End If
            End If
        End If
    End Sub

    Private Sub btnEstimate_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEstimate.Click
        ' Estimate Map Reference based on the CPHH
        Dim sCounty As String
        Dim sParish As String
        Dim sCPHH As String
        Dim objCase As New BSELib.clsCase()

        sCPHH = Session(SessionVars.SV_CPHHNumber)
        sCounty = Left$(sCPHH, 2)
        sParish = Mid$(sCPHH, 3, 3)
        If sCounty <> "" And sParish <> "" Then
            MapReference1.MapReference = objCase.EstimateMapReference(sCounty, sParish)
        End If
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

    Private Sub PopulateControls()

        Try

            GetFarmDetailsFromDatabase(Session.Item(SessionVars.SV_CPHHNumber), Session)
            lblRBSEHeader.Text = "RBSE: " & Session(SessionVars.SV_RBSENumber)
            lblCPHHHeader.Text = "CPHH: " & FormatCPHH(Session(SessionVars.SV_NewCPHHNumber))

            Dim dsOldFarmData As DataSet = Session.Item(SessionVars.SV_FarmDetails)
            Dim sCPHH As String

            If Not (dsOldFarmData Is Nothing) Then
                If dsOldFarmData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Count <> 0 Then
                    With dsOldFarmData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                        txtOwnerName.Text = .Item("OwnerName").ToString()
                        txtAddress1.Text = .Item("Address1").ToString()
                        txtAddress2.Text = .Item("Address2").ToString()
                        txtAddress3.Text = .Item("Address3").ToString()
                        txtPostcode.Text = .Item("Postcode").ToString()
                        txtParish.Text = .Item("Parish").ToString()
                        txtDistrict.Text = .Item("District").ToString()
                        SelectItemInDropDownList(ddlCounty, .Item("County").ToString())
                        SelectItemInDropDownList(ddlAuthorityCounty, .Item("AuthorityCountyID").ToString())
                        LoadAuthorityLookup(.Item("AuthorityCountyID").ToString())
                        SelectItemInDropDownList(ddlLocalAuthority, .Item("AuthorityID").ToString())
                        LoadADNSRegionLookup(.Item("AuthorityID").ToString())
                        SelectItemInDropDownList(ddlADNSRegion, .Item("ADNSRegionID").ToString())
                        SelectItemInDropDownList(ddlAHO, .Item("AHO").ToString())
                    End With
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Farm Details.", ex)
        End Try
    End Sub

End Class
