Public Interface IUKEartagFormat
    Inherits IEartagFormat

    ReadOnly Property FormatLocation() As String
    Function ValidateUKEartag(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

End Interface