'Notes on using the DataGridPager
'================================

'General layout considerations
'-----------------------------

'Put your DataGrid control and the DataGridPager together in a DIV to
'keep them together.

'If you specify the widths of bound columns using an 
'<ItemStyle Width=...> tag, the pager will make sure the text boxes get
'set to the same width when the grid is in edit mode.

'The DataGridPager uses the GridPagerNavLinks, GridPagerPageNum and
'GridPagerErrorText CSS classes.

'Initialisation
'--------------

'In Page_Load, call pager.SetGrid() to link the DataGrid to the
'DataGridPager (do this in every Page_Load, not just if IsPostBack is
'False).

'The rest of this section is one-time initialisation and could be
'performed in Page_Load when IsPostBack is False.

'Populate a DataTable dt with the data for the grid.  You must set
'dt.PrimaryKey to the primary key column if you want the DataGridPager to
'handle in-place editing (there is a function TBCultureDA.SetPrimaryKey
'to do this).

'If you want to support sorting and filtering of records, create a
'DataView:
'Dim dv As DataView = dt.DefaultView()

'Store both the DataTable and the DataView in the session object.  The ID
'strings used to identify them in the session will be passed to the
'DataGridPager later.

'Initialise the DataGrid control as follows.  Note that for the
'DataGridPager to support editing or deleting, you must set the grid's
'DataKeyField property to the name of the primary key column in the
'DataSet.
'    grdLookup.DataSource = dt
'    grdLookup.DataKeyField = "Key_Column_Name"
'    grdLookup.CurrentPageIndex = 0
'    grdLookup.SelectedIndex = -1
'    grdLookup.EditItemIndex = -1
'    grdLookup.DataBind()

'Initialise and set the behaviour of the DataGridPager as follows.
'    Pager.DataTableSessionID = "DataTable_Session_ID"
'    Pager.DataViewSessionID = "DataView_Session_ID"
'    Pager.PageLinkCount = 10
'    Pager.AllowAddNew = True    ' or False
'    Pager.AllowEdit = True      ' or False
'    Pager.AllowDelete = True    ' or False
'    Pager.ConfirmDelete = True  ' or False
'    Pager.Refresh()


'Template columns
'----------------

'The DataGridPager does not automatically handle template columns, as
'they cannot be bound to a DataTable field.  So you need to handle a
'couple of events to deal with any template columns in your grid.

'In the DataGrid's ItemDataBound event, set the initial values of the
'template column controls from the appropriate DataTable columns.

'In the DataGridPager's RowSave event, copy the values in any template
'columns back to the DataTable.  The DataGridPagerEventArgs parameter of
'the event will contain references to the grid row and DataRow that you
'need to work with.


'Editing, Adding and Deleting rows
'---------------------------------

'The DataGridPager will deal with all this automatically provided you set
'the primary keys on both the grid and the DataTable.


'Saving data
'-----------

'When the DataGridPager has made a change to the data in the DataTable
'(because a row has been either edited, deleted or added), it will raise
'the DataChanged event.  You could use this event to commit the DataTable
'contents back to the database.  Alternatively you might have a Save
'button on your form that will commit all row edits in one go; in this
'case then the DataChanged event would be a good place to enable the Save
'button.

'If your data adapter fails to save the DataTable to the database, check
'dt.HasErrors.  If this is true, then the DataTable will contain row
'error information.  Call pager.DisplayRowError() to cause the
'DataGridPager to display the first row error.


'Sorting
'-------

'Set the sort expressions on your DataGrid columns, and give the
'DataGridPager an ID for your DataView in the session object as described
'above.  Then the DataGridPager will take care of sorting for you.  It
'will even reverse the sort order on the second click of a column title.

'Note that the sort order will be lost when a row is added.


'Filtering
'---------

'If you provided the DataGridPager with a reference to your DataView then
'you can use it to perform filtering of the grid data.  When adding a
'filter it is advisable to set the grid page back to 0 in case the filter
'results in fewer pages being displayed.
'    dv.RowFilter = "ColumnName='Value'"
'    grid.CurrentPageIndex = 0
'    grid.SelectedIndex = -1
'    grid.EditItemIndex = -1
'    pager.Rebind()
'    pager.Refresh()


Public Class DataGridPagerEventArgs
    Inherits System.EventArgs

    Public GridRow As DataGridItem
    Public DataTableRow As DataRow
    Public bCarryOnEditing As Boolean
End Class


Partial  Class DataGridPager
    Inherits System.Web.UI.UserControl

#Region " Control declarations "
    Private WithEvents m_dgPager As System.Web.UI.WebControls.DataGrid
#End Region

    Private m_intPageLinkCount As Int32 = 10

    'for bookmarking
    Private mbEnableBookmarking As Boolean = False

#Region " Event declarations "
    Public Event AddNew(ByVal sender As System.Object, ByVal e As System.EventArgs)
    Public Event PageChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
    Public Event Edit(ByVal sender As System.Object, ByVal e As System.EventArgs)
    Public Event Save(ByVal sender As System.Object, ByVal e As System.EventArgs)
    Public Event Cancel(ByVal sender As System.Object, ByVal e As System.EventArgs)
    Public Event Delete(ByVal sender As System.Object, ByVal e As System.EventArgs)
    Public Event BeforeDataChanged(ByVal sender As System.Object, ByRef e As DataGridPagerEventArgs)
    Public Event DataChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
    Public Event RowSave(ByVal sender As System.Object, ByVal e As DataGridPagerEventArgs)
    Public Event SelectionChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
