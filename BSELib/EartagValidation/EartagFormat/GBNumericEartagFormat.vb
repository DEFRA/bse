Friend Class GBNumericEartagFormat
    Inherits UKNonNINumericEartagFormat

    Private mUKGeographicCodes As String() = {"58", "59", "22", "23", "70", "71", "72", "73", "10", "11", "74", "75", "24", "25", "36", "37", "56", "57", "32", "33", "50", "51", "52", "53", "12", "13", "20", "21", "14", "15", "54", "55", "18", "19", "28", "29", "26", "27", "16", "17", "34", "35", "38", "39", "30", "31"}

    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 2
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "Mainland GB Numeric"
        End Get
    End Property

    Public Overrides ReadOnly Property FormatLocation() As String
        Get
            Return "Mainland GB"
        End Get
    End Property

    Public Overrides Function ValidateUKEartag(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        Dim animalPattern As String = "\d{2,6}"
        Dim herdPattern As String = "\d{3,6}"

        If Not Eartag.IsRegexPatternMatch(herdPattern, herdComponent) Then
            Return "Herd component is invalid: It should contain 3 to 6 numerical digits"
        End If

        If Not IsUKGeographicCode(Mid(herdComponent, 1, 2)) Then
            Return "Herd component is invalid: UK geographic code not recognised"
        End If

        If CInt(Mid(herdComponent, 3)) <= 0 Then
            Return "Herd component is invalid: Digits after the geographic code are all zero"
        End If

        If Not Eartag.IsRegexPatternMatch(animalPattern, animalComponent) Then
            Return "Animal component is invalid: It should contain 2 to 6 numerical digits"
        End If

        Dim modulusString As String = herdComponent & Mid(animalComponent, 2)
        Dim modulus As Integer = CalculateModulus(modulusString, CStr(7))
        If Left(animalComponent, 1) <> CStr(modulus + 1) Then
            Return "Eartag is invalid: The eartag checksum failed"
        End If
        If CInt(CDbl(Mid(animalComponent, 2, Len(animalComponent)))) = 0 Then
            Return "Animal component is invalid: All digits in position 2 onwards are zero"
        End If

        Return String.Empty


    End Function


    Protected Function IsUKGeographicCode(ByVal testCode As String) As Boolean

        For Each currentCode As String In mUKGeographicCodes
            If testCode = currentCode Then
                Return True
            End If
        Next

        Return False

    End Function

End Class
