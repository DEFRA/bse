Friend Class IsleOfManAlphaNumericEartagFormat
    Inherits UKNonNIAlphaNumericEartagFormat

    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 7
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "Isle of Man Alpha-Numeric"
        End Get
    End Property

    Public Overrides ReadOnly Property FormatLocation() As String
        Get
            Return "Isle of Man"
        End Get
    End Property

    Public Overrides Function ReformatHerdComponent(ByVal herdComponent As String) As String

        Dim partA As String = Mid(herdComponent, 1, 2)
        Dim partB As String = Mid(herdComponent, 3)

        If Len(partB) < 3 Then
            partB = Mid("000", 1, 3 - Len(partB)) & partB
        End If

        Return partA & partB

    End Function

    Public Overrides Function ValidateUKEartag(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        Dim strHerd As String = "^(MN)[0-9]{1,3}$"
        Dim strAnimal As String = "^[0-9]{1,5}$"
        Dim strAnimalNum As String = "^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$"

        If Not Eartag.IsRegexPatternMatch(strHerd, herdComponent) Then
            Return "Herd component is invalid: It should consist of 'MN' followed by 1 to 3 numerical digits"
        End If

        If CInt(Mid(herdComponent, 3)) <= 0 Then
            Return "Herd component is invalid: All digits in position 3 onwards are zero"
        End If

        If Not Eartag.IsRegexPatternMatch(strAnimal, animalComponent) Then
            Return "Animal component is invalid: It should consist of 1 to 5 numerical digits"
        End If

        If Eartag.IsRegexPatternMatch(strAnimalNum, animalComponent) Then
            Return "Animal component is invalid: The numerical part contains only zeros"
        End If

        Return String.Empty

    End Function

End Class
