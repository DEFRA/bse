Friend MustInherit Class UKNonNINumericEartagFormat
    Inherits UKEartagFormat

    Public Overrides Function ReformatHerdComponent(ByVal herdComponent As String) As String

        Dim partA As String = Mid(herdComponent, 1, 2)
        Dim partB As String = Mid(herdComponent, 3)

        If Len(partB) < 4 Then
            partB = Mid("0000", 1, 4 - Len(partB)) & partB
        End If

        Return partA & partB

    End Function

    Public Overrides Function ReformatAnimalComponent(ByVal animalComponent As String) As String

        Dim partA As String = Mid(animalComponent, 1, 1)
        Dim partB As String = Mid(animalComponent, 2)

        If Len(partB) < 5 Then
            partB = Mid("00000", 1, 5 - Len(partB)) & partB
        End If

        Return partA & partB

    End Function

End Class
