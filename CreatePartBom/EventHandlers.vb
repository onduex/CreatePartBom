Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.Linq
Imports Autodesk.Connectivity.Extensibility.Framework
Imports Autodesk.Connectivity.WebServices

<Assembly: ApiVersion("20.0")>
<Assembly: ExtensionId("89facae7-a876-4ce5-8d9f-a20666971e21")>

Public Class EventHandlers
    Implements IWebServiceExtension

    Private Const PieceCategoryId As Long = 8
    Private Const ItemEntityClassId As String = "ITEM"

    Private Shared ReadOnly ProcessingItemIds As New HashSet(Of Long)()
    Private Shared ReadOnly ProcessingLock As New Object()

    Public Sub OnLoad() Implements IWebServiceExtension.OnLoad
        AddHandler ItemService.AddItemEvents.Post, AddressOf AddItemEvents_Post
        AddHandler ItemService.EditItemEvents.Post, AddressOf EditItemEvents_Post
        Debug.Print("Loading...")
    End Sub

    Private Sub AddItemEvents_Post(ByVal sender As Object, ByVal e As AddItemCommandEventArgs)
        Dim itemService As ItemService = TryCast(sender, ItemService)

        If itemService Is Nothing OrElse e Is Nothing OrElse e.ReturnValue Is Nothing Then
            Return
        End If

        ProcessItemForBom(itemService, e.ReturnValue)
    End Sub

    Private Sub EditItemEvents_Post(ByVal sender As Object, ByVal e As EditItemCommandEventArgs)
        Dim itemService As ItemService = TryCast(sender, ItemService)

        If itemService Is Nothing OrElse e Is Nothing OrElse e.ReturnValue Is Nothing Then
            Return
        End If

        For Each item As Item In e.ReturnValue
            ProcessItemForBom(itemService, item)
        Next
    End Sub

    Private Sub ProcessItemForBom(ByVal itemService As ItemService, ByVal parentItem As Item)
        If parentItem Is Nothing OrElse parentItem.Cat Is Nothing OrElse parentItem.Cat.CatId <> PieceCategoryId Then
            Return
        End If

        SyncLock ProcessingLock
            If Not ProcessingItemIds.Add(parentItem.Id) Then
                Return
            End If
        End SyncLock

        Try
            Dim propertyService As PropertyService = itemService.WebServiceManager.PropertyService
            Dim propertyDefinitions As PropDef() =
                propertyService.GetPropertyDefinitionsByEntityClassId(ItemEntityClassId)

            Dim rawMaterialNumberDef As PropDef =
                GetPropertyDefinition(propertyDefinitions, "_RM_NUMBER")
            Dim rawMaterialDescriptionDef As PropDef =
                GetPropertyDefinition(propertyDefinitions, "_RM_DESCRIPTION")
            Dim rawMaterialQuantityDef As PropDef =
                GetPropertyDefinition(propertyDefinitions, "_RM_QTY")
            Dim itemNumberDef As PropDef =
                GetPropertyDefinition(propertyDefinitions, "Número")

            Debug.Print("Loading...")

            Dim propertyInstances As PropInst() = propertyService.GetProperties(
                ItemEntityClassId,
                New Long() {parentItem.Id},
                New Long() {
                    rawMaterialNumberDef.Id,
                    rawMaterialDescriptionDef.Id,
                    rawMaterialQuantityDef.Id
                })

            Dim rawMaterialNumber As String =
                Convert.ToString(
                    GetPropertyValue(propertyInstances, rawMaterialNumberDef.Id),
                    CultureInfo.InvariantCulture)

            If String.IsNullOrWhiteSpace(rawMaterialNumber) Then
                Debug.Print("Sin _RM_NUMBER para el ítem " & parentItem.ItemNum & ".")
                Return
            End If

            rawMaterialNumber = rawMaterialNumber.Trim()

            Dim rawMaterialDescription As String =
                Convert.ToString(
                    GetPropertyValue(propertyInstances, rawMaterialDescriptionDef.Id),
                    CultureInfo.InvariantCulture)

            Dim quantity As Double
            If Not TryParseQuantity(
                GetPropertyValue(propertyInstances, rawMaterialQuantityDef.Id),
                quantity) OrElse
                quantity <= 0 OrElse
                Double.IsNaN(quantity) OrElse
                Double.IsInfinity(quantity) Then

                Debug.Print("Cantidad _RM_QTY no válida para el ítem " & parentItem.ItemNum & ".")
                Return
            End If

            Dim rawMaterial As Item =
                FindLatestItemByNumber(itemService, itemNumberDef, rawMaterialNumber)

            If rawMaterial Is Nothing Then
                Debug.Print("No se encontró la materia prima " & rawMaterialNumber & " en Vault.")
                Return
            End If

            If rawMaterial.MasterId = parentItem.MasterId Then
                Debug.Print("El ítem no puede ser su propia materia prima: " & parentItem.ItemNum & ".")
                Return
            End If

            Dim associations As ItemAssoc() =
                itemService.GetItemBOMAssociationsByItemIds(
                    New Long() {parentItem.Id},
                    False)

            Dim existingAssociation As ItemAssoc = Nothing

            If associations IsNot Nothing Then
                existingAssociation = associations.FirstOrDefault(
                    Function(association)
                        Return association.CldItemMasterID = rawMaterial.MasterId OrElse
                               association.CldItemID = rawMaterial.Id
                    End Function)
            End If

            If existingAssociation IsNot Nothing AndAlso
               existingAssociation.CldItemID = rawMaterial.Id AndAlso
               existingAssociation.IsIncluded AndAlso
               Math.Abs(existingAssociation.Quant - quantity) < 0.0000001 Then

                Debug.Print(
                    "La BOM de " & parentItem.ItemNum &
                    " ya contiene " & rawMaterialNumber &
                    " en cantidad " & quantity.ToString(CultureInfo.InvariantCulture) & ".")
                Return
            End If

            Dim associationParam As New ItemAssocParam With {
                .CldItemID = rawMaterial.Id,
                .Quant = quantity,
                .UnitID = rawMaterial.UnitId,
                .IsIncluded = True,
                .EditAct = BOMEditAction.Add
            }

            If existingAssociation IsNot Nothing Then
                associationParam.Id = existingAssociation.Id
                associationParam.EditAct = BOMEditAction.Update
            End If

            itemService.UpdateItemBOMAssociations(
                parentItem.Id,
                New ItemAssocParam() {associationParam},
                BOMViewEditOptions.Defaults)

            Debug.Print(
                "BOM actualizada: " & parentItem.ItemNum &
                " -> " & rawMaterialNumber &
                " (" & rawMaterialDescription & "), cantidad " &
                quantity.ToString(CultureInfo.InvariantCulture) & ".")

        Catch ex As Exception
            Debug.Print(
                "Error al procesar la BOM del ítem " &
                parentItem.ItemNum & ": " & ex.ToString())
        Finally
            SyncLock ProcessingLock
                ProcessingItemIds.Remove(parentItem.Id)
            End SyncLock
        End Try
    End Sub

    Private Function GetPropertyDefinition(
        ByVal propertyDefinitions As PropDef(),
        ByVal sysName As String) As PropDef

        Dim definition As PropDef = propertyDefinitions.FirstOrDefault(
            Function(candidate) String.Equals(
                candidate.DispName,
                sysName,
                StringComparison.OrdinalIgnoreCase))

        If definition Is Nothing Then
            Throw New InvalidOperationException(
                "No se encontró la propiedad de Vault con SysName '" & sysName & "'.")
        End If

        Return definition
    End Function

    Private Function GetPropertyValue(
        ByVal propertyInstances As PropInst(),
        ByVal propertyDefinitionId As Long) As Object

        If propertyInstances Is Nothing Then
            Return Nothing
        End If

        Dim propertyInstance As PropInst =
            propertyInstances.FirstOrDefault(
                Function(candidate) candidate.PropDefId = propertyDefinitionId)

        If propertyInstance Is Nothing Then
            Return Nothing
        End If

        Return propertyInstance.Val
    End Function

    Private Function FindLatestItemByNumber(
        ByVal itemService As ItemService,
        ByVal itemNumberDef As PropDef,
        ByVal itemNumber As String) As Item

        Dim condition As New SrchCond() With {
            .PropDefId = itemNumberDef.Id,
            .PropTyp = PropertySearchType.SingleProperty,
            .SrchOper = 3,
            .SrchRule = SearchRuleType.Must,
            .SrchTxt = itemNumber
        }

        Dim bookmark As String = String.Empty
        Dim status As SrchStatus = Nothing

        Dim results As Item() =
            itemService.FindItemRevisionsBySearchConditions(
                New SrchCond() {condition},
                Nothing,
                True,
                bookmark,
                status)

        If results Is Nothing Then
            Return Nothing
        End If

        Return results.FirstOrDefault(
            Function(candidate) String.Equals(
                candidate.ItemNum,
                itemNumber,
                StringComparison.OrdinalIgnoreCase))
    End Function

    Private Function TryParseQuantity(
        ByVal value As Object,
        ByRef quantity As Double) As Boolean

        If value Is Nothing Then
            Return False
        End If

        If TypeOf value Is String Then
            Dim text As String = DirectCast(value, String).Trim()

            If Double.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("es-ES"),
                quantity) Then

                Return True
            End If

            Return Double.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                quantity)
        End If

        Try
            quantity = Convert.ToDouble(value, CultureInfo.InvariantCulture)
            Return True
        Catch
            Return False
        End Try
    End Function
End Class