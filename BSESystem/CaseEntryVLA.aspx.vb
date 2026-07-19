Partial Class CaseEntryVLA
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents OtherOwnersPager As DataGridPager
    Protected WithEvents ctlDateOfBirth As CalendarDate
    Protected WithEvents ctlDatePurchased As CalendarDate
    Protected WithEvents ctlHerdEntryDate As CalendarDate
    Protected WithEvents ctlSlaughterDate As CalendarDate
    Protected WithEvents ctlOnsetDate As CalendarDate
    Protected WithEvents BatchNumberDisplay1 As BatchNumberDisplay
    Protected WithEvents ctlExitConfirmation As ExitConfirmation


    Private m_bContinueEditingOtherOwnersPrevious As Boolean = Nothing
    Private m_bContinueEditingOtherOwnersOwner As Boolean = Nothing

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

        VLAHeader1.PageTitle = "Case Details"
        'SetEnterKeys()
        lblRBSEHeader.Text = RBSE_CAPTION & Session(SessionVars.SV_RBSENumber)
        VLAHeader1.BatchNumber = Session(SessionVars.SV_BatchNumber)
        OtherOwnersPager.SetGrid(grdOtherOwners)
        If Not IsPostBack Then
            LoadLookupLists()
            LoadCaseDetails()
        End If

        litViewDocs.Text = GetSpolSiteButtonCode(Session(SessionVars.SV_RBSENumber))
        litViewDocs2.Text = litViewDocs.Text

        CheckOnsetDate()
        CheckOnsetAge()
        EnableControls()
    End Sub

    Private Sub SetEnterKeys()

        ctlDateOfBirth.SetControlOnEnter(ddlBirthDateSource.ClientID)
        ctlDatePurchased.SetControlOnEnter(ddlPurchasedCounty.ClientID)
        SetTextboxControlOnEnter(txtAgePurchasedYears, txtAgePurchasedMonths.ClientID)
        SetTextboxControlOnEnter(txtAgePurchasedMonths, ctlHerdEntryDate.FirstClientID)
        ctlHerdEntryDate.SetControlOnEnter(ctlOnsetDate.FirstClientID)
        ctlOnsetDate.SetControlOnEnter(chkOnsetDateEstimated.ClientID)
        SetTextboxControlOnEnter(txtMonthsPregnant, txtMonthsPostCalving.ClientID)
        SetTextboxControlOnEnter(txtMonthsPostCalving, ctlSlaughterDate.FirstClientID)
        ctlSlaughterDate.SetControlOnEnter(txtOnsetAgeYears.ClientID)
        SetTextboxControlOnEnter(txtOnsetAgeYears, txtOnsetAgeMonths.ClientID)
        SetTextboxControlOnEnter(txtOnsetAgeMonths, txtOnsetAgeYears.ClientID)

    End Sub

