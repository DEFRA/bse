Public Interface IEartagFormat

    ReadOnly Property FormatId() As Integer
    ReadOnly Property FormatName() As String

    Function ReformatHerdComponent(ByVal herdComponent As String) As String
    Function ReformatAnimalComponent(ByVal animalComponent As String) As String
    Function Validate(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String
    Function GetPresentationFormat(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String

End Interface