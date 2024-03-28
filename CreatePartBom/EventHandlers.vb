Imports Autodesk.Connectivity.Extensibility.Framework
Imports Autodesk.Connectivity.WebServices
Imports System.IO



<Assembly: ApiVersion("17.0")>
<Assembly: ExtensionId("89facae7-a876-4ce5-8d9f-a20666971e21")>

Public Class EventHandlers
    Implements IWebServiceExtension

#Region "IWebServiceExtension Members"

    Public Sub OnLoad() Implements IWebServiceExtension.OnLoad
        ' Item Events
        AddHandler ItemService.UpdateItemLifecycleStateEvents.GetRestrictions, AddressOf UpdateItemLifecycleStateEvents_GetRestrictions
        AddHandler ItemService.UpdateItemLifecycleStateEvents.Post, AddressOf UpdateItemLifecycleStateEvents_Post
    End Sub

#End Region

    Private Sub UpdateItemLifecycleStateEvents_GetRestrictions(ByVal sender As Object, ByVal e As UpdateItemLifeCycleStateCommandEventArgs)

        Dim mItemService As Autodesk.Connectivity.WebServices.ItemService = sender
        Dim mDocumentService As Autodesk.Connectivity.WebServices.DocumentService = mItemService.WebServiceManager.DocumentService
        Dim mPropertyService As Autodesk.Connectivity.WebServices.PropertyService = mItemService.WebServiceManager.PropertyService

        ' Acceso propiedades del item
        Dim itemPropDefs As PropDef() = mPropertyService.GetPropertyDefinitionsByEntityClassId("ITEM")
        ' Acceso a la propiedad Number del item
        Dim itemNumberPropDef As PropDef = itemPropDefs.[Single](Function(n) n.SysName = "Number")
        Dim itemRevNumberPropDef As PropDef = itemPropDefs.[Single](Function(n) n.SysName = "RevNumber")
        Dim results As Item() = mItemService.GetLatestItemsByItemMasterIds(e.ItemMasterIds)
        Dim result As Item

        For Each result In results
            Dim suffix As String = ""
            Dim prefixPng As String = "R:\dtecnic\PLANOS\0_PNG\"
            Dim prefixPdf As String = "R:\dtecnic\PLANOS\1_PDF\"

            If Len(result.RevNum) = 1 Then
                suffix = "_R0" & result.RevNum
            ElseIf Len(result.RevNum) = 2 Then
                suffix = "_R" & result.RevNum
            End If

            Dim itemNumber3 As String = result.ItemNum.Substring(0, 3) & "\"
            Dim itemNumber7 As String = result.ItemNum.Substring(0, 7) & "\"
            Dim pathPng As String = prefixPng & itemNumber3 & itemNumber7 & result.ItemNum & suffix & ".png"
            Dim pathPdf As String = prefixPdf & itemNumber3 & itemNumber7 & result.ItemNum & suffix & ".pdf"
            Dim restrictionPng As New ExtensionRestriction("Item " & result.ItemNum, "No existe: " & pathPng)
            Dim restrictionPdf As New ExtensionRestriction("Item " & result.ItemNum, "No existe: " & pathPdf)

            If Not System.IO.File.Exists(pathPng) Then
                e.AddRestriction(restrictionPng)
            End If
            If Not System.IO.File.Exists(pathPdf) Then
                e.AddRestriction(restrictionPdf)
            End If
        Next

    End Sub

    Private Sub UpdateItemLifecycleStateEvents_Post(ByVal sender As Object, ByVal e As UpdateItemLifeCycleStateCommandEventArgs)

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
                Debug.Print("Items de 10 dígitos borrados: " & results.Length)
            Else
                Exit While
            End If

        End While

        For Each result In totalResults
            ReDim Preserve resultIds(resultIds.Length)
            resultIds(resultIds.Length - 1) = result.MasterId
        Next

        If resultIds.Length <> 0 Then
            mItemService.DeleteItemsUnconditional(resultIds)
        End If

    End Sub

End Class