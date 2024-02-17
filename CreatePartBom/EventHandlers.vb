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
        AddHandler ItemService.PromoteItemEvents.Pre, AddressOf PromoteItemEvents_Pre
    End Sub

#End Region

    Private Sub PromoteItemEvents_Pre(ByVal sender As Object, ByVal e As PromoteItemCommandEventArgs)

        Dim mItemService As Autodesk.Connectivity.WebServices.ItemService = sender
        Dim mDocumentService As Autodesk.Connectivity.WebServices.DocumentService = mItemService.WebServiceManager.DocumentService
        Dim mPropertyService As Autodesk.Connectivity.WebServices.PropertyService = mItemService.WebServiceManager.PropertyService

        ' Acceso propiedades del item
        Dim itemPropDefs As PropDef() = mPropertyService.GetPropertyDefinitionsByEntityClassId("ITEM")
        ' Acceso a la propiedad Number del item
        Dim itemNumberPropDef As PropDef = itemPropDefs.[Single](Function(n) n.SysName = "Number")

        ' SrchOper 3 => equals
        Dim principalModelStateItems As New SrchCond() With {
        .PropDefId = itemNumberPropDef.Id,
        .PropTyp = PropertySearchType.SingleProperty,
        .SrchOper = 3,
        .SrchRule = SearchRuleType.Must,
        .SrchTxt = "???.??????"
        }

        Dim bookmark As String = String.Empty
        Dim status As SrchStatus = Nothing
        Dim totalResults As New List(Of Item)()
        Dim result As Item
        Dim resultIds As Long() = New Long() {}

        While status Is Nothing OrElse totalResults.Count < status.TotalHits
            Dim results As Item() = mItemService.FindItemRevisionsBySearchConditions(
                New SrchCond() {principalModelStateItems},
                Nothing,
                False,
                bookmark,
                status)

            If results IsNot Nothing Then
                totalResults.AddRange(results)
                Debug.Print(results.Length)
            Else
                Exit While
            End If

        End While

        For Each result In totalResults
            ReDim Preserve resultIds(resultIds.Length)
            resultIds(resultIds.Length - 1) = result.MasterId
        Next

        mItemService.DeleteItemsUnconditional(resultIds)

    End Sub

End Class