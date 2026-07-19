Friend Class IsleOfManNumericEartagFormat
    Inherits UKNonNINumericEartagFormat

    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 8
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "Isle of Man Numeric"
        End Get
    End Property

    Public Overrides ReadOnly Property FormatLocation() As String
        Get
            Return "Isle of Man"
        End Get
    End Property

    Public Overrides Function ValidateUKEartag(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        Dim herdPattern As String = "^(01)[0-9]{1,4}$"
        Dim animalPattern As String = "^[0-9]{2,6}$"

        If Not Eartag.IsRegexPatternMatch(herdPattern, herdComponent) Then
            Return "Herd component invalid: It should consist of 3 to 6 numerical digits, starting with '01'"
        End If

        If CInt(Mid(herdComponent, 3)) <= 0 Then
            Return "Herd component is invalid: All digits in position 3 onwards are zero"
        End If

        If Not Eartag.IsRegexPatternMatch(animalPattern, animalComponent) Then
            Return "Animal component is invalid: It should contain 2 to 6 numerical digits"
        End If

        Dim modulusString As String = herdComponent & Mid(animalComponent, 2)
        Dim modulus As Integer = CalculateModulus(modulusString, CStr(7))
        If Left(animalComponent, 1) <> CStr(modulus + 1) Then
            Return "Eartag is invalid: The eartag checksum failed"
        End If

        If CInt(Mid(animalComponent, 2, Len(animalComponent))) = 0 Then
            Return "Animal component is invalid: All digits in position 2 onwards are zero"
        End If

        Return String.Empty

    End Function

End Class
