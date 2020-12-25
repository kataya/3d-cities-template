# ------------------------------------------------------------------------------
# 3D City Information Model Python Toolbox/SchemaGenerator
# 1.3.1_2013-12-10
#
#
# Author: Thorsten Reitz, ESRI R&D Center Zurich
# 
# Changed getParameterInfo() datatype to ArcGIS 10.8.x by kataya 2020.12
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

    classesToDomains = {}

    def __init__(self):
        self.label = "3DCIM Database Schema Generator"
        self.description = "This tool creates the full or partial database schema " +\
                            "for the 3D City Information Model. Requires an (empty) File GDB to be created before."
        self.canRunInBackground = False

        # Create Map of Class to Domain dependencies from FC and Table Attributes.
        configpath = sys.path[0] + os.path.sep + r"Configuration\SchemaTools"
        classesToDomainsLookup = {}

        for line in open(configpath + '\\FeatureClassAttributes.csv'):
            line = line.rstrip()
            attributeParams = line.split(";")
            if attributeParams[7] != 'NO_DOMAIN':
                if attributeParams[0] in classesToDomainsLookup:
                    if classesToDomainsLookup[attributeParams[0]] is None:
                        classesToDomainsLookup[attributeParams[0]] = [attributeParams[7]]
                    else:
                        classesToDomainsLookup[attributeParams[0]].append(attributeParams[7])
                else:
                    classesToDomainsLookup[attributeParams[0]] = [attributeParams[7]]

        for line in open(configpath + '\\TableAttributes.csv'):
            line = line.rstrip()
            attributeParams = line.split(";")
            if attributeParams[7] != 'NO_DOMAIN':
                if attributeParams[0] in classesToDomainsLookup:
                    if classesToDomainsLookup[attributeParams[0]] is None:
                        classesToDomainsLookup[attributeParams[0]] = [attributeParams[7]]
                    else:
                        classesToDomainsLookup[attributeParams[0]].append(attributeParams[7])
                else:
                    classesToDomainsLookup[attributeParams[0]] = [attributeParams[7]]

        #f = open (sys.path[0] + os.path.sep + r"Configuration\SchemaTools\logInit.txt", 'w')
        #f.write(str(classesToDomainsLookup) + "\n")

        self.classesToDomains = classesToDomainsLookup

    def getParameterInfo(self):
        # Define parameter definitions
        configuration_files_location = arcpy.Parameter(
            displayName="Configuration files folder:",
            name="configuration_files_location",
            datatype="DEFolder",
            parameterType="Required",
            direction="Input")

        configuration_files_location.value = sys.path[0] + os.path.sep + r"Configuration\SchemaTools"

        # Input Geodatabase parameter
        in_gdb = arcpy.Parameter(
            displayName="Workspace:",
            name="in_gdb",
            datatype="DEWorkspace",
            parameterType="Required",
            direction="Input")

        in_gdb.value = ""
        in_gdb.filter.list = ["Local Database", "Remote Database"]

        # Spatial reference system to use for the Feature classes
        spatial_reference = arcpy.Parameter(
            displayName="Spatial Reference System:",
            name="spatial_reference",
            datatype="GPSpatialReference",
            parameterType="Required",
            direction="Input")

        # Generate Domains parameter
        generate_domains = arcpy.Parameter(
            displayName="Generate Domains:",
            name="generate_domains",
            datatype="GPString",
            parameterType="Optional",
            direction="Input",
            multiValue=True)

        # Set a value list for the Generate Domains parameter
        generate_domains.filter.type = "ValueList"
        createDomainNameList = []
        for line in open(str(configuration_files_location.value) + '\\Domains.csv'):
            line = line.rstrip()
            lineParams = line.split(";")
            createDomainNameList.append(lineParams[0])

        generate_domains.filter.list = createDomainNameList

        # Generate Classes paramater
        generate_classes = arcpy.Parameter(
            displayName="Generate Feature Classes:",
            name="generate_classes",
            datatype="GPString",
            parameterType="Optional",
            direction="Input",
            multiValue=True)

        # Set a value list for the Generate Classes field
        generate_classes.filter.type = "ValueList"
        createClassNameList = []
        for line in open(str(configuration_files_location.value) + '\\FeatureClasses.csv'):
            line = line.rstrip()
            lineParams = line.split(";")
            createClassNameList.append(lineParams[0])

        generate_classes.filter.list = createClassNameList

        # Generate Tables parameter
        generate_tables = arcpy.Parameter(
            displayName="Generate Tables:",
            name="generate_tables",
            datatype="GPString",
            parameterType="Optional",
            direction="Input",
            multiValue=True)

        # Set a value list for the Generate tables field
        generate_tables.filter.type = "ValueList"
        createTableNameList = []
        for line in open(str(configuration_files_location.value) + '\\Tables.csv'):
            line = line.rstrip()
            lineParams = line.split(";")
            createTableNameList.append(lineParams[0])

        generate_tables.filter.list = createTableNameList

        # Generate Relationship classes parameter
        generate_relationships = arcpy.Parameter(
            displayName="Generate Relationship Classes:",
            name="generate_relationships",
            datatype="GPString",
            parameterType="Optional",
            direction="Input",
            multiValue=True)

        # Set a value list for the Generate Relationships field
        generate_relationships.filter.type = "ValueList"
        createRelationShipsNameList = []
        for line in open(str(configuration_files_location.value) + '\\RelationshipClasses.csv'):
            line = line.rstrip()
            lineParams = line.split(";")
            createRelationShipsNameList.append(lineParams[0])

        generate_relationships.filter.list = createRelationShipsNameList

        # Derived Output Features parameter
        out_gdb = arcpy.Parameter(
            displayName="Output Workspace",
            name="out_gdb",
            datatype="DEWorkspace",
            parameterType="Derived",
            direction="Output")

        out_gdb.parameterDependencies = [in_gdb.name]
        parameters = [configuration_files_location, in_gdb, out_gdb, spatial_reference, generate_classes, generate_tables, generate_relationships, generate_domains]
        self.parameters = parameters
        return parameters

    def isLicensed(self):
        return True

    def updateParameters(self, parameters):
        # check if Feature Classes, Tables or Relationship Classes have been selected.
        candidateDomains = []
        if parameters[4].value:
            create_classes = parameters[4].values
            for new_class in create_classes:
                if new_class in self.classesToDomains:
                    candidateDomains.extend(self.classesToDomains[new_class])

        if parameters[5].value:
            create_classes = parameters[5].values
            for new_class in create_classes:
                if new_class in self.classesToDomains:
                    candidateDomains.extend(self.classesToDomains[new_class])

        # select the correct domains that are required.
        activateDomains = list(set(candidateDomains))

        #f = open (sys.path[0] + os.path.sep + r"Configuration\SchemaTools\log.txt", 'w')
        #f.write("The current selection requires the following domains to be present:" + str(activateDomains) + "\n")

        parameters[7].values = activateDomains

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
        existing_domains = []
        for domain in arcpy.da.ListDomains():
            existing_domains.append(domain.name)

        arcpy.AddMessage("Domains already defined in the GDB:" + str(existing_domains))

        if create_domains is not None:
            # read configuration file for domains
            createDomainParams = {}
            for line in open(configuration_files_location + '\\Domains.csv'):
                line = line.rstrip()
                lineParams = line.split(";")
                createDomainParams[lineParams[0]] = lineParams[1:]

            # create domains as indicated by user
            for new_domain in create_domains:
                if new_domain not in existing_domains:
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
            # Load attribute map
            classAttributeMap = self.loadAttributeMap(configuration_files_location + '\\FeatureClassAttributes.csv')

            # Now, create all feature classes.
            for new_class in create_classes:
                # Test existence of class to create first.
                if not arcpy.ListFeatureClasses(new_class):
                    arcpy.AddMessage("Adding Feature class " + new_class + ", type " + createClassParams[new_class][0])

                    # Create classes
                    out_path = arcpy.env.workspace
                    out_name = new_class
                    arcpy.AddMessage("out_path (env):" + out_path)
                    arcpy.AddMessage("out_name (env):" + out_name)
                    if str(createClassParams[new_class][0]) == "MULTIPATCH" and create_multipatches:
                        self.createMultipatchTemplate(out_path, out_name, spatial_reference_param, configuration_files_location)
                    else:
                        self.createStandardTemplate(out_path, out_name, createClassParams[new_class], spatial_reference_param)

                    self.addAttributes(classAttributeMap, new_class)
                    arcpy.AddMessage("Created Feature Class " + new_class)


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
                    arcpy.AddMessage("Adding Relationship class " + new_class + " between " + createClassParams[new_class][0] + " and " + createClassParams[new_class][1])
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

    def createMultipatchTemplate(self, path, name, spatial_reference_param, configuration_files_location):

        # read out domain for the target spatial reference
        crs_domain = ["-180", "-90", "180", "90"]
        if spatial_reference_param is not None:
            crs_domain_str = spatial_reference_param.domain.split(" ")
            crs_domain[0] = str(float(crs_domain_str[0]) / 1.1)
            crs_domain[1] = str(float(crs_domain_str[1]) / 1.1)
            crs_domain[2] = str(float(crs_domain_str[2]) / 1.1)
            crs_domain[3] = str(float(crs_domain_str[3]) / 1.1)

        # create a multipatch template file based on the valid xy domain
        multipatch_tpl = "#VRML V2.0 utf8" + os.linesep + r"Shape { appearance Appearance { material Material { diffuseColor 1 0 0 } }" +     r" geometry IndexedFaceSet { coord Coordinate { point ["
        multipatch_tpl += crs_domain[0] + " " + crs_domain[1] + " -50000, " # vertex 0
        multipatch_tpl += crs_domain[2] + " " + crs_domain[1] + " 0, " # vertex 1
        multipatch_tpl += crs_domain[2] + " " + crs_domain[3] + " 50000" # vertex 2
        multipatch_tpl += r"] }    coordIndex [0, 1, 2, -1] } }"

        # write multipatch template to temporary file
        template_filename = configuration_files_location + '\\multipatch_template.wrl'
        f = open (template_filename, 'w')
        f.write(multipatch_tpl)
        f.close()

        templateFC = path + '/' + name
        arcpy.AddMessage("Generating a Multipatch FC: " + templateFC)
        arcpy.ddd.Import3DFiles(in_files = template_filename,
                                out_featureClass = templateFC,
                                root_per_feature = "ONE_FILE_ONE_FEATURE",
                                spatial_reference = spatial_reference_param) # change to None if it doesn't work
        arcpy.DeleteFeatures_management(templateFC)
        arcpy.DeleteField_management(templateFC, "name")

        # delete temporary template file
        os.remove(template_filename)
        return templateFC

    def createStandardTemplate(self, path, name, createClassParams, spatial_reference_param):
        templateFC = arcpy.CreateUniqueName(path, name)
        table = arcpy.CreateFeatureclass_management(
                            out_path = path,
                            out_name = name,
                            geometry_type = createClassParams[0],
                            template = None,
                            has_m = createClassParams[1],
                            has_z = createClassParams[2],
                            spatial_reference = spatial_reference_param) # change to None if it doesn't work
        return table

    def loadAttributeMap(self, paramPath):
        classAttributeMap = {}
        # Load attribute map
        for line in open(paramPath):
            line = line.rstrip()
            attributeParams = line.split(";")
            if attributeParams[0] in classAttributeMap:
                classAttributeMap[attributeParams[0]].append(attributeParams[1:])
            else:
                classAttributeMap[attributeParams[0]] = [attributeParams[1:]]

        return classAttributeMap

    def addAttributes(self, classAttributeMap, new_class_name):
        # add common attributes that all FCs and Tables share
        arcpy.AddField_management(new_class_name, new_class_name + "FID", "TEXT", None, None, 50, new_class_name + " Feature ID", None, None)
        arcpy.AddField_management(new_class_name, "name", "TEXT", None, None, 100, "Name", None, None)
        arcpy.AddField_management(new_class_name, "description", "TEXT", None, None, 250, "Description", None, None)
        arcpy.AddField_management(new_class_name, "attribution", "TEXT", None, None, 250, "Attribution/Source", None, None)

        # add specific attributes
        for attributeParams in classAttributeMap[new_class_name]:
            arcpy.AddField_management(in_table = new_class_name,
                field_name = attributeParams[0],
                field_type = attributeParams[1],
                field_length = attributeParams[2],
                field_alias = attributeParams[3],
                field_is_nullable = attributeParams[4],
                field_is_required = attributeParams[5])

            # add domains to field if defined
            if str(attributeParams[6]) != "NO_DOMAIN":
                arcpy.AssignDomainToField_management(in_table = new_class_name,
                                                    field_name = attributeParams[0],
                                                    domain_name = attributeParams[6])
            arcpy.AddMessage("Added Field " + attributeParams[0])

# ------------------------------------------------------------------------------
