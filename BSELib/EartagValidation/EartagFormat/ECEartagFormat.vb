Friend Class ECEartagFormat
    Inherits EartagFormatBase

    Private Const mAnimalPattern As String = "^[0-9A-Z]{1,12}$"

    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 11
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "European Community Format"
        End Get
    End Property

    Public Overrides Function GetPresentationFormat(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        Return countryCode & " " & animalComponent

    End Function

    Public Overrides Function Validate(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        If Not Eartag.IsECCountryCode(countryCode) Then
            Return "Country code '" & countryCode & "' not recognised"
        End If

        If Not Eartag.IsRegexPatternMatch(mAnimalPattern, animalComponent) Then
            Return "Animal component is invalid: It should contain 1 to 12 numerical or uppercase alphabetical characters"
        End If

        Return String.Empty

    End Function

End Class
