Friend MustInherit Class EartagFormatBase
    Implements IEartagFormat

    Public MustOverride ReadOnly Property FormatId() As Integer Implements IEartagFormat.FormatId
    Public MustOverride ReadOnly Property FormatName() As String Implements IEartagFormat.FormatName
       
    Public Overridable Function GetPresentationFormat(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String Implements IEartagFormat.GetPresentationFormat

        Return countryCode & " " & herdComponent & " " & animalComponent

    End Function

    Public Overridable Function ReformatAnimalComponent(ByVal animalComponent As String) As String Implements IEartagFormat.ReformatAnimalComponent

        Return animalComponent

    End Function

    Public Overridable Function ReformatHerdComponent(ByVal herdComponent As String) As String Implements IEartagFormat.ReformatHerdComponent

        Return herdComponent

    End Function

    Public Overridable Function Validate(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String) As String Implements IEartagFormat.Validate

        Return String.Empty

    End Function

    Protected Function CalculateModulus(ByVal strNumer As String, ByVal strDenom As String) As Integer

        Dim dblNumer As Double
        Dim dblDenom As Double

        dblNumer = Int(CDbl(strNumer))
        dblDenom = Int(CDbl(strDenom))

        Return CInt(dblNumer - (dblDenom * Fix(dblNumer / dblDenom)))

    End Function

End Class
