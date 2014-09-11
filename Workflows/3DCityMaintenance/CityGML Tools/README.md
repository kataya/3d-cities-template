# Import and Export City GML Data
Interoperability between CityGML and 3DCIM platforms is essential to achieve all the functionality of the 3D Cities environment.  

In this workflow, you will learn how to use the tools which will be used for 3DCIM_CityGML toolbox for interoperability between CityGML files and 3DCIM:
* Import CityGML data into the 3DCIM environment
* Export 3DCIM features back into CityGML data

## Here's What You Need

Start by gathering the items listed below and following the steps in the directions. More detailed information is provided for each step

* ArcGIS Desktop 10.2.2
    * Data Interoperability Extension 10.2.2 or FME© Desktop 2014 
* City GML 1.0 or 2.0 source data
    * For samples of CityGML data that you can use in the workflow go to [Sample CityGML    Data](http://www.citygml.org/index.php?id=1539)
* 3D Cities Template
    * Download the CityGML Import and Export Toolbox https://github.com/Esri/3d-cities-template and save the toolbox to a local folder on your computer.
    * Download the current XML Workspace Document from [2] https://github.com/Esri/3d-cities-template/tree/master/InformationModel

## Directions

In this section:

* Step 1: Install the 3DCIM CityGML toolbox
* Step 2: Validate CityGML files ETL Tool
* Step 3: Import CityGML files into 3DCIM
* Step 4: Export 3DCIM files into CityGML format

## Step 1: Install the 3DCIM CityGML toolbox

After you have downloaded the 3D Cities template, you will activate the Data Interoperability Extension.

1. Start ArcCatalog, ArcMap, or  ArcScene.
2. In the Menu bar choose Customize> Extensions and check Data Interoperability.

Next, we will add add 3DCIM CityGML toolbox to our toolboxes. 

1. In ArcCatalog, go to the location you saved the 3D Cities Template.
2. Open up Workflows>3DCityMaintenance>CityGML Tools.

After adding the 3DCIM CityGML toolbox to the ArcGIS toolboxes, open the toolbox and see that it contains three different toolsets.  

* CityGML Export Toolset
* CityGML Import Toolset
* Helper Tools Toolset
 
## Step 2: Validate CityGML files ETL Tool 

In order for the 3CDIM CityGML toolbox to work, all CityGML files must be validated before use.  To do this, you will use the **Validate CityGML** files tool.  For more information on valid CityGML schema’s Versions 1.0 and 2.0 OGC please visit     (http://www.opengeospatial.org/standards/citygml).  To validate your CityGML files do the following:

1. Open the Helper Tools toolset
1. Open the Validate  CityGML files

This tool will verify that the Schema is correct for migrating into the 3DCIM.  If the data is not valid, the tool will display that CityGML data is invalid.  Some common reasons for invalid data are:

* Texture errors
* Undefined spatial reference
* Missing attributes

## Step 3: Import CityGML files into 3DCIM

In this step you will import valid CityGML files into the 3DCIM environment using the **CityGML Import** toolset.  Open the toolset and see that it has the following ETL tools:

* **Import Building** – imports solid CityGML 3D geometric 
* **Import Building LoD 1 and 2**  -imports CityGML LoD1 and LoD2 defined buildings into 3DCIM BuildingShell 
* **Import CityFurniture** -imports CityGML defined street furniture 
* **Import LandCover** - imports CityGML defined AuxilaryTrafficArea, TrafficArea, PlantCover, WaterBody, WaterGroundSurface, WaterClosureSurface and WaterSurface areas into the 
* **Import LandUse** -imports CityGML defined LandUse 
* **Import SolitaryVegitationObject** -  imports  CityGML 

In our example, we will use the Import Building tool.  To open, double-click the tool (or right-click and choose Open). 

**Source CityGML File(s):** Select CityGML file(s) to process.

**GML SRS Axis Order (optional):** In some cases (when the coordinate system is not defined or unknown) it might be necessary to define the axis order. 1, 2, 3 corresponds to an axis order of x, y, z.

**Ingnore xsi:schemaLocation in Dataset (optional):** Choose Yes in case the CityGML file contains local schema definitions.

**Destination Esri File Geodatabase:** Choose the name and location for the destination 3DCIM Geodatabase. Create a new 3DCIM Geodatabase or choose an existing one.

**XML Workspace Document (optional):** In case a new 3DCIM Geodatabase should be created, choose the XML Workspace Document you find in the examples folder.

**Overwrite Existing Geodatabase:** Choose Yes in case an existing 3DCIM Geodatabase should be overwritten. In this case, also enter the location path of the ML Workspace Document under XML Workspace Document.

The Output is now a Building Feature Class in the 3DCIM with new aggregated CityGML attributes.

## Step 4: Export 3DCIM files CityGML format

In this step you will export 3DCIM features back into CityGML format using the **CityGML Export** toolset.  When you open the toolset you will see that it has the following ETL tools:

* **Export Building** -  exports 3DCIM   Building and BuildingShell features to CityGML defined building module.
* **Export CityFurniture**- exports 3DCIM StreetFurniture features to CityGML defined street furniture module. 
* **Export LandCover** - exports 3DCIM LandCover features to CityGML defined AuxilaryTrafficArea, TrafficArea, PlantCover, * WaterBody, WaterGroundSurface, WaterClosureSurface and WaterSurface areas.
* **Export LandUse** -exports 3DCIM Usage and ZoningDistrict features to CityGML defined LandUse areas. 
* **Export SolitaryVegetationObject** - exports 3DCIM defined Trees feature to CityGML SolitaryVegetationObject.

In our example, we will use the **Export Building** tool.  To open, double-click the tool (or right-click and choose Open).  

**Source Esri File Geodatabase:** Choose the 3CDIM Geodatabase to from where to export CityGML file(s).

**CityGML Version:** Choose between CityGML Version 1.0 or 2.0. 

**Export with Building Footprint (CityGML 2.0 only):** CityGML 2.0 introduced a new geometry called LoD0FootPrint (a 2.5 polygon describing the building footprint). Choose Yes in case footprints should be exported.

**Destination CityGML Document:** Choose folder and name for the CityGML file. 

**CityGML Name (gml:name) (optional):** Assign a CityGML CityModel name.

**CityGML description (gml:description) (optional):** Enter a CityGML CityModel description.

The Output is now a Building Feature Class in the in CityGML 3DCIM attributes aggregated back to CityGML attributes.

###3DCIM CityGML ETL Tools Customization 

Since all CityGML Import and Export Tools are based on ETL Tools it is possible to customize them. To do this, right-click on the tool and choose Edit.  Please note that an advanced knowledge of FME is required to customize the tools.
