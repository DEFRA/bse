Friend Class NINumericEartagFormat
    Inherits NIEartagFormat

    Private Const mHerdPattern As String = "^(9)[0-9]{3,6}$"
    Private Const mAnimalPattern As String = "^[0-9]{2,5}$"
   
    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 10
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "Northern Ireland Numeric"
        End Get
    End Property

    Public Overrides Function GetPresentationFormat(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        Dim animalPartOne As String = Mid(animalComponent, 1, Len(animalComponent) - 1)
        Dim animalPartTwo As String = Mid(animalComponent, Len(animalComponent), 1)
        Return countryCode & " " & herdComponent & " " & animalPartOne & " " & animalPartTwo

    End Function

    Public Overrides Function ValidateUKEartag(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        If Not Eartag.IsRegexPatternMatch(mHerdPattern, herdComponent) Then
            Return "Herd component is invalid: It should consist of '9' followed by 3 to 6 numerical digits"
        End If

        If CInt(Mid(herdComponent, 4)) = 0 Then
            Return "Herd component is invalid: All digits in position 4 onwards are zero"
        End If

        If Not IsElectoralCode(Mid(herdComponent, 2, 2)) Then
            Return "Herd component is invalid: NI Electoral Code not recognised"
        End If

        If Not Eartag.IsRegexPatternMatch(mAnimalPattern, animalComponent) Then
            Return "Animal component is invalid: It should consist of 2 to 5 numerical digits"
        End If

        If Not IsCheckDigitFormat10(herdComponent, animalComponent) Then
            Return "Eartag is invalid: The eartag checksum failed"
        End If

        If Mid(animalComponent, 1, Len(animalComponent) - 1) = "0" Then
            Return "Animal component is invalid: If it contains 2 characters, the first should not be zero"
        End If

        Return String.Empty

    End Function

    Private Function IsCheckDigitFormat10(ByVal herdComponent As String, ByVal animalComponent As String) As Boolean

        Dim herdMark As String = Mid(herdComponent, 1, 3)
        Dim herdNumber As String = Mid(herdComponent, 4)
        Dim animalNumber As String = Mid(animalComponent, 1, Len(animalComponent) - 1)

        herdNumber = PadZero(herdNumber)
        animalNumber = PadZero(animalNumber)

        Dim checkString As String = herdMark & herdNumber & animalNumber
        Dim modulus As Integer = CalculateModulus(checkString, "7") + 1

        return CStr(modulus) = Right(animalComponent, 1)

    End Function

End Class
