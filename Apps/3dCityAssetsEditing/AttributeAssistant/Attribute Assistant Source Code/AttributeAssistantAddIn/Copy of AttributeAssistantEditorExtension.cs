using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Data;
using System.Windows.Forms;
using System.Net;
using System.Web;
using System.Xml;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;

using ESRI.ArcGIS.CartoUI;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.ArcMap;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.GeoDatabaseUI;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.ADF.Connection.Local;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Location;
using System.Drawing;
using System.Reflection;
using ESRI_WaterUtilitiesTemplate;



namespace ESRI_WaterUtilitiesTemplate
{
    /// <summary>
    /// AttributeAssistantEditorExtension class implementing custom ESRI Editor Extension functionalities.
    /// </summary>
    //public class EventNotify
    //{ 
    //public event editEvent()


    //}
    public static class AAState
    {

        public static ESRI.ArcGIS.esriSystem.IPropertySet2 lastValueProperties;
        public static string _filePath = "";
        //public static boolean EventL= "";
        public static ISpatialReference _sr1;
        public static RotationCalculator rCalc;
        public static int _processCount = 0;
        public static IEditEvents_Event _editEvents;
        public static ISpatialReferenceFactory spatRefFact = new SpatialReferenceEnvironmentClass();
        public static IGeographicCoordinateSystem srWGS84 = spatRefFact.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
        public static CurrentUserInfo _currentUserInfo;
        public static string _Debug = "false";
        public static bool _PerformUpdates = true;
        // public static bool _Init = false;
        public static Bitmap bmpOff;
        public static Bitmap bmpOn;
        public static ICommandItem commandItem;
        public static ITable _gentab;
        public static ITable _tab;
        public static DataTable _dt;
        public static DataView _dv;
        public static IEditor _editor;
        public static StreamWriter _sw;
        public static int dynTargetField = 2;
        public static bool _CheckEnvelope = false;

