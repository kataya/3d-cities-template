#######################################
# Title: 3DHeightTools.pyt
# Description: 3D height tools for use with the 3D City Information Model.
# Tools:
#   Calculate Building Elevations -  Calculates base heights and building heights by
#       using building footprints, a DTM raster, and a nDSM raster.
#   Calculate Tree Heights - Calculates tree heights and shapes using tree canopies,
#       a DTM raster, and a nDSM raster.
# Author: Timothy Hales & Jeff Swain
# Created: 8/5/2013
# Last Updated: 5/18/2014
#
# Python Version: 2.7
# Version 1.1 Notes: Fixed some error messages, and exposed the negative bufgfer distance for the building height tool.
# Version 1.1.1 Notes: Fixed the default option for creation of a new building feature class to false.
# Version 1.2 Notes: Added checkbox option for using the 3DCIM so that the totalHeight field is populated and the BuildingHeight
#                   and BaseHeight fields are not added. This maintains the schema of the 3DCIM.
# Version 1.3 Notes: Removed options to run Base Heights and Building Heights separately. Added an option for running a buffer
#                   under adnaved options.  The default is to use the building polygons without buffer.
# Version 1.4 Notes: added option to run Base Heights and Building Heights separately. Buffer is only used for Building Heights calculation.
#                   added new tool
#######################################

#import modules
import arcpy, os, arcpy.sa

# Check out any necessary licenses
arcpy.CheckOutExtension("spatial")
arcpy.CheckProduct("arcinfo")
arcpy.CheckOutExtension("3D")


class Toolbox(object):
    def __init__(self):
        """Define the toolbox (the name of the toolbox is the name of the
        .pyt file)."""
        self.label = "3D Height Tools"
        self.alias = ""

        # List of tool classes associated with this toolbox
        self.tools = [CalculateBuildingElevations, CalculateTreeHeightFromPoints, CalculateTreeHeights]


