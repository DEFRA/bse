Imports System.Linq
Imports BSELib.EartagValidation.EartagFormat

Public Class IsoEarTag
    Inherits Eartag

    Friend Sub New(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String)

        MyBase.New(countryCode, herdComponent, animalComponent)

    End Sub

    Protected Overrides Sub GetFormat()

        If CountryCode.Length > 2
            If Char.IsDigit(countryCode.ElementAt(0)) Then
                mFormat = New IsoNumericCountryEartagFormat()
            Else 
                mFormat = new IsoAlphaNumericCountryEartagFormat()
            End If
        End If

    End Sub

End Class
