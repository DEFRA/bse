Friend Class GBAlphaNumericEartagFormat
    Inherits UKNonNIAlphaNumericEartagFormat

    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 1
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "Mainland GB Alpha-Numeric"
        End Get
    End Property

    Public Overrides ReadOnly Property FormatLocation As String
        Get
            Return "Mainland GB"
        End Get
    End Property

    Public Overrides Function ValidateUKEartag(countryCode As String, herdComponent As String, animalComponent As String) As String

        Dim herdPattern As String = "^[a-zA-Z]{1,2}[0-9]{1,4}$"
        Dim animalPattern As String = "^[a-zA-Z]{0,1}[0-9]{1,5}[a-zA-Z]{0,1}$"
        Dim animalNumericPattern As String = "^[a-zA-Z]{0,1}[0]{1,5}[a-zA-Z]{0,1}$"
        Dim firstCharPattern As String = "^[a-zA-Z]{1}$"
        Dim invalidCharList As String = "I,O,P,R,U,X"

        If Not Eartag.IsRegexPatternMatch(herdPattern, herdComponent) Then
            Return "Herd component is not valid: It should consist of 1 or 2 alphabetical characters followed by 1 to 4 numerical digits"
        End If

        If CInt(Mid(herdComponent, IndexOfNumeric(2, herdComponent))) <= 0 Then
            Return "Herd component is invalid: All digits in position 2 onwards are zero"
        End If

        If Not Eartag.IsRegexPatternMatch(animalPattern, animalComponent) Then
            Return "Animal component is invalid: It should consist of an optional alphabetical character, then 1 to 5 numerical digits, then an optional alphabetical character"
        End If

        If Eartag.IsRegexPatternMatch(firstCharPattern, Left(animalComponent, 1)) AndAlso
            Mid(animalComponent, 1, 1) <> "X" AndAlso Mid(animalComponent, 1, 1) <> "R" Then
            Return "Animal component is invalid: The first character must be numeric, 'X' or 'R'"
        End If

        If CheckString(Right(animalComponent, 1), invalidCharList) Then
            Return "Animal component is invalid: The last character cannot be I, O, P, R, U or X"
        End If

        If Eartag.IsRegexPatternMatch(animalNumericPattern, animalComponent) Then
            Return "Animal component is invalid: The numerical part contains only zeros"
        End If

        Return String.Empty

    End Function

    Private Function IndexOfNumeric(ByVal lngStart As Integer, ByVal strString As String) As Integer

        Dim ChrString As String
        ChrString = "0123456789"
        Dim strNode As String
        Dim lngCount As Integer

        For lngCount = lngStart To Len(strString)
            strNode = Mid(strString, lngCount, 1)
            If (InStr(ChrString, strNode) > 0) Then
                IndexOfNumeric = lngCount
                Exit For
            End If
        Next
    End Function

    Private Function CheckString(ByVal strCheck As String, ByVal strSplit As String) As Boolean

        Dim strNode As String

        For Each strNode In Split(strSplit, ",")
            If (strCheck = strNode) Then
                Return True
            End If

        Next strNode

        Return False

    End Function


End Class
