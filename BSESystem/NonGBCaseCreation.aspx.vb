Partial Class NonGBCaseCreation
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents CPHH1 As CPHH
    Protected WithEvents ctlEartag As ThreePartEartag
    Protected WithEvents ctlFinalResultDate As CalendarDate
    Protected WithEvents ctlSlaughterDate As CalendarDate
    Protected WithEvents ctlExitConfirmation As ExitConfirmation

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

        VLAHeader1.PageTitle = "Non-GB Case Creation"
        SetEnterKeys()
        lblRBSEHeader.Text = RBSE_CAPTION & Session(SessionVars.SV_RBSENumber)
        If Not IsPostBack Then
            LoadLookupLists()
        End If
        EnableControls()
    End Sub

    Private Sub SetEnterKeys()

        ctlEartag.SetControlOnEnter(ddlFate.ClientID)
        ctlFinalResultDate.SetControlOnEnter(ctlSlaughterDate.FirstClientID)
        ctlSlaughterDate.SetControlOnEnter(CPHH1.FirstClientID)
        CPHH1.SetDefaultButton(btnLookUp)
        SetTextboxControlOnEnter(txtOwnerName, txtAddress1.ClientID)
        SetTextboxControlOnEnter(txtAddress1, txtAddress2.ClientID)
        SetTextboxControlOnEnter(txtAddress2, txtAddress3.ClientID)
        SetTextboxControlOnEnter(txtAddress3, txtPostcode.ClientID)
        SetTextboxControlOnEnter(txtPostcode, ddlCounty.ClientID)
        SetTextboxControlOnEnter(txtHerdmark, txtNumericHerdmark.ClientID)
        SetTextboxControlOnEnter(txtNumericHerdmark, btnSave.ClientID)

    End Sub

#Region "Lookup Lists"

    Private Sub LoadLookupLists()

        Dim blnResult As Boolean
        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_CASE_FATE)
            If Not (objDataTable Is Nothing) Then
                ddlFate.DataSource = objDataTable
                ddlFate.DataValueField = "Code"
                ddlFate.DataTextField = "Description"
                ddlFate.DataBind()
                SelectItemInDropDownList(ddlFate, "SL")
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_TEST_RESULT)
            If Not (objDataTable Is Nothing) Then
                ddlFinalResult.DataSource = objDataTable
                ddlFinalResult.DataValueField = "Code"
                ddlFinalResult.DataTextField = "Description"
                ddlFinalResult.DataBind()
                SelectItemInDropDownList(ddlFinalResult, "NE")
            End If

            objDataTable = objLookup.GetNonGBCounty()
            If Not (objDataTable Is Nothing) Then
                ddlCounty.DataSource = objDataTable
                ddlCounty.DataValueField = "Code"
                ddlCounty.DataTextField = "Description"
                ddlCounty.DataBind()
                Common.AddItemToDropDownList(ddlCounty)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Submission Registration' drop down lists.", ex)
        End Try
    End Sub

#End Region

#Region "Control Enabling - Disabling"

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
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "VLA Maintenance" Then
            'ctlEartag.AutoPostBack = True
            EnableFarmControls()
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

    Private Sub DisableFarmControls()
        txtOwnerName.Enabled = False
        rfvOwnerName.Enabled = False
        txtAddress1.Enabled = False
        rfvAddress1.Enabled = False
        txtAddress2.Enabled = False
        txtAddress3.Enabled = False
        txtPostcode.Enabled = False
        ddlCounty.Enabled = False
        txtHerdmark.Enabled = False
        txtNumericHerdmark.Enabled = False
    End Sub

    Private Sub EnableFarmControls()
        If CPHH1.CPHH <> "" Then
            txtOwnerName.Enabled = True
            rfvOwnerName.Enabled = True
            txtAddress1.Enabled = True
            rfvAddress1.Enabled = True
            txtAddress2.Enabled = True
            txtAddress3.Enabled = True
            txtPostcode.Enabled = True
            ddlCounty.Enabled = True
            txtHerdmark.Enabled = True
            txtNumericHerdmark.Enabled = True
        Else
            DisableFarmControls()
        End If
    End Sub

#End Region

