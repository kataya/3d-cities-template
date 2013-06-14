# ------------------------------------------------------------------------------
# 3D City Information Model Python Toolbox/GdbComparator
# 1.2.0_2013-06-14
#
#
# Author: Thorsten Reitz, ESRI R&D Lab Zurich
# License: BSD
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
            datatype="Workspace",
            parameterType="Required",
            direction="Input")
            
        ours_gdb.value = ""
        
        # Second Geodatabase parameter
        theirs_gdb = arcpy.Parameter(
            displayName="Workspace:",
            name="theirs_gdb",
            datatype="Workspace",
            parameterType="Required",
            direction="Input")
            
        theirs_gdb.value = ""
        
        # Differential Output Features parameter (the workspace in which each diff feature will be stored)
        out_gdb = arcpy.Parameter(
            displayName="Diff Output Workspace",
            name="out_gdb",
            datatype="Workspace",
            parameterType="Required",
            direction="Output")

        out_gdb.value = ""
        parameters = [ours_gdb, theirs_gdb, out_gdb]

        return parameters

    def isLicensed(self):
        return True

    def execute(self, parameters, messages):
        
        return