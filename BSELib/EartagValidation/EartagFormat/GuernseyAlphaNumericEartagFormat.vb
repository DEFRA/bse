Friend Class GuernseyAlphaNumericEartagFormat
    Inherits UKNonNIAlphaNumericEartagFormat

    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 5
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "Guernsey Alpha-Numeric"
        End Get
    End Property

    Public Overrides ReadOnly Property FormatLocation As String
        Get
            Return "Guernsey"
        End Get
    End Property

    Public Overrides Function ReformatHerdComponent(ByVal herdComponent As String) As String

        Return herdComponent

    End Function

    Public Overrides Function ValidateUKEartag(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        Dim herdPattern As String = "^(GY)[0-9]{1}$"
        Dim animalPattern As String = "^[0-9]{1,5}$"
        Dim animalNumericPattern As String = "^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$"

        If Not Eartag.IsRegexPatternMatch(herdPattern, herdComponent) Then
            Return "Herd component is invalid: It should consist of 'GY' followed by 1 numeric digit"
        End If

        If Mid(herdComponent, 3) = "0" Then
            Return "Herd component is invalid: Digit at position 3 should not be zero"
        End If

        If Not Eartag.IsRegexPatternMatch(animalPattern, animalComponent) Then
            Return "Animal component is invalid: It should consist of 1 to 5 numerical digits"
        End If

        If Eartag.IsRegexPatternMatch(animalNumericPattern, animalComponent) Then
            Return "Animal component is invalid: The numerical part contains only zeros"
        End If

        Return String.Empty

    End Function

End Class