class CalculateBuildingElevations(object):
    def __init__(self):
        """Define the tool (tool name is the name of the class)."""
        self.label = "Calculate Building Elevations"
        self.description = ""
        self.canRunInBackground = False

    def getParameterInfo(self):
        """Define parameter definitions"""
        pInputBuildings = arcpy.Parameter(
            displayName="Buildings",
            name="inputBuildings",
            datatype="DEFeatureClass",
            parameterType="Required",
            direction="Input")
        pInputBuildings.filter.list = ["Polygon"]

        pIdField = arcpy.Parameter(
            displayName="Unique Building Identifier",
            name="idField",
            datatype="GPString",
            parameterType="Required",
            direction="Input")
        pIdField.filter.type = "ValueList"

        pInput3DCIM = arcpy.Parameter(
            displayName="Input data uses 3DCIM schema",
            name="input3DCIM",
            datatype="GPBoolean",
            parameterType="Required",
            direction="Input")
        pInput3DCIM.value = False

        pCalculateBaseElevation = arcpy.Parameter(
            displayName="Calculate Base Elevation",
            name="calcBaseHeight",
            datatype="GPBoolean",
            parameterType="Required",
            direction="Input",
            category ="Base Elevation")
        pCalculateBaseElevation.value = False

        pInputDEM = arcpy.Parameter(
            displayName="Elevation Raster",
            name="inputDEM",
            datatype="DERasterDataset",
            parameterType="Optional",
            direction="Input",
            category ="Base Elevation")

        pDTMStatType = arcpy.Parameter(
            displayName="Statistic Type",
            name="statType",
            datatype="GPString",
            parameterType="Optional",
            direction="Input",
            category ="Base Elevation")
        pDTMStatType.filter.type = "ValueList"
        pDTMStatType.filter.list = ["MINIMUM", "MAXIMUM", "MEAN"]
        pDTMStatType.value = "MEAN"

        pCalculateBuildingHeight = arcpy.Parameter(
            displayName="Calculate Building Height",
            name="calcBuildingHeight",
            datatype="GPBoolean",
            parameterType="Required",
            direction="Input",
            category ="Total Height")
        pCalculateBuildingHeight.value = False

        pInputNDSM = arcpy.Parameter(
            displayName="nDSM Raster",
            name="inputNDSM",
            datatype="DERasterDataset",
            parameterType="Optional",
            direction="Input",
            category ="Total Height")

        pBuffer = arcpy.Parameter(
            displayName="Buffer buildings",
            name="useBuffer",
            datatype="GPBoolean",
            parameterType="Optional",
            direction="Input",
            category ="Total Height")
        pBuffer.value = False

        pBufferDist = arcpy.Parameter(
            displayName="Buffer Distance",
            name="BufferDist",
            datatype="GPLinearUnit",
            parameterType="Optional",
            direction="Input",
            category ="Total Height")

        pOverwriteExistingHeights = arcpy.Parameter(
            displayName="Overwrite Existing Heights",
            name="overwriteExistingHeights",
            datatype="GPBoolean",
            parameterType="Optional",
            direction="Input",
            category ="Total Height")
        pOverwriteExistingHeights.value = False

        parameters = [pInputBuildings, pIdField, pInput3DCIM, pCalculateBaseElevation,  pInputDEM, pDTMStatType, pCalculateBuildingHeight, pInputNDSM, pBuffer, pBufferDist, pOverwriteExistingHeights]
        return parameters

    def isLicensed(self):
        """Set whether tool is licensed to execute."""
        return True

    def updateParameters(self, parameters):
        """Modify the values and properties of parameters before internal
        validation is performed.  This method is called whenever a parameter
        has been changed."""
        if parameters[0].value:
            fields = arcpy.ListFields(parameters[0].ValueAsText)
            fieldNames = [f.name for f in fields]
            parameters[1].filter.list = fieldNames


        #Check Base Height options
        if parameters[3].value == True:
            parameters[4].enabled = True
            parameters[5].enabled = True
        else:
            parameters[4].enabled = False
            parameters[5].enabled = False

        #Check Building Total Height (and buffer) options
        if parameters[6].value == True:
            parameters[7].enabled = True
            parameters[8].enabled = True
            parameters[10].enabled = True
        else:
            parameters[7].enabled = False
            parameters[8].enabled = False
            parameters[10].enabled = False

        #Check buffer options
        if (parameters[8].value == True and parameters[6].value == True):
            parameters[9].enabled = True
        else:
            parameters[9].enabled = False

        #populate buffer with default value when raster is changed, if user has not changed it
        if parameters[7].value:
            if parameters[9].altered:
                pass
            else:
                desc = arcpy.Describe(parameters[7].value)
                #linearUnit = desc.spatialReference.linearUnitName
                cellSize = desc.meanCellWidth
                # workaround to handle scenario where unit is not accepted by the linearUnit dropdown
                parameters[9].value = str((cellSize * -2)) + " UNKNOWN"


    def updateMessages(self, parameters):
        """Modify the messages created by internal validation for each tool
        parameter.  This method is called after internal validation."""

        # Process: Delete Existing RASTERVALU, Building Heights, and Elevations
        fieldName = "RASTERVALU"
        fieldName2 = "BuildingHeight"
        fieldName3 = "BaseHeight"
        fieldsPresent = []
        printFields = 0

        fc = parameters[0].value
        if fc != None:
            ##fcName = os.path.basename(fc)
            fields = arcpy.ListFields(fc)
            for field in fields:
                if field.name == fieldName:
                    fieldsPresent.append(fieldName)
                    printFields = 1
                if field.name == fieldName2:
                    fieldsPresent.append(fieldName2)
                    printFields = 1
                if field.name == fieldName3:
                    fieldsPresent.append(fieldName3)
                    printFields = 1
            if printFields == 1:
                parameters[0].setWarningMessage("The '{}' feature class has the [{}] field(s) present. Running this tool will remove the field from the attribute table.".format(fc, fieldsPresent))

        if (parameters[4].enabled == True and parameters[4].value == None):
            parameters[4].setErrorMessage('An elevation raster is required.')

        if (parameters[7].enabled == True and parameters[7].value == None):
            parameters[7].setErrorMessage('An nDSM raster is required.')

        if parameters[8].value == True:
            parameters[8].setWarningMessage('Negative buffer values entered have the potential to eliminate the feature geometry for a feature and create NULL values for the BaseHeight and BuildingHeight fields in those features.')

        if parameters[10].value == True:
            parameters[10].setWarningMessage('Exisitng Height values will be overwritten.')



    def execute(self, parameters, messages):
        """The source code of the tool."""

        arcpy.env.overwriteOutput = True

        try:

            # Script arguments
            buildingFC = parameters[0].value
            zoneField = parameters[1].value
            use3DCIM = parameters[2].value
            calcBaseHeight = parameters[3].value
            if (calcBaseHeight):
                DTM_Raster = parameters[4].value
                dtmStatisticType = parameters[5].value

            calcBldgTotalHeight = parameters[6].value
            if (calcBldgTotalHeight):
                nDSM_Raster = parameters[7].value
                useBuffer = parameters[8].value
                if (useBuffer):
                    bufferDist = parameters[9].value
                overwriteExistingHt = parameters[10].value

            v3d_City_Information_Model = os.path.dirname(str(buildingFC))
            arcpy.env.workspace = v3d_City_Information_Model

            scratchGDB = "in_memory"
            #scratchGDB = arcpy.env.scratchGDB
            #arcpy.AddMessage(arcpy.env.scratchGDB)

            # Local variables:
            buildingFCName = os.path.basename(str(buildingFC))
            zonalStats_Bldgs = scratchGDB +"/ZonalStats_BLDGS"
            zonalStats_Bldgs_MAX = scratchGDB + "/ZonalStats_Bldgs_MAX"
            building_Point = scratchGDB + "/BuildingPT"
            building_Buffer = scratchGDB + "/BuffBuild"
            Building_Point_Elevations_shp = scratchGDB + "/BuildingPtsWelevation"

            # Process: Feature To Point
            arcpy.FeatureToPoint_management(buildingFC, building_Point, "INSIDE")
            arcpy.AddMessage ("Buildings converted to points - Done")

            fieldList = arcpy.ListFields(buildingFC)

            #*************
            # Base Heights
            #*************
            if (calcBaseHeight):
                # Create in_memory Building layer
                buildingTemp = arcpy.FeatureClassToFeatureClass_conversion(buildingFC, "in_memory" , "BuildingTemp" , "", "", "")

                # Process: Delete field
                for field in fieldList:
                    if field.name == "BaseHeight":
                        arcpy.DeleteField_management(buildingTemp, "BaseHeight")
                        arcpy.AddMessage ("BaseHeight field deleted")

                # Process: Zonal Statistics
                arcpy.gp.ZonalStatistics_sa(buildingFC, zoneField, DTM_Raster, zonalStats_Bldgs, dtmStatisticType, "DATA")
                arcpy.AddMessage ("Zonal Statistics: DTM - Done")

                # Process: Extract Values to Points
                arcpy.sa.ExtractMultiValuesToPoints(building_Point, [[zonalStats_Bldgs, "BaseHeight"]], "NONE")

                # Process: Join Field
                arcpy.JoinField_management(buildingTemp, zoneField, building_Point, zoneField, ["BaseHeight"])
                arcpy.AddMessage ("Joined Field to original building features - Done")

                # Process: Feature To 3D By Attribute
                arcpy.FeatureTo3DByAttribute_3d(buildingTemp, Building_Point_Elevations_shp, "BaseHeight", "")
                arcpy.AddMessage ("Converted 2d polygon to 3d - Done")

                 # Process: Calculate Total Height Only for 3DCIM
                if use3DCIM == True:
                    arcpy.DeleteField_management(Building_Point_Elevations_shp, ["BaseHeight"])

                arcpy.FeatureClassToFeatureClass_conversion(Building_Point_Elevations_shp, v3d_City_Information_Model , buildingFCName , "", "", "")
                arcpy.AddMessage ("Feature Class to Feature Class Done")

            #*************
            # Building Heights
            #*************
            if (calcBldgTotalHeight):
                # Process: Determine whether to use building footprints or the buffered buildings.
                if useBuffer == False:
                    inputBuildingZones = buildingFC
                else:
                    # Process: Buffer Polygons
                    arcpy.Buffer_analysis(buildingFC, building_Buffer, bufferDist, 'FULL', 'ROUND', 'NONE', '#')
                    arcpy.AddMessage ("Buildings buffered - Done")
                    inputBuildingZones = building_Buffer

                arcpy.gp.ZonalStatistics_sa(inputBuildingZones, zoneField, nDSM_Raster, zonalStats_Bldgs_MAX, "MAXIMUM", "DATA")
                arcpy.AddMessage ("Zonal Statistics: nDSM - Done")

                # Process: Extract Values to Points
                arcpy.sa.ExtractMultiValuesToPoints(building_Point, [[zonalStats_Bldgs_MAX, "BuildingHeight"]], "NONE")

                # Process: Delete field
                for field in fieldList:
                    if field.name == "BuildingHeight":
                        arcpy.DeleteField_management(buildingFC, "BuildingHeight")
                        arcpy.AddMessage ("BuildingHeight field deleted")

                # Process: Join Field
                arcpy.JoinField_management(buildingFC, zoneField, building_Point, zoneField, ["BuildingHeight"])
                arcpy.AddMessage ("Joined Field to original building features - Done")

                #create lyr
                buildingFCLyr = "in_memory\\" + buildingFCName + "Lyr"
                arcpy.MakeFeatureLayer_management(buildingFC,buildingFCLyr )

                if (overwriteExistingHt == False):
                    heightFld = arcpy.AddFieldDelimiters(buildingFCLyr, "totalHeight")
                    # SelectLayerByAttribute to select rows with null or zero values
                    nullZeroCriteria = heightFld +  " <= 0 OR " + heightFld + " IS NULL"
                    arcpy.SelectLayerByAttribute_management(buildingFCLyr, "NEW_SELECTION", nullZeroCriteria)
                    arcpy.AddMessage("Row count where Height is zero or less than zero or null = " +  arcpy.GetCount_management(buildingFCLyr).getOutput(0))

                # Use CalculateField to update selected rows or all rows
                arcpy.CalculateField_management(buildingFCLyr, "totalHeight", "!BuildingHeight!", "PYTHON_9.3", "#")
                arcpy.AddMessage ("Height values updated")


                # Process: Calculate Total Height Only for 3DCIM
                if use3DCIM == True:
                    #arcpy.CalculateField_management(buildingFC, "totalHeight", "!BuildingHeight!", "PYTHON_9.3", "#")
                    arcpy.DeleteField_management(buildingFC, ["BuildingHeight"])

        except Exception as e:
            arcpy.AddError("Exception : " + e.message)