        // Declare configuration variables 
        public static string _defaultsTableName = "DynamicValue";
        public static string _sequenceTableName = "GenerateId";
        public static void setIcon()
        {
            try
            {
                if (bmpOn == null)
                {
                    LoadBitmaps();
                }
                LoadCommand();
                if (commandItem == null)
                    return;

                if (AAState._PerformUpdates)
                {


                    commandItem.FaceID = ESRI.ArcGIS.ADF.COMSupport.OLE.GetIPictureDispFromBitmap(bmpOn);
                    commandItem.Caption = "Attribute Assistant is on";
                    // AAState._PerformUpdates = true;
                    //   MessageBox.Show("Attribute Assistant is now on");
                }
                else
                {

                    commandItem.FaceID = ESRI.ArcGIS.ADF.COMSupport.OLE.GetIPictureDispFromBitmap(bmpOff);
                    commandItem.Caption = "Attribute Assistant is off";
                    //AAState._PerformUpdates = false;
                    // MessageBox.Show("Attribute Assistant is now off");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting AA Toggle button: " + ex.Message);

            }

        }
        public static void LoadCommand()
        {
            try
            {
                AAState.commandItem = ArcMap.Application.Document.CommandBars.Find("ESRI_WaterUtilitiesTemplate_AttributeAssistantToggleCommand");
            }
            catch
            {
                // MessageBox.Show(ex.Message);

            }
        }
        public static void LoadBitmaps()
        {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            Stream myStream = myAssembly.GetManifestResourceStream("ESRI_WaterUtilitiesTemplate.Images.AttributeAssistantToogleOnSolidOff.png");
            AAState.bmpOff = new Bitmap(myStream);
            //AAState.bmpOff.MakeTransparent(AAState.bmpOff.GetPixel(0, 0));

            Stream myStream2 = myAssembly.GetManifestResourceStream("ESRI_WaterUtilitiesTemplate.Images.AttributeAssistantToogleOnSolidOn.png");

            AAState.bmpOn = new Bitmap(myStream2);
            //AAState.bmpOn.MakeTransparent(AAState.bmpOn.GetPixel(0, 0));




        }
        public static bool initTable()
        {
            ////System.Windows.Forms.MessageBox.Show("AA StartEditing");
            //if (_editor == null)
            //{
            //    _editor = ArcMap.Application.FindExtensionByName("esriEditor.Editor") as IEditor;
            //}
            //if (_editor == null)
            //{
            //    // AAState._Init = false;
            //    return false;
            //}
            //// Disable editor extension if editing shapefiles, coverages, etc.

            //if (_dt == null)
            //{
            //    _dt = getConfigDataTable();
            //    _dv = new DataView(_dt);

            //}
            //if (_dt == null)
            //{
            //    //  AAState._Init = false;
            //    return false;
            //}
            //if (_gentab == null)
            //{
            //    _gentab = Globals.FindTable(ArcMap.Document.FocusMap, _sequenceTableName);
            //}
            //if (_gentab == null)
            //{
            //    //       AAState._Init = false;
            //    return false;
            //}

            //System.Windows.Forms.MessageBox.Show("AA StartEditing");
            if (_editor == null)
            {
                _editor = ArcMap.Application.FindExtensionByName("esriEditor.Editor") as IEditor;
            }
            if (_editor == null)
            {
                // AAState._Init = false;
                return false;
            }
            // Disable editor extension if editing shapefiles, coverages, etc.
            initDynTable();



            if (_dt == null)
            {
                //  AAState._Init = false;
                return false;
            }
            _dv = new DataView(_dt);
            _gentab = Globals.FindTable(ArcMap.Document.FocusMap, _sequenceTableName);
            if (_gentab == null)
            {
                //       AAState._Init = false;
                //     return false;
            }



            //    if (_editor.EditWorkspace.Type == esriWorkspaceType.esriFileSystemWorkspace)
            //    {
            //        _dt = null;
            //        //AAState._Init = false;
            //        return false;
            //    }
            //    else
            //    {
            //}
            // AAState._Init = true;
            return true;
        }
        public static void initDynTable()
        {
            _dt = getConfigDataTable();


        }
        private static DataTable getConfigDataTable()
        {
            try
            {
                DataTable dt = new DataTable();

                _tab = Globals.FindTable(ArcMap.Document.FocusMap, _defaultsTableName);
                if (_tab == null)
                    return null;

                for (int i = 0; i < _tab.Fields.FieldCount; i++)
                {
                    if (_tab.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeString)
                        dt.Columns.Add(_tab.Fields.get_Field(i).Name, typeof(string));
                    else if (_tab.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeGlobalID)
                        dt.Columns.Add(_tab.Fields.get_Field(i).Name, typeof(string));
                    else if (_tab.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeGUID)
                        dt.Columns.Add(_tab.Fields.get_Field(i).Name, typeof(string));
                    else if (_tab.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeDate)
                        dt.Columns.Add(_tab.Fields.get_Field(i).Name, typeof(string));
                    else
                        dt.Columns.Add(_tab.Fields.get_Field(i).Name, typeof(int));
                }

                ICursor cur = _tab.Search(null, true);
                IRow row;
                System.Collections.ArrayList ar = new System.Collections.ArrayList();

                while ((row = cur.NextRow()) != null)
                {
                    for (int i = 0; i < _tab.Fields.FieldCount; i++)
                    {
                        ar.Add(row.get_Value(i));
                    }
                    dt.Rows.Add(ar.ToArray());
                    ar.Clear();
                }

                return dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetConfigDataTable: " + ex.Message);
                return null;
            }
        }
        public static void WriteLine(string valToWrite)
        {
            if (AAState._sw == null)
                return;
            else
                AAState._sw.WriteLine(valToWrite);


        }
        public static void createLastValueProperrtySet()
        {
            try
            {

                object nullObject = null;
                if (AAState.lastValueProperties == null || _clearLastValue == true)
                {
                    AAState.lastValueProperties = new PropertySetClass();
                }


                DataView dv = new DataView(AAState._dt);
                dv.RowFilter = "ValueMethod = 'LAST_VALUE'";

                foreach (DataRowView drv in dv)
                {
                    if (drv[dynTargetField].ToString() == "SHAPE")
                    {
                        MessageBox.Show("You have specified SHAPE in a last value entry in the Attribute Assistant, this is not supported");

                    }
                    else
                    {
                        if (_clearLastValue)
                            AAState.lastValueProperties.SetProperty(drv[dynTargetField].ToString(), nullObject);
                        else
                        {
                            try
                            {
                                object temp = AAState.lastValueProperties.GetProperty(drv[dynTargetField].ToString());

                            }
                            catch
                            {
                                AAState.lastValueProperties.SetProperty(drv[dynTargetField].ToString(), nullObject);
                            }


                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetView: " + ex.Message);
            }
        }

        public static void initEditing()
        {
            if (AAState._PerformUpdates == false)
                return;

            // wire events
            if (reInitExt() == false)
            {
                AAState._PerformUpdates = false;
                //AAState.setIcon();
                //return;
            }
            if (AAState._editor.EditWorkspace != null)
            {
                if (AAState._editor.EditWorkspace.Type == esriWorkspaceType.esriFileSystemWorkspace)
                {
                    AAState._PerformUpdates = false;
                    //AAState.setIcon();
                }
            }
            if (AAState._PerformUpdates)
            {
                if (_Debug.ToUpper() == "TRUE")
                {
                    AAState._filePath = Globals.PromptForSave();
                    if (AAState._filePath != "")
                    {
                        AAState._sw = Globals.createTextFile(AAState._filePath, FileMode.Create);
                    }
                }
                else
                {
                    AAState._sw = null;

                }


                // Get user Info
                _currentUserInfo = new CurrentUserInfo(AAState._editor);

                reInitExt();


                //obtain rotation calculator
                AAState.rCalc = new RotationCalculator(ArcMap.Application);

                //Create WGS_84 SR
                ISpatialReferenceFactory srFactory = new SpatialReferenceEnvironmentClass();
                IGeographicCoordinateSystem gcs = srFactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
                _sr1 = gcs;


                // Action[] handlers = _editEvents.OnChangeFeature.GetInvocationList(); 


                _editEvents.OnChangeFeature += FeatureChange;
                _editEvents.OnCreateFeature += FeatureCreate;
                // get configuration table

            }
            else
            {
                //MessageBox.Show("The Attribute Assistant is turned on, but the Dynamic Value table is missing, please add the table and toggle the extension on");

                // WriteLine("Dynamic Value Table is missing - Name in Config:" + AAState._defaultsTableName);

                AAState._PerformUpdates = false;
                AAState.setIcon();// ExecuteToggleAACommands();
                _editEvents.OnChangeFeature -= FeatureChange;
                _editEvents.OnCreateFeature -= FeatureCreate;
                // get configuration table

            }

            ArcMap.Application.StatusBar.set_Message(0, "Editor Extension Initialized");
        }
        public static bool reInitExt()
        {
            AAState.createLastValueProperrtySet();
            return AAState.initTable();


        }
        public static void unInitEditing()
        {
            try
            {
                if (AAState._sw != null)
                {
                    AAState._sw.Flush();
                    AAState._sw.Close();
                    AAState._sw = null;
                }
                AAState._filePath = "";

            }
            catch { }
            try
            {

                //lastValueProperties = null;


                _editEvents.OnChangeFeature -= FeatureChange;
                _editEvents.OnCreateFeature -= FeatureCreate;

                _currentUserInfo = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error on the Stop Editing: " + ex.Message);

            }
        }

        public static event OnChangeFeature changeFeature;
        public delegate void OnChangeFeature(ESRI.ArcGIS.Geodatabase.IObject obj);
        public static event OnCreateFeature createFeature;
        public delegate void OnCreateFeature(ESRI.ArcGIS.Geodatabase.IObject obj);
        private static void FeatureChange(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            try
            {
                //changeFeature = new changeFeature();
                if (changeFeature != null)
                    changeFeature(obj);

                //sendEvent(obj as IObject, "ON_CHANGE");
            }
            catch (Exception ex)
            {
                MessageBox.Show("OnChangeFeature:" + ex.Message + " \n" + obj.Class.AliasName + ": " + obj.OID);

            }
        }

        private static void FeatureCreate(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            try
            {

                if (createFeature != null)
                    createFeature(obj);

                //sendEvent(obj as IObject, "ON_CREATE");
            }
            catch (Exception ex)
            {
                MessageBox.Show("OnCreateFeature:" + ex.Message + " \n" + obj.Class.AliasName + ": " + obj.OID);
            }
        }
        public static bool _clearLastValue = false;

    }

    public class AttributeAssistantEditorExtension : ESRI.ArcGIS.Desktop.AddIns.Extension
    {





        static int _LastOID = -1;
        static string _LastMode = "";
        static string _LastFC = "";

        //The schema of the dynamic values table is fixed.  Please copy the AttributeAssistant table provided in the sample data.  
        //If it is modiifed these field positions may be incorrect.

        //private IApplication _application;
        //private IDocument _document;
        //private IMxDocument _mxDocument;
        //private IMap _map;

        private string lastEditorName;

        //  private IFeature inFeature;
        //private INetworkFeature inNetFeat;
        private IRowChanges inChanges;
        private bool changed;
        private string tableName, fieldName, valMethod, valData, valFC;

        private MSScriptControl.ScriptControlClass script;
        private object lastValue;
        private int fieldCopy, juncField;
        private Nullable<double> rotationAngle;


        private string[] args;
        private string[] sourceLayerNames;

        private string sequenceColumnName, sequenceFixedWidth, sequencePrefix = "", sequencePostfix = "";
        //, , 
        private string genIdAreaFieldName, areaLayerName, areaLayerFieldName, formatString;
        private string intersectLayerName, intersectLayerFieldName;
        private int sequenceColumnNum;
        private int sequencePadding;
        private long sequenceValue;
        private bool found;

        private string newValue;
        private string sourceLayerName;
        private string sourceFieldName;
        private double searchDistance, distance, lastDistance;
        private int sourceField;
        private string[] _currentDatasetNameItems;
        private ESRI.ArcGIS.ADF.ComReleaser _comReleaser;


        private bool _enabledOnStart = true;
        private IFeatureLayer areaLayer;
        private IFeatureLayer intersectLayer;
        private IStandaloneTable intersectTable;
        private IFeatureSelection intersectLayerSelection;
        private ITableSelection intersectTableSelection;
        private IQueryFilter qFilter;
        private IRow row;
        private IField testField;

        private ICurve curve;
        private IFeatureLayer sourceLayer;
        private IPoint _copyPoint;
        private IPolyline _copyPolyline;
        private IPolygon _copyPolygon;
        private ISpatialFilter sFilter;
        private IFeatureCursor fCursor;
        //private ICursor fCursor;
        //private Bitmap bmpOff;
        //private Bitmap bmpOn;
        //private ICommandItem commandItem;
        // private IFeatureCursor fCursor2;
        private IFeature sourceFeature;
        private IFeature nearestFeature;
        private IField fieldObj;
        private IDataset _currentDataset;
        private IProximityOperator proxOp;
        private IEnvelope searchEnvelope;
        private int areaFieldPos;//, genIdAreaFieldPos;
        private int intersectFieldPos;
        private string areaValue;
        private string intersectValue;

        string locatorURL;
        string reverseGeocode = "GeocodeServer/reverseGeocode";
        //private string agsOnlineLocators = "http:///sampleserver1.arcgisonline.com/ArcGIS/rest/services/Locators/";
        private string _agsOnlineLocators = "http://tasks.arcgisonline.com/ArcGIS/rest/services/Locators/TA_Address_NA/";
        /*http:///tasks.arcgisonline.com/ArcGIS/rest/services/Locators/TA_Address_NA/GeocodeServer
        http:///tasks.arcgisonline.com/ArcGIS/rest/services/Locators/TA_Address_NA/GeocodeServer/reverseGeocode*/
        private List<IObject> newFeatureList;

        //Retrieve configuration from XML config file (overrides defaults set above)
        private void GetConfigSettings()
        {
            //ConfigUtil cu = new ConfigUtil();
            AAState._defaultsTableName = ConfigUtil.GetConfigValue("AttributeAssistant_TableName", AAState._defaultsTableName);
            AAState._sequenceTableName = ConfigUtil.GetConfigValue("AttributeAssistant_GenerateId_TableName", AAState._sequenceTableName);
            AAState._CheckEnvelope = ConfigUtil.GetConfigValue("AttributeAssistant_CheckEnvelope ", AAState._CheckEnvelope); 
            _enabledOnStart = ConfigUtil.GetConfigValue("AttributeAssistant_EnabledOnStartUp", _enabledOnStart);
            _agsOnlineLocators = ConfigUtil.GetConfigValue("Geocoder", _agsOnlineLocators);

            if (ConfigUtil.GetConfigValue("AttributeAssistant_ClearLastValue", _agsOnlineLocators).ToUpper() == "TRUE")
                AAState._clearLastValue = true;
            else
                AAState._clearLastValue = false;


            AAState._Debug = ConfigUtil.GetConfigValue("AttributeAssistant_Debug", AAState._Debug);

        }


        //private void ExecuteToggleAACommands()
        //{
        //    try
        //    {



        //        //IDocument document = _editor.Parent.Document;
        //        //ICommandBars documentBars = document.CommandBars;

        //        //UID cmdID = new UIDClass();
        //        //cmdID.Value = (object)"ESRI_WaterUtilitiesTemplate_AttributeAssistantToggleCommad";

        //        //ICommandItem standardCmdItem = documentBars.Find(cmdID, false, true);

        //        ////ESRI.ArcGIS.Desktop.AddIns.AddInComponent c = ESRI.ArcGIS.Desktop.AddIns.AddInDispatchHelper.GetAddInComponent(standardCmdItem);
        //        ////c.SetProperty("tooltip", "New tooltip goes here.");

        //        //System.Drawing.Bitmap offIcon = new System.Drawing.Bitmap(GetType(), "AttributeAssistantToggleOff.bmp");
        //        //System.Drawing.Bitmap onIcon = new System.Drawing.Bitmap(GetType(), "AttributeAssistantToggleOn.bmp");


        //        //if (standardCmdItem != null && standardCmdItem is ICommandItem)
        //        //{
        //        ////    if (standardCmdItem.FaceID == ESRI.ArcGIS.ADF.COMSupport.OLE.GetIPictureDispFromBitmap(offIcon))
        //        ////        standardCmdItem.FaceID = ESRI.ArcGIS.ADF.COMSupport.OLE.GetIPictureDispFromBitmap(onIcon);
        //        ////    else
        //        ////        standardCmdItem.FaceID = ESRI.ArcGIS.ADF.COMSupport.OLE.GetIPictureDispFromBitmap(offIcon);

        //        ////    //standardCmdItem.Execute();
        //        ////    standardCmdItem.Refresh();
        //        //}
        //    }

        //    catch (Exception ex)
        //    {
        //        System.Windows.Forms.MessageBox.Show(ex.Message, ex.Source);
        //    }
        //}

        public AttributeAssistantEditorExtension()
        {


        }
        void appStatusEvents_Initialized()
        {
            // AAState.initEditing();
            if (localEventAdded == false)
            {
                init();
            }
            //  AAState.changeFeature += OnChangeFeature;
            // AAState.createFeature += OnCreateFeature;


        }
        private bool localEventAdded = false;
        void init()
        {

            AAState.initDynTable();
            if (!_enabledOnStart || AAState._dt == null)
            {

                AAState._PerformUpdates = false;
                //ExecuteToggleAACommands();
                AAState.setIcon();
            }
            else
            {
                AAState._PerformUpdates = true;
            }

            AAState.initEditing();
            //if (AAState.changeFeature != null)
            if (localEventAdded == false)
            {

                AAState.changeFeature += OnChangeFeature;
                //if (AAState.createFeature != null)
                AAState.createFeature += OnCreateFeature;
                localEventAdded = true;

            }

        }
        void ArcMap_NewOpenDocument()
        {
            //   AAState.initEditing();
            //if (!_enabledOnStart | AAState._dt == null)
            //{
            //    AAState._PerformUpdates = false;
            //    //ExecuteToggleAACommands();
            //    AAState.setIcon();
            //}
            try
            {
                AAState.unInitEditing();
            }
            catch { }

            try
            {
                if (AAState._sw != null)
                {
                    AAState._sw.Flush();
                    AAState._sw.Close();
                    AAState._sw = null;
                }

            }
            catch { }

            init();


        }
        void ArcMap_CloseDocument()
        {
            // Added AMG in attempt to fix crash on shutdown of ArcMap
            AAState._PerformUpdates = false;
            AAState.unInitEditing();
        }
        protected override void OnStartup()
        {
            ESRI.ArcGIS.ArcMap.IApplicationStatusEvents_Event appStatusEvents = ArcMap.Application as ESRI.ArcGIS.ArcMap.IApplicationStatusEvents_Event;
            appStatusEvents.Initialized += new ESRI.ArcGIS.ArcMap.IApplicationStatusEvents_InitializedEventHandler(appStatusEvents_Initialized);

            ArcMap.Events.NewDocument += ArcMap_NewOpenDocument;
            ArcMap.Events.OpenDocument += ArcMap_NewOpenDocument;
            ArcMap.Events.CloseDocument += ArcMap_CloseDocument;

            GetConfigSettings();
            if (!_enabledOnStart | AAState._dt == null)
            {
                AAState._PerformUpdates = false;
                //ExecuteToggleAACommands();
                AAState.setIcon();
            }
            IEditor _editor = Globals.getEditor(ArcMap.Application);

            //Wire editor events.
            AAState._editEvents = (IEditEvents_Event)_editor;
            AAState._editEvents.OnStartEditing += OnStartEditing;
            AAState._editEvents.OnStopEditing += OnStopEditing;

            _comReleaser = new ESRI.ArcGIS.ADF.ComReleaser();
            _comReleaser.ManageLifetime(fCursor);


            script = new MSScriptControl.ScriptControlClass();
            script.AllowUI = false;
            script.Language = "VBScript";
            string strScript = "function iif(psdStr, trueStr, falseStr)" + System.Environment.NewLine +
                      "if psdStr then" + System.Environment.NewLine +
                        "iif = trueStr" + System.Environment.NewLine +
                      "else " + System.Environment.NewLine +
                        "iif = falseStr" + System.Environment.NewLine +
                      "end if" + System.Environment.NewLine +
                    "end function" + System.Environment.NewLine;
            script.AddCode(strScript);

            //     strScript = "function trueStr () " + System.Environment.NewLine +
            //     "if trueStr== \"<Null>\" then " + System.Environment.NewLine +
            //     "return(DBNull.value)" + System.Environment.NewLine +
            //     "else" + System.Environment.NewLine +
            //     "return trueStr" + System.Environment.NewLine +
            //     "end function" + System.Environment.NewLine;

            //     script.AddCode(strScript);
            //     strScript = "function falseStr () " + System.Environment.NewLine +
            //"if falseStr== \"<Null>\" then " + System.Environment.NewLine +
            //"return(DBNull.value)" + System.Environment.NewLine +
            //"else" + System.Environment.NewLine +
            //"return falseStr" + System.Environment.NewLine +
            //"end function" + System.Environment.NewLine;

            //     script.AddCode(strScript);
            AAState.WriteLine("                  script to process: " + newValue);
            //_application = _editor.Parent;
            //if (_application != null)
            //{
            //    _document = _application.Document;
            //    _mxDocument = (IMxDocument)_document;
            //    _map = _mxDocument.FocusMap;
            //}
            //else
            //    MessageBox.Show("Couldn't get handle for ArcMap Application");

        }

        protected override void OnShutdown()
        {
            try
            {
                if (AAState._sw != null)
                {
                    AAState._sw.Flush();
                    AAState._sw.Close();
                    AAState._sw = null;
                }

            }
            catch { }
            try
            {
                //ESRI.ArcGIS.ArcMap.IApplicationStatusEvents_Event appStatusEvents = ArcMap.Application as ESRI.ArcGIS.ArcMap.IApplicationStatusEvents_Event;
                //appStatusEvents.Initialized -= new ESRI.ArcGIS.ArcMap.IApplicationStatusEvents_InitializedEventHandler(appStatusEvents_Initialized);

                ArcMap.Events.NewDocument -= ArcMap_NewOpenDocument;
                ArcMap.Events.OpenDocument -= ArcMap_NewOpenDocument;

                if (AAState._editor != null)
                {

                    //Wire editor events.
                    AAState._editEvents = (IEditEvents_Event)AAState._editor;
                    AAState._editEvents.OnStartEditing -= OnStartEditing;
                    AAState._editEvents.OnStopEditing -= OnStopEditing;

                    AAState._editor = null;
                    AAState._editEvents = null;

                }
                if (_comReleaser != null)
                    _comReleaser.Dispose();


                try
                {
                    AAState.bmpOff.Dispose();
                    AAState.bmpOn.Dispose();
                    AAState.commandItem = null;
                }
                catch { }
            }

            catch (Exception ex)
            {
                MessageBox.Show("OnShutdown: " + ex.Message);

            }
        }
        // protected override bool OnSetState(ExtensionState state) 

        #region Editor Events

        #region Shortcut properties to the various editor event interfaces
        private IEditEvents_Event Events
        {
            get
            {

                return Globals.getEditor(ArcMap.Application) as IEditEvents_Event;

            }
        }
        private IEditEvents2_Event Events2
        {
            get { return Globals.getEditor(ArcMap.Application) as IEditEvents2_Event; }
        }
        private IEditEvents3_Event Events3
        {
            get { return Globals.getEditor(ArcMap.Application) as IEditEvents3_Event; }
        }
        private IEditEvents4_Event Events4
        {
            get { return Globals.getEditor(ArcMap.Application) as IEditEvents4_Event; }
        }
        #endregion

        void WireEditorEvents()
        {
            try
            {
                //
                //  TODO: Sample code demonstrating editor event wiring
                //
                Events.OnCurrentTaskChanged += delegate
                {
                    if (Globals.getEditor(ArcMap.Application).CurrentTask != null)
                        System.Diagnostics.Debug.WriteLine(Globals.getEditor(ArcMap.Application).CurrentTask.Name);
                };
                Events2.BeforeStopEditing += delegate(bool save) { OnBeforeStopEditing(save); };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in the Wire Editor Events: " + ex.Message);

            }
        }

        void OnBeforeStopEditing(bool save)
        {
        }

        private void OnStartEditing()
        {

            try
            {
                AAState.initDynTable();



                if (AAState._PerformUpdates && AAState._Debug.ToUpper() == "TRUE")
                {
                    if (AAState._sw == null && AAState._filePath != "")
                    {

                        AAState._sw = Globals.createTextFile(AAState._filePath, FileMode.Append);

                        AAState.WriteLine("#######################################################");


                    }

                }


                //      AAState.initEditing();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error On Start Editing Event: " + ex.Message);

            }


        }
        private void OnStopEditing(Boolean save)
        {
            //AAState.unInitEditing();
        }



        private void OnChangeFeature(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            try
            {

                sendEvent(obj as IObject, "ON_CHANGE");
            }
            catch (Exception ex)
            {
                MessageBox.Show("OnChangeFeature:" + ex.Message + " \n" + obj.Class.AliasName + ": " + obj.OID);

            }
        }

        private void OnCreateFeature(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            try
            {
                sendEvent(obj as IObject, "ON_CREATE");
            }
            catch (Exception ex)
            {
                MessageBox.Show("OnCreateFeature:" + ex.Message + " \n" + obj.Class.AliasName + ": " + obj.OID);
            }
        }

        private void sendEvent(IObject inObject, string mode)
        {
            try
            {
                if (AAState._PerformUpdates == false)
                    return;
                if (_LastOID == inObject.OID && _LastMode == mode && _LastFC == inObject.Class.AliasName && AAState._processCount > 0)
                {
                    _LastOID = -1;
                    _LastMode = "";
                    _LastFC = "";
                    return;
                }
                _LastOID = inObject.OID;
                _LastMode = mode;
                _LastFC = inObject.Class.AliasName;

                //AAState._editEvents.OnChangeFeature -= OnChangeFeature;
                //AAState._editEvents.OnCreateFeature -= OnCreateFeature;
                AAState.changeFeature -= OnChangeFeature;
                //AAState.changeFeature -= OnChangeFeature;
                //AAState.changeFeature -= OnChangeFeature;
                //AAState.changeFeature -= OnChangeFeature;
                //AAState.changeFeature -= OnChangeFeature;
                //AAState.changeFeature -= OnChangeFeature;


                AAState.createFeature -= OnCreateFeature;


                if (AAState._Debug.ToUpper() == "TRUE")
                {
                    if (AAState._sw == null && AAState._filePath != "")
                    {

                        AAState._sw = Globals.createTextFile(AAState._filePath, FileMode.Append);


                    }
                    AAState.WriteLine("#######################################################");

                    AAState.WriteLine(inObject.Class.AliasName + " - " + mode.ToString());
                }
                newFeatureList = null;

                AAState._processCount++;
                //Set attributes based on the dynamic defaults configuration table
                SetDynamicValues(inObject, mode);
                if (newFeatureList != null)
                {
                    foreach (IObject feature in newFeatureList)
                    {
                        AAState.WriteLine("#######################################################");
                        AAState.WriteLine(feature.Class.AliasName + " - split feature ON_CREATE");
                        AAState._processCount++;
                        SetDynamicValues(feature, "ON_CREATE");
                        AAState._processCount--;
                    }
                    newFeatureList = null;
                }
                AAState._processCount--;
                if (AAState._sw != null)
                {
                    if (AAState._processCount == 0)
                    {
                        AAState.WriteLine("#######################################################");
                        AAState._sw.Flush();
                        // AAState._sw.Close();
                        //AAState._sw = null;
                    }
                }
                //AAState._editEvents.OnChangeFeature += OnChangeFeature;
                //AAState._editEvents.OnCreateFeature += OnCreateFeature;
                AAState.changeFeature += OnChangeFeature;
                AAState.createFeature += OnCreateFeature;


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error on the Send Event Object: " + ex.Message);

            }
        }
        #endregion

        #region Helper Methods

        public void SetDynamicValues(IObject inObject, string mode)
        {
            IFeature inFeature = null;
            // IRow inRow = null;

            IMSegmentation mseg = null;
            INetworkFeature netFeat = null;
            IEdgeFeature edgeFeat = null;
            IFeature juncFeat;
            IJunctionFeature iJuncFeat;
            //ProgressBar
            ESRI.ArcGIS.Framework.IProgressDialogFactory progressDialogFactory = null;
            ESRI.ArcGIS.esriSystem.IStepProgressor stepProgressor = null;
            ESRI.ArcGIS.Framework.IProgressDialog2 progressDialog = null;
            // Create a CancelTracker
            ESRI.ArcGIS.esriSystem.ITrackCancel trackCancel = new ESRI.ArcGIS.Display.CancelTrackerClass();

            try
            {
                if (AAState._PerformUpdates && AAState._dv == null)
                {

                    AAState.WriteLine("Dynamic Value Table is missing - Name in Config:" + AAState._defaultsTableName);

                    //    MessageBox.Show("The Attribute Assistant is turned on, but the Dynamic Value table is missing, please add the table and toggle the extension on");
                    AAState._PerformUpdates = false;
                    return;
                }
                if (AAState._PerformUpdates && AAState._dv != null)
                {
                    if (inObject == null)
                        return;
                    //Convert row to feature (test for feature is null before using - this could be a table update)
                    inFeature = inObject as IFeature;
                    // inRow = inObject as IRow;
                    string modeVal;

                    if (AAState._dv.Table.Columns[mode].DataType == System.Type.GetType("System.String"))
                    {
                        modeVal = "(" + mode + " = '1' or " + mode + " = 'Yes' or " + mode + " = 'YES' or " + mode + " = 'True' or " + mode + " = 'TRUE')";

                    }
                    else
                    {
                        modeVal = mode + " = 1";
                    }
                    System.Int32 int32_hWnd = ArcMap.Application.hWnd;

                    progressDialogFactory = new ESRI.ArcGIS.Framework.ProgressDialogFactoryClass();
                    stepProgressor = progressDialogFactory.Create(trackCancel, int32_hWnd);

                    stepProgressor.MinRange = 0;
                    stepProgressor.MaxRange = inObject.Fields.FieldCount;
                    stepProgressor.StepValue = 1;
                    stepProgressor.Message = "Attribute Assistant Progress";
                    // Create the ProgressDialog. This automatically displays the dialog
                    progressDialog = (ESRI.ArcGIS.Framework.IProgressDialog2)stepProgressor; // Explict Cast

                    // Set the properties of the ProgressDialog
                    progressDialog.CancelEnabled = true;
                    progressDialog.Description = "Checking rules for " + inObject.Class.AliasName;
                    progressDialog.Title = "Attribute Assistant Progress";
                    progressDialog.Animation = ESRI.ArcGIS.Framework.esriProgressAnimationTypes.esriProgressGlobe;
                    progressDialog.ShowDialog();
                    //for each field in this row/feature
                    // Skip orphan junctions (saves time)

                    if (Globals.isOrpanJunction(inFeature))
                        return;
                    //Get table name for this feature
                    _currentDataset = inObject.Class as IDataset;
                    _currentDatasetNameItems = _currentDataset.Name.Split('.');
                    tableName = _currentDatasetNameItems[_currentDatasetNameItems.GetLength(0) - 1];
                    
                    for (int fieldNum = 0; fieldNum <= inObject.Fields.FieldCount; fieldNum++)
                    {

                        stepProgressor.Step();
                        bool editable = true;
                        if (fieldNum == inObject.Fields.FieldCount)
                        {
                            fieldObj = null;

                            fieldName = "";
                            stepProgressor.Message = "Checking rules for field null fields in" + inObject.Class.AliasName;
                            progressDialog.Description = "Checking rules for field null fields in" + inObject.Class.AliasName;
                            AAState.WriteLine("***********************************************************");
                            AAState.WriteLine("");

                            AAState.WriteLine("############ " + DateTime.Now + " ################");
                            editable = true;
                            //  fieldNum++;

                        }
                        else
                        {
                            fieldObj = inObject.Fields.get_Field(fieldNum);

                            fieldName = fieldObj.Name;
                            stepProgressor.Message = "Checking rules for field " + fieldName + " in " + inObject.Class.AliasName;
                            progressDialog.Description = "Checking rules for field " + fieldName + " in " + inObject.Class.AliasName;
                            AAState.WriteLine("***********************************************************");
                            AAState.WriteLine("");
                            AAState.WriteLine("############ " + fieldName + " ################");
                            AAState.WriteLine("############ " + DateTime.Now + " ################");
                            AAState.WriteLine("Is field -  " + fieldName + " Editable = " + fieldObj.Editable);
                            editable = fieldObj.Editable;
                        }
                        bool proc = false;
                        if (editable == true && fieldObj != null)
                        {
                            AAState.WriteLine("Checking rules for field " + fieldName + " in " + inObject.Class.AliasName);


                            AAState._dv.RowFilter = "(TABLENAME = '" + tableName + "' OR TABLENAME like '" + tableName + "|*' OR TABLENAME like '" + tableName + "|%') And FIELDNAME = '" + fieldName + "' and " + modeVal;
                            //AAState._dv.rw
                            AAState.WriteLine("    Row Query: " + "TABLENAME = '" + tableName + "' OR TABLENAME like '" + tableName + "|*' OR TABLENAME like '" + tableName + "|%') And FIELDNAME = '" + fieldName + "' and " + modeVal);
                            if (AAState._dv.Count == 0)
                            {
                                AAState._dv.RowFilter = "TABLENAME = '*' And FIELDNAME = '" + fieldName + "' and " + modeVal;

                                AAState.WriteLine("    Query using " + fieldName + " did not produce any results, trying all fields");
                                AAState.WriteLine("    Row Query: " + "TABLENAME = '*' And FIELDNAME = '" + fieldName + "' and " + modeVal);
                            }
                            if (AAState._dv.Count == 0)
                            {
                                AAState.WriteLine("    No entries found");
                                proc = false;
                            }
                            else
                            {
                                proc = true;
                            }

                        }
                        //   else if (editable == false )
                        //{
                        //    AAState.WriteLine("Checking rules for field " + fieldName + " in " + inObject.Class.AliasName);


                        //    AAState._dv.RowFilter = "(TABLENAME = '" + tableName + "' OR TABLENAME like '" + tableName + "|*' OR TABLENAME like '" + tableName + "|%') And FIELDNAME = '" + fieldName + "' and " + modeVal;
                        //    //AAState._dv.rw
                        //    AAState.WriteLine("    Row Query: " + "TABLENAME = '" + tableName + "' OR TABLENAME like '" + tableName + "|*' OR TABLENAME like '" + tableName + "|%') And FIELDNAME = '" + fieldName + "' and " + modeVal);
                        //    if (AAState._dv.Count == 0)
                        //    {
                        //        AAState._dv.RowFilter = "TABLENAME = '*' And FIELDNAME = '" + fieldName + "' and " + modeVal;

                        //        AAState.WriteLine("    Query using " + fieldName + " did not produce any results, trying all fields");
                        //        AAState.WriteLine("    Row Query: " + "TABLENAME = '*' And FIELDNAME = '" + fieldName + "' and " + modeVal);
                        //    }
                        //    if (AAState._dv.Count == 0)
                        //    {
                        //        AAState.WriteLine("    No entries found");
                        //        proc = false;
                        //    }
                        //    else
                        //    {
                        //        proc = true;
                        //    }

                        //}
                        else if (editable && fieldObj == null)
                        {
                            fieldName = "";
                            AAState._dv.RowFilter = "(TABLENAME = '" + tableName + "' OR TABLENAME like '" + tableName + "|*' OR TABLENAME like '" + tableName + "|%') And (FIELDNAME = '" + fieldName + "' or FIELDNAME IS NULL) and " + modeVal;
                            //AAState._dv.rw
                            AAState.WriteLine("    Row Query: " + "TABLENAME = '" + tableName + "' OR TABLENAME like '" + tableName + "|*' OR TABLENAME like '" + tableName + "|%') And (FIELDNAME = '" + fieldName + "' or FIELDNAME IS NULL) and " + modeVal);
                            if (AAState._dv.Count == 0)
                            {
                                AAState._dv.RowFilter = "TABLENAME = '*' And (FIELDNAME = '" + fieldName + "' or FIELDNAME IS NULL) and " + modeVal;

                                AAState.WriteLine("    Query using " + fieldName + " did not produce any results, trying all fields");
                                AAState.WriteLine("    Row Query: " + "TABLENAME = '*' And And (FIELDNAME = '" + fieldName + "' or FIELDNAME IS NULL) and " + modeVal);
                            }
                            if (AAState._dv.Count == 0)
                            {
                                AAState.WriteLine("    No entries found");
                                proc = false;
                            }
                            else
                            {
                                proc = true;
                            }
                        }
                        if (proc)
                        {

                            AAState.WriteLine("          Value Entry Rows found: " + AAState._dv.Count);
                            AAState.WriteLine("          ----------------------------------------------");
                            // Get requested method and any data parameters
                            AAState.WriteLine("          Looping through Value Methods");
                            AAState.WriteLine("                  ------------------------");
                            foreach (DataRowView drv in AAState._dv)
                            {

                                AAState.WriteLine("                  Checking for Subtype Restriction");
                                valFC = drv["TABLENAME"].ToString().Trim();
                                if (valFC.Contains("|"))
                                {
                                    AAState.WriteLine("                  Subtype restriction Found");
                                    string[] spliVal = valFC.Split('|');

                                    if (Globals.IsInteger(spliVal[1]))
                                    {
                                        int SubVal = Convert.ToInt32(spliVal[1]);
                                        ISubtypes pSub = inObject.Class as ISubtypes;
                                        if (pSub != null)
                                        {
                                            if (pSub.HasSubtype)
                                            {
                                                int obSubVal = (int)inObject.get_Value(pSub.SubtypeFieldIndex);
                                                if (obSubVal == SubVal)
                                                {
                                                    AAState.WriteLine("                  Subtypes match");
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  Skipping, not the subtype defined");
                                                    continue;
                                                }
                                            }
                                            else
                                            {
                                                AAState.WriteLine("                  ERROR: Layer does not have subtypes");
                                            }
                                        }
                                        else
                                        {
                                            AAState.WriteLine("                  ERROR: Layer does not have subtypes");
                                        }
                                    }
                                    else
                                    {
                                        AAState.WriteLine("                  ERROR: Subtype not an integar");
                                    }
                                }
                                valMethod = drv["VALUEMETHOD"].ToString().ToUpper().Trim();
                                valData = drv["VALUEINFO"].ToString().Trim();


                                AAState.WriteLine("                  VALUEMETHOD: " + valMethod);
                                AAState.WriteLine("                  VALUEINFO: " + valData);
                                AAState.WriteLine("                  ------------------------");
                                //set field value based on specified method
                                try
                                {
                                    switch (valMethod)
                                    {
                                        case "SPLIT_INTERSECTING_FEATURE":
                                            {
                                                AAState.WriteLine("                  Trying: SPLIT_INTERSECTING_FEATURE");

                                                try
                                                {
                                                    if ((valData != null) && (inFeature != null))
                                                    {
                                                        intersectLayerName = "";
                                                        intersectLayer = null;
                                                        args = valData.Split('|');
                                                        if (args.Length > 0)
                                                        {
                                                            AAState.WriteLine("                  " + args.Length + " Layers listed ");

                                                            for (int i = 0; i < args.Length; i++)
                                                            {

                                                                intersectLayerName = args[i].Trim();
                                                                AAState.WriteLine("                  Searching for " + intersectLayerName);

                                                                intersectLayer = Globals.FindLayer(AAState._editor.Map, intersectLayerName) as IFeatureLayer;

                                                                if (intersectLayer != null)
                                                                {
                                                                    AAState.WriteLine("                  Layer Found " + intersectLayerName);
                                                                    if (intersectLayer.FeatureClass != null)
                                                                    {
                                                                        AAState.WriteLine("                  Datasource is valid for " + intersectLayerName);

                                                                        sFilter = new SpatialFilterClass();
                                                                        AAState.WriteLine("                  Checking source Geometry Type");

                                                                        if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                                        {
                                                                            // esriSpatialRelIntersects does not work properly for point intersecting line.
                                                                            // hence expand point envelope (code cribbed from below)
                                                                            try
                                                                            {
                                                                                ISpatialReferenceResolution pSRResolution;

                                                                                pSRResolution = ((sourceLayer.FeatureClass as IGeoDataset).SpatialReference) as ISpatialReferenceResolution;

                                                                                //  sFilter = new SpatialFilterClass();
                                                                                double intTol = pSRResolution.get_XYResolution(false);
                                                                                bool hasXY = ((sourceLayer.FeatureClass as IGeoDataset).SpatialReference).HasXYPrecision();

                                                                                searchEnvelope = new EnvelopeClass();
                                                                                searchEnvelope.XMin = 0 - intTol;
                                                                                searchEnvelope.YMin = 0 - intTol;
                                                                                searchEnvelope.XMax = 0 + intTol;
                                                                                searchEnvelope.YMax = 0 + intTol;
                                                                                searchEnvelope.CenterAt(inFeature.ShapeCopy as IPoint);

                                                                                //searchEnvelope.SpatialReference = ((inFeature.Class as IFeatureClass) as IGeoDataset).SpatialReference;
                                                                                searchEnvelope.SpatialReference = ((inFeature.Class as IFeatureClass) as IGeoDataset).SpatialReference;
                                                                                searchEnvelope.SnapToSpatialReference();
                                                                                searchEnvelope.Project(AAState._editor.Map.SpatialReference);


                                                                                sFilter.Geometry = Globals.Env2Polygon(searchEnvelope);

                                                                            }
                                                                            catch
                                                                            {
                                                                                sFilter.Geometry = inFeature.ShapeCopy;
                                                                            }

                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  Geoemetry is not a point, using shape envelope");

                                                                            sFilter.Geometry = inFeature.ShapeCopy;
                                                                        }
                                                                        sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                                                                        sFilter.GeometryField = intersectLayer.FeatureClass.ShapeFieldName;
                                                                        AAState.WriteLine("                  Searching " + intersectLayerName + "for intersected feature");

                                                                        fCursor = intersectLayer.FeatureClass.Search(sFilter, true);
                                                                        IFeature intsersectFeature;
                                                                        int idx = 1;
                                                                        while ((intsersectFeature = fCursor.NextFeature()) != null)
                                                                        {
                                                                            AAState.WriteLine("                  Splitting Intersected Feature number: " + idx);
                                                                            idx++;

                                                                            IFeatureEdit featureEdit = intsersectFeature as IFeatureEdit;
                                                                            ISet featset = featureEdit.Split(inFeature.Shape);
                                                                            AAState.WriteLine("                  Adding split features to array to call the AA ext");

                                                                            if (featset.Count > 0)
                                                                            {
                                                                                if (newFeatureList == null)
                                                                                {
                                                                                    newFeatureList = new List<IObject>();
                                                                                }
                                                                                object featobj;
                                                                                while ((featobj = featset.Next()) != null)
                                                                                {
                                                                                    IFeature feature = featobj as IFeature;
                                                                                    if (feature != null)
                                                                                    {
                                                                                        newFeatureList.Add(feature as IObject);
                                                                                    }
                                                                                }
                                                                            }
                                                                            AAState.WriteLine("                  Split feature " + intersectLayerName + " into " + featset.Count);
                                                                            if (intsersectFeature != null)
                                                                            {
                                                                                Marshal.ReleaseComObject(intsersectFeature);
                                                                            }

                                                                        }
                                                                    }
                                                                }

                                                                else
                                                                {
                                                                    AAState.WriteLine("                  Warning: Can't find intersecting layer: " + intersectLayerName);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Unsupported Value Info: " + valData);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: not a feature or no Value Info");
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: SPLIT_INTERSECTING_FEATURE" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: SPLIT_INTERSECTING_FEATURE");
                                                }
                                                break;
                                            }



                                        case "NEAREST_FEATURE_ATTRIBUTES":
                                            {
                                                AAState.WriteLine("                  Trying NEAREST_FEATURE_ATTRIBUTES");
                                                try
                                                {
                                                    if ((valData != null) && (inFeature != null))
                                                    {
                                                        sourceLayerName = "";
                                                        string[] sourceFieldNames = null;
                                                        string[] destFieldNames = null;
                                                        searchDistance = 0;

                                                        // Parse arguments
                                                        args = valData.Split('|');
                                                        if (args.Length == 3)
                                                        {
                                                            sourceLayerName = args[0].ToString().Trim();
                                                            sourceFieldNames = args[1].ToString().Split(',');
                                                            destFieldNames = args[2].ToString().Split(',');
                                                            AAState.WriteLine("                  WARNING:  search distance as not specified, defaulting to 0");

                                                        }
                                                        else if (args.Length == 4)
                                                        {
                                                            sourceLayerName = args[0].ToString().Trim();
                                                            sourceFieldNames = args[1].ToString().Split(',');
                                                            destFieldNames = args[2].ToString().Split(',');
                                                            Double.TryParse(args[3], out searchDistance);
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR:  the valuemethod is not defined properly for this rule");
                                                            continue;

                                                        }

                                                        if ((sourceFieldNames != null) && (destFieldNames != null) &&
                                                            (sourceFieldNames.Length > 0) && (destFieldNames.Length > 0) &&
                                                            (sourceFieldNames.Length == destFieldNames.Length))
                                                        {
                                                            AAState.WriteLine("                  Looking for layer: " + sourceLayerName);

                                                            sourceLayer = Globals.FindLayer(AAState._editor.Map, sourceLayerName) as IFeatureLayer;
                                                            if (sourceLayer != null)
                                                            {
                                                                if (sourceLayer.FeatureClass != null)
                                                                {
                                                                    AAState.WriteLine("                  layer Found: " + sourceLayerName);

                                                                    string missingFieldMess = null;
                                                                    int[] sourceFieldNums = new int[sourceFieldNames.Length];
                                                                    int[] destFieldNums = new int[destFieldNames.Length];
                                                                    AAState.WriteLine("                  Checking Field Mapping");

                                                                    for (int i = 0; i < sourceFieldNums.Length; i++)
                                                                    {
                                                                        int fnum = sourceLayer.FeatureClass.FindField(sourceFieldNames[i].Trim());
                                                                        if (fnum < 0)
                                                                        {
                                                                            missingFieldMess = sourceFieldNames[i].Trim() + " in table " + sourceLayerName;
                                                                            break;
                                                                        }
                                                                        sourceFieldNums[i] = fnum;
                                                                    }
                                                                    if (missingFieldMess == null)
                                                                    {
                                                                        for (int i = 0; i < destFieldNums.Length; i++)
                                                                        {
                                                                            int fnum = inFeature.Fields.FindField(destFieldNames[i].Trim());
                                                                            if (fnum < 0)
                                                                            {
                                                                                missingFieldMess = destFieldNames[i].Trim() + " in table " + tableName;
                                                                                break;
                                                                            }
                                                                            destFieldNums[i] = fnum;
                                                                        }
                                                                    }
                                                                    if (missingFieldMess == null)
                                                                    {
                                                                        AAState.WriteLine("                  Field Mapping verified");

                                                                        // found source and destination fields.
                                                                        sFilter = new SpatialFilterClass();

                                                                        if (searchDistance > 0)
                                                                        {
                                                                            searchEnvelope = inFeature.ShapeCopy.Envelope;
                                                                            searchEnvelope.Expand(searchDistance, searchDistance, false);
                                                                            sFilter.Geometry = searchEnvelope;
                                                                        }
                                                                        else
                                                                            sFilter.Geometry = inFeature.ShapeCopy;

                                                                        sFilter.GeometryField = sourceLayer.FeatureClass.ShapeFieldName;
                                                                        sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                                                                        AAState.WriteLine("                  Searching for Nearest Feature");

                                                                        fCursor = sourceLayer.FeatureClass.Search(sFilter, false);
                                                                        sourceFeature = fCursor.NextFeature();
                                                                        nearestFeature = null;

                                                                        proxOp = (IProximityOperator)inFeature.Shape;
                                                                        lastDistance = searchDistance;
                                                                        if (sourceFeature != null)
                                                                        {
                                                                            AAState.WriteLine("                  Features Found, looping for closest");

                                                                            while (sourceFeature != null)
                                                                            {

                                                                                distance = proxOp.ReturnDistance(sourceFeature.Shape);
                                                                                if (distance <= lastDistance)
                                                                                {
                                                                                    nearestFeature = sourceFeature;
                                                                                    lastDistance = distance;
                                                                                }
                                                                                sourceFeature = fCursor.NextFeature();
                                                                            }
                                                                        }
                                                                        if (nearestFeature != null)
                                                                        {
                                                                            AAState.WriteLine("                  Closest Feature is " + lastDistance + " Away with OID of " + nearestFeature.OID);


                                                                            for (int i = 0; i < sourceFieldNums.Length; i++)
                                                                            {
                                                                                try
                                                                                {
                                                                                    AAState.WriteLine("                  Trying to copy " + sourceFieldNums[i] + " to " + destFieldNums[i]);

                                                                                    inObject.set_Value(destFieldNums[i], nearestFeature.get_Value(sourceFieldNums[i]));
                                                                                }
                                                                                catch
                                                                                {
                                                                                    AAState.WriteLine("                  ERROR: copying " + sourceFieldNums[i] + " to " + destFieldNums[i]);

                                                                                }
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  No Feature was found, default fields");

                                                                            for (int i = 0; i < destFieldNums.Length; i++)
                                                                            {
                                                                                IField field = inObject.Fields.get_Field(destFieldNums[i]);
                                                                                object newval = field.DefaultValue;
                                                                                if (newval == null)
                                                                                {
                                                                                    if (field.IsNullable)
                                                                                    {
                                                                                        inObject.set_Value(destFieldNums[i], null);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    inObject.set_Value(destFieldNums[i], newval);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR: Cant find field " + missingFieldMess);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR: " + sourceLayerName + " data source is not set");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR: " + sourceLayerName + " was not found");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Invalid Value Info: " + valData);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Not a feature or missing Value Info");
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: EUK_NEAREST_FEATURE_ATTRIBUTES" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: EUK_NEAREST_FEATURE_ATTRIBUTES");
                                                }
                                                break;
                                            }
                                        case "MINIMUM_LENGTH":
                                            {
                                                try
                                                {
                                                    AAState.WriteLine("                  Trying MINIMUM_LENGTH");

                                                    double minlength;
                                                    AAState.WriteLine("                  Evaluating Minimum length value");

                                                    if (Double.TryParse(valData, out minlength))
                                                    {
                                                        if (inFeature != null)
                                                        {
                                                            ICurve curve = inFeature.Shape as ICurve;
                                                            if (curve != null)
                                                            {
                                                                if (curve.Length < minlength)
                                                                {
                                                                    String mess = "Line is shorter than " +
                                                                        String.Format("{0:0.00}", minlength) + " " + Globals.GetSpatRefUnitName(inFeature.Shape.SpatialReference, true) +
                                                                        ", aborting edit.";
                                                                    AAState.WriteLine("                  " + mess);

                                                                    MessageBox.Show(mess, "Line too short");
                                                                    AAState._editor.AbortOperation();
                                                                    return;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR:  Feature is not a Line");

                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  Error MINIMUM_LENGTH \n" + ex.Message);
                                                }
                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished MINIMUM_LENGTH");

                                                }

                                                break;
                                            }
                                        case "LINK_TABLE_ASSET":
                                            try
                                            {
                                                intersectLayerName = "";
                                                intersectTable = null;
                                                intersectLayer = null;
                                                intersectLayerFieldName = "";
                                                AAState.WriteLine("                  Trying LINK_TABLE_ASSET");
                                                args = valData.Split('|');
                                                switch (args.GetLength(0))
                                                {
                                                    case 1:  // Feature Layer only
                                                        intersectLayerName = args[0].ToString();
                                                        break;
                                                    case 2:  // Feature Layer| Field to copy
                                                        intersectLayerName = args[0].ToString();
                                                        intersectLayerFieldName = args[1].ToString();

                                                        break;
                                                    case 3:  // Feature Layer| Field to copy | for future
                                                        intersectLayerName = args[0].ToString();
                                                        intersectLayerFieldName = args[1].ToString();
                                                        //sequenceColumnName = args[2].ToString();
                                                        break;
                                                    default:
                                                        AAState.WriteLine("                  ERROR: Unsupported Value Method: " + valData);
                                                        break;
                                                }
                                                intersectLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, intersectLayerName);
                                                intersectTable = Globals.FindStandAloneTable(AAState._editor.Map, intersectLayerName);
                                                ICursor cCurs = null;
                                                if (intersectLayer != null)
                                                {
                                                    //Find Area Field
                                                    intersectFieldPos = intersectLayer.FeatureClass.Fields.FindField(intersectLayerFieldName);
                                                    if (intersectFieldPos < 0)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Asset feature Layer Field(" + intersectLayerFieldName + ") not found");
                                                        break;
                                                    }
                                                    intersectLayerSelection = (IFeatureSelection)intersectLayer;
                                                    if (intersectLayerSelection.SelectionSet.Count == 0)
                                                    {
                                                        AAState.WriteLine("                  ERROR: No assests selected in " + intersectLayerName);

                                                        break;
                                                    }
                                                    if (intersectLayerSelection.SelectionSet.Count > 1)
                                                    {
                                                        AAState.WriteLine("                  ERROR: To many assests are selected in " + intersectLayerName);

                                                        break;
                                                    }

                                                    intersectLayerSelection.SelectionSet.Search(null, true, out cCurs);
                                                }
                                                else if (intersectTable != null)
                                                {
                                                    if (intersectTable.Table == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Asset  Layer(" + intersectLayerName + ") not found");
                                                        break;
                                                    }
                                                    //Find Area Field
                                                    intersectFieldPos = intersectTable.Table.Fields.FindField(intersectLayerFieldName);
                                                    if (intersectFieldPos < 0)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Asset Layer Field(" + intersectLayerFieldName + ") not found");
                                                        break;
                                                    }
                                                    intersectTableSelection = (ITableSelection)intersectTable;
                                                    if (intersectTableSelection.SelectionSet.Count == 0)
                                                    {
                                                        AAState.WriteLine("                  ERROR: No assests selected in " + intersectLayerName);

                                                        break;
                                                    }
                                                    if (intersectTableSelection.SelectionSet.Count > 1)
                                                    {
                                                        AAState.WriteLine("                  ERROR: To many assests are selected in " + intersectLayerName);

                                                        break;
                                                    }

                                                    intersectTableSelection.SelectionSet.Search(null, true, out cCurs);
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  ERROR: Asset  Layer(" + intersectLayerName + ") not found");
                                                    break;
                                                }




                                                IRow row;

                                                while ((row = cCurs.NextRow()) != null)
                                                {
                                                    string val = row.get_Value(intersectFieldPos).ToString();

                                                    if (inObject.Fields.get_Field(fieldNum).Type == esriFieldType.esriFieldTypeString)
                                                        inObject.set_Value(fieldNum, val);
                                                    else
                                                    {
                                                        if (Globals.IsNumeric(val))
                                                        {

                                                            inObject.set_Value(fieldNum, row.get_Value(intersectFieldPos));
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Field is a number and ID from Asset is not - ID:" + val);

                                                        }
                                                    }
                                                }


                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: LINK_TABLE_ASSET" + Environment.NewLine + ex.Message);
                                            }

                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: LINK_TABLE_ASSET");
                                            }
                                            break;

                                        case "GET_ADDRESS_USING_GEOCODER":
                                            {
                                                try
                                                {
                                                    AAState.WriteLine("                  Trying GET_ADDRESS_USING_GEOCODER");

                                                    if (!String.IsNullOrEmpty(valData))
                                                    {
                                                        args = valData.Split('|');
                                                        if (args.Length != 2)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                        break;
                                                    }
                                                    AAState.WriteLine("                  Trying to get address locator");
                                                    IReverseGeocoding reverseGeocoding = Globals.OpenLocator(args[0], args[1]);

                                                    if (reverseGeocoding == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Reverse Address not locator found");
                                                        break;
                                                    }
                                                    AAState.WriteLine("                  Reverse Address locator found");
                                                    AAState.WriteLine("                  Retrieving Location to reverse geocode");
                                                    IPoint revGCLoc = Globals.GetGeomCenter(inFeature);
                                                    AAState.WriteLine("                  Location retrieved");
                                                    // Create a Point at which to find the address.
                                                    IAddressGeocoding addressGeocoding = (IAddressGeocoding)reverseGeocoding;

                                                    IFields matchFields = addressGeocoding.MatchFields;
                                                    int shpFld = matchFields.FindField("Shape");

                                                    IField shapeField = matchFields.get_Field(shpFld);
                                                    AAState.WriteLine("                  Setting distance");
                                                    // Set the search tolerance for reverse geocoding.
                                                    IReverseGeocodingProperties reverseGeocodingProperties =
                                                        (IReverseGeocodingProperties)reverseGeocoding;
                                                    reverseGeocodingProperties.SearchDistance = 100;
                                                    reverseGeocodingProperties.SearchDistanceUnits = esriUnits.esriFeet;

                                                    // Find the address nearest the Point.
                                                    IPropertySet addressProperties = reverseGeocoding.ReverseGeocode(revGCLoc, false);

                                                    // Print the address properties.
                                                    IAddressInputs addressInputs = (IAddressInputs)reverseGeocoding;
                                                    IFields addressFields = addressInputs.AddressFields;
                                                    object key, value;
                                                    addressProperties.GetAllProperties(out key, out value);

                                                    //object[] keyArray = key as object[];
                                                    //object[] valueArray = value as object[];

                                                    //for (int i = 0; i < keyArray.Length; i++)
                                                    //{
                                                    //    if (keyArray[i].ToString() == "Shape")
                                                    //    {
                                                    //        IPoint addressPoint = valueArray[i] as IPoint;
                                                    //        Console.WriteLine(keyArray[i] + " = " + addressPoint.X + ", " +
                                                    //            addressPoint.Y);
                                                    //    }
                                                    //    else
                                                    //        Console.WriteLine(keyArray[i] + " = " + valueArray[i]);
                                                    //}
                                                    for (int i = 0; i < addressFields.FieldCount; i++)
                                                    {
                                                        IField addressField = addressFields.get_Field(i);
                                                        string tempVal = addressProperties.GetProperty(addressField.Name).ToString();

                                                        inFeature.set_Value(fieldNum, tempVal);

                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: GET_ADDRESS_USING_GEOCODER" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: GET_ADDRESS_USING_GEOCODER");
                                                }
                                                break;

                                            }
                                            break;

                                        case "GET_ADDRESS_USING_ARCGIS_SERVICE":  //ARGS: url
                                            try
                                            {
                                                AAState.WriteLine("                  Trying GET_ADDRESS_USING_ARCGIS_SERVICE");
                                                if (fieldObj.Type == esriFieldType.esriFieldTypeString)
                                                {
                                                    if ((inFeature != null) && (inFeature.Shape != null) && !(inFeature.Shape.IsEmpty))
                                                    {

                                                        IPoint revGCLoc = Globals.GetGeomCenter(inFeature);

                                                        if (revGCLoc != null)
                                                        {
                                                            //Test for user specified URL 
                                                            if (Globals.IsUrl(valData))
                                                            {
                                                                locatorURL = valData;
                                                                /////locatorURL.TrimEnd(char[] );
                                                                if (!(locatorURL.EndsWith(reverseGeocode)))
                                                                    locatorURL += reverseGeocode;
                                                            }

                                                            //If no valid user speficied URL, test for keywords for North America or European Union locators
                                                            else if (valData == "ESRI_Geocode_NA" || valData == "ESRI_Geocode_EU")
                                                                locatorURL = _agsOnlineLocators + valData + reverseGeocode;
                                                            else if (valData == "TA_Streets_US " || valData == "TA_Address_NA" || valData == "TA_Address_EU")
                                                                locatorURL = _agsOnlineLocators + valData + reverseGeocode;
                                                            //Default to AGS Online USA geocode service

                                                            else if (_agsOnlineLocators.Substring(_agsOnlineLocators.LastIndexOf('/', _agsOnlineLocators.Length - 2)).Contains("Locator"))
                                                                locatorURL = _agsOnlineLocators + "TA_Address_NA" + reverseGeocode;
                                                            else
                                                                locatorURL = _agsOnlineLocators + reverseGeocode;

                                                            //Copy point from this current feature 
                                                            _copyPoint = revGCLoc;//inFeature.ShapeCopy as IPoint;

                                                            //Project point to WGS84
                                                            _copyPoint.Project(AAState.srWGS84);

                                                            //Include location parameters in URL
                                                            string results = Globals.FormatLocationRequest(locatorURL, _copyPoint.X, _copyPoint.Y, 100);

                                                            //Send and receieve the request
                                                            WebRequest req = WebRequest.Create(results);
                                                            WebResponse res = req.GetResponse();

                                                            //Convert response from JSON to XML
                                                            XmlDocument doc = new XmlDocument();
                                                            using (Stream s = res.GetResponseStream())
                                                            {
                                                                XmlDictionaryReader xr = JsonReaderWriterFactory.CreateJsonReader(s, XmlDictionaryReaderQuotas.Max);
                                                                doc.Load(xr);
                                                                xr.Close();
                                                                s.Close();

                                                                //Store Address element from Address Results in the specified field
                                                                //if (mode == "ON_CREATE")
                                                                //{
                                                                //    inFeature.set_Value(fieldNum, doc.DocumentElement.FirstChild.FirstChild.InnerText.Replace("    ", " "));
                                                                //}
                                                                //else//on change
                                                                //{
                                                                //    if (inFeature.Fields.get_Field(fieldNum).DefaultValue == inFeature.get_Value(fieldNum))
                                                                //    //if (inFeature.get_Value(fieldNum).ToString() != "")
                                                                //    {
                                                                //        inFeature.set_Value(fieldNum, doc.DocumentElement.FirstChild.FirstChild.InnerText.Replace("    ", " "));
                                                                //    }
                                                                //}
                                                                inFeature.set_Value(fieldNum, doc.DocumentElement.FirstChild.FirstChild.InnerText.Replace("    ", " "));

                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  Could not get location to Reverse Geocode");
                                                        }
                                                    }

                                                }

                                            }
                                            catch
                                            {
                                                AAState.WriteLine("                  ERROR: GET_ADDRESS_USING_ARCGIS_SERVICE");
                                            }

                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: GET_ADDRESS_USING_ARCGIS_SERVICE");
                                            }
                                            break;
                                        case "TIMESTAMP":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: TIMESTAMP");
                                                if (fieldObj.Type == esriFieldType.esriFieldTypeDate)
                                                    inObject.set_Value(fieldNum, DateTime.Now);
                                                else if (fieldObj.Type == esriFieldType.esriFieldTypeString)
                                                    inObject.set_Value(fieldNum, DateTime.Now.ToString());


                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TIMESTAMP " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TIMESTAMP");
                                            }
                                            break;

                                        case "LAST_VALUE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: LAST_VALUE");
                                                if (mode == "ON_CREATE")
                                                {

                                                    lastValue = AAState.lastValueProperties.GetProperty(fieldName);
                                                    if (lastValue != null)
                                                    {
                                                        inObject.set_Value(fieldNum, lastValue);
                                                        AAState.WriteLine("                  " + fieldName + ": " + lastValue);
                                                    }
                                                }
                                                else if (mode == "ON_CHANGE" && inObject.get_Value(fieldNum) == null)
                                                {
                                                    lastValue = AAState.lastValueProperties.GetProperty(fieldName);
                                                    if (lastValue != null)
                                                    {
                                                        inObject.set_Value(fieldNum, lastValue);
                                                        AAState.WriteLine("                  " + fieldName + ": " + lastValue);
                                                    }
                                                }


                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: LAST_VALUE " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: LAST_VALUE");
                                            }
                                            break;

                                        case "X_COORDINATE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: X_COORDINATE");
                                                if ((inFeature != null) && (inFeature.Shape != null) && !(inFeature.Shape.IsEmpty))
                                                {
                                                    if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                    {
                                                        _copyPoint = inFeature.Shape as IPoint;
                                                        inFeature.set_Value(fieldNum, _copyPoint.X);
                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                    {
                                                        _copyPolyline = inFeature.Shape as IPolyline;
                                                        if (valData == "")
                                                        {
                                                            inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolyline).X);
                                                        }
                                                        else
                                                        {
                                                            args = valData.Split('|');
                                                            if (args[0].ToUpper() == "S")
                                                            {
                                                                inFeature.set_Value(fieldNum, _copyPolyline.FromPoint.X);
                                                            }
                                                            else if (args[0].ToUpper() == "E")
                                                            {
                                                                inFeature.set_Value(fieldNum, _copyPolyline.ToPoint.X);
                                                            }
                                                            else
                                                            {
                                                                inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolyline).X);
                                                            }

                                                        }


                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon)
                                                    {
                                                        inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolygon).X);
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Unsupported Geometry");
                                                    }
                                                }


                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: X_COORDINATE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: X_COORDINATE");
                                            }
                                            break;

                                        case "Y_COORDINATE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: Y_COORDINATE");
                                                if ((inFeature != null) && (inFeature.Shape != null) && !(inFeature.Shape.IsEmpty))
                                                {
                                                    if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                    {
                                                        _copyPoint = inFeature.Shape as IPoint;
                                                        inFeature.set_Value(fieldNum, _copyPoint.Y);
                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                    {
                                                        _copyPolyline = inFeature.Shape as IPolyline;
                                                        if (valData == "")
                                                        {
                                                            inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolyline).Y);
                                                        }
                                                        else
                                                        {
                                                            args = valData.Split('|');
                                                            if (args[0].ToUpper() == "S")
                                                            {
                                                                inFeature.set_Value(fieldNum, _copyPolyline.FromPoint.Y);
                                                            }
                                                            else if (args[0].ToUpper() == "E")
                                                            {
                                                                inFeature.set_Value(fieldNum, _copyPolyline.ToPoint.Y);
                                                            }
                                                            else
                                                            {
                                                                inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolyline).Y);
                                                            }

                                                        }


                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon)
                                                    {
                                                        _copyPolygon = inFeature.ShapeCopy as IPolygon;

                                                        inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolygon).Y);
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Unsupported Geometry");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: Y_COORDINATE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: Y_COORDINATE");
                                            }
                                            break;

                                        case "LATITUDE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: LATITUDE");
                                                if ((inFeature != null) && (inFeature.Shape != null) && !(inFeature.Shape.IsEmpty))
                                                {

                                                    if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                    {
                                                        _copyPoint = inFeature.ShapeCopy as IPoint;
                                                        _copyPoint.Project(AAState._sr1);

                                                        inFeature.set_Value(fieldNum, _copyPoint.Y);
                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                    {
                                                        _copyPolyline = inFeature.ShapeCopy as IPolyline;
                                                        _copyPolyline.Project(AAState._sr1);

                                                        if (valData == "")
                                                        {
                                                            inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolyline).X);
                                                        }
                                                        else
                                                        {
                                                            args = valData.Split('|');
                                                            if (args[0].ToUpper() == "S")
                                                            {
                                                                inFeature.set_Value(fieldNum, _copyPolyline.FromPoint.X);
                                                            }
                                                            else if (args[0].ToUpper() == "E")
                                                            {
                                                                inFeature.set_Value(fieldNum, _copyPolyline.ToPoint.X);
                                                            }
                                                            else
                                                            {
                                                                inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolyline).X);
                                                            }

                                                        }


                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon)
                                                    {
                                                        _copyPolygon = inFeature.ShapeCopy as IPolygon;
                                                        _copyPolygon.Project(AAState._sr1);

                                                        inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolygon).X);
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Unsupported Geometry");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  Error: LATITUDE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: LATITUDE");
                                            }
                                            break;

                                        case "LONGITUDE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: LONGITUDE");
                                                if ((inFeature != null) && (inFeature.Shape != null) && !(inFeature.Shape.IsEmpty))
                                                {

                                                    if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                    {
                                                        _copyPoint = inFeature.ShapeCopy as IPoint;
                                                        _copyPoint.Project(AAState._sr1);

                                                        inFeature.set_Value(fieldNum, _copyPoint.Y);
                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                    {
                                                        _copyPolyline = inFeature.Shape as IPolyline;
                                                        _copyPolyline.Project(AAState._sr1);

                                                        if (valData == "")
                                                        {
                                                            inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolyline).Y);
                                                        }
                                                        else
                                                        {
                                                            args = valData.Split('|');
                                                            if (args[0].ToUpper() == "S")
                                                            {
                                                                inFeature.set_Value(fieldNum, _copyPolyline.FromPoint.Y);
                                                            }
                                                            else if (args[0].ToUpper() == "E")
                                                            {
                                                                inFeature.set_Value(fieldNum, _copyPolyline.ToPoint.Y);
                                                            }
                                                            else
                                                            {
                                                                inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolyline).Y);
                                                            }

                                                        }


                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon)
                                                    {
                                                        _copyPolygon = inFeature.ShapeCopy as IPolygon;
                                                        _copyPolygon.Project(AAState._sr1);

                                                        inFeature.set_Value(fieldNum, Globals.GetGeomCenter(_copyPolygon).Y);
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Unsupported Geometry");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: LONGITUDE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: LONGITUDE");
                                            }
                                            break;

                                        case "FIELD":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: Field");
                                                // verify that field to copy exists
                                                fieldCopy = inObject.Fields.FindField(valData as string);
                                                if (fieldCopy > -1)
                                                {
                                                    if (mode == "ON_CREATE")
                                                    {
                                                        inObject.set_Value(fieldNum, inObject.get_Value(fieldCopy));
                                                    }
                                                    if (mode == "ON_CHANGE")
                                                    {
                                                        //copy value only if current field is empty
                                                        if (inObject.get_Value(fieldNum).ToString() == "")
                                                            inObject.set_Value(fieldNum, inObject.get_Value(fieldCopy));
                                                    }
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  " + valData + " is not found");
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: Field: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: Field");
                                            }
                                            break;

                                        //CURRENT_USER
                                        //Value Data options:
                                        //U - windows username only 
                                        //W or (blank) - full username including domain i.e. domain\username
                                        //D - database user if available and not dbo
                                        case "CURRENT_USER":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: CURRENT_USER");
                                                lastEditorName = AAState._currentUserInfo.GetCurrentUser(valData, fieldObj.Length);

                                                if (!String.IsNullOrEmpty(lastEditorName))
                                                {
                                                    inObject.set_Value(fieldNum, lastEditorName);
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: CURRENT_USER: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: CURRENT_USER");
                                            }
                                            break;

                                        case "JUNCTION_ROTATION":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: JUNCTION_ROTATION");
                                                if ((inFeature != null))
                                                {
                                                    AAState.rCalc.RotationType = esriSymbolRotationType.esriRotateSymbolGeographic;
                                                    args = null;

                                                    if (!String.IsNullOrEmpty(valData))
                                                    {
                                                        args = valData.Split('|');
                                                        if (args[0].Length == 0)
                                                        {
                                                            if (args[0].Substring(0, 1).ToLower() == "a")
                                                                AAState.rCalc.RotationType = esriSymbolRotationType.esriRotateSymbolArithmetic;
                                                        }
                                                        if (args[0].Length == 1)
                                                        {
                                                            if (args[0].Substring(0, 1).ToLower() == "a")
                                                                AAState.rCalc.RotationType = esriSymbolRotationType.esriRotateSymbolArithmetic;
                                                            if (Globals.IsNumeric(args[1].ToString()))
                                                                AAState.rCalc.SpinAngle = Convert.ToDouble(args[1]);

                                                        }
                                                    }
                                                    AAState.WriteLine("                  " + AAState.rCalc.RotationType.ToString() + " is being used");
                                                    rotationAngle = AAState.rCalc.GetRotationUsingConnectedEdges(inFeature);
                                                    if (rotationAngle == null)
                                                    {
                                                        AAState.WriteLine("                  Rotation Angle not found or errored out");
                                                        continue;
                                                    }

                                                    //Accept optional second argument to provide extra rotation
                                                    //if ((args != null) && (args.Length > 1) && (Globals.IsInteger(args[1])))
                                                    //    rotationAngle += System.Convert.ToInt32(args[1]);
                                                    if (rotationAngle != -1)
                                                        inObject.set_Value(fieldNum, rotationAngle);
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: JUNCTION_ROTATION \r\n" + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: JUNCTION_ROTATION");
                                            }
                                            break;

                                        //For Release: 1.2
                                        //New Dynamic Value Method: Length - stores calculated length of line feature
                                        case "LENGTH":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: LENGTH");
                                                if (inFeature != null)
                                                {
                                                    curve = (ICurve)inFeature.Shape;
                                                    if (curve != null)
                                                    {
                                                        inObject.set_Value(fieldNum, curve.Length);
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: LENGTH: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: LENGTH");
                                            }
                                            break;

                                        //Release: 1.2
                                        //New Dynamic Value Method: SET_MEASURES - stores calculated M values (from 0 to length of line) for line feature 
                                        //Value Data options:
                                        //P = Percent - Ms will be zero to 100
                                        //default - Ms will be zero to length of line
                                        case "SET_MEASURES":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: SET_MEASURES");
                                                if (inFeature != null)
                                                {
                                                    curve = inFeature.Shape as ICurve;
                                                    mseg = inFeature.Shape as IMSegmentation;
                                                    if (curve != null && mseg != null)
                                                        if (valData != null && valData != "" && valData.Substring(0, 1).ToUpper() == "P")
                                                            mseg.SetAndInterpolateMsBetween(0, 100);
                                                        else
                                                            mseg.SetAndInterpolateMsBetween(0, curve.Length);
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: SET_MEASURES: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: SET_MEASURES");
                                            }
                                            break;
                                        //case "EDGE_INTERSECT_SECOND":
                                        //    try
                                        //    {
                                        //        if (inFeature != null)
                                        //        {
                                        //            netFeat = inFeature as INetworkFeature;
                                        //            if (netFeat != null)
                                        //            {
                                        //                if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                        //                {

                                        //                    iJuncFeat = (IJunctionFeature)netFeat;
                                        //                    // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                        //                    ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                        //                    if (iSJunc == null)
                                        //                        break;
                                        //                    if (iSJunc.EdgeFeatureCount <= 1)
                                        //                        break;
                                        //                    edgeFeat = iSJunc.get_EdgeFeature(1);


                                        //                    // verify that field (in junction) to copy exists

                                        //                    IRow pRow = edgeFeat as IRow;

                                        //                    juncField = pRow.Fields.FindField(valData as string);
                                        //                    if (juncField > -1)
                                        //                    {
                                        //                        inObject.set_Value(fieldNum, pRow.get_Value(juncField));
                                        //                    }
                                        //                }
                                        //            }
                                        //        }
                                        //    }
                                        //    catch
                                        //    {
                                        //    }
                                        //    break;
                                        //case "EDGE_INTERSECT_FIRST":
                                        //    try
                                        //    {
                                        //        if (inFeature != null)
                                        //        {
                                        //            netFeat = inFeature as INetworkFeature;
                                        //            if (netFeat != null)
                                        //            {
                                        //                if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                        //                {

                                        //                    iJuncFeat = (IJunctionFeature)netFeat;
                                        //                    // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                        //                    ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                        //                    if (iSJunc == null)
                                        //                        break;
                                        //                    if (iSJunc.EdgeFeatureCount <= 0)
                                        //                        break;
                                        //                    edgeFeat = iSJunc.get_EdgeFeature(0);


                                        //                    IRow pRow = edgeFeat as IRow;

                                        //                    juncField = pRow.Fields.FindField(valData as string);
                                        //                    if (juncField > -1)
                                        //                    {
                                        //                        inObject.set_Value(fieldNum, pRow.get_Value(juncField));
                                        //                    }
                                        //                }
                                        //            }
                                        //        }
                                        //    }
                                        //    catch
                                        //    {
                                        //    }
                                        //    break;


                                        //Release: 2.0
                                        //New Dynamic Value Method: TO_EDGE_FIELD transfers a field value from a connected egde feature to a junction feature
                                        //Takes value from the frist edge whose "TO" point connects with this junction
                                        case "TO_EDGE_FIELD":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: TO_EDGE_FIELD");
                                                if (inFeature != null)
                                                {
                                                    netFeat = inFeature as INetworkFeature;
                                                    if (netFeat != null)
                                                    {
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                        {

                                                            iJuncFeat = (IJunctionFeature)netFeat;
                                                            // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                            ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                            if (iSJunc == null)
                                                                break;
                                                            if (iSJunc.EdgeFeatureCount <= 0)
                                                                break;
                                                            if (iSJunc.EdgeFeatureCount > 0)
                                                            {
                                                                for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                {
                                                                    edgeFeat = iSJunc.get_EdgeFeature(i);
                                                                    try
                                                                    {
                                                                        juncFeat = (IFeature)edgeFeat.FromJunctionFeature;
                                                                        if (juncFeat.Shape.Equals(inFeature.Shape))
                                                                        {
                                                                            IRow pRow = edgeFeat as IRow;

                                                                            // verify that field (in junction) to copy exists
                                                                            juncField = pRow.Fields.FindField(valData as string);
                                                                            if (juncField > -1)
                                                                            {
                                                                                inObject.set_Value(fieldNum, pRow.get_Value(juncField));
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  " + valData + " field not found in edge");
                                                                            }
                                                                            break;


                                                                        }
                                                                    }
                                                                    catch
                                                                    {
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  No Connected Edges Found");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an junction feature");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  Not a Geometric Network Feature");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TO_EDGE_FIELD: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TO_EDGE_FIELD");
                                            }
                                            break;

                                        //Release: 2.0
                                        //New Dynamic Value Method: FROM_EDGE_FIELD transfers a field value from a connected egde feature to a junction feature
                                        //Takes value from the frist edge whose "FROM" point connects with this junction
                                        case "FROM_EDGE_FIELD":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: FROM_EDGE_FIELD");
                                                if (inFeature != null)
                                                {
                                                    netFeat = inFeature as INetworkFeature;
                                                    if (netFeat != null)
                                                    {
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                        {

                                                            iJuncFeat = (IJunctionFeature)netFeat;
                                                            // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                            ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                            if (iSJunc == null)
                                                                break;
                                                            if (iSJunc.EdgeFeatureCount <= 0)
                                                                break;
                                                            for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                            {
                                                                edgeFeat = iSJunc.get_EdgeFeature(i);
                                                                try
                                                                {
                                                                    juncFeat = (IFeature)edgeFeat.ToJunctionFeature;
                                                                    if (juncFeat.Shape.Equals(inFeature.Shape))
                                                                    {
                                                                        IRow pRow = edgeFeat as IRow;

                                                                        // verify that field (in junction) to copy exists
                                                                        juncField = pRow.Fields.FindField(valData as string);
                                                                        if (juncField > -1)
                                                                        {
                                                                            inObject.set_Value(fieldNum, pRow.get_Value(juncField));
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  " + valData + " field not found");
                                                                        }
                                                                        break;


                                                                    }

                                                                }

                                                                catch
                                                                {
                                                                    AAState.WriteLine("                  error ");
                                                                }
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an junction feature");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  Not a Geometric Network Feature");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: FROM_EDGE_FIELD: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: FROM_EDGE_FIELD");
                                            }
                                            break;

                                        case "FROM_JUNCTION_FIELD":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: FROM_JUNCTION_FIELD");
                                                if (inFeature != null)
                                                {
                                                    netFeat = inFeature as INetworkFeature;
                                                    if (netFeat != null)
                                                    {
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexEdge || inFeature.FeatureType == esriFeatureType.esriFTSimpleEdge)
                                                        {
                                                            edgeFeat = (IEdgeFeature)netFeat;
                                                            juncFeat = (IFeature)edgeFeat.FromJunctionFeature;

                                                            // verify that field (in junction) to copy exists
                                                            juncField = juncFeat.Fields.FindField(valData as string);
                                                            if (juncField > -1)
                                                            {
                                                                inObject.set_Value(fieldNum, juncFeat.get_Value(juncField));
                                                            }
                                                            else
                                                                AAState.WriteLine("                  " + valData + " field not found");
                                                        }
                                                        else
                                                            AAState.WriteLine("                  not an edge feature");
                                                    }
                                                    else
                                                        AAState.WriteLine("                  Not a network feature");
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: FROM_JUNCTION_FIELD: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: FROM_JUNCTION_FIELD");
                                            }
                                            break;


                                        //Release: 1.2
                                        //New Dynamic Value Method: TO_JUNCTION_FIELD transfers a field value from a junction connected at terminal end of a line feature
                                        case "TO_JUNCTION_FIELD":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: TO_JUNCTION_FIELD");
                                                if (inFeature != null)
                                                {

                                                    netFeat = inFeature as INetworkFeature;
                                                    if (netFeat != null)
                                                    {
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexEdge || inFeature.FeatureType == esriFeatureType.esriFTSimpleEdge)
                                                        {
                                                            edgeFeat = (IEdgeFeature)netFeat;
                                                            juncFeat = (IFeature)edgeFeat.ToJunctionFeature;

                                                            // verify that field (in junction) to copy exists
                                                            juncField = juncFeat.Fields.FindField(valData as string);
                                                            if (juncField > -1)
                                                            {
                                                                inObject.set_Value(fieldNum, juncFeat.get_Value(juncField));
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  Trying: TO_JUNCTION_FIELD");
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  not an edge feature");
                                                    }
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  not an geometric network feature");
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TO_JUNCTION_FIELD:" + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TO_JUNCTION_FIELD");
                                            }
                                            break;

                                        //Release: 1.2
                                        //New Dynamic Value Method: GENERATE_ID - uses value in specificed table and increments it as specified
                                        case "GENERATE_ID":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: GENERATE_ID");
                                                if (AAState._gentab != null)
                                                {
                                                    sequenceColumnName = "";
                                                    sequenceColumnNum = -1;
                                                    sequenceValue = -1;
                                                    sequenceFixedWidth = "";
                                                    sequencePadding = 0;
                                                    formatString = "";

                                                    // Parse arguments
                                                    if (valData == null) break;
                                                    args = valData.Split('|');
                                                    switch (args.GetLength(0))
                                                    {
                                                        case 1:  // sequenceColumnName only
                                                            sequenceColumnName = args[0].ToString(); break;
                                                        case 2:  // sequenceColumnName|sequenceFixedWidth
                                                            sequenceColumnName = args[0].ToString();
                                                            sequenceFixedWidth = args[1].ToString();
                                                            break;
                                                        case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                            sequenceColumnName = args[0].ToString();
                                                            sequenceFixedWidth = args[1].ToString();
                                                            formatString = args[2].ToString();
                                                            break;
                                                        default: break;
                                                    }

                                                    //Check for requested zero padding of sequence number
                                                    if (sequenceFixedWidth != "")
                                                        int.TryParse(sequenceFixedWidth.ToString(), out sequencePadding);
                                                    if (sequencePadding > 25)
                                                    {

                                                        AAState.WriteLine("                  WARNING: you are trying to pad your id with a more than 25 - 0's");
                                                        AAState.WriteLine("                  WARNING: " + sequencePadding + " 0's is what you have");

                                                    }
                                                    else if (sequencePadding > 50)
                                                    {
                                                        MessageBox.Show("You are trying to add 50 places to your ID, this is not supported, please fix your dynamic value table");
                                                    }

                                                    //Check for sequence column in generate id table
                                                    sequenceColumnNum = AAState._gentab.FindField(sequenceColumnName);
                                                    if (AAState._gentab.FindField(sequenceColumnName) >= 0)
                                                    {
                                                        //get value of first row, increment it, and return incremented value
                                                        for (int j = 0; j < 51; j++)
                                                        {
                                                            row = AAState._gentab.Update(null, false).NextRow();
                                                            if (row == null)
                                                            {
                                                                break;
                                                            }
                                                            if (row.get_Value(sequenceColumnNum) == null)
                                                            {
                                                                sequenceValue = 0;
                                                            }
                                                            else if (row.get_Value(sequenceColumnNum).ToString() == "")
                                                            {
                                                                sequenceValue = 0;
                                                            }
                                                            else
                                                                sequenceValue = Convert.ToInt32(row.get_Value(sequenceColumnNum));

                                                            sequenceValue += 1;
                                                            // _editEvents.OnChangeFeature -= OnChangeFeature;
                                                            // _editEvents.OnCreateFeature -= OnCreateFeature;

                                                            row.set_Value(sequenceColumnNum, sequenceValue);
                                                            AAState.WriteLine("                  " + row.Fields.get_Field(sequenceColumnNum).AliasName + " changed to " + sequenceValue);

                                                            row.Store();
                                                            //  _editEvents.OnChangeFeature += OnChangeFeature;
                                                            //  _editEvents.OnCreateFeature += OnCreateFeature;
                                                            if (Convert.ToInt32(row.get_Value(sequenceColumnNum)) == sequenceValue)
                                                                break;

                                                        }
                                                        if (sequenceValue == -1)
                                                        {
                                                            AAState.WriteLine("                  ERROR: GENERATE_ID: Sequence Not Found");
                                                        }

                                                        else
                                                        {
                                                            if (inObject.Fields.get_Field(fieldNum).Type == esriFieldType.esriFieldTypeString)
                                                                if (formatString == null || formatString == "" || formatString.ToLower().IndexOf("[seq]") == -1)
                                                                {
                                                                    string setVal = (sequenceValue.ToString("D" + sequencePadding) + sequencePostfix).ToString();

                                                                    if (inObject.Fields.get_Field(fieldNum).Length < setVal.Length && inObject.Fields.get_Field(fieldNum).Length != 0)
                                                                    {
                                                                        AAState.WriteLine("                  ERROR: " + sequenceValue + " is to long for field " + row.Fields.get_Field(sequenceColumnNum).AliasName);
                                                                    }
                                                                    else
                                                                    {
                                                                        inObject.set_Value(fieldNum, sequenceValue.ToString("D" + sequencePadding) + sequencePostfix);
                                                                        AAState.WriteLine("                  " + inObject.Fields.get_Field(fieldNum).AliasName + " set to " + sequenceValue.ToString("D" + sequencePadding) + sequencePostfix);

                                                                    }



                                                                }
                                                                else
                                                                {


                                                                    int locIdx = formatString.ToUpper().IndexOf("[SEQ]");
                                                                    if (locIdx >= 0)
                                                                    {
                                                                        formatString = formatString.Remove(locIdx, 5);
                                                                        formatString = formatString.Insert(locIdx, sequenceValue.ToString("D" + sequencePadding));
                                                                    }
                                                                    //formatString = formatString.Replace("[seq]", sequenceValue.ToString("D" + sequencePadding));


                                                                    if (inObject.Fields.get_Field(fieldNum).Length < formatString.Length && inObject.Fields.get_Field(fieldNum).Length != 0)
                                                                    {
                                                                        AAState.WriteLine("                  ERROR: " + formatString + " is to long for field " + row.Fields.get_Field(sequenceColumnNum).AliasName);
                                                                    }
                                                                    else
                                                                    {
                                                                        inObject.set_Value(fieldNum, formatString);
                                                                        AAState.WriteLine("                  " + inObject.Fields.get_Field(fieldNum).AliasName + " set to " + formatString);
                                                                    }


                                                                }
                                                            else
                                                            {

                                                                inObject.set_Value(fieldNum, sequenceValue);
                                                                AAState.WriteLine("                  " + sequenceColumnNum + " changed to " + sequenceValue);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  Sequence Field not found");
                                                    }
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  ERROR: GENERATE_ID table is not found");

                                                }

                                            }


                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: GENERATE_ID: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: GENERATE_ID");
                                            }
                                            break;

                                        case "GENERATE_ID_BY_INTERSECT":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: GENERATE_ID_BY_INTERSECT");
                                                if (AAState._gentab != null && inFeature != null && !(inFeature.Shape.IsEmpty))
                                                {
                                                    sequenceColumnName = "";
                                                    sequenceColumnNum = -1;
                                                    sequenceValue = -1;
                                                    sequenceFixedWidth = "";
                                                    sequencePadding = 0;
                                                    //genIdAreaFieldName = "";
                                                    intersectLayerName = "";
                                                    intersectLayerFieldName = "";
                                                    formatString = "";
                                                    intersectFieldPos = -1;

                                                    // Parse arguments
                                                    if (valData == null) break;

                                                    args = valData.Split('|');
                                                    if (args.GetLength(0) < 3)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Improper value method");
                                                        break;
                                                    }
                                                    switch (args.GetLength(0))
                                                    {
                                                        case 3:  //columnName
                                                            intersectLayerName = args[0].ToString();
                                                            intersectLayerFieldName = args[1].ToString();
                                                            // genIdAreaFieldName = args[2].ToString();
                                                            sequenceColumnName = args[2].ToString();
                                                            break;
                                                        case 4:  // columnName|sequenceFixedWidth 
                                                            //sequenceFixedWidth formats the sequence with leading zeros to create specified width
                                                            intersectLayerName = args[0].ToString();
                                                            intersectLayerFieldName = args[1].ToString();
                                                            //genIdAreaFieldName = args[2].ToString();
                                                            sequenceColumnName = args[2].ToString();
                                                            sequenceFixedWidth = Convert.ToString(0);
                                                            formatString = args[3].ToString();

                                                            break;
                                                        case 5:  // columnName|sequenceFixedWidth|formatString 
                                                            //formatString must contain [seq] and [id]  and may contain [area] plus any desired text
                                                            intersectLayerName = args[0].ToString();
                                                            intersectLayerFieldName = args[1].ToString();
                                                            //genIdAreaFieldName = args[2].ToString();
                                                            sequenceColumnName = args[2].ToString();
                                                            sequenceFixedWidth = args[3].ToString();
                                                            formatString = args[4].ToString();
                                                            break;
                                                        default: break;
                                                    }

                                                    //Find Area Layer
                                                    // FindLayerByName(areaLayerName, out areaLayer);
                                                    intersectLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, intersectLayerName);
                                                    if (intersectLayer == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Intersecting feature Layer(" + intersectLayerName + ") not found");
                                                        break;
                                                    }
                                                    //Find Area Field
                                                    intersectFieldPos = intersectLayer.FeatureClass.FindField(intersectLayerFieldName);
                                                    if (intersectFieldPos < 0)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Intersecting feature Layer Field(" + intersectLayerFieldName + ") not found");
                                                        break;
                                                    }

                                                    //Find GenID Area Field
                                                    //genIdAreaFieldPos = areaLayer.FeatureClass.FindField(genIdAreaFieldName);
                                                    //if (genIdAreaFieldPos < 0) break;

                                                    //Perform spatial search 
                                                    IGeometry pSearchGeo = (IGeometry)inFeature.ShapeCopy;
                                                    pSearchGeo.SpatialReference = (inFeature.Class as IGeoDataset).SpatialReference;
                                                    pSearchGeo.Project((intersectLayer as IGeoDataset).SpatialReference);
                                                    sFilter = new SpatialFilterClass();
                                                    sFilter.Geometry = pSearchGeo;
                                                    sFilter.GeometryField = intersectLayer.FeatureClass.ShapeFieldName;
                                                    sFilter.SubFields = intersectLayerFieldName;
                                                    sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                                                    fCursor = intersectLayer.FeatureClass.Search(sFilter, true);
                                                    sourceFeature = fCursor.NextFeature();
                                                    if (sourceFeature != null)
                                                        intersectValue = sourceFeature.get_Value(intersectFieldPos).ToString();
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Intersecting feature not found");
                                                        break;
                                                    }
                                                    //Check for requested zero padding of sequence number
                                                    if (sequenceFixedWidth != "")
                                                        int.TryParse(sequenceFixedWidth.ToString(), out sequencePadding);

                                                    if (sequencePadding > 25)
                                                    {
                                                        AAState.WriteLine("                  WARNING: you are trying to pad your id with a more than 25 - 0's");
                                                        AAState.WriteLine("                  WARNING: " + sequencePadding + " 0's is what you have");

                                                    }

                                                    sequenceColumnName = sequenceColumnName + intersectValue;
                                                    AAState.WriteLine("                  Looking for a field called " + sequenceColumnName + " in the generate ID table");
                                                    sequenceColumnNum = AAState._gentab.FindField(sequenceColumnName);
                                                    if (sequenceColumnNum > -1)
                                                    {
                                                        AAState.WriteLine("                  Field Found");

                                                        //get value of first row, increment it, and return incremented value
                                                        for (int j = 0; j < 51; j++)
                                                        {
                                                            row = AAState._gentab.Update(qFilter, false).NextRow();
                                                            if (row.get_Value(sequenceColumnNum) == null)
                                                            {
                                                                sequenceValue = 0;
                                                            }
                                                            else if (row.get_Value(sequenceColumnNum).ToString() == "")
                                                            {
                                                                sequenceValue = 0;
                                                            }
                                                            else
                                                                sequenceValue = Convert.ToInt32(row.get_Value(sequenceColumnNum));
                                                            sequenceValue += 1;
                                                            row.set_Value(sequenceColumnNum, sequenceValue);
                                                            row.Store();
                                                            if (Convert.ToInt32(row.get_Value(sequenceColumnNum)) == sequenceValue)
                                                                break;

                                                        }
                                                        if (sequenceValue == -1)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Sequence number not found");
                                                        }
                                                        else
                                                        {
                                                            if (inObject.Fields.get_Field(fieldNum).Type == esriFieldType.esriFieldTypeString)
                                                                if (formatString == null || formatString == "" || (formatString.ToUpper().IndexOf("[SEQ]") == -1 && formatString.ToUpper().IndexOf("[ID]") == -1))
                                                                    inObject.set_Value(fieldNum, intersectValue + sequenceValue.ToString("D" + sequencePadding) + sequencePostfix);
                                                                else
                                                                {

                                                                    int locIdx = formatString.ToUpper().IndexOf("[ID]");
                                                                    if (locIdx >= 0)
                                                                    {
                                                                        formatString = formatString.Remove(locIdx, 4);
                                                                        formatString = formatString.Insert(locIdx, intersectValue);
                                                                    }
                                                                    //  formatString = formatString.Replace("[id]", intersectValue);
                                                                    // formatString = formatString.Replace("[seq]", sequenceValue.ToString("D" + sequencePadding));
                                                                    locIdx = formatString.ToUpper().IndexOf("[SEQ]");
                                                                    if (locIdx >= 0)
                                                                    {
                                                                        formatString = formatString.Remove(locIdx, 5);
                                                                        formatString = formatString.Insert(locIdx, sequenceValue.ToString());
                                                                    }
                                                                    //
                                                                    inObject.set_Value(fieldNum, formatString);
                                                                }
                                                            else
                                                                inObject.set_Value(fieldNum, sequenceValue);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Field NOT Found");
                                                    }
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  ERROR: GENERATE_ID table is not found");

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: GENERATE_ID_BY_INTERSECT: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: GENERATE_ID_BY_INTERSECT");
                                            }
                                            break;
                                        //Modified for Release 1.2  (No longer uses ICalculator)
                                        //Requires valid VBScript expression
                                        //Can include string, numeric, and date fields by name in square brackets [] 
                                        //Example: DateDiff("yyyy",[INSTALLDATE],Now()) 
                                        case "GENERATE_ID_BY_AREA":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: GENERATE_ID_BY_AREA");
                                                if (AAState._gentab != null && inFeature != null && !(inFeature.Shape.IsEmpty))
                                                {
                                                    sequenceColumnName = "";
                                                    sequenceColumnNum = -1;
                                                    sequenceValue = -1;
                                                    sequenceFixedWidth = "";
                                                    sequencePadding = 0;
                                                    genIdAreaFieldName = "";
                                                    areaLayerName = "";
                                                    areaLayerFieldName = "";
                                                    formatString = "";

                                                    // Parse arguments
                                                    if (valData == null) break;

                                                    args = valData.Split('|');
                                                    if (args.GetLength(0) < 3)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Improper Value Method");
                                                        break;
                                                    }
                                                    switch (args.GetLength(0))
                                                    {
                                                        case 4:  //columnName
                                                            areaLayerName = args[0].ToString();
                                                            areaLayerFieldName = args[1].ToString();
                                                            genIdAreaFieldName = args[2].ToString();
                                                            sequenceColumnName = args[3].ToString();
                                                            break;
                                                        case 5:  // columnName|sequenceFixedWidth 
                                                            //sequenceFixedWidth formats the sequence with leading zeros to create specified width
                                                            areaLayerName = args[0].ToString();
                                                            areaLayerFieldName = args[1].ToString();
                                                            genIdAreaFieldName = args[2].ToString();
                                                            sequenceColumnName = args[3].ToString();
                                                            sequenceFixedWidth = args[4].ToString();
                                                            break;
                                                        case 6:  // columnName|sequenceFixedWidth|formatString 
                                                            //formatString must contain [seq] and may contain [area] plus any desired text
                                                            areaLayerName = args[0].ToString();
                                                            areaLayerFieldName = args[1].ToString();
                                                            genIdAreaFieldName = args[2].ToString();
                                                            sequenceColumnName = args[3].ToString();
                                                            sequenceFixedWidth = args[4].ToString();
                                                            formatString = args[5].ToString();
                                                            break;
                                                        default: break;
                                                    }

                                                    //Find Area Layer
                                                    // FindLayerByName(areaLayerName, out areaLayer);
                                                    areaLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, areaLayerName);
                                                    if (areaLayer == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Area Layer not found: " + areaLayerName);
                                                        break;
                                                    }

                                                    //Find Area Field
                                                    areaFieldPos = areaLayer.FeatureClass.FindField(areaLayerFieldName);
                                                    if (areaFieldPos < 0)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Area Layer field not found: " + areaLayerFieldName);
                                                        break;
                                                    }

                                                    //Find GenID Area Field
                                                    //genIdAreaFieldPos = areaLayer.FeatureClass.FindField(genIdAreaFieldName);
                                                    //if (genIdAreaFieldPos < 0) break;

                                                    //Perform spatial search 
                                                    sFilter = new SpatialFilterClass();
                                                    sFilter.Geometry = (IGeometry)inFeature.ShapeCopy;
                                                    sFilter.GeometryField = areaLayer.FeatureClass.ShapeFieldName;
                                                    sFilter.SubFields = areaLayerFieldName;
                                                    sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                                                    fCursor = areaLayer.FeatureClass.Search(sFilter, true);
                                                    sourceFeature = fCursor.NextFeature();
                                                    if (sourceFeature != null)
                                                        areaValue = sourceFeature.get_Value(areaFieldPos).ToString();
                                                    else
                                                        break;

                                                    //Check for requested zero padding of sequence number
                                                    if (sequenceFixedWidth != "")
                                                        int.TryParse(sequenceFixedWidth.ToString(), out sequencePadding);


                                                    sequenceColumnNum = AAState._gentab.FindField(sequenceColumnName);
                                                    if (sequenceColumnNum > -1)
                                                    {
                                                        qFilter = new QueryFilterClass();
                                                        qFilter.SubFields = areaLayerFieldName + "," + sequenceColumnName;

                                                        ISQLSyntax sqlSyntax = (ISQLSyntax)AAState._editor.EditWorkspace;
                                                        char[] charBuf = sqlSyntax.GetSpecialCharacter(esriSQLSpecialCharacters.esriSQL_DelimitedIdentifierPrefix).ToCharArray();
                                                        string strSC = new string(charBuf);
                                                        switch (strSC)
                                                        {
                                                            case "":
                                                                qFilter.WhereClause = genIdAreaFieldName + " = '" + areaValue + "'";
                                                                break;
                                                            case "[":
                                                                qFilter.WhereClause = "[" + genIdAreaFieldName + "]" + " = '" + areaValue + "'";
                                                                break;
                                                            default:
                                                                qFilter.WhereClause = "\"" + genIdAreaFieldName + "\" = '" + areaValue + "'";
                                                                break;
                                                        }

                                                        //get value of first row, increment it, and return incremented value
                                                        for (int j = 0; j < 51; j++)
                                                        {
                                                            row = AAState._gentab.Update(qFilter, false).NextRow();
                                                            if (row.get_Value(sequenceColumnNum) == null)
                                                            {
                                                                sequenceValue = 0;
                                                            }
                                                            else if (row.get_Value(sequenceColumnNum).ToString() == "")
                                                            {
                                                                sequenceValue = 0;
                                                            }
                                                            else
                                                                sequenceValue = Convert.ToInt32(row.get_Value(sequenceColumnNum));
                                                            sequenceValue += 1;
                                                            sequenceValue += 1;
                                                            row.set_Value(sequenceColumnNum, sequenceValue);
                                                            row.Store();
                                                            if (Convert.ToInt32(row.get_Value(sequenceColumnNum)) == sequenceValue)
                                                                break;

                                                        }
                                                        if (sequenceValue == -1)
                                                        {
                                                            //TODO raise error
                                                        }
                                                        else
                                                        {
                                                            if (inObject.Fields.get_Field(fieldNum).Type == esriFieldType.esriFieldTypeString)
                                                                if (formatString == null || formatString == "" || formatString.IndexOf("[seq]") == -1)
                                                                    inObject.set_Value(fieldNum, areaValue + sequenceValue.ToString("D" + sequencePadding) + sequencePostfix);
                                                                else
                                                                {
                                                                    formatString = formatString.Replace("[area]", areaValue);
                                                                    formatString = formatString.Replace("[seq]", sequenceValue.ToString("D" + sequencePadding));
                                                                    inObject.set_Value(fieldNum, formatString);
                                                                }
                                                            else
                                                                inObject.set_Value(fieldNum, sequenceValue);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  ERROR: GENERATE_ID table is not found");

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: GENERATE_ID_BY_AREA: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: GENERATE_ID_BY_AREA");
                                            }

                                            break;
                                        //Modified for Release 1.2  (No longer uses ICalculator)
                                        //Requires valid VBScript expression
                                        //Can include string, numeric, and date fields by name in square brackets [] 
                                        //Example: DateDiff("yyyy",[INSTALLDATE],Now()) 
                                        case "EXPRESSION":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: EXPRESSION");
                                                if (inObject != null & valData != null)
                                                {
                                                    newValue = valData;
                                                    for (int i = 0; i < inObject.Fields.FieldCount; i++)
                                                    {
                                                        testField = inObject.Fields.get_Field(i);

                                                        int indFld = newValue.ToUpper().IndexOf("[" + testField.Name.ToUpper() + "]");
                                                        while (indFld >= 0)
                                                        {
                                                            AAState.WriteLine("                  replace field: " + testField.Name + " with a value");
                                                            int fldLen = testField.Name.Length;
                                                            string tmpStr1 = newValue.Substring(0, indFld + 1);
                                                            string tmpStr2 = newValue.Substring(indFld + fldLen + 1);
                                                            newValue = tmpStr1 + "_REPLACE_VAL_" + tmpStr2;

                                                            switch (testField.Type)
                                                            {
                                                                case esriFieldType.esriFieldTypeString:

                                                                    if (inObject.get_Value(i) == null || inObject.get_Value(i).ToString() == "")
                                                                    {
                                                                        if (newValue.Contains("IsNull"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (inObject.get_Value(i) == null)
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", null);
                                                                        }
                                                                        else if (inObject.get_Value(i).ToString() == "")
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + inObject.get_Value(i).ToString() + "\"");
                                                                        }
                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + inObject.get_Value(i).ToString() + "\"");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (newValue.Contains("IsNull"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "False");
                                                                        }

                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + inObject.get_Value(i).ToString() + "\"");
                                                                        }
                                                                    }


                                                                    // newValue = newValue.Replace("[" + testField.Name + "]", "\"" + inObject.get_Value(i).ToString() + "\"");
                                                                    break;
                                                                case esriFieldType.esriFieldTypeDate:


                                                                    if (inObject.get_Value(i) == null || inObject.get_Value(i).ToString() == "")
                                                                    {
                                                                        if (newValue.Contains("IsNull"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (inObject.get_Value(i) == null)
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "CDATE(" + null + ")");
                                                                        }
                                                                        else if (inObject.get_Value(i).ToString() == "")
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + inObject.get_Value(i).ToString() + "\"");
                                                                        }
                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "CDATE(\"" + inObject.get_Value(i).ToString() + "\")");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (newValue.Contains("IsNull"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "False");
                                                                        }

                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "CDATE(\"" + inObject.get_Value(i).ToString() + "\")");
                                                                        }
                                                                    }



                                                                    // newValue = newValue.Replace("[" + testField.Name + "]", "CDATE(\"" + inObject.get_Value(i).ToString() + "\")");
                                                                    break;

                                                                default:
                                                                    if (inObject.get_Value(i) == null || inObject.get_Value(i).ToString() == "")
                                                                    {
                                                                        if (newValue.Contains("IsNull"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (inObject.get_Value(i) == null)
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", null);
                                                                        }
                                                                        else if (inObject.get_Value(i).ToString() == "")
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", null);// "\"" + inObject.get_Value(i).ToString() + "\"");
                                                                        }
                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "" + inObject.get_Value(i).ToString() + "");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (newValue.Contains("IsNull"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "False");
                                                                        }

                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", inObject.get_Value(i).ToString());
                                                                        }
                                                                    }
                                                                    //  newValue = newValue.Replace("[" + testField.Name + "]", inObject.get_Value(i).ToString());
                                                                    break;
                                                            }
                                                            indFld = newValue.ToUpper().IndexOf("[" + testField.Name.ToUpper() + "]");
                                                        }
                                                    }
                                                    //MessageBox.Show(newValue);

                                                    try
                                                    {
                                                        newValue = script.Eval(newValue).ToString();
                                                        if (inObject.get_Value(fieldNum).ToString() != newValue)
                                                            inObject.set_Value(fieldNum, newValue);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        AAState.WriteLine("                  ERROR: evaluating the expression for feature in " + inObject.Class.AliasName + " with OID of " + inObject.OID);
                                                        AAState.WriteLine("                         " + ex.Message);
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: EXPRESSION: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: EXPRESSION");
                                            }
                                            break;

                                        // GUID values are calculated into text fields or into native GUID field types
                                        // When using text field you have an optional argument (valdata) to specify the format
                                        // N-none 32 chars, D-dash 36, B-braces 38, P-Parenthesis 38
                                        case "GUID":
                                            try
                                            {
                                                if (inObject != null)
                                                {
                                                    if (inObject.Fields.get_Field(fieldNum).Type == esriFieldType.esriFieldTypeGUID)
                                                        inObject.set_Value(fieldNum, System.Guid.NewGuid().ToString("B"));
                                                    else if (inObject.Fields.get_Field(fieldNum).Type == esriFieldType.esriFieldTypeString &&
                                                             inObject.Fields.get_Field(fieldNum).Length >= 32)
                                                    {

                                                        valData = valData.Trim();
                                                        if (valData != "N" && valData != "D" && valData != "B" && valData != "P")
                                                            if (inObject.Fields.get_Field(fieldNum).Length >= 38)
                                                                valData = "B";  //Default to braces 
                                                            else if (inObject.Fields.get_Field(fieldNum).Length < 36)
                                                                valData = "N";
                                                            else
                                                                valData = "D";
                                                        inObject.set_Value(fieldNum, System.Guid.NewGuid().ToString(valData));
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: EXPRESSION: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: EXPRESSION");
                                            }
                                            break;

                                        case "CREATE_LINKED_RECORD"://Feature Layer|Field To Copy|Field To Populate|Primary Key Field|Foreign Key Field
                                            {
                                                try
                                                {
                                                    AAState.WriteLine("                  Trying: CREATE_LINKED_RECORD");
                                                    if (!String.IsNullOrEmpty(valData))
                                                    {
                                                        args = valData.Split('|');
                                                        if (args.Length != 5)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                        break;
                                                    }
                                                    if (inFeature == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: The input features is null");
                                                        break;
                                                    }
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    sourceField = -1;
                                                    found = false;
                                                    AAState.WriteLine("                  Getting Value Info");
                                                    sourceLayerNames = args[0].ToString().Split(',');
                                                    sourceFieldName = args[1].ToString();
                                                    string targetFieldName = args[2].ToString();
                                                    string sourceIDFieldName = args[3].ToString();
                                                    string targetIDFieldName = args[4].ToString();
                                                    AAState.WriteLine("                  Checking values");
                                                    if (sourceFieldName != null)
                                                    {
                                                        AAState.WriteLine("                  Checking Fields in Source Layer");
                                                        int fldValToCopyIdx = inObject.Fields.FindField(sourceFieldName);
                                                        int fldIDToCopyIdx = inObject.Fields.FindField(sourceIDFieldName);
                                                        if (fldValToCopyIdx > -1 && fldIDToCopyIdx > -1)
                                                        {

                                                            for (int i = 0; i < sourceLayerNames.Length; i++)
                                                            {
                                                                sourceLayerName = sourceLayerNames[i].ToString();

                                                                if (sourceLayerName != "")
                                                                {

                                                                    // Get layer
                                                                    AAState.WriteLine("                  Checking for table to populate");
                                                                    sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName);

                                                                    if (sourceLayer != null)
                                                                    {

                                                                    }
                                                                    else
                                                                    {
                                                                        ITable pTable = Globals.FindTable(AAState._editor.Map, sourceLayerName);
                                                                        if (pTable != null)
                                                                        {
                                                                            int fldValToPopIdx = pTable.Fields.FindField(targetFieldName);
                                                                            int fldIDToPopIdx = pTable.Fields.FindField(targetIDFieldName);
                                                                            if (fldValToPopIdx > -1 && fldIDToPopIdx > -1)
                                                                            {
                                                                                AAState.WriteLine("                  Trying to create a row in the target table");
                                                                                IRow pNewRow = pTable.CreateRow();
                                                                                AAState.WriteLine("                  Row Created");
                                                                                AAState.WriteLine("                  Trying to Copy ID");
                                                                                try
                                                                                {
                                                                                    pNewRow.set_Value(fldIDToPopIdx, inObject.get_Value(fldIDToCopyIdx));

                                                                                }
                                                                                catch
                                                                                {
                                                                                    AAState.WriteLine("                  ERROR: Could not Copy: " + inObject.get_Value(fldIDToCopyIdx) + " to field: " + targetIDFieldName);
                                                                                }
                                                                                AAState.WriteLine("                  ID successfully copied");
                                                                                AAState.WriteLine("                  Trying to Copy Value");
                                                                                try
                                                                                {
                                                                                    pNewRow.set_Value(fldValToPopIdx, inObject.get_Value(fldValToCopyIdx));

                                                                                }
                                                                                catch
                                                                                {
                                                                                    AAState.WriteLine("                  ERROR: Could not Copy: " + inObject.get_Value(fldValToCopyIdx) + " to field: " + targetFieldName);
                                                                                }
                                                                                AAState.WriteLine("                  Value successfully copied");
                                                                                pNewRow.Store();
                                                                                if (newFeatureList == null)
                                                                                {
                                                                                    newFeatureList = new List<IObject>();
                                                                                }
                                                                                IObject featobj = pNewRow as IObject;


                                                                                if (featobj != null)
                                                                                {
                                                                                    newFeatureList.Add(featobj);
                                                                                }

                                                                                AAState.WriteLine("                  Row successfully stored");
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  ERROR: ID or Field to populate was not found");
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  ERROR: Table to populate not found: " + sourceLayerName);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: ID or Field to Copy was not found");
                                                        }

                                                        //if ((!found) && (inObject.Fields.get_Field(fieldNum).IsNullable))
                                                        //{
                                                        //    inObject.set_Value(fieldNum, null);
                                                        //}
                                                    }

                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: CREATE_LINKED_RECORD" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: CREATE_LINKED_RECORD");
                                                    // fieldNum = -1;

                                                }
                                                break;

                                            }

                                        case "UPDATE_INTERSECTING_FEATURE"://Intersected Feature|FieldIntersectingFeatureToChange|FromFieldinModifiedFeature
                                            {
                                                try
                                                {
                                                    AAState.WriteLine("                  Trying: UPDATE_INTERSECTING_FEATURE");
                                                    if (!String.IsNullOrEmpty(valData))
                                                    {
                                                        args = valData.Split('|');
                                                        if (args.Length != 3)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                        break;
                                                    }
                                                    if (inFeature == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: The input features is null");
                                                        break;
                                                    }
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    sourceField = -1;
                                                    found = false;
                                                    AAState.WriteLine("                  Getting Value Info");
                                                    sourceLayerNames = args[0].ToString().Split(',');
                                                    sourceFieldName = args[1].ToString();
                                                    string targetFieldName = args[2].ToString();
                                                    AAState.WriteLine("                  Checking values");
                                                    if (sourceFieldName != null)
                                                    {
                                                        for (int i = 0; i < sourceLayerNames.Length; i++)
                                                        {
                                                            sourceLayerName = sourceLayerNames[i].ToString();
                                                            if (sourceLayerName != "")
                                                            {
                                                                // Get layer
                                                                sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName);

                                                                if (sourceLayer != null)
                                                                {
                                                                    if (inObject.Class != sourceLayer.FeatureClass)
                                                                    {
                                                                        if (Globals.IsEditable(sourceLayer, AAState._editor))
                                                                        {

                                                                            sourceField = sourceLayer.FeatureClass.FindField(sourceFieldName);

                                                                            if (sourceField > -1)
                                                                            {
                                                                                sFilter = new SpatialFilterClass();
                                                                                if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                                                {

                                                                                    try
                                                                                    {
                                                                                        ISpatialReferenceResolution pSRResolution;

                                                                                        pSRResolution = ((sourceLayer.FeatureClass as IGeoDataset).SpatialReference) as ISpatialReferenceResolution;

                                                                                        //  sFilter = new SpatialFilterClass();
                                                                                        double intTol = pSRResolution.get_XYResolution(false);
                                                                                        bool hasXY = ((sourceLayer.FeatureClass as IGeoDataset).SpatialReference).HasXYPrecision();

                                                                                        searchEnvelope = new EnvelopeClass();
                                                                                        searchEnvelope.XMin = 0 - intTol;
                                                                                        searchEnvelope.YMin = 0 - intTol;
                                                                                        searchEnvelope.XMax = 0 + intTol;
                                                                                        searchEnvelope.YMax = 0 + intTol;
                                                                                        searchEnvelope.CenterAt(inFeature.ShapeCopy as IPoint);

                                                                                        //searchEnvelope.SpatialReference = ((inFeature.Class as IFeatureClass) as IGeoDataset).SpatialReference;
                                                                                        searchEnvelope.SpatialReference = ((inFeature.Class as IFeatureClass) as IGeoDataset).SpatialReference;
                                                                                        searchEnvelope.SnapToSpatialReference();
                                                                                        searchEnvelope.Project(AAState._editor.Map.SpatialReference);


                                                                                        sFilter.Geometry = Globals.Env2Polygon(searchEnvelope);

                                                                                    }
                                                                                    catch
                                                                                    {
                                                                                        sFilter.Geometry = inFeature.ShapeCopy;
                                                                                    }




                                                                                }
                                                                                else
                                                                                {
                                                                                    sFilter.Geometry = inFeature.ShapeCopy;
                                                                                }
                                                                                sFilter.GeometryField = sourceLayer.FeatureClass.ShapeFieldName;
                                                                                sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                                                                                fCursor = sourceLayer.FeatureClass.Search(sFilter, false);
                                                                                sourceFeature = fCursor.NextFeature();
                                                                                if (sourceFeature != null)
                                                                                {
                                                                                    int fldIdx = inFeature.Fields.FindField(targetFieldName);
                                                                                    AAState.WriteLine("                  " + targetFieldName + " at index " + fldIdx);
                                                                                    string test = targetFieldName;
                                                                                    if (fldIdx > -1)
                                                                                    {
                                                                                        test = inFeature.get_Value(fldIdx).ToString();
                                                                                        AAState.WriteLine("                  Value Found " + test);
                                                                                    }
                                                                                    AAState.WriteLine("                  Value used " + test);
                                                                                    try
                                                                                    {

                                                                                        if (sourceFeature.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeString)
                                                                                        {
                                                                                            sourceFeature.set_Value(sourceField, test);
                                                                                            sourceFeature.Store();
                                                                                            found = true;
                                                                                            break;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            if (Globals.IsNumeric(test))
                                                                                            {
                                                                                                if (sourceFeature.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeSmallInteger ||
                                                                                                    sourceFeature.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeInteger)
                                                                                                {

                                                                                                    sourceFeature.set_Value(sourceField, Convert.ToInt32(test));
                                                                                                    sourceFeature.Store();
                                                                                                    found = true;
                                                                                                    break;
                                                                                                }
                                                                                                else if (sourceFeature.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeDouble ||
                                                                                                    sourceFeature.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeSingle)
                                                                                                {
                                                                                                    sourceFeature.set_Value(sourceField, Convert.ToDouble(test));

                                                                                                    sourceFeature.Store(); found = true;
                                                                                                    break;
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    sourceFeature.set_Value(sourceField, test as object);
                                                                                                    sourceFeature.Store();
                                                                                                    found = true;
                                                                                                    break;
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                sourceFeature.set_Value(sourceField, test as object);
                                                                                                sourceFeature.Store();
                                                                                                found = true;
                                                                                                break;
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                    catch
                                                                                    {
                                                                                        AAState.WriteLine("                  ERROR setting value");
                                                                                    }
                                                                                    finally
                                                                                    {
                                                                                        if (found)
                                                                                        {
                                                                                            //   break;
                                                                                        }
                                                                                    }


                                                                                }

                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  ERROR: Source field not found: " + sourceFieldName);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  ERROR/WARNING: Source Layer is not editable: " + sourceLayerName);
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR/WARNING: Source Layer not found: " + sourceLayerName);
                                                                }
                                                            }
                                                        }
                                                        //if ((!found) && (inObject.Fields.get_Field(fieldNum).IsNullable))
                                                        //{
                                                        //    inObject.set_Value(fieldNum, null);
                                                        //}
                                                    }

                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: UPDATE_INTERSECTING_FEATURE" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: UPDATE_INTERSECTING_FEATURE");
                                                    // fieldNum = -1;

                                                }
                                                break;

                                            }
                                        case "MULTI_FIELD_INTERSECT":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: MULTI_FIELD_INTERSECT");
                                                if (inFeature != null & valData != null)
                                                {
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    sourceField = -1;
                                                    found = false;
                                                    //LayerToIntersect|Field To Elevate
                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    int popFldIdx = 0;
                                                    if (args.GetLength(0) > 2)
                                                    {
                                                        AAState.WriteLine("                  Parsing Valueinfo");

                                                        sourceLayerNames = args[0].ToString().Split(',');
                                                        sourceFieldName = args[1].ToString();
                                                        string[] fieldsToPop = args[2].ToString().Split(',');
                                                        if (args.GetLength(0) == 4)
                                                        {
                                                            AAState.WriteLine("                  Search distance specified");

                                                            if (Globals.IsDouble(args[3]))
                                                            {
                                                                searchDistance = Convert.ToDouble(args[3]);
                                                            }
                                                            else
                                                            {
                                                                searchDistance = 0.0;
                                                            }
                                                        }
                                                        else
                                                        {

                                                            searchDistance = 0.0;
                                                        }

                                                        if (sourceFieldName != null)
                                                        {
                                                            AAState.WriteLine("                  Looping Through Layers");

                                                            for (int i = 0; i < sourceLayerNames.Length; i++)
                                                            {
                                                                if (fieldsToPop.Length == popFldIdx)
                                                                    break;


                                                                sourceLayerName = sourceLayerNames[i].ToString();
                                                                if (sourceLayerName != "")
                                                                {
                                                                    // Get layer
                                                                    sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName);
                                                                    if (sourceLayer != null)
                                                                    {
                                                                        if (sourceLayer.FeatureClass != null)
                                                                        {
                                                                            sourceField = sourceLayer.FeatureClass.FindField(sourceFieldName);

                                                                            if (sourceField > -1)
                                                                            {
                                                                                sFilter = new SpatialFilterClass();
                                                                                if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                                                {

                                                                                    searchEnvelope = Globals.CalcSearchExtent(sourceLayer, inFeature, searchDistance);

                                                                                    sFilter.Geometry = searchEnvelope;


                                                                                }
                                                                                else
                                                                                {
                                                                                    sFilter.Geometry = inFeature.ShapeCopy;
                                                                                }
                                                                                sFilter.GeometryField = sourceLayer.FeatureClass.ShapeFieldName;
                                                                                sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                                                                                fCursor = sourceLayer.FeatureClass.Search(sFilter, true);
                                                                                sourceFeature = fCursor.NextFeature();
                                                                                while (sourceFeature != null)
                                                                                {
                                                                                    if (fieldsToPop.Length == popFldIdx)
                                                                                        break;

                                                                                    string test = sourceFeature.get_Value(sourceField).ToString();

                                                                                    int tempFieldNum = inObject.Fields.FindField(fieldsToPop[popFldIdx]);
                                                                                    popFldIdx++;
                                                                                    if (tempFieldNum > -1)
                                                                                    {
                                                                                        inObject.set_Value(tempFieldNum, sourceFeature.get_Value(sourceField));
                                                                                    }
                                                                                    sourceFeature = fCursor.NextFeature();

                                                                                }

                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  ERROR: Source field not found: " + sourceFieldName);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  ERROR/WARNING: Datasource is invalid: " + sourceLayerName);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR/WARNING: Source Layer not found: " + sourceLayerName);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR/WARNING: Source Layer string is empty");

                                                                }
                                                            }


                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Field name is invalid");


                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Invalid Value method definition");

                                                    }

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: MULTI_FIELD_INTERSECT: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: MULTI_FIELD_INTERSECT");
                                            }
                                            break;
                                        case "INTERSECT_STATS":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: INTERSECT_STATS");
                                                if (inFeature != null & valData != null)
                                                {
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    sourceField = -1;
                                                    found = false;
                                                    //LayerToIntersect|Field To Elevate
                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    int AverageCount = 0;
                                                    if (args.GetLength(0) > 2)
                                                    {
                                                        AAState.WriteLine("                  Parsing Valueinfo");

                                                        sourceLayerNames = args[0].ToString().Split(',');
                                                        sourceFieldName = args[1].ToString();
                                                        string statType = args[2].ToString();
                                                        if (args.GetLength(0) == 4)
                                                        {
                                                            AAState.WriteLine("                  Search distance specified");

                                                            if (Globals.IsDouble(args[3]))
                                                                searchDistance = Convert.ToDouble(args[3]);
                                                            else
                                                                searchDistance = 0.0;
                                                        }
                                                        else
                                                        {

                                                            searchDistance = 0.0;
                                                        }
                                                        double result = -999999.1;

                                                        if (sourceFieldName != null)
                                                        {
                                                            AAState.WriteLine("                  Looping Through Layers");

                                                            for (int i = 0; i < sourceLayerNames.Length; i++)
                                                            {

                                                                sourceLayerName = sourceLayerNames[i].ToString();
                                                                if (sourceLayerName != "")
                                                                {
                                                                    // Get layer
                                                                    sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName);
                                                                    if (sourceLayer != null)
                                                                    {
                                                                        if (sourceLayer.FeatureClass != null)
                                                                        {
                                                                            sourceField = sourceLayer.FeatureClass.FindField(sourceFieldName);

                                                                            if (sourceField > -1)
                                                                            {
                                                                                sFilter = new SpatialFilterClass();
                                                                                if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                                                {

                                                                                    searchEnvelope = Globals.CalcSearchExtent(sourceLayer, inFeature, searchDistance);

                                                                                    sFilter.Geometry = searchEnvelope;


                                                                                }
                                                                                else
                                                                                {
                                                                                    sFilter.Geometry = inFeature.ShapeCopy;
                                                                                }
                                                                                sFilter.GeometryField = sourceLayer.FeatureClass.ShapeFieldName;
                                                                                sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                                                                                fCursor = sourceLayer.FeatureClass.Search(sFilter, true);
                                                                                sourceFeature = fCursor.NextFeature();
                                                                                while (sourceFeature != null)
                                                                                {
                                                                                    string test = sourceFeature.get_Value(sourceField).ToString();
                                                                                    if (Globals.IsNumeric(test))
                                                                                    {
                                                                                        double valToTest = Convert.ToDouble(test);
                                                                                        if (result == -999999.1)
                                                                                        {
                                                                                            result = valToTest;


                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            switch (statType.ToUpper())
                                                                                            {
                                                                                                case "MAX":
                                                                                                    if (result < valToTest)
                                                                                                    {
                                                                                                        result = valToTest;

                                                                                                    }
                                                                                                    break;
                                                                                                case "MIN":
                                                                                                    if (result > valToTest)
                                                                                                    {
                                                                                                        result = valToTest;

                                                                                                    }
                                                                                                    break;
                                                                                                case "SUM":
                                                                                                    result += valToTest;


                                                                                                    break;
                                                                                                case "AVERAGE":
                                                                                                    result += valToTest;
                                                                                                    AverageCount++;
                                                                                                    break;
                                                                                                case "MEAN":
                                                                                                    result += valToTest;
                                                                                                    AverageCount++;

                                                                                                    break;
                                                                                                default:
                                                                                                    AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                                    break;
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  ERROR/WARNING: Non numeric value returned: " + test);
                                                                                    }
                                                                                    sourceFeature = fCursor.NextFeature();

                                                                                }

                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  ERROR: Source field not found: " + sourceFieldName);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  ERROR/WARNING: Datasource is invalid: " + sourceLayerName);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR/WARNING: Source Layer not found: " + sourceLayerName);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR/WARNING: Source Layer string is empty");

                                                                }
                                                            }

                                                            if (result != -999999.1)
                                                            {
                                                                if (AverageCount != 0)
                                                                {
                                                                    result = result / AverageCount;
                                                                }
                                                                inObject.set_Value(fieldNum, result);

                                                            }
                                                            else
                                                            {
                                                                IField field = inObject.Fields.get_Field(fieldNum);
                                                                object newval = field.DefaultValue;
                                                                if (newval == null)
                                                                {
                                                                    if (field.IsNullable)
                                                                    {
                                                                        inObject.set_Value(fieldNum, null);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    inObject.set_Value(fieldNum, newval);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Field name is invalid");


                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Invalid Value method definition");

                                                    }

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: INTERSECT_STATS: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: INTERSECT_STATS");
                                            }
                                            break;
                                        case "INTERSECTING_FEATURE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: INTERSECTING_FEATURE");
                                                if (inFeature != null & valData != null)
                                                {
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    sourceField = -1;
                                                    found = false;

                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    if (args.GetLength(0) >= 2)
                                                    {
                                                        sourceLayerNames = args[0].ToString().Split(',');
                                                        sourceFieldName = args[1].ToString();

                                                        if (sourceFieldName != null)
                                                        {
                                                            for (int i = 0; i < sourceLayerNames.Length; i++)
                                                            {
                                                                sourceLayerName = sourceLayerNames[i].ToString();
                                                                if (sourceLayerName != "")
                                                                {
                                                                    // Get layer
                                                                    sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName);
                                                                    if (sourceLayer != null)
                                                                    {
                                                                        sourceField = sourceLayer.FeatureClass.FindField(sourceFieldName);

                                                                        if (sourceField > -1)
                                                                        {
                                                                            sFilter = new SpatialFilterClass();

                                                                            sFilter.GeometryField = sourceLayer.FeatureClass.ShapeFieldName;
                                                                            sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                                                                            sFilter.set_OutputSpatialReference(sourceLayer.FeatureClass.ShapeFieldName, (sourceLayer.FeatureClass as IGeoDataset).SpatialReference);
                                                                            //sFilter.set_OutputSpatialReference(sourceLayer.FeatureClass.ShapeFieldName, AAState._editor.Map.SpatialReference);
                                                                            if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                                            {

                                                                                try
                                                                                {
                                                                                    IGeometry pSourceGeo = inFeature.ShapeCopy as IPoint;
                                                                                    pSourceGeo.SpatialReference = ((inFeature.Class as IFeatureClass) as IGeoDataset).SpatialReference;
                                                                                    pSourceGeo.Project((sourceLayer.FeatureClass as IGeoDataset).SpatialReference);
                                                                                    //pSourceGeo.Project(AAState._editor.Map.SpatialReference);


                                                                                    bool hasXY;
                                                                                    hasXY = ((sourceLayer.FeatureClass as IGeoDataset).SpatialReference).HasXYPrecision();
                                                                                    //hasXY = (AAState._editor.Map.SpatialReference).HasXYPrecision();

                                                                                    double intTol = .001;
                                                                                    if (hasXY)
                                                                                    {
                                                                                        ISpatialReferenceResolution pSRResolution;

                                                                                        pSRResolution = ((sourceLayer.FeatureClass as IGeoDataset).SpatialReference) as ISpatialReferenceResolution;
                                                                                        //pSRResolution = (AAState._editor.Map.SpatialReference) as ISpatialReferenceResolution;
                                                                                        intTol = pSRResolution.get_XYResolution(false) * 2;
                                                                                    }


                                                                                    searchEnvelope = new EnvelopeClass();
                                                                                    searchEnvelope.XMin = 0 - intTol;
                                                                                    searchEnvelope.YMin = 0 - intTol;
                                                                                    searchEnvelope.XMax = 0 + intTol;
                                                                                    searchEnvelope.YMax = 0 + intTol;
                                                                                    searchEnvelope.CenterAt(pSourceGeo as IPoint);

                                                                                    searchEnvelope.SpatialReference = ((sourceLayer.FeatureClass as IGeoDataset).SpatialReference);
                                                                                    //searchEnvelope.SpatialReference = (AAState._editor.Map.SpatialReference);


                                                                                    //searchEnvelope.SpatialReference = ((inFeature.Class as IFeatureClass) as IGeoDataset).SpatialReference;
                                                                                    //searchEnvelope.SnapToSpatialReference();
                                                                                    //if (AAState._editor.Map.SpatialReference != ((inFeature.Class as IFeatureClass) as IGeoDataset).SpatialReference)
                                                                                    //{

                                                                                    //  searchEnvelope.Project((sourceLayer.FeatureClass as IGeoDataset).SpatialReference);

                                                                                    //searchEnvelope.Project(AAState._editor.Map.SpatialReference);
                                                                                    //}

                                                                                    sFilter.Geometry = Globals.Env2Polygon(searchEnvelope);

                                                                                    //searchEnvelope.Expand(.1, .1, true);
                                                                                    //searchEnvelope.Expand(searchDistance, searchDistance, false);
                                                                                }
                                                                                catch
                                                                                {
                                                                                    IGeometry pGeo = inFeature.ShapeCopy;
                                                                                    pGeo.SpatialReference = ((inFeature.Class as IFeatureClass) as IGeoDataset).SpatialReference;
                                                                                    pGeo.Project(AAState._editor.Map.SpatialReference);

                                                                                    sFilter.Geometry = pGeo;

                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                IGeometry pGeo = inFeature.ShapeCopy;
                                                                                pGeo.SpatialReference = ((inFeature.Class as IFeatureClass) as IGeoDataset).SpatialReference;
                                                                                pGeo.Project(AAState._editor.Map.SpatialReference);

                                                                                sFilter.Geometry = pGeo;

                                                                            }
                                                                            fCursor = sourceLayer.FeatureClass.Search(sFilter, true);
                                                                            sourceFeature = fCursor.NextFeature();
                                                                            while (sourceFeature != null)
                                                                            {
                                                                                string test = sourceFeature.get_Value(sourceField).ToString();
                                                                                inObject.set_Value(fieldNum, sourceFeature.get_Value(sourceField));
                                                                                found = true;
                                                                                sourceFeature = fCursor.NextFeature();

                                                                            }




                                                                            if (found == false  && AAState._CheckEnvelope)
                                                                            
                                                                            {
                                                                                sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
                                                                              //  sFilter.SpatialRelDescription = "T*T***T*T";
                                                                                fCursor = sourceLayer.FeatureClass.Search(sFilter, true);
                                                                                sourceFeature = fCursor.NextFeature();
                                                                                while (sourceFeature != null)
                                                                                {
                                                                                    string test = sourceFeature.get_Value(sourceField).ToString();
                                                                                    inObject.set_Value(fieldNum, sourceFeature.get_Value(sourceField));
                                                                                    found = true;
                                                                                    sourceFeature = fCursor.NextFeature();

                                                                                }

                                                                            
                                                                            }
                                                                            if (found)
                                                                            {
                                                                                break;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  ERROR: Source field not found: " + sourceFieldName);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR/WARNING: Source Layer not found: " + sourceLayerName);
                                                                    }
                                                                }
                                                            }
                                                            if (!found)
                                                            {
                                                                IField field = inObject.Fields.get_Field(fieldNum);
                                                                object newval = field.DefaultValue;
                                                                if (newval == null)
                                                                {
                                                                    if (field.IsNullable)
                                                                    {
                                                                        inObject.set_Value(fieldNum, null);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    inObject.set_Value(fieldNum, newval);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: INTERSECTING_FEATURE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: INTERSECTING_FEATURE");
                                            }
                                            break;
                                        case "INTERSECTING_RASTER":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: INTERSECTING_RASTER");
                                                if (inFeature != null & valData != null)
                                                {

                                                    sourceLayerName = "";
                                                    formatString = "";
                                                    found = false;

                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    if (args.Length < 1) break;
                                                    switch (args.Length)
                                                    {
                                                        case 1:
                                                            sourceLayerNames = args[0].ToString().Split(',');
                                                            break;
                                                        case 2:
                                                            sourceLayerNames = args[0].ToString().Split(',');
                                                            formatString = args[1].ToString();
                                                            break;
                                                        default: break;
                                                    }

                                                    // Get layer
                                                    for (int i = 0; i < sourceLayerNames.Length; i++)
                                                    {
                                                        sourceLayerName = sourceLayerNames[i].ToString();
                                                        IPoint pLoc = Globals.GetGeomCenter(inFeature);

                                                        if (pLoc != null)
                                                        {
                                                            string cellVal = GetCellValue(sourceLayerName, pLoc, AAState._editor.Map);// Globals.GetCellValue(sourceLayerName, pLoc, _editor.Map);
                                                            AAState.WriteLine("                  ERROR/WARING: No cell value or raster was found: " + sourceLayerName);
                                                            if (cellVal != null && cellVal != "" && cellVal != "No Raster")
                                                            {

                                                                if (formatString == null || formatString == "" || (inObject.Fields.get_Field(fieldNum).Type != esriFieldType.esriFieldTypeString))
                                                                {
                                                                    inObject.set_Value(fieldNum, cellVal);
                                                                    found = true;
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    // formatString = formatString.Replace("[value]",cellVal);
                                                                    formatString = formatString + cellVal;
                                                                    inObject.set_Value(fieldNum, formatString);

                                                                    found = true;
                                                                    break;
                                                                }

                                                            }

                                                        }
                                                    }
                                                    if (!(found) && inObject.Fields.get_Field(fieldNum).IsNullable)
                                                        inObject.set_Value(fieldNum, null);
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: INTERSECTING_RASTER: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: INTERSECTING_RASTER");
                                            }
                                            break;
                                        case "INTERSECTING_LAYER_DETAILS":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: INTERSECTING_LAYER_DETAILS");
                                                if (inFeature != null & valData != null)
                                                {

                                                    sourceLayerName = "";
                                                    formatString = "";
                                                    found = false;

                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    if (args.Length < 1) break;
                                                    switch (args.Length)
                                                    {
                                                        case 1:
                                                            sourceLayerNames = args[0].ToString().Split(',');
                                                            formatString = "P";
                                                            break;
                                                        case 2:
                                                            sourceLayerNames = args[0].ToString().Split(',');
                                                            formatString = args[1].ToString();
                                                            break;
                                                        default: break;
                                                    }

                                                    // Get layer
                                                    for (int i = 0; i < sourceLayerNames.Length; i++)
                                                    {
                                                        sourceLayerName = sourceLayerNames[i].ToString();
                                                        //AAState.WriteLine("                  Getting Features Centeroid");

                                                        //IPoint pLoc = Globals.GetGeomCenter(inFeature);
                                                        IGeometry pGeo = inFeature.ShapeCopy;


                                                        if (pGeo != null)
                                                        {
                                                            // AAState.WriteLine("                  Centroid Found");
                                                            AAState.WriteLine("                  Getting list of " + sourceLayerName + " Layers");
                                                            IEnumLayer pEnum = Globals.GetLayers(AAState._editor.Map, sourceLayerName);

                                                            if (pEnum != null)
                                                            {
                                                                AAState.WriteLine("                  List retrieved of " + sourceLayerName + " Layers");
                                                                AAState.WriteLine("                  Starting Loop");
                                                                ILayer pLay = pEnum.Next();
                                                                ISpatialFilter pSpatFilt;
                                                                while (pLay != null)
                                                                {

                                                                    if (found)
                                                                    {
                                                                        AAState.WriteLine("                  Exiting Layer Loop");
                                                                        break;
                                                                    }
                                                                    AAState.WriteLine("                  Checking " + pLay.Name);

                                                                    if (pLay is IRasterLayer)
                                                                    {

                                                                        IRasterLayer pRasLay = pLay as IRasterLayer;
                                                                        AAState.WriteLine("                  Trying " + pRasLay.Name);
                                                                        IEnvelope pEnv = pRasLay.AreaOfInterest;
                                                                        // ITopologicalOperator pTopo = pEnv as ITopologicalOperator;
                                                                        IRelationalOperator pRel = pEnv as IRelationalOperator;
                                                                        IRelationalOperator2 pRel2 = pEnv as IRelationalOperator2;

                                                                        if (pRel.Crosses(pGeo) || pRel.Touches(pGeo) || pRel.Overlaps(pGeo) || pRel2.ContainsEx(pGeo, esriSpatialRelationExEnum.esriSpatialRelationExClementini))
                                                                        {
                                                                            AAState.WriteLine("                  Geometry does intersect " + pRasLay.Name);
                                                                            switch (formatString)
                                                                            {
                                                                                case "P":
                                                                                    // IDataset pDS = pFLay.FeatureClass as IDataset;

                                                                                    inObject.set_Value(fieldNum, Globals.GetPathForALayer(pLay));
                                                                                    pRasLay = null;
                                                                                    pEnv = null;

                                                                                    pRel = null;
                                                                                    pRel2 = null;
                                                                                    found = true;
                                                                                    break;
                                                                                case "N":
                                                                                    inObject.set_Value(fieldNum, pLay.Name);
                                                                                    pRasLay = null;
                                                                                    pEnv = null;

                                                                                    pRel = null;
                                                                                    pRel2 = null;
                                                                                    found = true;
                                                                                    break;
                                                                                default:
                                                                                    inObject.set_Value(fieldNum, Globals.GetPathForALayer(pLay));
                                                                                    pRasLay = null;
                                                                                    pEnv = null;

                                                                                    pRel = null;
                                                                                    pRel2 = null;
                                                                                    found = true;
                                                                                    break;
                                                                            }
                                                                        }
                                                                    }
                                                                    else if (pLay is IFeatureLayer)
                                                                    {
                                                                        IFeatureLayer pFLay = pLay as IFeatureLayer;
                                                                        if (pFLay.FeatureClass == inObject.Class)
                                                                        {
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  Trying " + pFLay.Name);
                                                                            pSpatFilt = new SpatialFilterClass();
                                                                            pSpatFilt.GeometryField = pFLay.FeatureClass.ShapeFieldName;
                                                                            pSpatFilt.Geometry = pGeo as IGeometry;
                                                                            pSpatFilt.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                                                                            if (pFLay.FeatureClass.FeatureCount(pSpatFilt) > 0)
                                                                            {
                                                                                AAState.WriteLine("                  Geometry does intersect " + pFLay.Name);
                                                                                switch (formatString)
                                                                                {
                                                                                    case "P":
                                                                                        // IDataset pDS = pFLay.FeatureClass as IDataset;
                                                                                        // AAState.WriteLine("                  Exiting Layer Loop");
                                                                                        inObject.set_Value(fieldNum, Globals.GetPathForALayer(pLay));
                                                                                        pFLay = null;
                                                                                        found = true;
                                                                                        break;
                                                                                    case "N":
                                                                                        inObject.set_Value(fieldNum, pLay.Name);
                                                                                        pFLay = null;
                                                                                        found = true;
                                                                                        break;
                                                                                    default:
                                                                                        inObject.set_Value(fieldNum, Globals.GetPathForALayer(pLay));
                                                                                        pFLay = null;
                                                                                        found = true;
                                                                                        //inObject.set_Value(fieldNum, null);
                                                                                        break;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Does not intersect " + pFLay.Name);
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  Warning: Unsupported type");
                                                                    }
                                                                    pLay = pEnum.Next();
                                                                }
                                                                pEnum = null;

                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR: not matching layer types found");
                                                            }


                                                        }
                                                        else
                                                            AAState.WriteLine("                  ERROR: Geo not Found");
                                                    }
                                                    //if (!(found) && inObject.Fields.get_Field(fieldNum).IsNullable)
                                                    //    inObject.set_Value(fieldNum, null);
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: INTERSECTING_LAYER_DETAILS: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: INTERSECTING_LAYER_DETAILS");
                                            }
                                            break;
                                        case "INTERSECTING_FEATURE_DISTANCE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: INTERSECTING_FEATURE_DISTANCE");
                                                if (inFeature != null & valData != null)
                                                {
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    sourceField = -1;

                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    if (args.GetLength(0) >= 2)
                                                    {
                                                        //    sourceLayerName = args[0].ToString();
                                                        sourceLayerNames = args[0].ToString().Split(',');
                                                        sourceFieldName = args[1].ToString();
                                                    }
                                                    // Get layer

                                                    if (sourceFieldName != null)
                                                    {
                                                        for (int i = 0; i < sourceLayerNames.Length; i++)
                                                        {
                                                            sourceLayerName = sourceLayerNames[i].ToString();
                                                            if (sourceLayerName != "")

                                                                sourceLayerName = args[i].ToString();
                                                            if (i == 0)
                                                                i++;

                                                            sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName);
                                                            if (sourceLayer == null)
                                                            {
                                                                AAState.WriteLine("                  ERROR/WARNING: " + sourceLayer + " was not found");
                                                                continue;
                                                            }

                                                            IFeatureClass iFC = inFeature.Class as IFeatureClass;
                                                            if (sourceLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                                                            {
                                                                AAState.WriteLine("                  ERROR: " + sourceLayer + " is a polygon layer");

                                                                break;
                                                            }
                                                            //if (sourceLayer.FeatureClass.ShapeType != esriGeometryType.esriGeometryPolyline || iFC.ShapeType != esriGeometryType.esriGeometryPoint)
                                                            //    break;
                                                            //FindLayerByName(sourceLayerName, out sourceLayer);
                                                            if (sourceLayer != null)
                                                            {
                                                                sourceField = sourceLayer.FeatureClass.FindField(sourceFieldName);

                                                                if (sourceField > -1)
                                                                {
                                                                    sFilter = new SpatialFilterClass();
                                                                    sFilter.Geometry = inFeature.ShapeCopy;
                                                                    sFilter.GeometryField = sourceLayer.FeatureClass.ShapeFieldName;
                                                                    sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                                                                    fCursor = sourceLayer.FeatureClass.Search(sFilter, true);
                                                                    sourceFeature = fCursor.NextFeature();
                                                                    if (sourceFeature != null)
                                                                    {

                                                                        IPoint pIntPnt;
                                                                        if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                                        {
                                                                            pIntPnt = Globals.GetIntersection(inFeature.ShapeCopy, sourceFeature.ShapeCopy as IPolyline) as IPoint;

                                                                        }
                                                                        else
                                                                            pIntPnt = inFeature.ShapeCopy as IPoint;

                                                                        double dAlong = Globals.PointDistanceOnLine(pIntPnt, sourceFeature.Shape as IPolyline, 2);
                                                                        string strUnit = Globals.GetSpatRefUnitName(Globals.GetLayersCoordinateSystem(sourceLayer.FeatureClass), true);
                                                                        if (strUnit == "Foot" && dAlong != 1)
                                                                        {
                                                                            strUnit = "Feet";
                                                                        }
                                                                        else if (strUnit == "Meter" && dAlong != 1)
                                                                        {
                                                                            strUnit = "Meters";
                                                                        }
                                                                        string strDis = dAlong + " " + strUnit + " along " + sourceLayer.Name + " with " + sourceLayer.FeatureClass.Fields.get_Field(sourceField).AliasName + " of " + sourceFeature.get_Value(sourceField);



                                                                        if (inObject.Fields.get_Field(fieldNum).Length < strDis.Length - 1)
                                                                        {

                                                                            strDis = dAlong + " " + strUnit + ": " + sourceFeature.get_Value(sourceField);
                                                                            AAState.WriteLine("                  Text is to long, defaulting to length along: " + strDis);

                                                                            if (inObject.Fields.get_Field(fieldNum).Length < strDis.Length - 1)
                                                                            {

                                                                                if (inObject.Fields.get_Field(fieldNum).Length < strDis.Length - 1)
                                                                                {
                                                                                    strDis = dAlong.ToString();
                                                                                    inObject.set_Value(fieldNum, strDis);
                                                                                    break;
                                                                                }
                                                                                else
                                                                                {
                                                                                    inObject.set_Value(fieldNum, strDis);
                                                                                    break;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                strDis = dAlong.ToString();
                                                                                AAState.WriteLine("                  Text is to long, defaulting to length along: " + strDis);
                                                                                if (inObject.Fields.get_Field(fieldNum).Length < strDis.Length - 1)
                                                                                {
                                                                                    strDis = dAlong.ToString();
                                                                                    inObject.set_Value(fieldNum, strDis);
                                                                                    break;
                                                                                }
                                                                                else
                                                                                {
                                                                                    inObject.set_Value(fieldNum, strDis);
                                                                                    break;
                                                                                }
                                                                            }

                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  Value set to: " + strDis);
                                                                            inObject.set_Value(fieldNum, strDis);
                                                                            break;
                                                                        }
                                                                    }

                                                                    else if (inObject.Fields.get_Field(fieldNum).IsNullable)
                                                                        inObject.set_Value(fieldNum, null);
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR: " + sourceLayer + ": field: " + sourceFieldName + " was not found");
                                                                }
                                                            }
                                                            else { }
                                                        }
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: INTERSECTING_FEATURE_DISTANCE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: INTERSECTING_FEATURE_DISTANCE");
                                            }
                                            break;


                                        //Release: 1.2
                                        //New Dynamic Value Method: NEARSET_FEATURE - similiar to INTERSECTING_FEATURE but requires a search distance.  

                                        case "NEAREST_FEATURE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: NEAREST_FEATURE");
                                                if (inFeature != null & valData != null)
                                                {
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    searchDistance = 0;
                                                    found = false;

                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    if (args.GetLength(0) > 1)
                                                    {
                                                        sourceLayerNames = args[0].ToString().Split(',');
                                                        sourceFieldName = args[1].ToString();
                                                    }

                                                    if (args.GetLength(0) > 2)
                                                        Double.TryParse(args[2], out searchDistance);

                                                    if (sourceLayerNames.Length > 0 & sourceFieldName != null)
                                                    {
                                                        for (int i = 0; i < sourceLayerNames.Length; i++)
                                                        {
                                                            sourceLayerName = sourceLayerNames[i].ToString();
                                                            if (sourceLayerName != "")
                                                            {
                                                                sourceLayer = Globals.FindLayer(AAState._editor.Map, sourceLayerName) as IFeatureLayer;
                                                                if (sourceLayer != null)
                                                                {
                                                                    sourceField = sourceLayer.FeatureClass.FindField(sourceFieldName);

                                                                    if (sourceField > -1)
                                                                    {
                                                                        sFilter = new SpatialFilterClass();
                                                                        if (searchDistance > 0)
                                                                        {
                                                                            searchEnvelope = inFeature.ShapeCopy.Envelope;
                                                                            searchEnvelope.Expand(searchDistance, searchDistance, false);
                                                                            sFilter.Geometry = searchEnvelope;
                                                                        }
                                                                        else
                                                                            sFilter.Geometry = inFeature.ShapeCopy;

                                                                        sFilter.GeometryField = sourceLayer.FeatureClass.ShapeFieldName;
                                                                        sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                                                                        fCursor = sourceLayer.FeatureClass.Search(sFilter, false);
                                                                        sourceFeature = fCursor.NextFeature();
                                                                        nearestFeature = null;

                                                                        proxOp = (IProximityOperator)inFeature.Shape;
                                                                        lastDistance = searchDistance;
                                                                        while (!(sourceFeature == null))
                                                                        {
                                                                            distance = proxOp.ReturnDistance(sourceFeature.Shape);
                                                                            if (distance <= lastDistance)
                                                                            {
                                                                                nearestFeature = sourceFeature;
                                                                                lastDistance = distance;
                                                                            }
                                                                            sourceFeature = fCursor.NextFeature();
                                                                        }

                                                                        if (nearestFeature != null)
                                                                        {
                                                                            inObject.set_Value(fieldNum, nearestFeature.get_Value(sourceField));
                                                                            found = true;
                                                                            break;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR: " + sourceLayer + ": field: " + sourceFieldName + " was not found");
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR: " + sourceLayer + " was not found");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR: Empty source layer name");
                                                            }

                                                        }
                                                        if (!found)
                                                        {
                                                            IField field = inObject.Fields.get_Field(fieldNum);
                                                            object newval = field.DefaultValue;
                                                            if (newval == null)
                                                            {
                                                                if (field.IsNullable)
                                                                {
                                                                    inObject.set_Value(fieldNum, null);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                inObject.set_Value(fieldNum, newval);
                                                            }
                                                        }
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: NEAREST_FEATURE: " + ex.Message);

                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: NEAREST_FEATURE");
                                            }
                                            break;

                                        default:
                                            //    MessageBox.Show(valMethod + " for layer " + tableName + " is not a valid method, check the dynamic value table", "Attribute Assistant");
                                            AAState.WriteLine("ERROR: " + valMethod + " for layer " + tableName + " is not a valid method, check the dynamic value table");

                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("TableName:" + tableName + "  FieldName:" + fieldName + System.Environment.NewLine + "ValueMethod:" + valMethod + "  ValueData:" + valData + System.Environment.NewLine + "Message: " + ex.Message, "Attribute Assistant Message");
                                    AAState.WriteLine("ERROR: TableName:" + tableName + "  FieldName:" + fieldName + System.Environment.NewLine + "ValueMethod:" + valMethod + "  ValueData:" + valData + System.Environment.NewLine + "Message: " + ex.Message);
                                }


                            }

                            // if (mode == "ON_CREATE")
                            //{
                            if (inObject != null)
                            {

                                inChanges = inObject as IRowChanges;
                                if (fieldNum < inObject.Fields.FieldCount)
                                {
                                    changed = inChanges.get_ValueChanged(fieldNum);
                                    if (changed)
                                        //  if (fieldName.ToUpper() != "SHAPE")
                                        try
                                        {
                                            // AAState.WriteLine("                      Setting Last Value");
                                            if (AAState.lastValueProperties.GetProperty(fieldName) != null)
                                            {
                                                AAState.WriteLine("                      Setting Last Value");
                                                AAState.WriteLine("                           " + fieldName + ": " + inObject.get_Value(fieldNum).ToString());
                                                AAState.lastValueProperties.SetProperty(fieldName, inObject.get_Value(fieldNum));
                                            }

                                            else
                                            {
                                                AAState.WriteLine("                      Setting Last Value");
                                                AAState.WriteLine("                           " + fieldName + ": " + inObject.get_Value(fieldNum).ToString());

                                                AAState.lastValueProperties.SetProperty(fieldName, inObject.get_Value(fieldNum));
                                            }

                                        }
                                        catch
                                        {
                                            //AAState.WriteLine("        Error Setting Last Value " + inObject.Fields.get_Field(fieldNum).Name);

                                        }
                                    //}
                                }
                            }

                        }

                     }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem in setup." + System.Environment.NewLine + "Message:" + ex.Message, "Attribute Assistant Message");
                AAState.WriteLine("Error in setup");
            }
            finally
            {
                if (AAState._tab == inObject.Class)
                {
                    AAState.reInitExt();

                }
                if (progressDialog != null)
                {
                    progressDialog.HideDialog();
                }
                AAState.WriteLine("DONE");
                AAState.WriteLine("---------------------------------------");
                if (fCursor != null)
                {
                    Marshal.ReleaseComObject(fCursor);
                    GC.Collect(300);
                    GC.WaitForFullGCComplete();
                }
            }

        }

        //Returns rows from defaults table which match this object's table name or wildcard
        private void getDefaultRows(ref IObject inObject, out ICursor outCursor)
        {
            outCursor = null;
            try
            {


                AAState._gentab = Globals.FindTable(ArcMap.Document.FocusMap, AAState._sequenceTableName);

                ITable tab = Globals.FindTable(ArcMap.Document.FocusMap, AAState._defaultsTableName);
                //ICursor tabCursor = null;
                if (tab != null)
                {
                    IDataset dataset = inObject.Class as IDataset;
                    IQueryFilter qFilter = new QueryFilterClass();
                    IQueryFilterDefinition qFilterDef = qFilter as IQueryFilterDefinition;
                    qFilterDef.PostfixClause = "ORDERBY TABLENAME";
                    string[] items = dataset.Name.Split('.');
                    string name = items[items.GetLength(0) - 1];
                    qFilter.WhereClause = "TABLENAME = '" + name + "' or TABLENAME = '*'";
                    outCursor = tab.Search(qFilter, true) as ICursor;
                }
                else
                {
                    outCursor = null;
                }
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("getDefaultRows: " + ex.Message);

            }
        }

        private void getView(DataTable dt)
        {
            try
            {
                DataView view = new DataView(dt);
                foreach (DataRowView drv in view)
                {
                    Console.WriteLine(drv["OBJECTID"].ToString());
                }
                view.RowFilter = "ValData = 'Evy'";
                foreach (DataRowView drv in view)
                {
                    Console.WriteLine(drv["OBJECTID"].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetView: " + ex.Message);
            }
        }

        //Creates lastValueProperties Property Set which stores fields and values for last value method
        //private void constructFieldArray()
        //{
        //    ITable tab = getTable(_defaultsTableName);

        //    IDataset dataset = tab as IDataset;
        //    IQueryFilter qFilter = new QueryFilterClass();
        //    qFilter.WhereClause = "ValueMethod = 'LAST_VALUE'";

        //    lastValueProperties = new PropertySetClass();

        //    if (tab != null)
        //    {
        //        ICursor tabCursor = tab.Search(qFilter, true);
        //        IRow row;
        //        while (((row = tabCursor.NextRow()) != null))
        //        {
        //            object objRow = row.get_Value(dynTargetField);
        //            object nullObject = null;
        //            lastValueProperties.SetProperty(objRow.ToString(), nullObject);
        //        }
        //        System.Runtime.InteropServices.Marshal.ReleaseComObject(tabCursor);
        //    }

        //}
        public static void TransformDataXYToPixelXY(IRaster pRaster, ref double dblX, ref double dblY)
        {// notes:
            // assume LOWER left origin for projected coordinate system
            // origin is UPPER left for raster pixels

            // Function usage: dblX and dblY are passed in in data coords and passed out in pixel coords

            IRasterProps pRasterProps;
            pRasterProps = (IRasterProps)pRaster;

            // get cell size dimensions
            IPnt pPnt;
            pPnt = new PntClass();
            pPnt = pRasterProps.MeanCellSize();

            double dblCellSizeX;
            double dblCellSizeY;
            dblCellSizeX = pPnt.X;
            dblCellSizeY = pPnt.Y;

            double dblPixelX;
            double dblPixelY;

            IEnvelope pEnv;
            pEnv = new EnvelopeClass();
            pEnv = pRasterProps.Extent;

            double dblDataX;
            dblDataX = dblX;

            double dblDataY;
            dblDataY = dblY;

            // X
            dblPixelX = (dblDataX - pEnv.XMin) / dblCellSizeX;

            // Y (taking care of different origins)
            double dblHeight;
            dblHeight = pRasterProps.Height;
            dblPixelY = dblHeight - ((dblDataY - pEnv.YMin) / dblCellSizeY);

            // make sure I don't get anything larger than height/width or less than 0
            double dblWidth;
            dblWidth = pRasterProps.Width;

            if (dblPixelX > dblWidth)
                dblPixelX = dblWidth;
            else if (dblPixelX < 0)
                dblPixelX = 0;

            if (dblPixelY > dblHeight)
                dblPixelY = dblHeight;
            else if (dblPixelY < 0)
                dblPixelY = 0;


            // return transformed coordinates
            dblX = dblPixelX;
            dblY = dblPixelY;

        }
        public string GetCellValue(string layerName, IPoint pLoc, IMap pMap)
        {

            try
            {

                IRasterLayer pLayer = null;
                double dblX = pLoc.X;
                double dblY = pLoc.Y;


                pLayer = GetRasterLayer(pMap, layerName);
                if (pLayer == null) return "";

                TransformDataXYToPixelXY(pLayer.Raster, ref dblX, ref dblY);

                IPnt pBlockSize = new DblPnt();
                pBlockSize.SetCoords(1.0, 1.0);

                IPixelBlock pPixelBlock;
                pPixelBlock = pLayer.Raster.CreatePixelBlock(pBlockSize);

                IPnt pPixel;
                pPixel = new DblPnt();
                pPixel.X = dblX;
                pPixel.Y = dblY;

                pLayer.Raster.Read(pPixel, pPixelBlock);
                object vValue;

                for (int j = 0; j < pPixelBlock.Planes; j++)
                {
                    vValue = pPixelBlock.GetVal(j, 0, 0);

                    if (vValue.ToString() != "No Raster")
                        return vValue.ToString(); //= sPixelVals + ", ";

                }
                //if (sPixelVals != "No Raster") 
                //      return "";
                //else
                return "No Raster";

            }
            catch (Exception ex)
            {
                MessageBox.Show("GetView: " + ex.Message);
                return "No Raster";
            }
        }
        public string GetCellValueOrig_MayNotBeRight(string layerName, IPoint pLoc, IMap pMap)
        {

            try
            {
                IPnt pBlockSize = new DblPnt();

                pBlockSize.SetCoords(1.0, 1.0);


                IRasterLayer pLayer = null;
                IPixelBlock pPixelBlock;
                object vValue;

                //   string sPixelVals;
                // sPixelVals = "No Raster";
                IRasterProps pRasterProps;
                double dXSize;
                double dYSize;
                IPnt pPixel;
                pPixel = new DblPnt();

                pLayer = GetRasterLayer(pMap, layerName);
                if (pLayer == null) return "";


                pPixelBlock = pLayer.Raster.CreatePixelBlock(pBlockSize);
                pRasterProps = pLayer.Raster as IRasterProps;
                dXSize = pRasterProps.Extent.XMax - pRasterProps.Extent.XMin;
                dYSize = pRasterProps.Extent.YMax - pRasterProps.Extent.YMin;

                dXSize = dXSize / pRasterProps.Width;
                dYSize = dYSize / pRasterProps.Height;

                pPixel.X = (pLoc.X - pRasterProps.Extent.XMin) / dXSize;
                pPixel.Y = (pRasterProps.Extent.YMax - pLoc.Y) / dYSize;

                pLayer.Raster.Read(pPixel, pPixelBlock);

                for (int j = 0; j < pPixelBlock.Planes; j++)
                {
                    vValue = pPixelBlock.GetVal(j, 0, 0);

                    if (vValue.ToString() != "No Raster")
                        return vValue.ToString(); //= sPixelVals + ", ";

                }
                //if (sPixelVals != "No Raster") 
                //      return "";
                //else
                return "No Raster";

            }
            catch (Exception ex)
            {
                MessageBox.Show("GetView: " + ex.Message);
                return "No Raster";
            }
        }
        public IRasterLayer GetRasterLayer(IMap map, string layerName)
        {
            try
            {
                if (layerName == "")
                    return null;
                //Get list of geofeature layers in the map
                UID geoRasterLayerID = new UIDClass();
                geoRasterLayerID.Value = "{D02371C7-35F7-11D2-B1F2-00C04F8EDEFF}";
                IEnumLayer enumLayer = map.get_Layers(((ESRI.ArcGIS.esriSystem.UID)geoRasterLayerID), true);

                // Step through each geofeature layer in the map
                enumLayer.Reset();
                IRasterLayer layer = enumLayer.Next() as IRasterLayer;
                //IRasterProps pRastProp = layer.raster as IRasterProps;

                while (layer != null)
                {
                    if (layer.Valid)
                    {


                        if ((layer.Name).ToUpper() == (layerName).ToUpper())
                        {
                            return layer;

                        }
                        // MessageBox.Show(layer.FilePath);

                    }
                    layer = enumLayer.Next() as IRasterLayer;
                }
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetView: " + ex.Message);
                return null;
            }
        }
        //Creates lastValueProperties Property Set which stores fields and values for last value method


        #endregion
    }
}





