Public Class UKEartag
    Inherits Eartag

    Friend Sub New(ByVal countryCode As String, ByVal herdComponent As String, ByVal animalComponent As String)

        MyBase.New(countryCode, herdComponent, animalComponent)

    End Sub

    Protected Overrides Sub GetFormat()

        If (IsNumeric(HerdComponent)) And (IsNumeric(AnimalComponent)) Then
            If (Mid(HerdComponent, 1, 1) = "9") Then
                mFormat = New NINumericEartagFormat()
            ElseIf (Mid(HerdComponent, 1, 2) = "01") Then
                mFormat = New IsleOfManNumericEartagFormat()
            ElseIf (Mid(HerdComponent, 1, 2) = "02") Then
                mFormat = New GuernseyNumericEartagFormat()
            ElseIf (Mid(HerdComponent, 1, 2) = "03") Then
                mFormat = New JerseyNumericEartagFormat()
            Else
                mFormat = New GBNumericEartagFormat()
            End If
        ElseIf (IsNumeric(HerdComponent)) Then
            mFormat = New NIAlphaNumericEartagFormat()
        Else
            If (Mid(HerdComponent, 1, 2) = "MN") Then
                mFormat = New IsleOfManAlphaNumericEartagFormat()
            ElseIf (Mid(HerdComponent, 1, 2) = "GY") Then
                mFormat = New GuernseyAlphaNumericEartagFormat()
            ElseIf (Mid(HerdComponent, 1, 2) = "JY") Then
                mFormat = New JerseyAlphaNumericEartagFormat()
            Else
                mFormat = New GBAlphaNumericEartagFormat()
            End If
        End If

    End Sub

End Class
