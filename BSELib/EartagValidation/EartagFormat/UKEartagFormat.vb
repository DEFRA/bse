Imports System.Linq

Friend MustInherit Class UKEartagFormat
    Inherits EartagFormatBase
    Implements IUKEartagFormat

    'Helper functions for validating UK eartags.
    Public MustOverride ReadOnly Property FormatLocation() As String Implements IUKEartagFormat.FormatLocation
    Public MustOverride Function ValidateUKEartag(countryCode As String, herdComponent As String, animalComponent As String) As String Implements IUKEartagFormat.ValidateUKEartag

    'Validate the UK part of the Eartag. If that's OK, proceed to the other parts in the subclasses.
    Public Overrides Function Validate(countryCode As String, herdComponent As String, animalComponent As String) As String
        If Eartag.mUKCountries.Contains(countryCode) Then
            Return Me.ValidateUKEartag(countryCode, herdComponent, animalComponent)
        Else
            Return "Country code should be UK for " & Me.FormatLocation & " eartags"
        End If
    End Function

End Class
