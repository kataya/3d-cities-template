# ------------------------------------------------------------------------------
# 3D City Information Model Python Toolbox/SchemaGenerator
# 1.2.0_2013-06-14
#
#
# Author: Thorsten Reitz, ESRI R&D Lab Zurich
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
# http://www.apache.org/licenses/LICENSE-2.0
# ------------------------------------------------------------------------------

import arcpy
import sys, os

class SchemaGenerator(object):
    def __init__(self):
        self.label = "3DCIM Database Schema Generator"
        self.description = "This tool creates the full or partial database schema " +\
                            "for the 3D City Information Model. Requires an (empty) File GDB to be created before."
        self.canRunInBackground = False

    def getParameterInfo(self):
        # Define parameter definitions

        # Input Geodatabase parameter
        in_gdb = arcpy.Parameter(
            displayName="Workspace:",
            name="in_gdb",
            datatype="Workspace",
            parameterType="Required",
            direction="Input")
            
        in_gdb.value = ""
        in_gdb.filter.list = ["Local Database", "Remote Database"]

        # Spatial reference system to use for the Feature classes
        spatial_reference = arcpy.Parameter(
            displayName="Spatial Reference System:",
            name="spatial_reference",
            datatype="Spatial Reference",
            parameterType="Optional",
            direction="Input")
        
        # Generate Domains parameter
        generate_domains = arcpy.Parameter(
            displayName="Generate Domains:",
            name="generate_domains",
            datatype="String",
            parameterType="Optional",
            direction="Input",
            multiValue=True)
    
        # Set a value list for the Generate Domains parameter
        generate_domains.filter.type = "ValueList"
        generate_domains.filter.list = ["AccessType", "AnnotationType", "Boolean", "BuildingInteriorSpaceType", "BuildingInteriorStructureType", "BuildingRoofForm", "BuildingUsage", "BuildingType", "ConstructionStatus", "InteriorInstallationType", "LandCoverType", "ParcelType", "PlanType", "SensorType", "ShellPartType", "StreetFurnitureType", "TreeCanopyShape", "TreeGenusType", "TreeSpeciesType", "ZoningUsageType", "CityFabricRelationType", "FlowDirection", "RegulationOperator", "RegulationAspect", "RegulationType", "TransectType", "RegulationBlockType", "RegulationFootprintType", "RegulationFootprintAlignmentType", "Unit", "VerticalExtentType", "DirectionType", "PavementType", "TransportSegmentType", "SegmentNameDirection", "SegmentNameGeneric", "AdministrativeDistrictType", "NutsCode"]

        # Generate Classes paramater
        generate_classes = arcpy.Parameter(
            displayName="Generate Feature Classes:",
            name="generate_classes",
            datatype="String",
            parameterType="Optional",
            direction="Input",
            multiValue=True)

        # Set a value list for the Generate Classes field
        generate_classes.filter.type = "ValueList"
        generate_classes.filter.list = ["SpatialVolumeAnnotation", "SpatialPointAnnotation", "SpatialAreaAnnotation", "SpatialLineAnnotation",
                                        "Building", "BuildingEntrancePoint", "BuildingShell", "BuildingShellPart", "SupportingStructure", "Fence",
                                        "BuildingInteriorSpace", "BuildingFloor", "BuildingInteriorStructure",
                                        "InteriorInstallation", "StreetFurniture", "Sensor", "SensorCoverage", "Sign",
                                        "Tree", "ZoningDistrict", "Parcel", "LandCover", "AdministrativeDistrict",
                                        "TransportNetworkSegment"]

        # Generate Tables parameter
        generate_tables = arcpy.Parameter(
            displayName="Generate Tables:",
            name="generate_tables",
            datatype="String",
            parameterType="Optional",
            direction="Input",
            multiValue=True)

        # Set a value list for the Generate tables field
        generate_tables.filter.type = "ValueList"
        generate_tables.filter.list = ["CityFabricRelation", "AttributeContainer", "Regulation", "Neighborhood", "Usage"]

        # Generate Relationship classes parameter
        generate_relationships = arcpy.Parameter(
            displayName="Generate Relationship Classes:",
            name="generate_relationships",
            datatype="String",
            parameterType="Optional",
            direction="Input",
            multiValue=True)

        # Set a value list for the Generate Relationships field
        generate_relationships.filter.type = "ValueList"
        generate_relationships.filter.list = ["BuildingHasEntrance", "BuildingHasShell", "BuildingHasShellPart", "BuildingHasSpace", "BuildingHasFloor", 
                                        "FloorHasSpace", "FloorHasStructure", "SensorHasCoverage", "SpaceHasInstallation", "SpaceHasUsage",
                                        "ZoneHasRegulation", "ZoneHasUsage", "ParcelHasRegulation", "ParcelHasNeigborhood", "ParcelHasUsage"]


        configuration_files_location = arcpy.Parameter(
            displayName="Configuration files folder:",
            name="configuration_files_location",
            datatype="DEFolder",
            parameterType="Required",
            direction="Input")

        configuration_files_location.value = sys.path[0] + os.path.sep + r"Configuration\SchemaTools"

        # Derived Output Features parameter
        out_gdb = arcpy.Parameter(
            displayName="Output Workspace",
            name="out_gdb",
            datatype="Workspace",
            parameterType="Derived",
            direction="Output")

        out_gdb.parameterDependencies = [in_gdb.name]
        parameters = [configuration_files_location, in_gdb, out_gdb, spatial_reference, generate_classes, generate_tables, generate_relationships, generate_domains]

        return parameters

    def isLicensed(self):
        return True

    def execute(self, parameters, messages):

        configuration_files_location = str(parameters[0].value)
        arcpy.env.workspace = parameters[1].value
        spatial_reference_param = parameters[3].value
        create_classes = parameters[4].values
        create_tables = parameters[5].values
        create_relationships = parameters[6].values
        create_domains = parameters[7].values
        
        create_multipatches = True
        if arcpy.CheckExtension("3D") == "Available":
            arcpy.CheckOutExtension("3D")
        else:
            arcpy.AddWarning("3D Analyst extension is not licensed. The script will not create Multipatch feature classes.")
            create_multipatches = False

        arcpy.AddMessage("Creating Domains...")
        if create_domains is not None:
            # read configuration file for domains
            createDomainParams = {}
            for line in open(configuration_files_location + '\\Domains.csv'):
                line = line.rstrip()
                lineParams = line.split(";")
                createDomainParams[lineParams[0]] = lineParams[1:]
            
            # create domains as indicated by user
            for new_domain in create_domains:
                if new_domain not in arcpy.da.ListDomains():
                    arcpy.AddMessage("Adding domain " + new_domain + " with params " + str(createDomainParams[new_domain]))
                    arcpy.CreateDomain_management(arcpy.env.workspace, new_domain,
                                                    domain_description = createDomainParams[new_domain][0],
                                                    field_type = createDomainParams[new_domain][1],
                                                    domain_type = createDomainParams[new_domain][2])
                    
                    for line in open(configuration_files_location + '\\CodedValues.csv'):
                        line = line.rstrip()
                        codedValueParams = line.split(";")
                        if codedValueParams[0] == new_domain:
                            arcpy.AddCodedValueToDomain_management(arcpy.env.workspace,
                                                        domain_name = codedValueParams[0],
                                                        code = codedValueParams[1],
                                                        code_description = codedValueParams[2])
                            arcpy.AddMessage("...Added coded value " + codedValueParams[1] + " to domain " + codedValueParams[0])

        arcpy.AddMessage("Completed adding of Domains and Coded Values")
                                                    
        # read configuration file for feature classes
        createClassParams = {}
        for line in open(configuration_files_location + '\\FeatureClasses.csv'):
            line = line.rstrip()
            lineClassParams = line.split(";")
            createClassParams[lineClassParams[0]] = lineClassParams[1:]

        # create Feature classes that have been selected.
        arcpy.AddMessage("Create Feature classes...")
        if create_classes is not None:
            for new_class in create_classes:
                # Test existence of class to create first.
                if not arcpy.ListFeatureClasses(new_class):
                    arcpy.AddMessage("Adding Feature class " + new_class + ", type " + createClassParams[new_class][0])
                    # Create class
                    if str(createClassParams[new_class][0]) == "MULTIPATCH" and create_multipatches:
                        arcpy.Import3DFiles_3d(in_files = configuration_files_location + '\\multipatch_template.wrl', 
                                                out_featureClass = new_class, 
                                                root_per_feature = "ONE_FILE_ONE_FEATURE", 
                                                spatial_reference = spatial_reference_param)
                        arcpy.DeleteFeatures_management(new_class)
                    else:    
                        arcpy.CreateFeatureclass_management(arcpy.env.workspace, new_class,
                                                    geometry_type = createClassParams[new_class][0],
                                                    template = None,
                                                    has_m = createClassParams[new_class][1],
                                                    has_z = createClassParams[new_class][2],
                                                    spatial_reference = spatial_reference_param)
                        arcpy.AddField_management(new_class, "name", "TEXT", None, None, 100, "Name", None, None)


                # add common attributes
                arcpy.AddField_management(new_class, new_class + "FID", "TEXT", None, None, 50, new_class + " Feature ID", None, None)
                arcpy.AddField_management(new_class, "description", "TEXT", None, None, 250, "Description", None, None)
                arcpy.AddField_management(new_class, "attribution", "TEXT", None, None, 250, "Attribution/Source", None, None)
                arcpy.AddField_management(new_class, "subtype", "TEXT", None, None, 50, "Subtype", None, None)

            # add specific attributes to feature classes that have been selected
            for line in open(configuration_files_location + '\\FeatureClassAttributes.csv'):
                line = line.rstrip()
                attributeParams = line.split(";")
                
                if attributeParams[0] in create_classes:
                    arcpy.AddMessage("Adding Attribute " + attributeParams[1] + " to table " + attributeParams[0] + ", parameters " + str(attributeParams[2:]))
                    arcpy.AddField_management(in_table = attributeParams[0],
                                            field_name = attributeParams[1],
                                            field_type = attributeParams[2],
                                            field_length = attributeParams[3],
                                            field_alias = attributeParams[4],
                                            field_is_nullable = attributeParams[5],
                                            field_is_required = attributeParams[6])

                    # add domains to field if defined
                    if str(attributeParams[7]) != "NO_DOMAIN":
                        arcpy.AssignDomainToField_management(in_table = attributeParams[0],
                                                        field_name = attributeParams[1],
                                                        domain_name = attributeParams[7])

            arcpy.AddMessage("Added fields to Feature Classes")
        
        # read configuration file for tables
        createClassParams = {}
        for line in open(configuration_files_location + '\\Tables.csv'):
            line = line.rstrip()
            lineClassParams = line.split(";")
            createClassParams[lineClassParams[0]] = lineClassParams[1:]

        # create Tables that have been selected.
        arcpy.AddMessage("Create Tables...")
        if create_tables is not None:
            for new_class in create_tables:
                # Test existence of class to create first.
                if arcpy.ListTables(new_class):
                    break

                # Then create table (if necessary)
                arcpy.AddMessage("Adding Table class " + new_class)
                arcpy.CreateTable_management(arcpy.env.workspace, new_class)

            # add specific attributes for tables
            for line in open(configuration_files_location + '\\TableAttributes.csv'):
                line = line.rstrip()
                attributeParams = line.split(";")
                
                if attributeParams[0] in create_tables:
                    arcpy.AddField_management(in_table = attributeParams[0],
                                                field_name = attributeParams[1],
                                                field_type = attributeParams[2],
                                                field_length = attributeParams[3],
                                                field_alias = attributeParams[4],
                                                field_is_nullable = attributeParams[5],
                                                field_is_required = attributeParams[6])

                    # add domains to field if defined
                    if str(attributeParams[7]) != "NO_DOMAIN":
                        arcpy.AssignDomainToField_management(in_table = attributeParams[0],
                                                            field_name = attributeParams[1],
                                                            domain_name = attributeParams[7])

        # create Relationship classes
        arcpy.AddMessage("Create Relationship classes...")
        createClassParams = {}
        for line in open(configuration_files_location + '\\RelationshipClasses.csv'):
            line = line.rstrip()
            lineClassParams = line.split(";")
            createClassParams[lineClassParams[0]] = lineClassParams[1:]

        if create_relationships is not None:
            for new_class in create_relationships:
                # check if origin and destination relationship classes have been created beforehand.
                if (arcpy.ListTables(createClassParams[new_class][0]) or arcpy.ListFeatureClasses(createClassParams[new_class][0])) and (arcpy.ListTables(createClassParams[new_class][1]) or arcpy.ListFeatureClasses(createClassParams[new_class][1])):
                    arcpy.AddMessage("Adding Relationship class " + new_class)
                    arcpy.CreateRelationshipClass_management (origin_table = createClassParams[new_class][0],
                                                                destination_table = createClassParams[new_class][1],
                                                                out_relationship_class = new_class,
                                                                relationship_type = createClassParams[new_class][2],
                                                                forward_label = createClassParams[new_class][3],
                                                                backward_label = createClassParams[new_class][4],
                                                                message_direction = createClassParams[new_class][5],
                                                                cardinality = createClassParams[new_class][6],
                                                                attributed = createClassParams[new_class][7],
                                                                origin_primary_key = createClassParams[new_class][8],
                                                                origin_foreign_key = createClassParams[new_class][9])

        if create_multipatches:
            arcpy.CheckInExtension("3D")

# ------------------------------------------------------------------------------