#Region "Table Handling"

    Private Sub DisableOtherOwnersGrid()
        grdOtherOwners.DataSource = New DataTable()
        grdOtherOwners.DataBind()
        grdOtherOwners.Enabled = False
        OtherOwnersPager.AllowAddNew = False
        OtherOwnersPager.AllowEdit = False
        OtherOwnersPager.AllowDelete = False
        OtherOwnersPager.PageLinkCount = 0
        OtherOwnersPager.Refresh()
    End Sub

    Private Sub EnableOtherOwnersGrid()
        Dim dtData As DataTable = Session.Item(SessionVars.SV_OtherOwnerTable)
        Dim dvData As DataView = dtData.DefaultView
        Session.Item(SessionVars.SV_OtherOwnerView) = dvData

        grdOtherOwners.Enabled = True
        grdOtherOwners.DataSource = dtData
        grdOtherOwners.DataKeyField = "ID"
        grdOtherOwners.CurrentPageIndex = 0
        grdOtherOwners.SelectedIndex = -1
        grdOtherOwners.EditItemIndex = -1
        grdOtherOwners.DataBind()

        OtherOwnersPager.DataTableSessionID = SessionVars.SV_OtherOwnerTable
        OtherOwnersPager.DataViewSessionID = SessionVars.SV_OtherOwnerView
        OtherOwnersPager.AllowAddNew = True
        OtherOwnersPager.AllowEdit = True
        OtherOwnersPager.AllowDelete = True
        OtherOwnersPager.PageLinkCount = 10
        OtherOwnersPager.Refresh()
    End Sub

    Private Sub grdOtherOwners_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdOtherOwners.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim lblOwnerType As Label = Nothing
                Dim ddlOwnerType As DropDownList = Nothing
                Dim ctlCPHH As CPHH = Nothing
                Dim lblCPHHLabel As Label = Nothing
                Dim lblNoOwner As Label = Nothing
                Dim lblPrevious As Label = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                    ' populate edit mode controls
                    ddlOwnerType = CType(e.Item.FindControl("ddlOwnerType"), DropDownList)
                    ctlCPHH = CType(e.Item.FindControl("ctlCPHH"), CPHH)
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblOwnerType = CType(e.Item.FindControl("lblOwnerType"), Label)
                    lblCPHHLabel = CType(e.Item.FindControl("lblCPHH"), Label)
                End If

                If Not lblOwnerType Is Nothing Then
                    If Not IsDBNull(drv("Type")) Then
                        lblOwnerType.Text = GetOtherOwnerType(drv("Type"))
                    Else
                        lblOwnerType.Text = ""
                    End If
                End If

                If Not lblCPHHLabel Is Nothing Then
                    If Not IsDBNull(drv("CPHH")) Then
                        Dim sCPHH As String
                        sCPHH = drv("CPHH")

                        sCPHH = FormatCPHH(sCPHH)

                        lblCPHHLabel.Text = sCPHH
                    Else
                        lblCPHHLabel.Text = ""
                    End If
                End If

                If Not ddlOwnerType Is Nothing Then
                    If Not IsDBNull(drv("Type")) Then
                        SelectItemInDropDownList(ddlOwnerType, drv("Type"))
                    End If
                End If

                If Not ctlCPHH Is Nothing Then
                    If Not IsDBNull(drv("CPHH")) Then
                        ctlCPHH.CPHH = drv("CPHH")
                    End If

                    lblNoOwner = CType(e.Item.FindControl("lblOwnerError"), Label)
                    If Not lblNoOwner Is Nothing Then
                        If m_bContinueEditingOtherOwnersOwner = True Then
                            lblNoOwner.Visible = True
                        Else
                            lblNoOwner.Visible = False
                        End If
                    End If

                    lblPrevious = CType(e.Item.FindControl("lblPreviousError"), Label)
                    If Not lblPrevious Is Nothing Then
                        If m_bContinueEditingOtherOwnersPrevious = True Then
                            lblPrevious.Visible = True
                        Else
                            lblPrevious.Visible = False
                        End If
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Other Owners grid", ex)
        End Try
    End Sub

    Private Sub OtherOwnersPager_RowSave(ByVal sender As Object, ByVal e As BSESystem.DataGridPagerEventArgs) Handles OtherOwnersPager.RowSave
        ' save template column values to the dataset here
        'if the row is new, add a reference to the farm it belongs to
        m_bContinueEditingOtherOwnersPrevious = False
        m_bContinueEditingOtherOwnersOwner = False

        If e.DataTableRow.RowState = DataRowState.Added Then
            e.DataTableRow("RBSE") = Replace(Session(SessionVars.SV_RBSENumber), "/", "")
        End If

        Dim lst As DropDownList = CType(e.GridRow.FindControl("ddlOwnerType"), DropDownList)
        e.DataTableRow("Type") = lst.SelectedItem.Value
        Dim ctlName As TextBox = CType(e.GridRow.FindControl("txtOtherOwnerNameEdit"), TextBox)
        e.DataTableRow("Name") = ctlName.Text
        Dim ctlCPHH As CPHH = CType(e.GridRow.FindControl("ctlcphh"), CPHH)
        e.DataTableRow("CPHH") = Replace(ctlCPHH.CPHH, "/", "")

        If ctlName.Text = "" And ctlCPHH.CPHH = "" Then
            m_bContinueEditingOtherOwnersOwner = True
        End If

        ' Check to ensure that there is only one owner of type previous
        Dim iCount As Integer
        Dim dtData As DataTable = Session.Item(SessionVars.SV_OtherOwnerTable)
    End Sub

    Private Sub OtherOwnersPager_BeforeDataChanged(ByVal sender As Object, ByRef e As BSESystem.DataGridPagerEventArgs) Handles OtherOwnersPager.BeforeDataChanged
        e.bCarryOnEditing = m_bContinueEditingOtherOwnersPrevious Or m_bContinueEditingOtherOwnersOwner
    End Sub

#End Region

#Region "Lookup List Population"

    Private Sub LoadLookupLists()

        Dim objDataTable As DataTable
        Dim objLookup As New BSELib.LookupData()

        Try
            objDataTable = objLookup.GetLookupData(LOOKUP_BIRTH_DATE_SOURCE)
            If Not (objDataTable Is Nothing) Then
                ddlBirthDateSource.DataSource = objDataTable
                ddlBirthDateSource.DataValueField = "Code"
                ddlBirthDateSource.DataTextField = "Description"
                ddlBirthDateSource.DataBind()
                Common.AddItemToDropDownList(ddlBirthDateSource)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_ANIMAL_ORIGIN)
            If Not (objDataTable Is Nothing) Then
                ddlOrigin.DataSource = objDataTable
                ddlOrigin.DataValueField = "Code"
                ddlOrigin.DataTextField = "Description"
                ddlOrigin.DataBind()
                Common.AddItemToDropDownList(ddlOrigin)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_SEX)
            If Not (objDataTable Is Nothing) Then
                ddlSex.DataSource = objDataTable
                ddlSex.DataValueField = "Code"
                ddlSex.DataTextField = "Description"
                ddlSex.DataBind()
                Common.AddItemToDropDownList(ddlSex)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_BREED)
            If Not (objDataTable Is Nothing) Then
                ddlBreed.DataSource = objDataTable
                ddlBreed.DataValueField = "Code"
                ddlBreed.DataTextField = "FullName"
                ddlBreed.DataBind()
                Common.AddItemToDropDownList(ddlBreed)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_BSE_COUNTY)
            If Not (objDataTable Is Nothing) Then
                ddlPurchasedCounty.DataSource = objDataTable
                ddlPurchasedCounty.DataValueField = "Code"
                ddlPurchasedCounty.DataTextField = "Description"
                ddlPurchasedCounty.DataBind()
                Common.AddItemToDropDownList(ddlPurchasedCounty)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'VLA Tab' drop down lists.", ex)
        End Try

    End Sub

