# ------------------------------------------------------------------------------
# 3D City Information Model Python Toolbox
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
from schemagenerator import SchemaGenerator
from featureidgenerator import FeatureIdGenerator
from gdbcomparator import GdbComparator

class Toolbox(object):
    def __init__(self):
        self.label = "3DCIM Schema Tools"
        self.alias = "ArcGIS_for_3D_Cities_Database_Schema_Tools"

        # List of tool classes associated with this toolbox
        self.tools = [SchemaGenerator, FeatureIdGenerator, GdbComparator]