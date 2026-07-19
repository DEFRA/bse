Friend MustInherit Class UKNonNIAlphaNumericEartagFormat
    Inherits UKEartagFormat

    Public Overrides Function ReformatHerdComponent(ByVal herdComponent As String) As String

        Const twoCharPattern As String = "^[A-Z]{2}$"
        Dim partA As String
        Dim partB As String

        If (Eartag.IsRegexPatternMatch(twoCharPattern, Mid(herdComponent, 1, 2))) Then

            partA = Mid(herdComponent, 1, 2)
            partB = Mid(herdComponent, 3)

            If Len(partB) < 4 Then
                partB = Mid("0000", 1, 4 - Len(partB)) & partB
            End If

            Return partA & partB

        Else

            partA = Mid(herdComponent, 1, 1)
            partB = Mid(herdComponent, 2)

            If Len(partB) < 4 Then
                partB = Mid("0000", 1, 4 - Len(partB)) & partB
            End If

            Return partA & partB

        End If

    End Function

    Public Overrides Function ReformatAnimalComponent(ByVal animalComponent As String) As String

        Const animalPattern1 As String = "^[a-zA-Z]{1}[0-9]{1,5}[a-zA-Z]{1}$"
        Const animalPattern2 As String = "^[a-zA-Z]{1}[0-9]{1,5}$"
        Const animalPattern3 As String = "^[0-9]{1,5}[a-zA-Z]{1}$"

        Dim partA As String
        Dim partB As String

        If (Eartag.IsRegexPatternMatch(animalPattern1, animalComponent)) Then

            partA = Mid(animalComponent, 1, 1)
            partB = Mid(animalComponent, 2)

            If Len(partB) < 6 Then
                partB = Mid("000000", 1, 6 - Len(partB)) & partB
            End If

            Return partA & partB

        ElseIf (Eartag.IsRegexPatternMatch(animalPattern2, animalComponent)) Then

            partA = Mid(animalComponent, 1, 1)
            partB = Mid(animalComponent, 2)

            If Len(partB) < 5 Then
                partB = Mid("00000", 1, 5 - Len(partB)) & partB
            End If

            Return partA & partB

        ElseIf (Eartag.IsRegexPatternMatch(animalPattern3, animalComponent)) Then

            Return Mid("000000", 1, 6 - Len(animalComponent)) & animalComponent

        Else

            If Len(animalComponent) < 5 Then
                Return Mid("00000", 1, 5 - Len(animalComponent)) & animalComponent
            End If

        End If

        Return animalComponent

    End Function

End Class
