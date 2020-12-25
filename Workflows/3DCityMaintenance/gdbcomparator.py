# ------------------------------------------------------------------------------
# 3D City Information Model Python Toolbox/GdbComparator
# 1.2.0_2013-06-14
#
#
# Author: Thorsten Reitz, ESRI R&D Lab Zurich
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

class GdbComparator(object):
    def __init__(self):
        self.label = "3DCIM Database Comparator tool"
        self.description = "This tool compares one workspace with another, doing a full diff on the content of the databases."
        self.canRunInBackground = False

    def getParameterInfo(self):
        # First Geodatabase parameter
        ours_gdb = arcpy.Parameter(
            displayName="Workspace:",
            name="ours_gdb",
            datatype="DEWorkspace",
            parameterType="Required",
            direction="Input")
            
        ours_gdb.value = ""
        
        # Second Geodatabase parameter
        theirs_gdb = arcpy.Parameter(
            displayName="Workspace:",
            name="theirs_gdb",
            datatype="DEWorkspace",
            parameterType="Required",
            direction="Input")
            
        theirs_gdb.value = ""
        
        # Differential Output Features parameter (the workspace in which each diff feature will be stored)
        out_gdb = arcpy.Parameter(
            displayName="Diff Output Workspace",
            name="out_gdb",
            datatype="DEWorkspace",
            parameterType="Required",
            direction="Output")

        out_gdb.value = ""
        parameters = [ours_gdb, theirs_gdb, out_gdb]

        return parameters

    def isLicensed(self):
        return True

    def execute(self, parameters, messages):
    
        # read parameters
        workspace_ours = parameters[0].value
        workspace_theirs = parameters[1].value
        workspace_diff = parameters[2].value
        arcpy.env.workspace = workspace_ours
        
        # check differenences in feature classes
        fc_list = arcpy.ListFeatureClasses()
        
        # first, check whether the schema is identical
        
        # now, compare records
        
        return
