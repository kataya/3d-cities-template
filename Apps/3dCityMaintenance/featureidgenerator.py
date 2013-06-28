# ------------------------------------------------------------------------------
# 3D City Information Model Python Toolbox/FeatureIdGenerator
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
                            arcpy.AddMessage("Hi Sequence " + str(hi_counter) + " exhausted, using next Sequence.")
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