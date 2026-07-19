Public Class clsDataCheck
    Public Function DataSetHasChanges(ByRef dsData As DataSet) As Boolean
        Dim iTableCount As Integer
        Dim dsChanges As DataSet

        If Not dsData Is Nothing Then
            dsChanges = dsData.GetChanges()

            If Not dsChanges Is Nothing Then
                For iTableCount = 0 To dsChanges.Tables.Count - 1
                    If DataTableHasChanges(dsChanges.Tables(iTableCount)) Then
                        Return True
                    End If
                Next
            End If
        End If

        Return False
    End Function

    Public Function DataTableHasChanges(ByRef dtData As DataTable) As Boolean
        Dim iRowCount As Integer
        Dim iColumnCount As Integer
        Dim dtChanges As DataTable

        If Not dtData Is Nothing Then

            dtChanges = dtData.GetChanges()

            If Not dtChanges Is Nothing Then
                For iRowCount = 0 To dtChanges.Rows.Count - 1
                    If dtChanges.Rows(iRowCount).RowState = DataRowState.Added Or dtChanges.Rows(iRowCount).RowState = DataRowState.Deleted Then
                        Return True
                    ElseIf dtChanges.Rows(iRowCount).RowState = DataRowState.Modified Then
                        For iColumnCount = 0 To dtChanges.Columns.Count - 1
                            If dtChanges.Rows(iRowCount)(iColumnCount, DataRowVersion.Current).ToString <> dtChanges.Rows(iRowCount)(iColumnCount, DataRowVersion.Original).ToString Then
                                Return True
                            End If
                        Next
                    End If
                Next
            End If
        End If

        Return False
    End Function

End Class