#End Region

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
        'Put user code to initialize the page here

        ' Remove any previous error message
        lblError.Text = ""
    End Sub

#Region " Public properties "
    ' this points to the grid that we need to page
    Public Sub SetGrid(ByRef Grid As System.Web.UI.WebControls.DataGrid)
        m_dgPager = Grid
        AddHandler Grid.ItemCommand, AddressOf OnGridItemCommand
    End Sub

    Public ReadOnly Property InEditMode() As Boolean
        Get
            Return cmdSave.Visible
        End Get
    End Property

    Public Property PageLinkCount() As Int32
        Get
            Return m_intPageLinkCount
        End Get
        Set(ByVal Value As Int32)
            If Value > 10 Then
                Value = 10
            End If
            If Value < 0 Then
                Value = 0
            End If
            m_intPageLinkCount = Value
        End Set
    End Property

    Public Property AllowAddNew() As Boolean
        Get
            Return imbtnNewRec.Enabled
        End Get
        Set(ByVal Value As Boolean)
            imbtnNewRec.Visible = Value
        End Set
    End Property

    Public Property ShowPageCount() As Boolean
        Get
            Return lblPageLocation.Visible
        End Get
        Set(ByVal Value As Boolean)
            lblPageLocation.Visible = Value
        End Set
    End Property

    Public Property AllowEdit() As Boolean
        Get
            Return cmdEdit.Enabled
        End Get
        Set(ByVal Value As Boolean)
            cmdEdit.Visible = Value
        End Set
    End Property

    Public Property AllowDelete() As Boolean
        Get
            Return cmdDel.Enabled
        End Get
        Set(ByVal Value As Boolean)
            cmdDel.Visible = Value
        End Set
    End Property

    Public WriteOnly Property ConfirmDelete() As Boolean
        Set(ByVal Value As Boolean)
            If Value Then
                cmdDel.Attributes.Add("onClick", _
                                      "javascript:return confirm('The selected record will be deleted.  Continue?')")
            Else
                If Not cmdDel.Attributes("onClick") Is Nothing Then
                    cmdDel.Attributes("onClick") = ""
                End If
            End If
        End Set
    End Property

    Public WriteOnly Property ConfirmDeleteMessage() As String
        Set(ByVal Value As String)
            ConfirmDelete = False
            If Not Value Is Nothing AndAlso Value <> "" Then
                cmdDel.Attributes.Add("onClick", _
                                      "javascript:return confirm('" & Value & "')")
            End If
        End Set
    End Property

    Public Property DataTableSessionID() As String
        Get
            Return ViewStateGet(ViewStateID_SessionTable)
        End Get
        Set(ByVal Value As String)
            ViewStateSet(ViewStateID_SessionTable, Value)
        End Set
    End Property

    Public Property DataViewSessionID() As String
        Get
            Return ViewStateGet(ViewStateID_SessionView)
        End Get
        Set(ByVal Value As String)
            ViewStateSet(ViewStateID_SessionView, Value)
        End Set
    End Property

#End Region