#Region "Event Handling"

    Private Sub ctlEartag_EartagCountryChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ctlEartag.EartagCountryChanged
        ctlEartag.Validate()
    End Sub

    Private Sub ctlEartag_EartagHerdmarkChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ctlEartag.EartagHerdmarkChanged
        ctlEartag.Validate()
    End Sub

    Private Sub ctlEartag_EartagAnimalChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ctlEartag.EartagAnimalChanged
        ctlEartag.Validate()
    End Sub

    Private Sub btnLookUp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnLookUp.Click
        If CPHH1.CPHH = "" Then
            lblCPHHEmpty.Visible = True
        Else
            lblCPHHEmpty.Visible = False
            If Left$(CPHH1.CPHH, 6) = "009999" Then
                Session(SessionVars.SV_CPHHNumber) = CPHH1.CPHH
                CPHH1.CPHH = CPHH1.CPHH     ' Reformats the CPHH
                If Not LoadFarmDetails() Then
                    'Do Nothing, enable the controls so the user can enter the details.
                End If
                lblCPHHError.Visible = False
                EnableControls()
            Else
                lblCPHHError.Visible = True
            End If
        End If
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        CancelCaseEdit()
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        If ctlEartag.EartagCountry = "" AndAlso ctlEartag.EartagHerdmark = "" AndAlso ctlEartag.EartagAnimal = "" Then
            lblEartagEmpty.Visible = True
        Else
            lblEartagEmpty.Visible = False
            If ctlFinalResultDate.DateField = "" Then
                lblFinalResultDateEmpty.Visible = True
            Else
                lblFinalResultDateEmpty.Visible = False
                If ctlSlaughterDate.DateField = "" Then
                    lblSlaughterDateEmpty.Visible = True
                Else
                    lblSlaughterDateEmpty.Visible = False
                    If CPHH1.CPHH = "" Then
                        lblCPHHEmpty.Visible = True
                    Else
                        lblCPHHEmpty.Visible = False
                        If ddlCounty.SelectedItem.Value = "" Then
                            lblCountyEmpty.Visible = True
                        Else
                            lblCountyEmpty.Visible = False
                            If CreateCase() Then
                                ' Save details to the database
                                Response.Redirect("MaintenanceConfirmation.aspx?title=Case Created&message=The Non-GB Case " & FormatRBSE(Session.Item(SessionVars.SV_RBSENumber)) & " was created")
                            End If
                        End If
                    End If
                End If
            End If
        End If
        EnableControls()
    End Sub

#End Region

