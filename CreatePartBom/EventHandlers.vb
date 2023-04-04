Imports Autodesk.Connectivity.Extensibility.Framework
Imports Autodesk.Connectivity.WebServices
Imports ACW = Autodesk.Connectivity.WebServices
Imports ACWT = Autodesk.Connectivity.WebServicesTools

<Assembly: ApiVersion("16.0")>
<Assembly: ExtensionId("89facae7-a876-4ce5-8d9f-a20666971e21")>


Public Class EventHandlers
    Implements IWebServiceExtension

#Region "IWebServiceExtension Members"

    Public Sub OnLoad() Implements IWebServiceExtension.OnLoad
        ' Item Events
        AddHandler ItemService.PromoteItemEvents.Post, AddressOf PromoteItemEvents_Post
    End Sub

#End Region

    Private Sub PromoteItemEvents_Post(ByVal sender As Object, ByVal e As PromoteItemCommandEventArgs)
        Dim mFileId As Long = e.FileIds(0)
        Dim mItemService As Autodesk.Connectivity.WebServices.ItemService = sender
        Dim mItem As GetPromoteOrderResults = mItemService.GetPromoteComponentOrder(timestamp:=Now)
        Debug.Print(mItem.PrimaryArray(0))
        Debug.Print("Eureka")
    End Sub

End Class