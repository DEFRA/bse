Friend Class JerseyAlphaNumericEartagFormat
    Inherits UKNonNIAlphaNumericEartagFormat

    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 3
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "Jersey Alpha-Numeric"
        End Get
    End Property

    Public Overrides ReadOnly Property FormatLocation() As String
        Get
            Return "Jersey"
        End Get
    End Property

    Public Overrides Function ValidateUKEartag(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        Dim herdPattern As String = "^(JY)[0-9]{1,4}$"
        Dim animalPattern As String = "^[0-9]{1,5}$"
        Dim animalNumericPattern As String = "^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$"

        If Not Eartag.IsRegexPatternMatch(herdPattern, herdComponent) Then
            Return "Herd component is invalid: It should consist of 'JY' followed by 1 to 4 numeric digits"
        End If

        If CInt(Mid(herdComponent, 3)) <= 0 Then
            Return "Herd component is invalid: All digits in position 3 onwards are zero"
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
