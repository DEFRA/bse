Friend Class NIAlphaNumericEartagFormat
    Inherits NIEartagFormat

    Private Const mHerdPattern As String = "^[0-9]{3,6}$"
    Private Const mAnimalPattern As String = "^[0-9]{1,4}[A-Z]{1}$"

    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 9
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "Northern Ireland Alpha-Numeric"
        End Get
    End Property

    Public Overrides Function GetPresentationFormat(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        Dim animalPartOne As String = Mid(animalComponent, 1, Len(animalComponent) - 1)
        animalPartOne = Replace(animalPartOne, "-", "")
        Dim animalPartTwo As String = Mid(animalComponent, Len(animalComponent), 1)
        Return countryCode & " " & herdComponent & " " & animalPartOne & "-" & animalPartTwo

    End Function

    Public Overrides Function ValidateUKEartag(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        If Not Eartag.IsRegexPatternMatch(mHerdPattern, herdComponent) Then
            Return "Herd component is invalid: It should consist of 3 to 6 numerical digits"
        End If

        If Not IsElectoralCode(Mid(herdComponent, 1, 2)) Then
            Return "Herd component is invalid: NI Electoral Code not recognised"
        End If

        If CInt(Mid(herdComponent, 3)) = 0 Then
            Return "Herd component is invalid: All digits in position 3 onwards are zero"
        End If

        If Not Eartag.IsRegexPatternMatch(mAnimalPattern, animalComponent) Then
            Return "Animal component is invalid: It should consist of 1 to 4 numerical digits followed by one uppercase character"
        End If

        If Not IsCheckDigitFormat9(herdComponent, animalComponent) Then
            Return "Eartag is invalid: The eartag checksum failed"
        End If

        If Mid(animalComponent, 1, Len(animalComponent) - 1) = "0" Then
            Return "Animal component is invalid: If it contains 2 characters, the first should not be zero"
        End If

        Return String.Empty

    End Function

    Private Function IsCheckDigitFormat9(ByVal herdComponent As String, ByVal animalComponent As String) As Boolean

        Dim electoralID As String = Mid(herdComponent, 1, 2)
        Dim herdNumber As String = PadZero(Mid(herdComponent, 3))
        Dim animalNumber As String = Left(animalComponent, Len(animalComponent) - 1)

        Dim checkString As String = electoralID & herdNumber
        Dim checkNumber As Double = CDbl(checkString) * 10000
        checkNumber = checkNumber + CInt(animalNumber)

        Dim modulus As Integer = CalculateModulus(CStr(checkNumber), "23")

        Dim checkCharacters As String() = {"A", "B", "C", "D", "E", "F", "H", "I", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"}
        checkString = checkCharacters(modulus)

        Return Right(animalComponent, 1) = checkString

    End Function

End Class