#End Region

#Region "Event Handlers"

    Private Sub ctlDateOfBirth_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlDateOfBirth.DateChanged
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("BirthDate") Is DBNull.Value Then
            lblInformDEFRA.Visible = False
        Else
            lblInformDEFRA.Visible = True
        End If
        CheckBirthDateSource()
        CalculateAgePurchased()
        CalculateOnsetAge()
    End Sub

    Private Sub ctlDatePurchased_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlDatePurchased.DateChanged
        CalculateAgePurchased()
    End Sub

    Private Sub ctlOnsetDate_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlOnsetDate.DateChanged
        CheckOnsetDate()
    End Sub

    Private Sub ctlHerdEntryDate_DateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctlHerdEntryDate.DateChanged
        CheckHerdEntryDate()
    End Sub

    Private Sub CheckOnsetDate()
        CalculateOnsetAge()

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        Dim dFormADate As Date
        Dim dOnsetDate As Date

        If OnsetDateValid(dsData.Tables(BSELib.clsCase.CASE_TABLE)) Then
            If ctlOnsetDate.DateField <> "" Then
                If Not dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("FormADate") Is DBNull.Value Then
                    dFormADate = dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("FormADate")
                    dOnsetDate = CDate(ctlOnsetDate.DateField)
                    If dOnsetDate < dFormADate.AddMonths(-3) Then
                        lblOnsetDateWarning.Visible = True
                    Else
                        lblOnsetDateWarning.Visible = False
                    End If
                End If
            Else
                lblOnsetDateWarning.Visible = False
            End If
        End If
    End Sub

    Private Sub CheckBirthDateSource()
        Dim sMessage As String
        If ctlDateOfBirth.DateField = "" Then
            If ddlBirthDateSource.SelectedItem.Value <> "" Then
                sMessage = "The Date of Birth Source you entered for this date has been cleared."
            End If

            SelectItemInDropDownList(ddlBirthDateSource, "")
            ShowMessage(sMessage)
        End If
    End Sub

    Private Sub ddlOrigin_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlOrigin.SelectedIndexChanged
        Dim sMessage As String = ""

        If ddlOrigin.SelectedItem.Value <> "P" Then
            EmptyPurchaseFields(sMessage)
            EmptyTracedFields(sMessage)
            ShowMessage(sMessage)
        End If
    End Sub

    Private Sub ShowMessage(ByVal sMessage As String)
        If sMessage <> "" Then
            Dim jScript As System.Text.StringBuilder = New System.Text.StringBuilder()
            With jScript
                .Append("<script language=""JavaScript"">")
                .Append("alert('" & sMessage & "')")
                .Append("</script>")
            End With
            Page.RegisterStartupScript("navigate", jScript.ToString())
        End If
    End Sub

    Private Sub ddlSex_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlSex.SelectedIndexChanged
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        If ddlSex.SelectedItem.Value <> "F" Then
            If txtMonthsPostCalving.Text <> "" Or txtMonthsPregnant.Text <> "" Then
                ShowMessage("Pregnancy data has been entered on this Case.  If you click Save then this data will be lost.")
            End If

            txtMonthsPostCalving.Text = ""
            txtMonthsPregnant.Text = ""
        End If
    End Sub

    Private Sub btnCaseAuditLog_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCaseAuditLog.Click
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseAuditLogReport.aspx?page=CaseEntryVLA")
        End If
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click, btnSave2.Click
        OtherOwnersPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntrySave.aspx")
        End If
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click, btnCancel2.Click
        CancelCaseEdit()
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As BSESystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        CancelCaseEdit()
        e.bNavigateHome = False
    End Sub

    Private Sub hlbFarm_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbFarm.Click
        OtherOwnersPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryFarm.aspx")
        End If
    End Sub

    Private Sub hlbCaseDEFRA_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbCaseDEFRA.Click
        OtherOwnersPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryDEFRA.aspx")
        End If
    End Sub

    Private Sub hlbBAB_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbBAB.Click
        OtherOwnersPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryBAB.aspx")
        End If
    End Sub

    Private Sub hlbClinical_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbClinical.Click
        OtherOwnersPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryClinical.aspx")
        End If
    End Sub

    Private Sub hlbFeeds_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbFeeds.Click
        OtherOwnersPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryFeeds.aspx")
        End If
    End Sub

    Private Sub hlbRelations_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles hlbRelations.Click
        OtherOwnersPager.CleanUpBlankRows()
        If UpdateSessionWithCaseDetails() Then
            Response.Redirect("CaseEntryRelations.aspx")
        End If
    End Sub

    Private Sub btnCalculateAgePurchased_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCalculateAgePurchased.Click
        CalculateAgePurchased()
    End Sub

    Private Sub btnCalculateOnsetAge_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCalculateOnsetAge.Click
        CalculateOnsetAge()
    End Sub