#Region " Public methods "

    Public Sub CleanUpBlankRows()
        Dim iRowCount As Int32
        Dim iColCount As Int32
        Dim iNumberOfBlankColumns As Int32
        Dim dtData As DataTable = GetDataTableFromSession()

        If Not (dtData Is Nothing) AndAlso dtData.Rows.Count > 0 Then
            For iRowCount = (dtData.Rows.Count - 1) To 0 Step -1
                If dtData.Rows(iRowCount).RowState = DataRowState.Added OrElse dtData.Rows(iRowCount).RowState = DataRowState.Modified Then
                    iNumberOfBlankColumns = 0
                    For iColCount = (dtData.Columns.Count - 1) To 0 Step -1
                        If dtData.Rows(iRowCount)(iColCount).ToString = "" Then
                            iNumberOfBlankColumns += 1
                        End If
                    Next iColCount
                    If iNumberOfBlankColumns = (dtData.Columns.Count - 1) Then
                        dtData.Rows.Remove(dtData.Rows(iRowCount))
                    End If
                End If
            Next iRowCount
        End If
    End Sub

    Public Sub Refresh()
        'EnableEditModeControls(False)

        'enable navigation buttons based on page numbers and current page
        EnableNavButtons(True)

        ' edit/delete buttons
        EnableEditDelButtons(True)

        'display the appropriate number of page links

        ' Holding a "Scrolling" number ... will display the middle "n" buttons that are to be visible.
        ' (if if displaying 5 pages, and 20 pages are returned, if you are on page 11, then
        ' pages 9, 10, 11, 12, 13 will be displayed.  Clicking on page 13 will cause 11 to 15 to be 
        ' displayed ... etc.

        If m_intPageLinkCount > m_dgPager.PageCount Then
            m_intPageLinkCount = m_dgPager.PageCount
        End If

        ' Calculate the starting point for the first button. 
        '(Current Page, less half the number to be displayed) or 1 ... if at start (or Last Page - no of pages if at end)
        Dim intIndex As Int32 = m_dgPager.CurrentPageIndex - CType((m_intPageLinkCount / 2), Integer)

        If (intIndex + m_intPageLinkCount) > m_dgPager.PageCount Then
            intIndex = m_dgPager.PageCount - m_intPageLinkCount + 1
        End If

        If intIndex < 1 Then
            intIndex = 1
        End If

        'Disable all the buttons
        lbtnPage1.Visible = False
        lbtnPage2.Visible = False
        lbtnPage3.Visible = False
        lbtnPage4.Visible = False
        lbtnPage5.Visible = False
        lbtnPage6.Visible = False
        lbtnPage7.Visible = False
        lbtnPage8.Visible = False
        lbtnPage9.Visible = False
        lbtnPage10.Visible = False

        ' Only show the number of buttons they need to see.
        If m_intPageLinkCount >= 1 Then lbtnPage1.Visible = True
        If m_intPageLinkCount >= 2 Then lbtnPage2.Visible = True
        If m_intPageLinkCount >= 3 Then lbtnPage3.Visible = True
        If m_intPageLinkCount >= 4 Then lbtnPage4.Visible = True
        If m_intPageLinkCount >= 5 Then lbtnPage5.Visible = True
        If m_intPageLinkCount >= 6 Then lbtnPage6.Visible = True
        If m_intPageLinkCount >= 7 Then lbtnPage7.Visible = True
        If m_intPageLinkCount >= 8 Then lbtnPage8.Visible = True
        If m_intPageLinkCount >= 9 Then lbtnPage9.Visible = True
        If m_intPageLinkCount >= 10 Then lbtnPage10.Visible = True

        'Set the page text for the paging buttons
        SetPageText(lbtnPage1, intIndex)
        SetPageText(lbtnPage2, intIndex + 1)
        SetPageText(lbtnPage3, intIndex + 2)
        SetPageText(lbtnPage4, intIndex + 3)
        SetPageText(lbtnPage5, intIndex + 4)
        SetPageText(lbtnPage6, intIndex + 5)
        SetPageText(lbtnPage7, intIndex + 6)
        SetPageText(lbtnPage8, intIndex + 7)
        SetPageText(lbtnPage9, intIndex + 8)
        SetPageText(lbtnPage10, intIndex + 9)

        EnablePageLinks(True)

        ' Information about what point in the recordset we are at.
        lblPageLocation.Text = "Page " & (m_dgPager.CurrentPageIndex + 1).ToString & " of " & m_dgPager.PageCount.ToString
    End Sub

    ' rebind the grid to the data table, applying any sorting and filtering
    Public Sub Rebind()

        Dim dt As DataTable = GetDataTableFromSession()
        Dim dv As DataView = GetDataViewFromSession()

        If Not dv Is Nothing AndAlso (dv.Sort <> "" Or dv.RowFilter <> "") Then
            m_dgPager.DataSource = dv
        ElseIf Not dt Is Nothing Then
            m_dgPager.DataSource = dt
        End If

        ' record the current page number, and set back to the first page before
        ' rebinding, in case the datatable now has fewer pages than are 
        ' currently displayed.
        Dim iPage As Integer = m_dgPager.CurrentPageIndex
        m_dgPager.CurrentPageIndex = 0
        Dim iSelection As Integer = m_dgPager.SelectedIndex
        m_dgPager.SelectedIndex = -1
        Dim iEdit As Integer = m_dgPager.EditItemIndex
        m_dgPager.EditItemIndex = -1

        m_dgPager.DataBind()

        If iPage >= m_dgPager.PageCount Then
            m_dgPager.CurrentPageIndex = m_dgPager.PageCount - 1
            ' bind _again_ to get back to the correct page
            m_dgPager.DataBind()
            RaiseEvent PageChanged(Me, Nothing)
        Else
            If iPage >= 0 Then
                m_dgPager.CurrentPageIndex = iPage
                ' bind _again_ to get back to the correct page
                m_dgPager.DataBind()
            End If

            If iSelection < m_dgPager.Items.Count And iSelection >= 0 Then
                m_dgPager.SelectedIndex = iSelection
            End If

            If iEdit < m_dgPager.Items.Count And iEdit >= 0 Then
                m_dgPager.EditItemIndex = iEdit
                ' bind _again_ to get into edit mode
                m_dgPager.DataBind()
            End If
        End If

    End Sub

    ' Checks the DataTable for row errors; if there are any, then the
    ' first error is displayed and the associated row is selected.
    Public Sub DisplayRowError(Optional ByVal dtData As DataTable = Nothing, _
                               Optional ByVal blnSetEditMode As Boolean = False, _
                               Optional ByVal blnShowFullError As Boolean = True)
        Try
            Dim dt As DataTable
            dt = GetDataTableFromSession()

            If dt Is Nothing Then
                dt = dtData
            End If

            If Not dt Is Nothing AndAlso dt.HasErrors() Then

                Dim iRow As Integer
                For iRow = 0 To dt.Rows.Count - 1
                    Dim row As DataRow = dt.Rows(iRow)

                    If row.HasErrors() Then
                        Dim sMsg As String = "Unable to "

                        Select Case row.RowState
                            Case DataRowState.Added
                                sMsg &= "add"
                            Case DataRowState.Deleted
                                ' reject changes to get the row back in
                                ' the grid
                                row.RejectChanges()
                                sMsg &= "delete"
                            Case Else
                                sMsg &= "save"
                        End Select

                        If SelectGridRowForDataRow(dt, iRow) Then
                            If (blnSetEditMode) Then
                                ' Put error row in edit mode
                                cmdEdit_Click(Me, Nothing)
                                sMsg &= " the editable row:"
                            Else
                                sMsg &= " the selected row:"
                            End If
                        Else
                            ' didn't find the problem row in the grid for some reason
                            sMsg &= " a row:"
                        End If
                        If blnShowFullError Then
                            sMsg &= "<br>" & row.RowError
                        End If

                        lblError.Text = sMsg

                        Exit For
                    End If
                Next iRow
            End If
        Catch ex As Exception
            DisplayError("Failed to display a row error", ex)
        End Try
    End Sub

    Public Function GetRowIndexFromID(ByVal iID As Integer) As Integer
        'takes in an ID (the primary key of the table) and returns the
        'row number with that ID
        Dim iRetValue As Integer = -1
        Dim iCurrentRow As Integer
        Dim iRowCount As Integer = m_dgPager.DataKeys.Count - 1
        For iCurrentRow = 0 To iRowCount
            If iID = CType(m_dgPager.DataKeys(iCurrentRow), Integer) Then
                iRetValue = iCurrentRow
                Exit For
            End If
        Next iCurrentRow

        Return iRetValue

    End Function

    Public Sub ShowErrorString(ByVal sError As String)
        lblError.Text = sError
    End Sub

