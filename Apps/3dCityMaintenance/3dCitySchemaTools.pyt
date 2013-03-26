# ------------------------------------------------------------------------------
# 3D City Information Model Python Toolbox
# 1.0.0_2012-09-28
#
#
# Author: Thorsten Reitz, ESRI R&D Lab Zurich, Craig McCabe, Esri Inc.
# License:
# ------------------------------------------------------------------------------

import arcpy

class Toolbox(object):
    def __init__(self):
        self.label = "3DCIM Schema Tools"
        self.alias = "3DCIM Schema Tools"

        # List of tool classes associated with this toolbox
        self.tools = [SchemaGenerator, FeatureIdGenerator]


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
            displayName="Input Workspace:",
            name="in_gdb",
            datatype="Workspace",
            parameterType="Required",
            direction="Input")
            
        in_gdb.value = ""

        # Spatial reference system to use for the Feature classes
        spatial_reference = arcpy.Parameter(
            displayName="Spatial Reference System:",
            name="spatial_reference",
            datatype="Spatial Reference",
            parameterType="Optional",
            direction="Input")
            

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
        generate_classes.filter.list = ["AnnotatedSpace",
                                        "Building", "BuildingEntrancePoint", "BuildingShell", "BuildingShellPart",
                                        "BuildingInteriorSpace", "BuildingFloor", "BuildingInteriorStructure",
                                        "InteriorInstallation", "StreetFurniture", "Sensor", "SensorCoverage", "Sign",
                                        "Tree", "ZoningDistrict", "Parcel", "LandCover"]

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
        generate_tables.filter.list = ["CityFabricRelation", "AttributeContainer", "Regulation", "Neighborhood"]

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
        generate_relationships.filter.list = ["BuildingHasEntrance", "BuildingHasShell", "BuildingHasShellPart", "BuildingHasSpace", "BuildingHasFloor", "FloorHasSpace", "FloorHasStructure", "SensorHasCoverage", "SpaceHasInstallation", "ZoneHasRegulation", "ParcelHasRegulation", "ParcelHasNeigborhood"]


        configuration_files_location = arcpy.Parameter(
            displayName="Path to Configuration files Folder:",
            name="configuration_files_location",
            datatype="String",
            parameterType="Required",
            direction="Input")

        configuration_files_location.value = "D:\\3D Cities\\3d-cities-template\\Apps\\3dCityMaintenance\\Configuration\\SchemaTools"

        # Derived Output Features parameter
        out_gdb = arcpy.Parameter(
            displayName="Output Workspace",
            name="out_gdb",
            datatype="Workspace",
            parameterType="Derived",
            direction="Output")

        out_gdb.parameterDependencies = [in_gdb.name]
        parameters = [configuration_files_location, in_gdb, out_gdb, spatial_reference, generate_classes, generate_tables, generate_relationships]

        return parameters

    def isLicensed(self):
        return True

    def execute(self, parameters, messages):

        configuration_files_location = parameters[0].value
        arcpy.env.workspace = parameters[1].value
        spatial_reference_param = parameters[3].value
        create_classes = parameters[4].values
        create_tables = parameters[5].values
        create_relationships = parameters[6].values
        
        
        create_multipatches = True
        if arcpy.CheckExtension("3D") == "Available":
            arcpy.CheckOutExtension("3D")
        else:
            arcpy.AddWarning("3D Analyst extension is not licensed. The script will not create Multipatch feature classes.")
            create_multipatches = False

        # read configuration file for domains
        createDomainParams = {}
        for line in open(configuration_files_location + '\\Domains.csv'):
            line = line.rstrip()
            lineParams = line.split(";")
            createDomainParams[lineParams[0]] = lineParams[1:]

        precreated_domains = []
        for domain in createDomainParams:
            # test existence of domain.
            precreated = False
            for existing_domain in arcpy.da.ListDomains():
                if domain == existing_domain.name:
                    precreated = True
                    precreated_domains.append(domain)
                    break

            # create the domain
            if not precreated:
                arcpy.AddMessage(domain + " = " + str(createDomainParams[domain]))
                arcpy.CreateDomain_management(arcpy.env.workspace, domain,
                                                domain_description = createDomainParams[domain][0],
                                                field_type = createDomainParams[domain][1],
                                                domain_type = createDomainParams[domain][2])

        # add coded values to domains
        for line in open(configuration_files_location + '\\CodedValues.csv'):
            line = line.rstrip()
            codedValueParams = line.split(";")
            if codedValueParams[0] not in precreated_domains:
                arcpy.AddCodedValueToDomain_management(arcpy.env.workspace,
                                                    domain_name = codedValueParams[0],
                                                    code = codedValueParams[1],
                                                    code_description = codedValueParams[2])

        arcpy.AddMessage("Completed adding of Domains and Coded Values")
                                                    
        # read configuration file for feature classes
        createClassParams = {}
        for line in open(configuration_files_location + '\\FeatureClasses.csv'):
            line = line.rstrip()
            lineClassParams = line.split(";")
            createClassParams[lineClassParams[0]] = lineClassParams[1:]

        # create Feature classes that have been selected.
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