class CalculateTreeHeightFromPoints(object):
    def __init__(self):
        """Define the tool (tool name is the name of the class)."""
        self.label = "Calculate Tree Heights from Points"
        self.description = ""
        self.canRunInBackground = False

    def getParameterInfo(self):
        """Define parameter definitions"""
        inputTreePts = arcpy.Parameter(
            displayName="Trees",
            name="inputTreePts",
            datatype="DEFeatureClass",
            parameterType="Required",
            direction="Input")
        inputTreePts.filter.list = ["Point"]

        idField = arcpy.Parameter(
            displayName="Unique Tree ID",
            name="idField",
            datatype="GPString",
            parameterType="Required",
            direction="Input")
        idField.filter.type = "ValueList"


        pCalculateBaseElevation = arcpy.Parameter(
            displayName="Calculate Base Elevation",
            name="calcBaseHeight",
            datatype="GPBoolean",
            parameterType="Required",
            direction="Input",
            category ="Base Elevation")
        pCalculateBaseElevation.value = False

        pGroundDTEM= arcpy.Parameter(
            displayName="Ground Raster",
            name="inputGroundDTM",
            datatype="DERasterDataset",
            parameterType="Optional",
            direction="Input",
            category ="Base Elevation")

        pCalculateTreeHeight = arcpy.Parameter(
            displayName="Calculate Tree Height",
            name="calcTreeHeight",
            datatype="GPBoolean",
            parameterType="Required",
            direction="Input",
            category ="Total Height")
        pCalculateTreeHeight.value = False

        inputNDSM = arcpy.Parameter(
            displayName="Normalized Digital Surface Model",
            name="inputNDSM",
            datatype="DERasterDataset",
            parameterType="Optional",
            direction="Input",
            category ="Total Height")

        inputMinimumHeight = arcpy.Parameter(
            displayName="Minimum Tree Height",
            name="inputHeight",
            datatype="GPDouble",
            parameterType="Optional",
            direction="Input",
            category ="Total Height")
        inputMinimumHeight.value = 5 #****

        pBufferDist = arcpy.Parameter(
            displayName="Buffer Distance",
            name="bufferDistance",
            datatype="GPLinearUnit",
            parameterType="Optional",
            direction="Input",
            category ="Total Height")

        pOverwriteExistingHeights = arcpy.Parameter(
            displayName="Overwrite Existing Heights",
            name="overwriteExistingHeights",
            datatype="GPBoolean",
            parameterType="Optional",
            direction="Input",
            category ="Total Height")
        pOverwriteExistingHeights.value = False

        parameters = [inputTreePts, idField, pCalculateBaseElevation, pGroundDTEM, pCalculateTreeHeight, inputNDSM, inputMinimumHeight, pBufferDist, pOverwriteExistingHeights ]
        return parameters

    def isLicensed(self):
        """Set whether tool is licensed to execute."""
        return True

    def updateParameters(self, parameters):
        """Modify the values and properties of parameters before internal
        validation is performed.  This method is called whenever a parameter
        has been changed."""
        if parameters[0].value:
            fields = arcpy.ListFields(parameters[0].ValueAsText)
            fieldNames = [f.name for f in fields]
            parameters[1].filter.list = fieldNames


        #Check Base Height options
        if parameters[2].value == True:
            parameters[3].enabled = True
        else:
            parameters[3].enabled = False

        #Check Tree Total Height (and buffer) options
        if parameters[4].value == True:
            parameters[5].enabled = True
            parameters[6].enabled = True
            parameters[7].enabled = True
            parameters[8].enabled = True
        else:
            parameters[5].enabled = False
            parameters[6].enabled = False
            parameters[7].enabled = False
            parameters[8].enabled = False

        #populate buffer with default value when raster is changed, if user has not changed it
        if parameters[5].value:
            if parameters[7].altered:
                pass
            else:
                # add positive bufferDist to create buffer around point
                desc = arcpy.Describe(parameters[5].value)
                #linearUnit = desc.spatialReference.linearUnitName
                cellSize = desc.meanCellWidth
                # workaround to handle scenario where unit is not accepted by the linearUnit dropdown
                parameters[7].value = str((cellSize * 2)) + " UNKNOWN"


    def updateMessages(self, parameters):
        """Modify the messages created by internal validation for each tool
        parameter.  This method is called after internal validation."""


        if (parameters[3].enabled == True and parameters[3].value == None):
            parameters[3].setErrorMessage('A ground raster is required.')


        if (parameters[5].enabled == True and parameters[5].value == None):
            parameters[5].setErrorMessage('An nDSM raster is required.')

        if (parameters[6].enabled == True and parameters[6].value == None):
            parameters[6].setErrorMessage('A minimum height value is required.')

        if parameters[7].value == True:
            parameters[7].setWarningMessage('Negative buffer values entered have the potential to eliminate the feature geometry for a feature and create NULL values for the BaseHeight and BuildingHeight fields in those features.')

        if parameters[8].value == True:
            parameters[8].setWarningMessage('Exisitng Height values will be overwritten.')


    def execute(self, parameters, messages):
        """The source code of the tool."""

        arcpy.env.overwriteOutput = True

        try:

            # Script arguments
            treePtFC = parameters[0].value
            treePtIDField = parameters[1].value

            calcBaseHeight = parameters[2].value
            if (calcBaseHeight):
                DTM_Raster = parameters[3].value

            calcTreeTotalHeight = parameters[4].value
            if (calcTreeTotalHeight):
                nDSM_Raster = parameters[5].value
                minTreeHt = parameters[6].value
                bufferDist = parameters[7].value
                overwriteExistingHt = parameters[8].value

            v3d_City_Information_Model = os.path.dirname(str(treePtFC))
            arcpy.env.workspace = v3d_City_Information_Model

            #scratchGDB = "in_memory"
            scratchGDB = arcpy.env.scratchGDB
            #arcpy.AddMessage(arcpy.env.scratchGDB)

            # Local variables:
            treePtFCName = os.path.basename(str(treePtFC))
            zonalStats_Trees = scratchGDB +"/ZonalStats_Trees"
            treePt3DTemp = scratchGDB + "/TreePt3DTemp"
            zonalStats_Trees_MAX = scratchGDB + "/ZonalStats_Trees_MAX"
            zonalStats_Trees_MinHt = scratchGDB + "/ZonalStats_Trees_MinHt"
            treePt_Buffer = scratchGDB + "/TreePt_Buffer"
            tempTableView = scratchGDB + "/TreePtTempTableView"

            fieldList = arcpy.ListFields(treePtFC)

            #*************
            # Base Heights
            #*************
            if (calcBaseHeight):
                # Create in_memory Building layer
                treePtTemp = arcpy.FeatureClassToFeatureClass_conversion(treePtFC, scratchGDB , "TreePtTemp" , "", "", "")

                # Process: Delete field
                for field in fieldList:
                    if field.name == "BaseHeight":
                        arcpy.DeleteField_management(treePtTemp, "BaseHeight")
                        arcpy.AddMessage ("BaseHeight field deleted")

                # Process: Zonal Statistics
                arcpy.gp.ZonalStatistics_sa(treePtFC, treePtIDField, DTM_Raster, zonalStats_Trees, "", "DATA")
                arcpy.AddMessage ("Zonal Statistics: DTM - Done")

                # Process: Extract Values to Points
                arcpy.sa.ExtractMultiValuesToPoints(treePtTemp, [[zonalStats_Trees, "BaseHeight"]], "NONE")

                # Process: Feature To 3D By Attribute
                arcpy.FeatureTo3DByAttribute_3d(treePtTemp, treePt3DTemp, "BaseHeight", "")
                arcpy.AddMessage ("Converted 2d point to 3d - Done")

                arcpy.DeleteField_management(treePt3DTemp, "BaseHeight")

                nameTreePtFC = os.path.basename(str(treePtFC))
                arcpy.FeatureClassToFeatureClass_conversion(treePt3DTemp, v3d_City_Information_Model , nameTreePtFC, "", "", "")
                arcpy.AddMessage ("Feature Class to Feature Class Done")

            #*************
            # Tree Heights
            #*************
            if (calcTreeTotalHeight):

                # Process: Buffer Point
                arcpy.Buffer_analysis(treePtFC, treePt_Buffer, bufferDist, 'FULL', 'ROUND', 'NONE', '#')
                arcpy.AddMessage ("Tree Points buffered - Done")

                # Get the Max value
                arcpy.sa.ZonalStatisticsAsTable(treePt_Buffer, treePtIDField, nDSM_Raster, zonalStats_Trees_MAX, "DATA", "MAXIMUM")
                arcpy.AddMessage ("Zonal Statistics: nDSM - Done")

                # Table View
                arcpy.MakeTableView_management(zonalStats_Trees_MAX, tempTableView)

                # SelectLayerByAttribute to determine which rows to delete
                minHtCriteria = "MAX <= {0}".format(minTreeHt)
                arcpy.SelectLayerByAttribute_management(tempTableView, "NEW_SELECTION", minHtCriteria)

                # Delete rows with values less than min height
                arcpy.DeleteRows_management(tempTableView)
                arcpy.AddMessage ("Minimum Height query applied")

                # Join MAX field to input table
                arcpy.JoinField_management(treePtFC, treePtIDField, tempTableView, treePtIDField, ["MAX"] )

                #create lyr
                treePtFCLyr = "in_memory\\" + treePtFCName + "Lyr"
                arcpy.MakeFeatureLayer_management(treePtFC,treePtFCLyr )

                if (overwriteExistingHt == False):
                    heightFld = arcpy.AddFieldDelimiters(treePtFCLyr, "height")
                    # SelectLayerByAttribute to select rows with null or zero values
                    nullZeroCriteria = heightFld +  " <= 0 OR " + heightFld + " IS NULL"
                    arcpy.SelectLayerByAttribute_management(treePtFCLyr, "NEW_SELECTION", nullZeroCriteria)
                    arcpy.AddMessage("Row count where Height is zero or less than zero or null = " +  arcpy.GetCount_management(treePtFCLyr).getOutput(0))

                # Use CalculateField to update selected rows or all rows
                arcpy.CalculateField_management(treePtFCLyr, "height", "!MAX!", "PYTHON_9.3", "#")
                arcpy.AddMessage ("Height values updated")


                # Process: Delete field
                fieldList = arcpy.ListFields(treePtFCLyr)
                for field in fieldList:
                    if field.name == "MAX":
                        arcpy.DeleteField_management(treePtFCLyr, "MAX")

        except Exception as e:
            arcpy.AddError("Exception : " + e.message)



