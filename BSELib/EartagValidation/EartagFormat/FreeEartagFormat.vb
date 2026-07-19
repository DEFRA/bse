Friend Class FreeEartagFormat
    Inherits EartagFormatBase

    Private Const mFormatPattern13 As String = "^[A-Z0-9]{1,}$"

    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 13
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "Free Format"
        End Get
    End Property

    Public Overrides Function GetPresentationFormat(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        Return animalComponent

    End Function

    Public Overrides Function ReformatAnimalComponent(ByVal animalComponent As String) As String

        Do While ((Eartag.IsRegexPatternMatch(mFormatPattern13, Mid(animalComponent, 1, 1)) = False) And Len(animalComponent) > 0)
            animalComponent = Mid(animalComponent, 2)
        Loop

        Return animalComponent

    End Function

    Public Overrides Function Validate(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

        If countryCode.Length > 0 Then
            Return "Country code should be empty for Free format eartags"
        End If

        If animalComponent.Length = 0 OrElse animalComponent.Length > 22 Then
            Return "Animal component is invalid: It should contain 1 to 22 characters"
        End If

        Return String.Empty

    End Function

End Class
