Public Class ECEartag
    Inherits Eartag

    Friend Sub New(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String)

        MyBase.New(countryCode, herdComponent, animalComponent)

    End Sub

    Protected Overrides Sub GetFormat()

        mFormat = New ECEartagFormat()

    End Sub

End Class
