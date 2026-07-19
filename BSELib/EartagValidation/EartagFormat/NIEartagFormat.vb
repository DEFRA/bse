Friend MustInherit Class NIEartagFormat
    Inherits UKEartagFormat

    Private mElectoralCodes As String() = {"18", "20", "21", "25", "30", "12", "16", "17", "14", "15", "49", "50", "51", "56", "58", "59", "61", "45", "46", "47", "48", "02", "04", "06", "10", "52", "53", "54", "55", "24", "27", "40", "41", "42", "43", "31", "33", "35", "37", "38", "39", "57", "63", "64", "65", "66"}

    Public Overrides ReadOnly Property FormatLocation As String
        Get
            Return "Northern Ireland"
        End Get
    End Property

    Protected Function IsElectoralCode(ByVal testCode As String) As Boolean

        For Each currentElectoralCode As String In mElectoralCodes
            If testCode = currentElectoralCode Then
                Return True
            End If
        Next

        Return False

    End Function

    Protected Function PadZero(ByVal part As String) As String

        If Len(part) < 4 Then
            Return Mid("0000", 1, 4 - Len(part)) & part
        Else
            Return part
        End If

    End Function

End Class
