Imports ESRI.ArcGIS.ArcMapUI
Imports ESRI.ArcGIS.Carto
Imports ESRI.ArcGIS.Geodatabase
Imports ESRI.ArcGIS.Display
Imports ESRI.ArcGIS.esriSystem
Imports ESRI.ArcGIS.Geometry
Imports ESRI.ArcGIS.Geoprocessing

Imports System.Windows.Forms
Public Class E2SFCAInputForm
    Public Function GetFeatureLayer(pMap As IMap, name As String)
        Dim pLayer As ILayer
        Dim pInputLayer As IFeatureLayer = New FeatureLayer
        For i = 0 To pMap.LayerCount - 1
            pLayer = pMap.Layer(i)
            If pLayer.Name = name Then
                pInputLayer = pMap.Layer(i)
            End If
        Next
        Return pInputLayer
    End Function

    Public Function GetLayers(pMap As IMap)
        Dim listLayers As New List(Of String)
        Dim pLayer As ILayer
        For i = 0 To pMap.LayerCount - 1
            pLayer = pMap.Layer(i)
            listLayers.Add(pLayer.Name)
        Next
        Return listLayers
    End Function

    Private Sub Form_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim pMxDoc As IMxDocument = My.ArcMap.Document
        Dim pMap As IMap = pMxDoc.FocusMap
        Dim listLayers As List(Of String) = GetLayers(pMap)
        For i = 0 To listLayers.ToArray.Length - 1
            SuppliersList.Items.Add(listLayers(i))
            DemandList.Items.Add(listLayers(i))
        Next
    End Sub

    Private Sub SuppliersList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles SuppliersList.SelectedIndexChanged
        SupplyWeight.Items.Clear()
        Dim pMxDoc As IMxDocument = My.ArcMap.Document
        Dim pMap As IMap = pMxDoc.FocusMap
        Dim selectedSuppliers As String = SuppliersList.SelectedItem()
        Dim pSupplierLayer As IFeatureLayer = GetFeatureLayer(pMap, selectedSuppliers)
        Dim pFeatureLayer As IFeatureClass = pSupplierLayer.FeatureClass
        Dim fields As IFields = pFeatureLayer.Fields
        Dim field As IField
        For i As Integer = 0 To fields.FieldCount - 1
            field = fields.Field(i)
            SupplyWeight.Items.Add(field.Name)
        Next
    End Sub

    Private Sub DemandList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles DemandList.SelectedIndexChanged
        DemandWeight.Items.Clear()
        Dim pMxDoc As IMxDocument = My.ArcMap.Document
        Dim pMap As IMap = pMxDoc.FocusMap
        Dim selectedDemand As String = DemandList.SelectedItem()
        Dim pDemandLayer As IFeatureLayer = GetFeatureLayer(pMap, selectedDemand)
        Dim pFeatureLayer As IFeatureClass = pDemandLayer.FeatureClass
        Dim fields As IFields = pFeatureLayer.Fields
        Dim field As IField
        For i As Integer = 0 To fields.FieldCount - 1
            field = fields.Field(i)
            DemandWeight.Items.Add(field.Name)
        Next
    End Sub

    Public Function CreateBuffer(pMxDoc As IMxDocument, pMap As IMap, pFClassS As IFeatureClass, weightSupplyField As Integer, pfLayerD As IFeatureLayer, weightDemandField As Integer, bufferSize As Integer, SecondStep As Boolean)
        Dim pFieldsS As IFields = pFClassS.Fields
        Dim intPosStandValue As Integer = pFieldsS.FindField("Access")

        If intPosStandValue = -1 Then
            Dim pFieldEdit As IFieldEdit = New Field
            pFieldEdit.Name_2 = "Access"
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeDouble
            pFClassS.AddField(pFieldEdit)
        End If

        Dim pFCursorS As IFeatureCursor = pFClassS.Update(Nothing, True)
        Dim pfS As IFeature = pFCursorS.NextFeature
        Do Until pfS Is Nothing
            Dim Sj As Double = Double.Parse(pfS.Value(weightSupplyField).ToString())
            Dim topologicalOperator As ITopologicalOperator
            Dim element As IElement
            Dim pGeometry As IGeometry = Nothing
            topologicalOperator = pfS.Shape
            element = New PolygonElementClass()
            pGeometry = topologicalOperator.Buffer(bufferSize)
            element.Geometry = pGeometry
            Dim pFillSymbElement As IFillShapeElement = element
            Dim pColor_2 As IRgbColor = New RgbColor
            pColor_2.Red = 30
            pColor_2.Blue = 0
            pColor_2.Green = 0
            pColor_2.Transparency = 0
            Dim pLineSymbol As ILineSymbol = New SimpleLineSymbol
            Dim pLineColor As IRgbColor = New RgbColor
            pLineColor.Red = 30
            pLineColor.Green = 0
            pLineColor.Blue = 0
            pLineSymbol.Color = pLineColor
            pLineSymbol.Width = 1
            Dim pFillSymbol As IFillSymbol = New SimpleFillSymbol
            pFillSymbol.Color = pColor_2
            pFillSymbol.Outline = pLineSymbol
            pFillSymbElement.Symbol = pFillSymbol
            Dim graphicsContainer As IGraphicsContainer = pMap
            'graphicsContainer.AddElement(element, 0)

            Dim pSpatialFilter As ISpatialFilter = New SpatialFilter
            With pSpatialFilter
                .Geometry = pGeometry
                .SpatialRel = esriSpatialRelEnum.esriSpatialRelContains
            End With

            Dim pDemandFSelection As IFeatureSelection = pfLayerD
            pDemandFSelection.SelectFeatures(pSpatialFilter, esriSelectionResultEnum.esriSelectionResultNew, False)
            pDemandFSelection.SetSelectionSymbol = True
            Dim pSelectionSet As ISelectionSet = pDemandFSelection.SelectionSet
            Dim pSelectionCount As Long = pSelectionSet.Count

            'iterate over all elements in selection
            Dim Accessibility As Double = 0
            If pSelectionCount > 0 Then
                Dim pDemandSelectedFeatures As IFeatureCursor
                pSelectionSet.Search(Nothing, False, pDemandSelectedFeatures)
                Dim pFeatureSelection As IFeature = pDemandSelectedFeatures.NextFeature
                Dim CumulativeWeight As Double = 0
                Do Until pFeatureSelection Is Nothing
                    CumulativeWeight += pFeatureSelection.Value(weightDemandField).ToString()
                    pFeatureSelection = pDemandSelectedFeatures.NextFeature
                Loop
                If SecondStep Then
                    Accessibility = CumulativeWeight
                Else
                    Accessibility = Sj / CumulativeWeight
                End If
            End If
            Dim pfSFields As IFields = pfS.Fields
            Dim PosAccessValue As Integer = pfSFields.FindField("Access")
            pfS.Value(PosAccessValue) = Accessibility
            pFCursorS.UpdateFeature(pfS)
            pfS = pFCursorS.NextFeature
        Loop
        pMxDoc.ActiveView.Refresh()
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim pMxDoc As IMxDocument = My.ArcMap.Document
        Dim pMap As IMap = pMxDoc.FocusMap
        Dim graphicsContainer As IGraphicsContainer = pMap
        graphicsContainer.DeleteAllElements()
        Dim bufferSize As Integer = Double.Parse(DistanceTreshold.Text)
        Dim supplierLayerName As String = SuppliersList.SelectedItem()

        Dim pfLayerS As IFeatureLayer = GetFeatureLayer(pMap, supplierLayerName)
        Dim pFClassS As IFeatureClass = pfLayerS.FeatureClass


        Dim pFieldsS As IFields = pFClassS.Fields
        Dim weightSupplyField As Integer = pFieldsS.FindField(SupplyWeight.SelectedItem())

        Dim selectedDemand As String = DemandList.SelectedItem()
        Dim pfLayerD As IFeatureLayer = GetFeatureLayer(pMap, selectedDemand)
        Dim pFClassD As IFeatureClass = pfLayerD.FeatureClass

        Dim pFieldsD As IFields = pFClassD.Fields
        Dim weightDemandField As Integer = pFieldsD.FindField(DemandWeight.SelectedItem())


        CreateBuffer(pMxDoc, pMap, pFClassS, weightSupplyField, pfLayerD, weightDemandField, bufferSize, False)

        Dim weightSupplyField2Step As Integer = pFieldsS.FindField("Access")
        CreateBuffer(pMxDoc, pMap, pFClassD, weightDemandField, pfLayerS, weightSupplyField2Step, bufferSize, True)

        Dim pFClassDF As IFeatureClass = pfLayerD.FeatureClass
        Dim dTable As ITable = pFClassDF
        Dim dCursor As ICursor = dTable.Search(Nothing, False)
        Dim dRow As IRow = dCursor.NextRow()
        Dim dFinalFields As IFields = pFClassD.Fields()

        Dim FILE_NAME As String = "C:\Users\lab415\Desktop\AdvGIS\FinalProject\Results\test.csv"
        Dim i As Integer
        Dim aryText(dFinalFields.FieldCount - 1) As String
        Dim objWriter As New System.IO.StreamWriter(FILE_NAME)

        Do Until dRow Is Nothing
            For i = 0 To dFinalFields.FieldCount - 1
                objWriter.Write(dRow.Value(i).ToString())
                objWriter.Write(";")
            Next
            objWriter.WriteLine("")
            dRow = dCursor.NextRow()
        Loop
        objWriter.Close()
        MessageBox.Show("Processing completed")
    End Sub
End Class