#End Region

#Region "Handle Data"

    Private Sub CheckOnsetAge()
        If txtOnsetAgeMonths.Text <> "" Then
            If CInt(txtOnsetAgeMonths.Text) < 18 Then
                lblOnsetAgeIncorrect.Visible = True
            Else
                lblOnsetAgeIncorrect.Visible = False
            End If
        Else
            lblOnsetAgeIncorrect.Visible = False
        End If
    End Sub

    Private Sub CalculateOnsetAge()
        Dim dDate As Date
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        If DateOfBirthValid(dsData.Tables(BSELib.clsCase.CASE_TABLE)) And OnsetDateValid(dsData.Tables(BSELib.clsCase.CASE_TABLE)) Then
            If ctlDateOfBirth.DateField <> "" And ctlOnsetDate.DateField <> "" Then
                ' Calculate Onset age from Date of Birth and Onset Date
                Dim dDateOfBirth As Date = CDate(ctlDateOfBirth.DateField)
                Dim dOnsetDate As Date = CDate(ctlOnsetDate.DateField)
                Dim dStartDate As Date
                Dim lMonths As Long
                Dim lDays As Long

                lMonths = DateDiff(DateInterval.Month, dDateOfBirth, dOnsetDate)
                dStartDate = DateAdd(DateInterval.Month, lMonths, dDateOfBirth)
                If dStartDate > dOnsetDate Then
                    dStartDate = DateAdd(DateInterval.Month, -1, dStartDate)
                    lMonths = lMonths - 1
                End If

                txtOnsetAgeMonths.Text = CStr(lMonths)
            Else
                ' Check to see if the date has been entered as years and months
                If txtOnsetAgeYears.Text <> "" Then
                    Dim iMonths As Int32
                    Dim iYears As Int32
                    If txtOnsetAgeMonths.Text <> "" Then
                        iMonths = Convert.ToInt32(txtOnsetAgeMonths.Text)
                    Else
                        iMonths = 0
                    End If
                    iYears = Convert.ToInt32(txtOnsetAgeYears.Text)
                    txtOnsetAgeMonths.Text = Convert.ToString(iMonths + (iYears * 12))
                End If
            End If
            txtOnsetAgeYears.Text = ""
            ' Display message if onset age is less than 18 months
        End If
        CheckOnsetAge()
    End Sub

    Private Sub CalculateAgePurchased()
        Dim dDate As Date
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        If DateOfBirthValid(dsData.Tables(BSELib.clsCase.CASE_TABLE)) And PurchaseDateValid(dsData.Tables(BSELib.clsCase.CASE_TABLE)) Then
            If ctlDateOfBirth.DateField <> "" And ctlDatePurchased.DateField <> "" Then
                ' Calculate purchased age from Date of Birth and Purchased Date
                txtAgePurchasedMonths.Text = CStr(DateDiff(DateInterval.Month, CDate(ctlDateOfBirth.DateField), CDate(ctlDatePurchased.DateField)))
            Else
                ' Check to see if the date has been entered as years and months
                If txtAgePurchasedYears.Text <> "" Then
                    Dim iMonths As Int32
                    Dim iYears As Int32
                    If txtAgePurchasedMonths.Text <> "" Then
                        iMonths = Convert.ToInt32(txtAgePurchasedMonths.Text)
                    Else
                        iMonths = 0
                    End If
                    iYears = Convert.ToInt32(txtAgePurchasedYears.Text)
                    txtAgePurchasedMonths.Text = Convert.ToString(iMonths + (iYears * 12))
                End If
            End If
            txtAgePurchasedYears.Text = ""
        End If
    End Sub

    Private Function LoadCaseDetails() As Boolean

        Dim dsData As DataSet

        Try

            dsData = Session.Item(SessionVars.SV_CaseDetails)

            If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then
                With dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)
                    Session.Item(SessionVars.SV_CPHHNumber) = .Item("CPHH").ToString()

                    ctlDateOfBirth.DateField = FormatDate(.Item("BirthDate").ToString())
                    SelectItemInDropDownList(ddlBirthDateSource, .Item("BirthDateSource").ToString())
                    chkDateOfBirthEstimated.Checked = GetRowColumnData(.Item("IsBirthDateEst"))
                    SelectItemInDropDownList(ddlSex, .Item("Sex").ToString())
                    SelectItemInDropDownList(ddlBreed, .Item("Breed").ToString())
                    SelectItemInDropDownList(ddlOrigin, .Item("Origin").ToString())
                    ctlDatePurchased.DateField = FormatDate(.Item("PurchaseDate").ToString())
                    txtAgePurchasedMonths.Text = .Item("PurchaseAgeInMonths").ToString()
                    SelectItemInDropDownList(ddlPurchasedCounty, .Item("PurchasedCounty").ToString())
                    ctlHerdEntryDate.DateField = FormatDate(.Item("HerdEntryDate").ToString())
                    ctlOnsetDate.DateField = FormatDate(.Item("OnsetDate").ToString())
                    chkOnsetDateEstimated.Checked = GetRowColumnData(.Item("IsOnsetDateEst"))
                    txtMonthsPregnant.Text = .Item("MonthsPregnant").ToString()
                    txtMonthsPostCalving.Text = .Item("MonthsPostCalving").ToString()
                    txtOnsetAgeMonths.Text = .Item("OnsetAgeInMonths").ToString()
                    ctlSlaughterDate.DateField = FormatDate(.Item("SlaughterDate").ToString())
                End With

                EnableOtherOwnersGrid()

                Return True
            End If

            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to 'Load Case Details'.", ex)
            Return False
        End Try
    End Function

    Private Function UpdateSessionWithCaseDetails() As Boolean

        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        Dim dDate As Date

        Try
            If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then
                With dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)

                    If Not DateOfBirthValid(dsData.Tables(BSELib.clsCase.CASE_TABLE)) Then Return False
                    .Item("BirthDate") = FormatEmptyString(ctlDateOfBirth.DateField)

                    .Item("BirthDateSource") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlBirthDateSource))

                    If ctlDateOfBirth.DateField = "" Then
                        .Item("IsBirthDateEst") = DBNull.Value
                    Else
                        .Item("IsBirthDateEst") = chkDateOfBirthEstimated.Checked
                    End If

                    .Item("Sex") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlSex))
                    .Item("Breed") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlBreed))
                    .Item("Origin") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlOrigin))

                    If Not PurchaseDateValid(dsData.Tables(BSELib.clsCase.CASE_TABLE)) Then Return False
                    .Item("PurchaseDate") = FormatEmptyString(ctlDatePurchased.DateField)

                    If txtAgePurchasedYears.Text <> "" Then CalculateAgePurchased()
                    .Item("PurchaseAgeInMonths") = FormatEmptyString(txtAgePurchasedMonths.Text)

                    If Not HerdEntryDateValid(dsData.Tables(BSELib.clsCase.CASE_TABLE)) Then Return False
                    .Item("HerdEntryDate") = FormatEmptyString(ctlHerdEntryDate.DateField)

                    If Not OnsetDateValid(dsData.Tables(BSELib.clsCase.CASE_TABLE)) Then Return False
                    .Item("OnsetDate") = FormatEmptyString(ctlOnsetDate.DateField)

                    If txtMonthsPregnant.Text <> "" And txtMonthsPostCalving.Text <> "" Then
                        lblPregnantError.Visible = True
                        Return False
                    Else
                        lblPregnantError.Visible = False
                    End If
                    .Item("MonthsPregnant") = FormatEmptyString(txtMonthsPregnant.Text)
                    .Item("MonthsPostCalving") = FormatEmptyString(txtMonthsPostCalving.Text)

                    If txtOnsetAgeYears.Text <> "" Then CalculateOnsetAge()
                    .Item("OnsetAgeInMonths") = FormatEmptyString(txtOnsetAgeMonths.Text)

                    If Not SlaughterDateValid(dsData.Tables(BSELib.clsCase.CASE_TABLE)) Then Return False
                    .Item("SlaughterDate") = FormatEmptyString(ctlSlaughterDate.DateField)

                    If ctlOnsetDate.DateField = "" Then
                        .Item("IsOnsetDateEst") = DBNull.Value
                    Else
                        .Item("IsOnsetDateEst") = chkOnsetDateEstimated.Checked
                    End If

                    .Item("PurchasedCounty") = FormatEmptyString(GetSelectedItemFromDropDownList(ddlPurchasedCounty))
                    If OtherOwnersPager.InEditMode Then Return False
                End With

                Return True
            End If

            Return False

        Catch ex As Exception
            clsAppError.DisplayError("Failed to 'Save VLA Case Details'.", ex)
            Return False
        End Try
    End Function

    Private Function DateOfBirthValid(ByRef dtData As DataTable) As Boolean
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date

        dEarliestDate = CDate("01 Jan 1970")

        If dtData.Rows(0)("FormADate") Is DBNull.Value Then
            dLatestDate = Now()
        Else
            dLatestDate = dtData.Rows(0)("FormADate")
        End If

        If Not ctlDateOfBirth.Validate(dDate, dEarliestDate, dLatestDate, "Birth Date must be after 31/12/1969 and before the Form A Date") Then
            Return False
        End If

        Return True
    End Function

    Private Function PurchaseDateValid(ByRef dtData As DataTable) As Boolean
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date

        If Not dtData.Rows(0)("BirthDate") Is DBNull.Value Then
            dEarliestDate = dtData.Rows(0)("BirthDate")

            If dtData.Rows(0)("FormADate") Is DBNull.Value Then
                dLatestDate = Now()
            Else
                dLatestDate = dtData.Rows(0)("FormADate")
            End If

            If Not ctlDatePurchased.Validate(dDate, dEarliestDate, dLatestDate, "Purchase Date must be after the birth date and before the Form A Date") Then
                Return False
            End If
        End If

        Return True
    End Function

    Private Sub CheckHerdEntryDate()
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)
        Dim dtData As DataTable = dsData.Tables(BSELib.clsCase.CASE_TABLE)
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date

        If Not dtData.Rows(0)("BirthDate") Is DBNull.Value Then
            dEarliestDate = DateAdd(DateInterval.Month, 18, dtData.Rows(0)("BirthDate"))

            If dtData.Rows(0)("FormADate") Is DBNull.Value Then
                dLatestDate = Now()
            Else
                dLatestDate = dtData.Rows(0)("FormADate")
            End If

            ctlHerdEntryDate.Validate(dDate, dLatestDate, CalendarDate.ValidationType.eValidateLatest, "The Herd Entry Date must be before the Form A Date.")

            If ctlHerdEntryDate.DateField.Trim = "" Then
                lblHerdDateWarning.Visible = False
            Else
                If (dDate >= dEarliestDate) Then
                    lblHerdDateWarning.Visible = False
                Else
                    lblHerdDateWarning.Visible = True
                End If
            End If

        End If
    End Sub

    Private Function HerdEntryDateValid(ByRef dtData As DataTable) As Boolean
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date
        If Not dtData.Rows(0)("BirthDate") Is DBNull.Value Then
            dEarliestDate = DateAdd(DateInterval.Month, 18, dtData.Rows(0)("BirthDate"))

            If dtData.Rows(0)("FormADate") Is DBNull.Value Then
                dLatestDate = Now()
            Else
                dLatestDate = dtData.Rows(0)("FormADate")
            End If

            If ctlHerdEntryDate.DateField.Trim <> "" Then
                dDate = ctlHerdEntryDate.DateField

                If Not ctlHerdEntryDate.Validate(dDate, dLatestDate, CalendarDate.ValidationType.eValidateLatest, "The Herd Entry Date must be before the Form A Date.") Then
                    Return False
                End If

                If (dDate >= dEarliestDate) Then

                Else
                    lblHerdEntryDate.Visible = True
                    Return True
                End If
            Else
                lblHerdEntryDate.Visible = False
            End If
        End If


            Return True
    End Function

    Private Function OnsetDateValid(ByRef dtData As DataTable) As Boolean
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date

        If dtData.Rows(0)("FormADate") Is DBNull.Value Then
            dLatestDate = Now()
        Else
            dLatestDate = dtData.Rows(0)("FormADate")
        End If

        If ctlDateOfBirth.DateField <> "" AndAlso DateOfBirthValid(dtData) Then
            dEarliestDate = CDate(ctlDateOfBirth.DateField)

            If Not ctlOnsetDate.Validate(dDate, dEarliestDate, dLatestDate, "Onset Date must be after the Date Of Birth and before the Form A Date") Then
                Return False
            End If
        Else
            If Not ctlOnsetDate.Validate(dLatestDate, CalendarDate.ValidationType.eValidateLatest, "Onset Date must be before the Form A Date") Then
                Return False
            End If
        End If

        Return True
    End Function

    Private Function SlaughterDateValid(ByRef dtData As DataTable) As Boolean
        Dim dDate As Date
        Dim dEarliestDate As Date
        Dim dLatestDate As Date
        Dim sMessage As String

        dLatestDate = Now()

        If dtData.Rows(0)("FormADate") Is DBNull.Value Then
            If dtData.Rows(0)("BirthDate") Is DBNull.Value Then
                If Not ctlSlaughterDate.Validate(dDate, dLatestDate, CalendarDate.ValidationType.eValidateLatest, "You have entered a future date for the slaughter date") Then
                    Return False
                End If
            Else
                dEarliestDate = dtData.Rows(0)("BirthDate")
                sMessage = "You must enter a date between the Birth Date and todays date"
            End If
        Else
            dEarliestDate = dtData.Rows(0)("FormADate")
            sMessage = "You must enter a date between the Form A Date and todays date"
        End If

        If Not ctlSlaughterDate.Validate(dEarliestDate, dLatestDate, sMessage) Then
            Return False
        End If

        Return True
    End Function

    Public Function GetOwnerTypeList() As DataTable
        Try
            Dim objLookup As New BSELib.LookupData()
            Dim dt As DataTable = objLookup.GetLookupData(LOOKUP_OWNER_TYPE)

            If dt Is Nothing Then
                Throw New Exception("LookupData.GetLookupData returned Nothing")
            End If

            Return dt

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve the list of Owner Types", ex)
        End Try
    End Function

    Private Function GetOtherOwnerType(ByVal sCode As String) As String

        Try
            Dim dt As DataTable = GetOwnerTypeList()

            If Not dt Is Nothing Then
                Dim dv As New DataView(dt, "", "Code", DataViewRowState.CurrentRows)
                Dim iRow As Integer = dv.Find(sCode)
                If iRow >= 0 Then
                    Return dv(iRow).Item("Description").ToString()
                Else
                    Return ""
                End If
            Else
                Return ""
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve the list of Other Owner Types", ex)
        End Try
    End Function

