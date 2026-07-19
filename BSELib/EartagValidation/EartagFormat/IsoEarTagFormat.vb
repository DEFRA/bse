Namespace EartagValidation.EartagFormat
    Friend MustInherit Class IsoEarTagFormat
        Inherits EartagFormatBase

        Protected ReadOnly Property CommonCountryPart As String = Configuration.ConfigurationManager.AppSettings.Get("IsoEarTagCommonPartExpression").ToString()
        Private ReadOnly mAnimalPattern As String = Configuration.ConfigurationManager.AppSettings.Get("IsoEarTagAnimalPartExpression").ToString()
        Private ReadOnly mHerdPattern As String = Configuration.ConfigurationManager.AppSettings.Get("IsoEarTagHerdmarkPartExpression").ToString()

        Protected Function ValidateHerdComponent(ByVal herdComponent As String) As String
            If Not Eartag.IsRegexPatternMatch(mHerdPattern, herdComponent)
                Return "Herd component is invalid: It should contain 6 numerical digits "
            End If

            Return String.Empty
        End Function

        Protected Function ValidateAnimalComponent(ByVal animalComponent As String) As String
            If Not Eartag.IsRegexPatternMatch(mAnimalPattern, animalComponent)
                Return "Animal component is invalid: It should contain 5 numerical digits"
            End If

            Return String.Empty
        End Function
    End Class
End NameSpace