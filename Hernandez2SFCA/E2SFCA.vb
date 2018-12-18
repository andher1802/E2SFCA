Public Class E2SFCA
    Inherits ESRI.ArcGIS.Desktop.AddIns.Button

    Public Sub New()
    End Sub

    Protected Overrides Sub OnClick()
        Dim inputForm As New E2SFCAInputForm
        inputForm.Show()
    End Sub

    Protected Overrides Sub OnUpdate()
        Enabled = My.ArcMap.Application IsNot Nothing
    End Sub
End Class