#End Region

#Region "Enable / Disable Controls"

    Private Sub EnableControls()
        VLAHeader1.GetUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "DEFRA Viewer" Then
            DEFRAViewerEnable()
        ElseIf sGroupName = "DEFRA Data Entry" Then
            DEFRADataEntryEnable()
        ElseIf sGroupName = "DEFRA Maintenance" Then
            DEFRAMaintenanceEnable()
        ElseIf sGroupName = "VLA Data Entry" Then
            VLADataEntryEnable()
        ElseIf sGroupName = "VLA Maintenance" Then
            VLAMaintenanceEnable()
        Else
            Response.Redirect("SearchMenu.aspx")
        End If
    End Sub

    Private Sub DEFRAViewerEnable()
        BatchNumberDisplay1.Visible = False
        MakeControlsReadOnly()
        btnSave.Enabled = False
    End Sub

    Private Sub DEFRADataEntryEnable()
        BatchNumberDisplay1.Visible = False
        MakeControlsReadOnly()
        btnSave.Enabled = True
    End Sub

    Private Sub DEFRAMaintenanceEnable()
        BatchNumberDisplay1.Visible = False
        MakeControlsReadOnly()
        btnSave.Enabled = True
    End Sub

    Private Sub VLADataEntryEnable()
        BatchNumberDisplay1.Visible = True
        If IsVLAAllowedMainCaseEdit(Session) Then
            MakeControlsWritable()
        Else
            MakeControlsReadOnly()
        End If
        btnSave.Enabled = True
    End Sub

    Private Sub VLAMaintenanceEnable()
        BatchNumberDisplay1.Visible = True
        If IsVLAAllowedMainCaseEdit(Session) Then
            MakeControlsWritable()
        Else
            MakeControlsReadOnly()
        End If
        btnSave.Enabled = True
    End Sub

    Private Sub MakeControlsReadOnly()
        ctlDateOfBirth.Enabled = False
        ctlDateOfBirth.Mandatory = False
        chkDateOfBirthEstimated.Enabled = False
        ddlSex.Enabled = False
        ddlBreed.Enabled = False
        ddlOrigin.Enabled = False
        ctlDatePurchased.Enabled = False
        ctlDatePurchased.Mandatory = False
        txtAgePurchasedYears.Enabled = False
        txtAgePurchasedMonths.Enabled = False
        btnCalculateAgePurchased.Enabled = False
        ddlPurchasedCounty.Enabled = False
        ctlHerdEntryDate.Enabled = False
        ctlHerdEntryDate.Mandatory = False
        ctlOnsetDate.Enabled = False
        ctlOnsetDate.Mandatory = False
        chkOnsetDateEstimated.Enabled = False
        txtMonthsPregnant.Enabled = False
        txtMonthsPostCalving.Enabled = False
        txtOnsetAgeYears.Enabled = False
        txtOnsetAgeMonths.Enabled = False
        btnCalculateOnsetAge.Enabled = False
        ctlSlaughterDate.Enabled = False
        ctlSlaughterDate.Mandatory = False
        OtherOwnersPager.AllowAddNew = False
        OtherOwnersPager.AllowEdit = False
        OtherOwnersPager.AllowDelete = False
        ddlBirthDateSource.Enabled = False
    End Sub

    Private Sub MakeControlsWritable()
        ctlDateOfBirth.Enabled = True
        ctlDateOfBirth.Mandatory = False
        ctlDateOfBirth.AutoPostBack = True
        If ctlDateOfBirth.DateField = "" Then
            chkDateOfBirthEstimated.Checked = False
            chkDateOfBirthEstimated.Enabled = False
            ddlBirthDateSource.Enabled = False
        Else
            chkDateOfBirthEstimated.Enabled = True
            ddlBirthDateSource.Enabled = True
        End If
        ddlSex.Enabled = True
        ddlBreed.Enabled = True
        ddlOrigin.Enabled = True
        If ddlOrigin.SelectedItem.Value = "P" Then
            ctlDatePurchased.Enabled = True
            ctlDatePurchased.AutoPostBack = True
            txtAgePurchasedYears.Enabled = True
            txtAgePurchasedMonths.Enabled = True
            btnCalculateAgePurchased.Enabled = True
            ddlPurchasedCounty.Enabled = True
        Else
            ctlDatePurchased.Enabled = False
            txtAgePurchasedYears.Enabled = False
            txtAgePurchasedMonths.Enabled = False
            btnCalculateAgePurchased.Enabled = False
            ddlPurchasedCounty.Enabled = False
        End If
        ctlDatePurchased.Mandatory = False
        ctlHerdEntryDate.Enabled = True
        ctlHerdEntryDate.AutoPostBack = True
        ctlHerdEntryDate.Mandatory = False
        ctlOnsetDate.Enabled = True
        ctlOnsetDate.AutoPostBack = True
        ctlOnsetDate.Mandatory = False
        If ctlOnsetDate.DateField = "" Then
            chkOnsetDateEstimated.Checked = False
            chkOnsetDateEstimated.Enabled = False
        Else
            chkOnsetDateEstimated.Enabled = True
        End If
        If ddlSex.SelectedItem.Value = "F" Then
            txtMonthsPregnant.Enabled = True
            txtMonthsPostCalving.Enabled = True
        Else
            txtMonthsPregnant.Enabled = False
            txtMonthsPostCalving.Enabled = False
        End If
        txtOnsetAgeYears.Enabled = True
        txtOnsetAgeMonths.Enabled = True
        btnCalculateOnsetAge.Enabled = True
        ctlSlaughterDate.Enabled = True
        ctlSlaughterDate.Mandatory = False
        OtherOwnersPager.AllowAddNew = True
        OtherOwnersPager.AllowEdit = True
        OtherOwnersPager.AllowDelete = True
    End Sub