#End Region

#Region " Navigation events and functions "

    Private Sub ChangePage(ByVal intPage As Int32)
        ' When Navigating through Pages, 
        ' turn off Editing, and select the relevant page, and rebind data.
        Try

            AddBookmark()

            m_dgPager.EditItemIndex = -1
            SetSelectedIndex(-1)
            m_dgPager.CurrentPageIndex = intPage - 1

            Rebind()
            Refresh()

            RaiseEvent PageChanged(Me, Nothing)

        Catch ex As Exception
            DisplayError("Error changing data grid pages", ex)
        End Try
    End Sub

    Private Sub imbtnFirstPage_Click(ByVal sender As System.Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles imbtnFirstPage.Click
        ChangePage(1)
    End Sub

    Private Sub imbtnPrevPage_Click(ByVal sender As System.Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles imbtnPrevPage.Click
        ChangePage(m_dgPager.CurrentPageIndex)
    End Sub

    Private Sub imbtnNextPage_Click(ByVal sender As System.Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles imbtnNextPage.Click
        ' Index is plus 2 ... as current index is always - 1 (Base 0) and 
        ' lbtnPage subtracts 1 from number to bring to base 0 (ie Page 1 is CurrentPageIndex 0)
        ' to get from Page 1 (0) to Page 2 (1) ... you need to pass 2 which is CurrentPageIndex + 2.
        ChangePage(m_dgPager.CurrentPageIndex + 2)
    End Sub

    Private Sub imbtnLastPage_Click(ByVal sender As System.Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles imbtnLastPage.Click
        ChangePage(m_dgPager.PageCount)
    End Sub

    Private Sub lbtnPage_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbtnPage1.Click, lbtnPage2.Click, lbtnPage3.Click, lbtnPage4.Click, lbtnPage5.Click, lbtnPage6.Click, lbtnPage7.Click, lbtnPage8.Click, lbtnPage9.Click, lbtnPage10.Click
        ChangePage(CType(CType(sender, System.Web.UI.WebControls.LinkButton).CommandArgument, Integer))
    End Sub

#End Region

#Region " Add/Delete/Edit events and functions "

    Private Sub imbtnNewRec_Click(ByVal sender As System.Object, ByVal e As ImageClickEventArgs) Handles imbtnNewRec.Click
        Try
            Dim dtSource As DataTable = GetDataTableFromSession()
            Dim dv As DataView = GetDataViewFromSession()

            If Not dv Is Nothing Then
                ' need to switch off sorting on the dataview so that the added
                ' row will always be at the end
                dv.Sort = ""
                ' also disable any row filter in case it would prevent the new
                ' row being displayed
                dv.RowFilter = ""
            End If

            If Not dtSource Is Nothing Then
                dtSource.Rows.Add(dtSource.NewRow())
                ' rebind so we can get the correct page count
                Rebind()
            Else
                ' raise an event so the caller knows to add a record and rebind to
                ' the datasource
                RaiseEvent AddNew(Me, Nothing)
            End If

            ChangePage(m_dgPager.PageCount)
            txtEditState.Text = "NEW"
            SetSelectedIndex(m_dgPager.Items.Count - 1)
            m_dgPager.EditItemIndex = m_dgPager.SelectedIndex
            Rebind()
            EnableEditModeControls(True)

            SetEditColumnWidths()

        Catch ex As Exception
            DisplayError("Failed to add a new data grid row", ex)
        End Try
    End Sub

    Private Sub cmdEdit_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles cmdEdit.Click
        EditSelectedRow()
    End Sub

    Public Sub EditSelectedRow()
        Try
            Dim iRow As Integer

            iRow = m_dgPager.SelectedIndex

            If iRow >= 0 Then
                m_dgPager.EditItemIndex = iRow

                AddBookmark()

                Dim dtSource As DataTable = GetDataTableFromSession()
                If Not dtSource Is Nothing Then
                    Rebind()
                Else
                    ' raise an event so the caller knows to rebind to
                    ' the datasource
                    RaiseEvent Edit(Me, Nothing)
                End If

                EnableEditModeControls(True)
                txtEditState.Text = "EDIT"
                SetEditColumnWidths()
            End If

        Catch ex As Exception
            DisplayError("Failed to edit a data grid row", ex)
        End Try
    End Sub

    Private Sub cmdDel_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles cmdDel.Click
        Try
            Dim iRow As Integer

            iRow = m_dgPager.SelectedIndex

            If iRow >= 0 Then

                AddBookmark()

                Dim dtSource As DataTable = GetDataTableFromSession()
                If Not dtSource Is Nothing Then

                    ' the primary key must have been set on the grid and on the
                    ' DataTable for this to work:
                    Dim sKey As String = m_dgPager.DataKeys(m_dgPager.SelectedIndex).ToString()
                    Dim rowUpdate As DataRow = dtSource.Rows.Find(sKey)
                    If rowUpdate Is Nothing Then
                        Throw New Exception("No data row found with key """ & sKey & """")
                    End If

                    rowUpdate.Delete()

                    ' remove the current selection
                    SetSelectedIndex(-1)
                    Rebind()

                    RaiseEvent DataChanged(Me, Nothing)
                Else
                    ' remove the current selection
                    SetSelectedIndex(-1)
                    m_dgPager.DataBind()

                    ' raise an event so the caller knows to rebind to
                    ' the datasource
                    RaiseEvent Delete(Me, Nothing)
                End If

                ' update the page links in case we've reduced the page count
                Refresh()
            End If

        Catch ex As Exception
            DisplayError("Failed to delete a data grid row", ex)
        End Try
    End Sub

    Private Sub cmdSave_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles cmdSave.Click
        ' if we're in edit mode, then save the current record from the grid to
        ' the data source
        CommitEdit()

    End Sub

    Public Sub CommitEdit()

        Try
            If m_dgPager.EditItemIndex >= 0 Then

                AddBookmark()

                Dim dt As DataTable
                dt = GetDataTableFromSession()

                If dt Is Nothing Then
                    ' the caller hasn't given us the DataTable so we'll just notify
                    ' them so they can handle their own updating
                    RaiseEvent Save(Me, Nothing)
                    ' change the grid out of edit mode
                    m_dgPager.EditItemIndex = -1
                    m_dgPager.DataBind()
                    EnableEditModeControls(False)
                Else
                    ' save the updated data to the data source
                    If SaveRowData(dt) Then

                        ' Raise an event so that the user can select to continue editing
                        Dim arg As New DataGridPagerEventArgs()
                        arg.GridRow = Nothing
                        arg.DataTableRow = Nothing
                        arg.bCarryOnEditing = False
                        RaiseEvent BeforeDataChanged(Me, arg)

                        If arg.bCarryOnEditing = False Then
                            ' change the grid out of edit mode
                            m_dgPager.EditItemIndex = -1

                            EnableEditModeControls(False)
                        End If

                        RaiseEvent DataChanged(Me, Nothing)

                        ' Rebind now to make sure we display any dependent data
                        ' that has changed as a result of the page saving to the
                        ' database in the DataChanged event
                        Rebind()
                    Else
                        ' failed to save data - don't switch out of edit mode
                    End If
                End If
            End If

        Catch ex As Exception
            DisplayError("Failed to save a data grid update", ex)
        End Try

    End Sub

    Private Sub cmdCancel_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles cmdCancel.Click

        AddBookmark()
        Try
            Dim dt As DataTable
            dt = GetDataTableFromSession()

            If dt Is Nothing Then
                ' the caller hasn't given us the DataTable so we'll just notify
                ' them so they can handle their own cancelling
                RaiseEvent Cancel(Me, Nothing)
            Else
                ' the primary key must have been set on the grid and on the
                ' DataTable for this to work:
                Dim sKey As String = m_dgPager.DataKeys(m_dgPager.EditItemIndex).ToString()
                Dim rowUpdate As DataRow = dt.Rows.Find(sKey)
                If rowUpdate Is Nothing Then
                    Throw New Exception("No data row found with key """ & sKey & """")
                End If

                If rowUpdate.RowState = DataRowState.Added And txtEditState.Text = "NEW" Then
                    rowUpdate.Delete()
                End If
                'Make sure we revert any changes that could not be saved successfully
                If dt.HasErrors() Then
                    Dim rowItem As DataRow

                    For Each rowItem In dt.Rows
                        If rowItem.HasErrors() Then
                            rowItem.RejectChanges()
                        End If
                    Next
                End If
            End If

            ' change the grid out of edit mode
            m_dgPager.EditItemIndex = -1
            Rebind()

            EnableEditModeControls(False)

        Catch ex As Exception
            DisplayError("Failed to cancel a data grid update", ex)
        End Try

    End Sub

    Private Function SaveRowData(ByRef dt As DataTable) As Boolean
        With m_dgPager
            ' the primary key must have been set on the grid and on the
            ' DataTable for this to work:
            Dim sKey As String = .DataKeys(.EditItemIndex).ToString()
            Dim rowUpdate As DataRow = dt.Rows.Find(sKey)
            'Dim rowUpdate As DataRow = dt.Rows(.EditItemIndex)
            If rowUpdate Is Nothing Then
                Throw New Exception("No data row found with key """ & sKey & """")
            End If

            rowUpdate.BeginEdit()

            ' iterate over the columns in the grid and transfer values
            ' back to the dataset for any that are bound columns
            Dim iCol As Integer
            For iCol = 0 To .Items(.EditItemIndex).Cells.Count - 1

                Dim GridCol As DataGridColumn = .Columns(iCol)

                If TypeOf GridCol Is BoundColumn Then
                    Dim BoundCol As BoundColumn = CType(GridCol, BoundColumn)

                    Dim sFieldName As String = BoundCol.DataField
                    Dim ctl As Control

                    ' Make sure the bound column is not read only
                    If (.Items(.EditItemIndex).Cells(iCol).Controls.Count() > 0) Then
                        ctl = .Items(.EditItemIndex).Cells(iCol).Controls(0)
                        ' if we got a control and a field name, then we can save
                        ' the value.
                        If sFieldName <> "" And Not ctl Is Nothing Then
                            ' different operation dependent on the type of the
                            ' control
                            Dim sData As String

                            If TypeOf ctl Is TextBox Then
                                Dim txt As TextBox = CType(ctl, TextBox)
                                sData = txt.Text
                            End If

                            ' put the data value into the data table
                            Try
                                If sData = "" Then
                                    rowUpdate(sFieldName) = System.DBNull.Value
                                Else
                                    rowUpdate(sFieldName) = sData
                                End If
                            Catch
                                ' error saving the data - the value must be invalid.
                                Dim sMsg As String = "The value in column """
                                sMsg &= GridCol.HeaderText & """ is not valid."
                                sMsg &= "<br>Please enter a value of type: "
                                sMsg &= dt.Columns(sFieldName).DataType.Name
                                lblError.Text = sMsg
                                SetFocus(ctl, True)
                                rowUpdate.CancelEdit()
                                Return False
                            End Try
                        End If  ' sFieldName <> "" And Not ctl Is Nothing
                    End If ' Column not read only
                End If ' TypeOf GridCol Is BoundColumn
            Next iCol

            ' raise the SaveRow event to allow the containing page to save
            ' values to the dataset for any columns that are not data bound
            ' (e.g. template columns); and also to modify any values that
            ' we've just saved if necessary.
            Dim arg As New DataGridPagerEventArgs()
            arg.GridRow = .Items(.EditItemIndex)
            arg.DataTableRow = rowUpdate
            RaiseEvent RowSave(Me, arg)

            rowUpdate.EndEdit()
        End With

        Return True
    End Function

    ' Attempt to set the widths of the edit controls in bound columns to match
    ' the widths of the columns
    Private Sub SetEditColumnWidths()
        With m_dgPager
            Dim iCol As Integer
            For iCol = 0 To .Items(.EditItemIndex).Cells.Count - 1

                Dim GridCol As DataGridColumn = .Columns(iCol)

                If TypeOf GridCol Is BoundColumn Then
                    Dim ctl As Control

                    ' Make sure the bound column is not read only
                    If (.Items(.EditItemIndex).Cells(iCol).Controls.Count() > 0) Then

                        ctl = .Items(.EditItemIndex).Cells(iCol).Controls(0)
                        If Not ctl Is Nothing _
                        AndAlso TypeOf ctl Is TextBox _
                        AndAlso Not GridCol.ItemStyle.Width.IsEmpty Then
                            Dim txt As TextBox = CType(ctl, TextBox)
                            txt.Width = GridCol.ItemStyle.Width
                        End If
                    End If
                End If
            Next iCol
        End With
    End Sub

#End Region

#Region " Session/ViewState private functions "

    Private Function ViewStateGet(ByVal sID As String) As String
        If viewstate.Item(sID) Is Nothing Then
            Return ""
        Else
            Return CStr(viewstate.Item(sID))
        End If
    End Function

    Private Sub ViewStateSet(ByVal sID As String, _
                             ByVal sData As String)
        If viewstate.Item(sID) Is Nothing Then
            viewstate.Add(sID, sData)
        Else
            viewstate(sID) = sData
        End If
    End Sub

    ' Get the Viewstate ID used to store the session identifier
    ' for the data table
    Private ReadOnly Property ViewStateID_SessionTable() As String
        Get
            Return Me.ID & "_dtsessionid"
        End Get
    End Property

    ' Get the Viewstate ID used to store the session identifier
    ' for the data view
    Private ReadOnly Property ViewStateID_SessionView() As String
        Get
            Return Me.ID & "_dvsessionid"
        End Get
    End Property

    Private Function GetDataTableFromSession() As DataTable
        Dim sDataTableSessionID As String _
            = CType(viewstate.Item(ViewStateID_SessionTable), String)

        If sDataTableSessionID Is Nothing OrElse sDataTableSessionID = "" Then
            Return Nothing
        Else
            Dim dt As DataTable
            dt = CType(Session.Item(sDataTableSessionID), DataTable)
            Return dt
        End If
    End Function

    Private Function GetDataViewFromSession() As DataView
        Dim sDataViewSessionID As String _
            = CType(viewstate.Item(ViewStateID_SessionView), String)

        If sDataViewSessionID Is Nothing OrElse sDataViewSessionID = "" Then
            Return Nothing
        Else
            Dim dv As DataView
            dv = CType(Session.Item(sDataViewSessionID), DataView)
            Return dv
        End If
    End Function

#End Region

#Region " Enable controls private functions "

    Private Sub EnableEditModeControls(ByVal bEnable As Boolean)
        EnableEditDelButtons(Not bEnable)
        EnableNavButtons(Not bEnable)
        EnablePageLinks(Not bEnable)
        cmdSave.Visible = bEnable
        cmdCancel.Visible = bEnable

        If Not bEnable Then
            ' clear the error text
            lblError.Text = ""
        End If
    End Sub

    Private Sub EnableEditDelButtons(ByVal bValue As Boolean)
        cmdEdit.Enabled = bValue And cmdEdit.Visible And m_dgPager.SelectedIndex >= 0
        cmdEdit.ImageUrl = CStr(IIf(cmdEdit.Enabled, "Images/GridPager/edit.gif", "Images/GridPager/edit_disabled.gif"))
        cmdDel.Enabled = bValue And cmdDel.Visible And m_dgPager.SelectedIndex >= 0
        cmdDel.ImageUrl = CStr(IIf(cmdDel.Enabled, "Images/GridPager/del.gif", "Images/GridPager/del_disabled.gif"))
        imbtnNewRec.Enabled = bValue And imbtnNewRec.Visible
        imbtnNewRec.ImageUrl = CStr(IIf(imbtnNewRec.Enabled, "Images/GridPager/new.gif", "Images/GridPager/new_disabled.gif"))
    End Sub

    Private Sub EnableNavButtons(ByVal bValue As Boolean)
        imbtnPrevPage.Enabled = bValue AndAlso (m_dgPager.CurrentPageIndex > 0)
        imbtnPrevPage.ImageUrl = CStr(IIf(imbtnPrevPage.Enabled, "Images/GridPager/rew.gif", "Images/GridPager/rew_disabled.gif"))
        imbtnNextPage.Enabled = bValue AndAlso (m_dgPager.CurrentPageIndex < m_dgPager.PageCount - 1)
        imbtnNextPage.ImageUrl = CStr(IIf(imbtnNextPage.Enabled, "Images/GridPager/fwd.gif", "Images/GridPager/fwd_disabled.gif"))
        imbtnFirstPage.Enabled = bValue AndAlso (m_dgPager.CurrentPageIndex > 0)
        imbtnFirstPage.ImageUrl = CStr(IIf(imbtnFirstPage.Enabled, "Images/GridPager/frew.gif", "Images/GridPager/frew_disabled.gif"))
        imbtnLastPage.Enabled = bValue AndAlso (m_dgPager.CurrentPageIndex < m_dgPager.PageCount - 1)
        imbtnLastPage.ImageUrl = CStr(IIf(imbtnLastPage.Enabled, "Images/GridPager/ffwd.gif", "Images/GridPager/ffwd_disabled.gif"))
    End Sub

    Private Sub EnablePageLinks(ByVal bValue As Boolean)
        lbtnPage1.Enabled = bValue
        lbtnPage2.Enabled = bValue
        lbtnPage3.Enabled = bValue
        lbtnPage4.Enabled = bValue
        lbtnPage5.Enabled = bValue
        lbtnPage6.Enabled = bValue
        lbtnPage7.Enabled = bValue
        lbtnPage8.Enabled = bValue
        lbtnPage9.Enabled = bValue
        lbtnPage10.Enabled = bValue
    End Sub

#End Region

#Region " Other private helper functions "

    Private Sub SetPageText(ByVal lbtnControl As LinkButton, ByVal intPage As Integer)
        ' Set the Text for the Paging Buttons, and their ALT text for mouseover.
        lbtnControl.CommandArgument = CStr(intPage)
        lbtnControl.ToolTip = "Page " & intPage
        lbtnControl.Text = CStr(intPage)

        If (intPage - 1) = m_dgPager.CurrentPageIndex Then
            lbtnControl.Enabled = False
        Else
            lbtnControl.Enabled = True
        End If
    End Sub

    Private Sub DisplayError(ByVal sDescription As String, ByVal ex As Exception)

        Dim sMsg As String = sDescription

        Try
            sMsg &= " on page " & Page.Request.Url.AbsolutePath
            sMsg &= " using GridPager " & Me.ID
        Finally
            clsAppError.DisplayError(sMsg, ex)
        End Try

    End Sub

    Public Function SelectGridRowForDataRow(ByRef dr As DataRow) As Boolean
        Try
            Dim dt As DataTable = GetDataTableFromSession()

            If dt Is Nothing Then
                Return False
            End If

            Dim acolKeys() As DataColumn = dt.PrimaryKey

            If acolKeys.GetLength(0) <> 1 Then
                Return False
            End If

            Dim colKey As DataColumn = acolKeys(0)
            Dim objKey As Object = dr.Item(colKey)

            Dim iRow As Integer
            For iRow = 0 To dt.Rows.Count - 1
                If dt.Rows(iRow).Item(colKey) Is objKey Then
                    Return SelectGridRowForDataRow(dt, iRow)
                End If
            Next iRow

            Return False

        Catch ex As Exception
            DisplayError("Failed to select a row in the grid corresponding to a specified DataTable row", ex)
        End Try
    End Function

    ' Finds the grid row associated with the specified row in a DataTable,
    ' and selects it if the row is found.
    ' Returns True if the row was found and selected; False otherwise.
    ' Assumes that a single primary key column has been set on both the grid
    ' and the DataTable.
    Public Function SelectGridRowForDataRow(ByRef dt As DataTable, _
                                             ByVal iDataRow As Integer) As Boolean

        ' first find the primary key value of the data row.
        Dim acolKeys() As DataColumn = dt.PrimaryKey
        If acolKeys.GetLength(0) = 1 Then
            Dim colKey As DataColumn = acolKeys(0)
            Dim objKey As Object = dt.Rows(iDataRow).Item(colKey)

            ' store the current page and selected item so we can restore the
            ' values if we don't find a match.
            Dim iOldPage As Integer = m_dgPager.CurrentPageIndex
            Dim iOldSelection As Integer = m_dgPager.SelectedIndex

            Dim sOldSort As String
            Dim sOldFilter As String
            Dim dv As DataView = GetDataViewFromSession()
            If Not dv Is Nothing Then
                ' remove any filtering and sorting otherwise
                ' we might not be able to find the DataTable
                ' row in the grid
                dv.Sort = ""
                dv.RowFilter = ""
                Rebind()
            End If

            ' Estimate which page of the grid the row is likely to be on.
            ' Don't forget to use with the ChangePage function, iPage
            ' will be 1-based.
            Dim iPage As Integer = iDataRow \ m_dgPager.PageSize + 1

            If iPage > m_dgPager.PageCount Then
                iPage = m_dgPager.PageCount
            End If

            ' search backwards from the estimated page location
            ' (in case prior rows have been deleted, moving the
            ' row we're looking for to an earlier page)
            Dim iFoundGridRow As Integer = -1

            While iFoundGridRow < 0 And iPage > 0
                ChangePage(iPage)
                ' check each row on this page to see if it matches the key
                Dim iPageRow As Integer
                For iPageRow = 0 To m_dgPager.DataKeys.Count - 1
                    If CInt(m_dgPager.DataKeys(iPageRow)) = CInt(objKey) Then
                        iFoundGridRow = iPageRow
                        Exit For
                    End If
                Next iPageRow
                iPage -= 1
            End While

            If iFoundGridRow > -1 Then
                ' we found the row, so select it
                SetSelectedIndex(iFoundGridRow)
                m_dgPager.DataBind()
                Return True
            Else
                ' we didn't get a match so revert to old page and selection
                If Not dv Is Nothing Then
                    dv.Sort = sOldSort
                    dv.RowFilter = sOldFilter
                    Rebind()
                End If
                ChangePage(iOldPage)
                SetSelectedIndex(iOldSelection)
                m_dgPager.DataBind()
                Return False
            End If
        Else
            ' did not find a single primary key column in data table
            Return False
        End If
    End Function

    Private Sub SetSelectedIndex(ByVal iIndex As Integer)
        If iIndex <> m_dgPager.SelectedIndex Then
            m_dgPager.SelectedIndex = iIndex
            RaiseEvent SelectionChanged(Me, Nothing)
        End If
    End Sub

    Private Function GetSortExpressions(ByVal sSortExpression As String, ByRef sNewSortAsc As String, ByRef sNewSortDesc As String)
        Dim strArray() As String = sSortExpression.Split(",")
        Dim iCount As Integer
        Dim iElements As Integer = 0

        'Check if the sort criteria for the column is a comma delimited list
        If sSortExpression.IndexOf(",") >= 0 Then
            iElements = strArray.Length()
            For iCount = 0 To iElements - 1
                If iCount = iElements - 1 Then
                    sNewSortAsc = sNewSortAsc & strArray(iCount).ToString() & " ASC"
                    sNewSortDesc = sNewSortDesc & strArray(iCount).ToString() & " DESC"
                Else
                    sNewSortAsc = sNewSortAsc & strArray(iCount).ToString() & " ASC,"
                    sNewSortDesc = sNewSortDesc & strArray(iCount).ToString() & " DESC,"
                End If
            Next
        Else
            sNewSortAsc = sSortExpression & " ASC"
            sNewSortDesc = sSortExpression & " DESC"
        End If
    End Function

#End Region

#Region " Other event handlers "

    Private Sub m_dgPager_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_dgPager.SelectedIndexChanged
        Dim bEnable As Boolean = m_dgPager.SelectedIndex >= 0
        If cmdEdit.Visible And Not cmdSave.Visible Then
            cmdEdit.Enabled = bEnable
            cmdEdit.ImageUrl = "Images/GridPager/edit.gif"
        End If
        If cmdDel.Visible And Not cmdSave.Visible Then
            cmdDel.Enabled = bEnable
            cmdDel.ImageUrl = "Images/GridPager/del.gif"
        End If
    End Sub

    Private Sub m_dgPager_SortCommand(ByVal source As Object, ByVal e As System.Web.UI.WebControls.DataGridSortCommandEventArgs) Handles m_dgPager.SortCommand
        Dim dv As DataView = GetDataViewFromSession()
        ' don't allow resorting when in edit mode
        If m_dgPager.EditItemIndex = -1 And Not dv Is Nothing Then
            If e.SortExpression = "" Then
                dv.Sort = ""
            Else
                Dim sNewSortAsc As String
                Dim sNewSortDesc As String
                Dim sNewSort As String
                sNewSort = e.SortExpression

                GetSortExpressions(sNewSort, sNewSortAsc, sNewSortDesc)

                ' if the new sort order matches the current one, then just
                ' reverse the sort order; otherwise, apply the new sort order
                ' as is.
                If dv.Sort = sNewSort Or dv.Sort = sNewSortAsc Then
                    dv.Sort = sNewSortDesc
                ElseIf dv.Sort = sNewSortDesc Then
                    dv.Sort = sNewSortAsc
                Else
                    dv.Sort = sNewSort
                End If
            End If

            SetSelectedIndex(-1)
            Rebind()
            Refresh()
        End If
    End Sub

#End Region

#Region "Bookmarking"

    Private Sub OnGridItemCommand(ByVal source As Object, ByVal e As DataGridCommandEventArgs)

        AddBookmark()

    End Sub

    Private Sub AddBookmark()

        If (mbEnableBookmarking) Then
            litAnchor.Text = "<a name=""" + m_dgPager.ID.ToString() + """>"
            InsertScriptBlock()
        End If

    End Sub
    Private Sub InsertScriptBlock()

        Dim jScript As System.Text.StringBuilder = New System.Text.StringBuilder()
        jScript.Append("<script language=""JavaScript"">")
        jScript.Append("location.href=""#")
        jScript.Append(m_dgPager.ID.ToString())
        jScript.Append(""";")
        jScript.Append("</script>")

        Page.RegisterClientScriptBlock("Bookmark", jScript.ToString())

    End Sub

#End Region

End Class