class CalculateTreeHeights(object):
    def __init__(self):
        """Define the tool (tool name is the name of the class)."""
        self.label = "Calculate Tree Heights"
        self.description = ""
        self.canRunInBackground = False

    def getParameterInfo(self):
        """Define parameter definitions"""
        inputCanopies = arcpy.Parameter(
            displayName="Tree Canopies",
            name="inputCanopies",
            datatype="DEFeatureClass",
            parameterType="Required",
            direction="Input")
        inputCanopies.filter.list = ["Polygon"]

        idField = arcpy.Parameter(
            displayName="Unique Tree ID",
            name="idField",
            datatype="GPString",
            parameterType="Required",
            direction="Input")
        idField.filter.type = "ValueList"

        inputHeight = arcpy.Parameter(
            displayName="Minimum Tree Height",
            name="inputHeight",
            datatype="GPDouble",
            parameterType="Required",
            direction="Input")
        inputHeight.value = 5

        inputNDSM = arcpy.Parameter(
            displayName="Normalized Digital Surface Model",
            name="inputNDSM",
            datatype="DERasterDataset",
            parameterType="Required",
            direction="Input")

        inputGround = arcpy.Parameter(
            displayName="Ground Raster",
            name="inputGround",
            datatype="DERasterDataset",
            parameterType="Required",
            direction="Input")

        appendValue = arcpy.Parameter(
            displayName="Append trees to existing feature class",
            name="appendValue",
            datatype="GPBoolean",
            parameterType="Required",
            direction="Input")
        appendValue.value = True

        appendTrees = arcpy.Parameter(
            displayName="Existing Tree Feature Class",
            name="appendTrees",
            datatype="DEFeatureClass",
            parameterType="Optional",
            direction="Input")

        outputTrees = arcpy.Parameter(
            displayName="New Tree Feature Class",
            name="outputTrees",
            datatype="DEFeatureClass",
            parameterType="Optional",
            direction="Output")
        outputTrees.enabled = False

        parameters = [inputCanopies, idField, inputHeight, inputNDSM, inputGround, appendValue, appendTrees, outputTrees]
        return parameters
    def isLicensed(self):
        """Set whether tool is licensed to execute."""
        return True

    def updateParameters(self, parameters):
        """Modify the values and properties of parameters before internal
        validation is performed.  This method is called whenever a parameter
        has been changed."""
        if parameters[0].value:
            fields = arcpy.ListFields(parameters[0].ValueAsText)
            fieldNames = [f.name for f in fields]
            parameters[1].filter.list = fieldNames

        if parameters[5].value == False:
            parameters[6].enabled = False
            parameters[7].enabled = True
        else:
            parameters[6].enabled = True
            parameters[7].enabled = False

    def updateMessages(self, parameters):
        """Modify the messages created by internal validation for each tool
        parameter.  This method is called after internal validation."""
        if parameters[5].value == False and parameters[7].value == None:
            parameters[7].setIDMessage("Error", 735, parameters[7].displayName)
        elif parameters[5].value == True and parameters[6].value == None:
            parameters[6].setIDMessage("Error", 735, parameters[6].displayName)

    def execute(self, parameters, messages):
        """The source code of the tool."""
        #script arguments
        arcpy.env.overwriteOutput = True

        treecircles = str(parameters[0].value)
        treecircledir = os.path.dirname(treecircles)
        treeID = parameters[1].value
        treepoints = "in_memory/treepoint"
        treepointBuff = "in_memory/treepointbuff"
        nDSM = parameters[3].value
        zonalheight = treecircledir + "/zonalheight"
        selectedTreeCircles = "in_memory/SelectedTreeCircles"
        selectionCriteria = "Height > {}".format(parameters[2].value)
        #modifiedSelectionCrit = "'"+ selectionCriteria +"'"

        NewTrees = str(parameters[7].value)
        NewTreeActual = os.path.basename(NewTrees)
        NewTreeDir =os.path.dirname(NewTrees)
        existingTrees = str(parameters[6].value)
        pointsWheight = "in_memory/ptsWHeight"
        tree3d ="in_memory/ThreeDTrees"
        GroundRaster = parameters[4].value

        #Check for keys fields
        diameterField = False
        heightField = False
        valueField = False

        canopyFields = arcpy.ListFields(treecircles)
        for field in canopyFields:
            if field.name == "diameter":
                diameterField = True
                arcpy.AddMessage("Diameter field exists")
            elif field.name == "height":
                heightField = True
                arcpy.AddMessage("Height field exists")
            elif field.name == "RASTERVALU":
                valueField = True
                arcpy.AddMessage("RASTERVALUE field exists")
            else:
                pass

        #Delete RASTERVALU field
        if valueField == True:
            arcpy.DeleteField_management(treecircles, "RASTERVALU")
            arcpy.AddMessage("Deleted RASTERVALU Field")
        else:
            pass

        #Add Diameter Field
        if diameterField != True:
                arcpy.AddField_management(treecircles, "diameter", "FLOAT", "","","","","NULLABLE","NON_REQUIRED","")
                arcpy.AddMessage ("Diameter Field Added to Tree Crowns")
        else:
            pass

        #calculate diameter field
        arcpy.CalculateField_management(treecircles,"diameter", "2*(( [SHAPE_Area]/3.131592)^(1/2))", "VB")
        arcpy.AddMessage ("Diameter Field Calculated on Tree Crowns")

        #Feature to Point
        arcpy.FeatureToPoint_management(treecircles,treepoints,"INSIDE")
        arcpy.AddMessage ("Feature Converted To Point")

        #Add canopydiameterhalf Field
        arcpy.AddField_management(treepoints, "canopydiameterhalf", "DOUBLE", "", "","","","NULLABLE","NON_REQUIRED","")
        arcpy.AddMessage ("Canopy Diameter Field Added")

        #calculate canopyhalfdiameter field
        arcpy.CalculateField_management(treepoints,'canopydiameterhalf', "[diameter]/4", "VB","")
        arcpy.AddMessage ("Canopy Diameter Field Calculated")

        #buffer out
        arcpy.Buffer_analysis(treepoints, treepointBuff, 'canopydiameterhalf', 'FULL','ROUND', 'NONE', "" )
        arcpy.AddMessage ("Points Buffered")

        #zonal stats calculation
        arcpy.gp.ZonalStatistics_sa(treepointBuff, treeID, nDSM, zonalheight, 'MAXIMUM',"DATA")
        arcpy.AddMessage ("Zonal Stats Calculated")

        ###extract Values to Point
        ##arcpy.sa.ExtractValuesToPoints(treepoints,zonalheight,pointsWheight,"INTERPOLATE","VALUE_ONLY")
        ##arcpy.AddMessage ("Values Extracted to Points")

        #Extract Multi Values To Point
        arcpy.sa.ExtractMultiValuesToPoints(treepoints,[[zonalheight, "RASTERVALU"]],"")
        arcpy.AddMessage ("Extracted Multivalues to Point")

        ###Create New Feature Class
        ####arcpy.FeatureClassToFeatureClass_conversion(treepoints,treecircledir,"PtswHeight","","")
        ##arcpy.FeatureClassToFeatureClass_conversion(pointsWheight,treecircledir,"PtswHeight","","")
        ##arcpy.AddMessage ("ZonalHeight feature class created")

        ### Process: Join Field
        ##arcpy.JoinField_management(treecircles, 'TreeFID', pointsWheight, 'TreeFID', "RASTERVALU")
        ###arcpy.JoinField_management(pointsWheight, 'TreeFID', treepoints, 'TreeFID', "RASTERVALU")
        ##arcpy.AddMessage ("Joined Field to original tree crowns - Done")



        #Add Height Field
        if heightField != True:
            arcpy.AddField_management(treepoints, "HEIGHT", "DOUBLE", "","","","","NULLABLE","NON_REQUIRED","")
            arcpy.AddMessage ("Height Field Added")
        else:
            pass

        #Populate Height Field
        arcpy.CalculateField_management(treepoints,"HEIGHT","[RASTERVALU]", 'VB',"")
        arcpy.AddMessage ("Height Field Populated")

        #Delete Field
        arcpy.DeleteField_management(treepoints,"RASTERVALU")
        arcpy.AddMessage ("RASTERVALU Deleted")

        #Select Points
        arcpy.Select_analysis(treepoints,selectedTreeCircles,selectionCriteria)
        arcpy.AddMessage ("Points Selected")

        #Make Tree Points 3d
        arcpy.InterpolateShape_3d(GroundRaster,selectedTreeCircles,tree3d,"","","BILINEAR", "DENSIFY","0")
        arcpy.AddMessage("It is 3d")

        #Append or Create New Feature Class
        if parameters[5].value == True:
            #Appending the data
            arcpy.Append_management(tree3d, existingTrees,"NO_TEST","","")
            arcpy.AddMessage ("Data Appended")

            #Delete Identical
            arcpy.DeleteIdentical_management(existingTrees, treeID)
            arcpy.AddMessage ("Identical info deleted")
        else:
            #Create New Feature Class
            arcpy.FeatureClassToFeatureClass_conversion(tree3d,NewTreeDir,NewTreeActual,"","")
            arcpy.AddMessage ("Tree File Created")

            #Delete Identical
            arcpy.DeleteIdentical_management(NewTrees, treeID)
            arcpy.AddMessage ("Identical info deleted")
