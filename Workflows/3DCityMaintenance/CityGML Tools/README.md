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