class FeatureIdGenerator(object):
    def __init__(self):
        self.label = "3DCIM Feature ID Generator"
        self.description = "This tool adds Feature ID fields and values to any " +\
                            "Feature Classes in an input workspace (File GDB), which are used as persistent " +\
                            "identifiers for referencing of 3DCIM features."
        self.canRunInBackground = False

    def getParameterInfo(self):
        # Define parameter definitions

        # Input Geodatabase parameter
        in_gdb = arcpy.Parameter(
            displayName="Input Workspace",
            name="in_gdb",
            datatype="Workspace",
            parameterType="Required",
            direction="Input")


        # Generation Method Field parameter
        generation_field = arcpy.Parameter(
            displayName="Generation Method",
            name="generation_method",
            datatype="String",
            parameterType="Required",
            direction="Input")

        # Set a value list for the Generation method
        generation_field.filter.type = "ValueList"
        generation_field.filter.list = ["Global Hi-Lo Counter"]
        generation_field.value = "Global Hi-Lo Counter"

        # Hi Batchsize Method Field parameter
        hi_batchsize_field = arcpy.Parameter(
            displayName="Hi batch size",
            name="hi_batchsize",
            datatype="Long",
            parameterType="Required",
            direction="Input")

        hi_batchsize_field.value = 10000

        # Derived Output Features parameter
        out_gdb = arcpy.Parameter(
            displayName="Output Workspace",
            name="out_gdb",
            datatype="Workspace",
            parameterType="Derived",
            direction="Output")

        out_gdb.parameterDependencies = [in_gdb.name]

        parameters = [in_gdb, generation_field, hi_batchsize_field, out_gdb]

        return parameters

    def isLicensed(self):
        """Set whether tool is licensed to execute."""
        return True

    def updateParameters(self, parameters):
        """Modify the values and properties of parameters before internal
        validation is performed.  This method is called whenever a parameter
        has been changed."""
        return

    def updateMessages(self, parameters):
        """Modify the messages created by internal validation for each tool
        parameter.  This method is called after internal validation."""
        return

    def execute(self, parameters, messages):
        """The source code of the tool."""

        arcpy.env.workspace = parameters[0].value

        # Number of low IDs per hi ID
        # Higher batch sizes mean less updating of the table, lower batch sizes more
        # efficient ID usage especially when multiple processes access the table.
        hi_batchsize = parameters[2].value

        # Name of the table used to maintain hi/lo counter status per feature class. Could also become a parameter.
        generate_ID_table_name = "GenerateID"

        # check whether sequences table has already been created.
        new_table = None
        counter_tbl_list = arcpy.ListTables(generate_ID_table_name)
        if not counter_tbl_list:
            arcpy.AddMessage("Creating new GenerateID table.")
            new_table = True
            generate_ID_table = arcpy.CreateTable_management(arcpy.env.workspace, generate_ID_table_name)
            arcpy.AddField_management(generate_ID_table, "name", "TEXT", None, None, 50, "Feature Class Name", "NON_NULLABLE", "REQUIRED")
            arcpy.AddField_management(generate_ID_table, "hi", "LONG", None, None, None, "Hi counter", "NON_NULLABLE", "REQUIRED")
            arcpy.AddField_management(generate_ID_table, "low", "LONG", None, None, None, "Low counter", "NON_NULLABLE", "REQUIRED")
        else:
            new_table = False
            generate_ID_table = counter_tbl_list[0]

        # go through feature classes to create FIDs where needed.
        fc_list = arcpy.ListFeatureClasses()
        for fc in fc_list:
            arcpy.AddMessage("Processing " + fc)
            hi_counter = 0
            low_counter = 0

            # if we only created the GenerateID table, we know we have to insert the counter.
            if new_table:
                insert_new_counter_cursor = arcpy.da.InsertCursor(generate_ID_table_name, ["name", "hi", "low"])
                insert_new_counter_cursor.insertRow((fc, 0, 0))
                del insert_new_counter_cursor

            # check if a counter of fc_name exists and retrieve value
            with arcpy.da.SearchCursor(generate_ID_table_name, ["name", "hi", "low"]) as rows:
                for row in rows:
                    if row[0] == fc:
                        hi_counter = row[1]
                        low_counter = row[2]
                        break

            # increment hi counter to indicate that it is in active usage
            with arcpy.da.UpdateCursor(generate_ID_table_name, ["name", "hi"]) as rows:
                for row in rows:
                    if row[0] == fc:
                        row[1] = 1 + hi_counter
                        rows.updateRow(row)
                        break

            # check if feature class alread has a FID, add it if not.
            fid_name = fc + "FID"
            fields_list = arcpy.ListFields(fc, fid_name)
            if not fields_list:
                arcpy.AddField_management(fc, fid_name, "TEXT", None, None, 50, "Feature ID", None, None)

            # modify FID of object if required
            with arcpy.da.UpdateCursor(fc, [fid_name]) as rows:
                for row in rows:
                    if row[0] == None:
                        if low_counter >= hi_batchsize:
                            # update hi_counter, reset low_counter
                            arcpy.AddMessage("Hi Sequence " + hi_counter + " exhausted, using next Sequence.")
                            escaped_name = arcpy.AddFieldDelimiters(generate_ID_table_name, "name")
                            where_clause = escaped_name + " = " + "'" + fc + "'"
                            new_hi_row = [row[0] for rows in arcpy.da.SearchCursor(generate_ID_table_name, ["hi"], where_clause)]
                            hi_counter = new_hi_row[0]
                            low_counter = 0
                        row[0] = fc + "/" + str(hi_counter * hi_batchsize + low_counter)
                        low_counter += 1
                        rows.updateRow(row)

            # write back the new low value to the GenerateID table.
            with arcpy.da.UpdateCursor(generate_ID_table_name, ["name", "low"]) as rows:
                for newRow in rows:
                    if newRow[0] == fc:
                        newRow[1] = low_counter
                        rows.updateRow(newRow)
                        break

        arcpy.AddMessage("Completed adding of Feature IDs.")
        return
