Imports System.Configuration
Imports BSELib.EartagValidation.EartagFormat

Friend Class IsoNumericCountryEartagFormat
    Inherits IsoEarTagFormat

    Public Overrides ReadOnly Property FormatId() As Integer
        Get
            Return 100
        End Get
    End Property

    Public Overrides ReadOnly Property FormatName() As String
        Get
            Return "ISO Numeric Bovine EID"
        End Get
    End Property

    Public Overrides Function Validate(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String
        
        Dim pattern As String = ConfigurationManager.AppSettings.Get("IsoEarTagNumericCountryPartExpression").ToString()
        Dim countryPattern As String = $"{pattern}{CommonCountryPart}"

        If Not Eartag.IsRegexPatternMatch(countryPattern, countryCode) Then
            Return "Country component is invalid: It should contain 3 numerical digits followed by 0, 1, 2, _ or space character"
        End If

        Dim herdValidation As String = ValidateHerdComponent(herdComponent)
        Dim animalValidation As String = ValidateAnimalComponent(animalComponent)

        If Not String.IsNullOrEmpty(herdValidation) Then
            Return herdValidation
        End If

        If Not String.IsNullOrEmpty(animalValidation) Then
            Return animalValidation
        End If
        
        Return String.Empty
 
    End Function

End Class