#Region "Handle Data"

    Private Function LoadCaseDetails() As Boolean

        Dim dsData As DataSet

        Try
            dsData = Session.Item(SessionVars.SV_FarmDetails)

            If Not (dsData Is Nothing) Then
                If dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows.Count <> 0 Then
                    With dsData.Tables(BSELib.clsFarm.FARM_TABLE).Rows(0)
                        Dim sCPHH As String = .Item("CPHH").ToString()
                        CPHH1.CPHH = sCPHH

                        txtAddress1.Text = .Item("Address1").ToString()
                        txtAddress2.Text = .Item("Address2").ToString()
                        txtAddress3.Text = .Item("Address3").ToString()
                        txtPostcode.Text = .Item("Postcode").ToString()
                        txtOwnerName.Text = .Item("OwnerName").ToString()
                        SelectItemInDropDownList(ddlCounty, .Item("County").ToString())
                    End With

                    Return True
                End If
            End If

            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Case Details.", ex)
            Return False
        End Try
    End Function

    Private Function LoadFarmDetails() As Boolean

        Dim sCPHH As String
        Dim dsData As DataSet

        Try
            sCPHH = CPHH1.CPHH
            If Trim(sCPHH) <> "" Then
                Session.Item(SessionVars.SV_CPHHNumber) = sCPHH
                dsData = Session.Item(SessionVars.SV_CaseDetails)
                dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("CPHH") = sCPHH
                GetFarmDetailsFromDatabase(sCPHH, Session)

                If LoadCaseDetails() Then
                    Return True
                End If
            End If
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Load Farm Details.", ex)
            Return False
        End Try
    End Function

    Private Function CreateCase() As Boolean

        Try
            Dim objCase As New BSELib.clsCase()
            Dim sRBSE As String = Replace(Session.Item(SessionVars.SV_RBSENumber), "/", "")
            Dim sEartag As String = ctlEartag.EartagAnimal
            Dim sEartagHerdmark As String = ctlEartag.EartagHerdmark
            Dim sEartagCountry As String = ctlEartag.EartagCountry
            Dim sFate As String = GetSelectedItemFromDropDownList(ddlFate)
            Dim sFinalResult As String = GetSelectedItemFromDropDownList(ddlFinalResult)
            Dim sFinalResultDate As String = ctlFinalResultDate.DateField
            Dim sSlaughterDate As String = ctlSlaughterDate.DateField
            Dim sCPHH As String = CPHH1.CPHH
            Dim sOwnerName As String = txtOwnerName.Text
            Dim sAddress1 As String = txtAddress1.Text
            Dim sAddress2 As String = txtAddress2.Text
            Dim sAddress3 As String = txtAddress3.Text
            Dim sPostcode As String = txtPostcode.Text
            Dim sCounty As String = GetSelectedItemFromDropDownList(ddlCounty)
            Dim sHerdmark1 As String = txtHerdmark.Text
            Dim sNumericHerdmark1 As String = txtNumericHerdmark.Text
            Dim iUserID As Integer = Session.Item(SessionVars.SV_HeaderUserID)
            Dim sRBSEDate As String = CStr(Today)
            Dim sBarcode As String = ""
            Dim sAHFReference As String = ""

            GetBarcodeAHFReferenceFromBSESS(sRBSE, sBarcode, sAHFReference)

            If ctlEartag.Validate() AndAlso DatesValid() Then
                If Not objCase.CreateNonGBCase(sRBSE, _
                                            sEartag, _
                                            sEartagHerdmark, _
                                            sEartagCountry, _
                                            sFate, _
                                            sFinalResult, _
                                            sFinalResultDate, _
                                            sSlaughterDate, _
                                            sCPHH, _
                                            sOwnerName, _
                                            sAddress1, _
                                            sAddress2, _
                                            sAddress3, _
                                            sPostcode, _
                                            sCounty, _
                                            sHerdmark1, _
                                            sNumericHerdmark1, _
                                            iUserID, _
                                            sRBSEDate, _
                                            sBarcode, _
                                            sAHFReference) Then
                    clsAppError.DisplayError("Failed to Save New Case Details.")
                End If
                Return True
            End If
            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Save New Case Details.", ex)
            Return False
        End Try
    End Function

    Private Function DatesValid() As Boolean
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date

        ' Check Slaughter Date
        dLatestDate = Now()

        If Not ctlSlaughterDate.Validate(dDate, dLatestDate, CalendarDate.ValidationType.eValidateLatest, "Slaughter Date is later than today") Then
            Return False
        End If

        ' Check Final Result Date
        dEarliestDate = ctlSlaughterDate.DateField

        If Not ctlFinalResultDate.Validate(dEarliestDate, dLatestDate, "Final Result date must be between the Slaughter Date and today") Then
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Query Save"

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As BSESystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        CancelCaseEdit()
        e.bNavigateHome = False
    End Sub

    Private Sub CancelCaseEdit()
        Dim bChanges As Boolean = False

        If ctlEartag.EartagCountry <> "" OrElse ctlEartag.EartagHerdmark <> "" OrElse ctlEartag.EartagAnimal <> "" Then bChanges = True
        If GetSelectedItemFromDropDownList(ddlFate) <> "SL" Then bChanges = True
        If GetSelectedItemFromDropDownList(ddlFinalResult) <> "NE" Then bChanges = True
        If ctlFinalResultDate.DateField <> "" Then bChanges = True
        If ctlSlaughterDate.DateField <> "" Then bChanges = True
        If CPHH1.CPHH <> "" Then bChanges = True
        If txtOwnerName.Text <> "" Then bChanges = True
        If txtAddress1.Text <> "" Then bChanges = True
        If txtAddress2.Text <> "" Then bChanges = True
        If txtAddress3.Text <> "" Then bChanges = True
        If txtPostcode.Text <> "" Then bChanges = True
        If GetSelectedItemFromDropDownList(ddlCounty) <> "" Then bChanges = True
        If txtHerdmark.Text <> "" Then bChanges = True
        If txtNumericHerdmark.Text <> "" Then bChanges = True

        If bChanges Then
            ctlExitConfirmation.ShowExitConfirmation()
            'Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript("Modifications have been made to this page.  Are you sure you wish to exit without saving?", "home.aspx"))
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

#End Region

End Class
