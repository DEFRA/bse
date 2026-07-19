Friend Class PreBarimoEartagFormat
    Inherits EartagFormatBase

    Private Const mHerdPattern As String = "^[A-Z]{1,2}[0-9]{1,4}$"
    Private Const mAnimalPattern As String = "^[0-9]{1,5}[A-Z]{0,1}$"

    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 12
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "Pre-BARIMO"
        End Get
    End Property

    Public Overrides Function GetPresentationFormat(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        Return herdComponent & " " & animalComponent

    End Function


    Public Overrides Function Validate(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        If countryCode.Length > 0 Then
            Return "Country code should be empty for Pre-BARIMO eartags"
        End If

        If Not Eartag.IsRegexPatternMatch(mHerdPattern, herdComponent) Then
            Return "Herd component is invalid: It should contain 1 or 2 uppercase alphabetical characters followed by 1 to 4 numeric characters"
        End If

        If Mid(herdComponent, 3) = "0" Then
            Return "Herd component is invalid: All digits in position 3 onwards are zero"
        End If

        If Not Eartag.IsRegexPatternMatch(mAnimalPattern, animalComponent) Then
            Return "Animal component is invalid: It should contain 1 to 5 numeric characters, optionally followed by 1 uppercase character"
        End If

        If animalComponent = "0" Then
            Return "The animal component is set to zero"
        End If

        Return String.Empty
        
    End Function

End Class