#End Region

#Region "Private Methods"

    Private Sub CancelCaseEdit()
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder()
        Dim dsFarm As DataSet = Session(SessionVars.SV_FarmDetails)
        Dim dsCase As DataSet = Session(SessionVars.SV_CaseDetails)
        Dim clsDataCheck As New BSELib.clsDataCheck()
        Dim bFarmChanges As Boolean
        Dim bCaseChanges As Boolean

        UpdateSessionWithCaseDetails()

        If clsDataCheck.DataSetHasChanges(dsCase) Then bCaseChanges = True
        If clsDataCheck.DataSetHasChanges(dsFarm) Then bFarmChanges = True

        If bFarmChanges Or bCaseChanges Then
            ctlExitConfirmation.ShowExitConfirmation()
            'Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript("You have not saved.  Cancel then Save or OK to exit without saving.", "home.aspx"))
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub EmptyPurchaseFields(ByRef sMessage As String)
        If ctlDatePurchased.DateField <> "" Or ddlPurchasedCounty.SelectedItem.Value <> "" Or txtAgePurchasedMonths.Text <> "" Then
            sMessage = "Purchase data has been entered on this Case.  If you click Save then this data will be lost."
        End If

        ctlDatePurchased.DateField = ""
        SelectItemInDropDownList(ddlPurchasedCounty, "")
        txtAgePurchasedYears.Text = ""
        txtAgePurchasedMonths.Text = ""
    End Sub

    Private Sub EmptyTracedFields(ByRef sMessage As String)
        Dim dsData As DataSet = Session.Item(SessionVars.SV_CaseDetails)

        If dsData.Tables(BSELib.clsCase.BAB_TABLE).Rows.Count <> 0 Then
            With dsData.Tables(BSELib.clsCase.BAB_TABLE).Rows(0)
                If .Item("TracedAddress1").ToString() <> "" Or .Item("TracedAddress2").ToString() <> "" Or .Item("TracedAddress3").ToString() <> "" Or .Item("NatalCPHH").ToString() <> "" Or .Item("TracedName").ToString() <> "" Or .Item("TracedPostcode").ToString() <> "" Then
                    If sMessage = "" Then
                        sMessage = "Trace data has been entered on this Case.  If you click Save then this data will be lost."
                    Else
                        sMessage = "Purchase And Trace data has been entered on this Case.  If you click Save then this data will be lost."
                    End If
                End If

                .Item("TracedAddress1") = FormatEmptyString("")
                .Item("TracedAddress2") = FormatEmptyString("")
                .Item("TracedAddress3") = FormatEmptyString("")
                .Item("NatalCPHH") = FormatEmptyString("")
                .Item("TracedName") = FormatEmptyString("")
                .Item("TracedPostcode") = FormatEmptyString("")
            End With
        End If
    End Sub

#End Region


End Class
