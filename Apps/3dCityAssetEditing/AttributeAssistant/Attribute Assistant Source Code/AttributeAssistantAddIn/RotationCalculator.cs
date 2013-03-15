
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.ADF.Connection.Local;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.ArcMapUI;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ESRI_WaterUtilitiesTemplate

{
 
    public class RotationCalculator
    {
        public RotationCalculator()   
        {

        }      

        #region Properties

        public ESRI.ArcGIS.Carto.esriSymbolRotationType RotationType
        {
            get { return m_rotationType; }
            set { m_rotationType = value; }
        }
        public Boolean UseDiameter
        {
            get { return m_useDiameter; }
            set { m_useDiameter = value; }
        }
        public string DiameterFieldName
        {
            get { return m_diameterFieldName; }
            set { m_diameterFieldName = value; }
        }
        public int SpinAngle 
        {
            get { return m_spinAngle; }
            set { m_spinAngle = value; }
        }

        private esriSymbolRotationType m_rotationType;
        private Boolean m_useDiameter;
        private string m_diameterFieldName;
        private int m_spinAngle;
        
        #endregion
     
        // Uses Geometric Network to find connected edges which determine desired rotation of point
 
        public  Nullable<double> GetRotationUsingConnectedEdges(IFeature inFeature)
        {
            Nullable<double> rotationAngle = null;

            if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
            {

                try
                {
                    double diameter = 0;
                    List<double> angles = new List<double>();
                    List<double> diameters = new List<double>();
                    List<Boolean> flipDirections = new List<Boolean>();

                    IPoint pnt = (ESRI.ArcGIS.Geometry.IPoint)inFeature.Shape;
                    INetworkFeature netFeat = (INetworkFeature)inFeature;
                    ISimpleJunctionFeature simpleJunctionFeature = (ISimpleJunctionFeature)netFeat;
                    INetworkClass netClass = (INetworkClass)inFeature.Class;
                    IGeometricNetwork geomNetwork = (IGeometricNetwork)netClass.GeometricNetwork;
                    INetwork network = (INetwork)geomNetwork.Network;
                    INetElements netElements = (INetElements)network;

                    IFeatureClass fc;
                    IFeature feat;
                    IGeometry geometry;
                    IPolyline polyline;
                    ISegmentCollection segmentCollection;
                    ISegmentCollection segColTest;
                    ISegment testSegment;
                    IEnumSegment enumSegment;
                    System.Object edgeWeight;
                    Boolean edgeOrient;
                    int partIndex = 0;
                    int segmentIndex = 0;
                    int edgesCount;
                    int edgeEID;
                    int classId; int userId; int subId;
                    int posField; double angle;
                    object Missing = Type.Missing;

                    IPoint toPoint;
                    ITopologicalOperator topoOp;
                    IPolygon poly;
                    IRelationalOperator relOp;

                    IForwardStarGEN forwardStar = (IForwardStarGEN)network.CreateForwardStar(false, null, null, null, null);
                    forwardStar.FindAdjacent(0, simpleJunctionFeature.EID, out edgesCount);
                    

                    for (int i = 0; i < edgesCount; i++)
                    {

                        forwardStar.QueryAdjacentEdge(i, out edgeEID, out edgeOrient, out edgeWeight);
                        geometry = geomNetwork.get_GeometryForEdgeEID(edgeEID);
                        polyline = (IPolyline5)geometry;

                        //Special case for reducer
                        if (m_useDiameter & (edgesCount == 2))
                        {
                            netElements.QueryIDs(edgeEID, esriElementType.esriETEdge, out classId, out userId, out subId);
                            fc = GetFeatureClassByClassId(classId, geomNetwork);
                            feat = fc.GetFeature(userId);
                            posField = GetFieldPosition(m_diameterFieldName, feat);
                            if (posField > -1)
                            {
                                diameter = (double)feat.get_Value(posField);
                            }
                        }
                        
                        //given line and point, return angles of all touching segments
                        segmentCollection = (ISegmentCollection)polyline;
                        enumSegment = (IEnumSegment)segmentCollection.EnumSegments;
                        enumSegment.Next(out testSegment, ref partIndex, ref segmentIndex);

                        while (testSegment != null)
                        {
                            angle = GetAngleOfSegment(testSegment);
                            toPoint = testSegment.ToPoint;
                            topoOp = toPoint as ITopologicalOperator;
                            poly = topoOp.Buffer(0.01) as IPolygon;

                            //ML: 20090617 Added test for segment touching point to be rotated
                            segColTest = new PolylineClass();
                            segColTest.AddSegment(testSegment, ref Missing, ref Missing);
                            relOp = segColTest as IRelationalOperator;
                            
                            if (relOp.Touches(pnt))
                            {
                                relOp = poly as IRelationalOperator;
                                flipDirections.Add(relOp.Contains(pnt));
                                diameters.Add(diameter);
                                angles.Add(angle);
                            }
                            enumSegment.Next(out testSegment, ref partIndex, ref segmentIndex);

                        }

                        ///end of possible function returning list of angles


                    }
                    switch (angles.Count)
                    {
                        case 0:
                            break;
                        case 1:
                            // End cap or plug fitting or simliar.
                            rotationAngle = angles[0];
                            if (flipDirections[0]) rotationAngle += 180;
                            break;
                        case 2:
                            if (m_useDiameter & (diameters[0] < diameters[1]))
                            {
                                rotationAngle = angles[0];
                                if (flipDirections[0]) rotationAngle += 180;
                            }
                            else if (m_useDiameter & (diameters[0] >= diameters[1]))
                            {
                                rotationAngle = angles[1];
                                if (flipDirections[1]) rotationAngle += 180;
                            }
                            else rotationAngle = angles[0];

                            break;
                        case 3:
                            //Tee or Tap fitting or similiar.  Rotate toward the odd line.
                            int tee = FindTee(angles[0], angles[1], angles[2]);
                            rotationAngle = angles[tee];
                            if (flipDirections[tee]) rotationAngle += 180;
                            break;
                        case 4:
                            // Cross fitting or similar. Any of the angles should work.
                            rotationAngle = (int)angles[0];
                            break;
                        default:
                            break;
                    }
                }
                catch 
                {
                    return -1;
                }

            }


           //If needed, convert to geographic degrees(zero north clockwise)
           if (rotationAngle > 360) rotationAngle -= 360;
           if (rotationAngle < 0) rotationAngle += 360;
           if (rotationAngle != null & m_rotationType == esriSymbolRotationType.esriRotateSymbolGeographic)
            {
                   int a = (int)rotationAngle;

                   if (a > 0 & a <= 90)
                       rotationAngle = 90 - a;
                   else if (a > 90 & a <= 360)
                       rotationAngle = 450 - a;

             }

            //Apply any spin angle 
           if (rotationAngle != null)
           {
               rotationAngle += m_spinAngle;
               if (rotationAngle > 360) rotationAngle -= 360;
               if (rotationAngle < 0) rotationAngle += 360;
           }

           
            return rotationAngle;
        }

        #region Helper methods
        private static double GetAngleOfSegment(ISegment inSegment)
        {
            ILine line = new LineClass();
            double outAngle;
            double pi = 4 * System.Math.Atan(1);
            line.FromPoint = inSegment.FromPoint;
            line.ToPoint = inSegment.ToPoint;
           // outAngle = (int)System.Math.Round(((180 * line.Angle) / pi), 0);
            outAngle = line.Angle;

            if (outAngle < 0)
            {
              outAngle += 360;
            }
            return outAngle;
        }
        private static Boolean IsStraight(int angleA, int angleB)
        {   
            int tolerance = 20;
            int testAngle = Math.Abs(angleA - angleB);
            if (testAngle > 180)  testAngle -= 180;
            if (testAngle <= tolerance) return true;
            return false;
        }
        private static int FindTee(double angleA, double angleB, double angleC)
        {
            int tolerance = 20;
            double testAngle;

            testAngle = Math.Abs(angleB - angleC);
            if (testAngle > 180) testAngle -= 180;
            if (testAngle <= tolerance) return 0;

            testAngle = Math.Abs(angleA - angleC);
            if (testAngle > 180) testAngle -= 180;
            if (testAngle <= tolerance) return 1;

            return 2;
        }
        private int GetFieldPosition(string fieldName, IFeature feature)

        {
            int posField = feature.Fields.FindField(fieldName);
            if (posField > -1)
            {
                switch (feature.Fields.get_Field(posField).Type)
                {
                    case esriFieldType.esriFieldTypeDouble:
                    case esriFieldType.esriFieldTypeInteger:
                    case esriFieldType.esriFieldTypeSingle:
                    case esriFieldType.esriFieldTypeSmallInteger:
                        return posField;
                    default:
                        posField = -1;
                        return posField;
                }
            }
            return posField;

         }
        private IFeatureClass GetFeatureClassByClassId(int classId, IGeometricNetwork geomNetwork)
        {
            IEnumFeatureClass enumFC;
            IFeatureClass fc;
            enumFC= geomNetwork.get_ClassesByType(esriFeatureType.esriFTSimpleEdge);
            fc = enumFC.Next();
            while (fc != null)
            {
                if (fc.FeatureClassID == classId) return fc;
                fc = enumFC.Next();
            }
            enumFC = geomNetwork.get_ClassesByType(esriFeatureType.esriFTComplexEdge);
            fc = enumFC.Next();
            while (fc != null)
            {
                if (fc.FeatureClassID == classId) return fc;
                fc = enumFC.Next();
            }
            return null;

        }
        #endregion
        
        
    }
}