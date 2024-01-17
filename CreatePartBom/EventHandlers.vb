Imports Autodesk.Connectivity.Extensibility.Framework
Imports Autodesk.Connectivity.WebServices
Imports ACW = Autodesk.Connectivity.WebServices
Imports ACWT = Autodesk.Connectivity.WebServicesTools

<Assembly: ApiVersion("17.0")>
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
        Dim mFileId As Long
        Dim mItemService As Autodesk.Connectivity.WebServices.ItemService = sender
        Dim mDocumentService As Autodesk.Connectivity.WebServices.DocumentService = mItemService.WebServiceManager.DocumentService
        Dim mPropertyService As Autodesk.Connectivity.WebServices.PropertyService = mItemService.WebServiceManager.PropertyService
        Dim mItemIds As GetPromoteOrderResults = mItemService.GetPromoteComponentOrder(timestamp:=Now)
        Dim mFile As File
        Dim mFileProperties As PropInst()
        Dim mProperty As PropInst
        Dim mPropertyDefinition As PropDefInfo()
        Dim mCompIds As Long()
        Dim mItem As Item

        mCompIds = mItemIds.PrimaryArray()
        mItemService.PromoteComponents(timestamp:=Now, mCompIds)
        Dim mItemsAndFiles As ItemsAndFiles = mItemService.GetPromoteComponentsResults(timestamp:=Now)

        For Each mItems In mItemsAndFiles.ItemRevArray()
            Debug.Print(mItems.ItemNum)
        Next

        For Each mFileId In e.FileIds
            For Each mItemId In mItemIds.PrimaryArray()
                mFile = mDocumentService.GetFileById(mFileId)
                mFileProperties = mPropertyService.GetPropertiesByEntityIds("FILE", New Long() {mFile.Id})

                ' Dim itemProperties As PropInst() = propSvc.GetPropertiesByEntityIds("ITEM", New Long() {Item.Id})
                Debug.Print("##################################################################################################")
                Debug.Print("El fichero: " & mFile.Name &
                            " con Id: " & mFileId &
                            " se va a promocionar al Item: " & mItemId)

                For Each mProperty In mFileProperties
                    mPropertyDefinition = mPropertyService.GetPropertyDefinitionInfosByEntityClassId("FILE", New Long() {mProperty.PropDefId})
                    If mPropertyDefinition(0).PropDef.DispName <> "Miniatura" And
                    (mPropertyDefinition(0).PropDef.DispName = "Nº de pieza" Or mPropertyDefinition(0).PropDef.DispName = "Extensión de archivo") Then
                        ' If mPropertyDefinition(0).PropDef.DispName <> "Miniatura" Then
                        Debug.Print(mPropertyDefinition(0).PropDef.Id &
                                    " : " & mPropertyDefinition(0).PropDef.DispName &
                                    " = " & mProperty.Val)
                    End If
                Next
            Next
        Next
        Debug.Print("eureka or not? parece que si")
    End Sub

End Class