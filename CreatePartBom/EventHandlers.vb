Imports Autodesk.Connectivity.Extensibility.Framework
Imports Autodesk.Connectivity.WebServices

<Assembly: ApiVersion("16.0")>
<Assembly: ExtensionId("89facae7-a876-4ce5-8d9f-a20666971e21")>


Public Class EventHandlers
    Implements IWebServiceExtension

#Region "IWebServiceExtension Members"

    Public Sub OnLoad() Implements IWebServiceExtension.OnLoad
        ' Item Events
        AddHandler ItemService.PromoteItemEvents.GetRestrictions, AddressOf PromoteItemEvents_GetRestrictions
        ' AddHandler ItemService.PromoteItemEvents.Post, AddressOf PromoteItemEvents_Post
    End Sub

#End Region

    Private Sub PromoteItemEvents_GetRestrictions(ByVal sender As Object, ByVal e As PromoteItemCommandEventArgs)
        MsgBox("Hola Rafa, encabronados con el Vault ahora " & e.Status.ToString)
        e.AddRestriction(New ExtensionRestriction(e.FileIds(0).ToString, "Test restriction"))
    End Sub

End Class

