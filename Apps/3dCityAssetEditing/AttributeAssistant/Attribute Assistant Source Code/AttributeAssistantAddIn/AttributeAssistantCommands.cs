using System;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using System.Reflection;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using A4LGSharedFunctions;


namespace ArcGIS4LocalGovernment
{
    public class AttributeAssistantLoadLastValue : ESRI.ArcGIS.Desktop.AddIns.Button
    {

        public AttributeAssistantLoadLastValue()
        {


        }

        protected override void OnClick()
        {

                //AAState. = false;

            AAState.promptLastValueProperrtySet();

        }
        protected override void Dispose(bool value)
        {

            base.Dispose(value);

        }
        protected override void OnUpdate()
        {

        }


    }
    public class AttributeAssistantToggleCommand : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        private IEditor m_Editor;

        public AttributeAssistantToggleCommand()
        {
            m_Editor = Globals.getEditor(ArcMap.Application);
            AAState.setIcon();

        }

        protected override void OnClick()
        {

            if (AAState._PerformUpdates)
            {
                AAState._PerformUpdates = false;
                AAState.unInitEditing();
            }
            else
            {
                AAState._PerformUpdates = true;
                AAState.initEditing();
            }
            //if (m_Editor == null)
            //{

            //    return;
            //}
            //if (m_Editor.EditState == esriEditState.esriStateEditing)
            //{
            //    MessageBox.Show("Please stop editing before toggling the extension");
            //    return;
            //}

            //if (AAState.initTable() == false)
            //{
            //    MessageBox.Show("The required tables are missing\r\n" + "Dynamic Value Table: " + AAState._defaultsTableName + "\r\n" + "Generate ID Table: " + AAState._sequenceTableName);
            //    AAState._PerformUpdates = false;

            //    AAState.setIcon();

            //    return;
            //}
            ////Check orginal state
            //bool origState = AAState._PerformUpdates;

            ////Perform work
            //AAState._PerformUpdates = !origState;

            //AAState.setIcon();
            AAState.setIcon();

        }
        protected override void Dispose(bool value)
        {
            m_Editor = null;
            base.Dispose(value);

        }
        protected override void OnUpdate()
        {
            //if (m_Editor == null)
            //{
            //    Enabled = false;
            //    return;
            //}
            //if (m_Editor.EditState == esriEditState.esriStateEditing)
            //{
            //    Enabled = false;
            //    return;
            //}
            //Enabled = true;
        }


    }
    public class AttributeAssistantSuspendCommand : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        
        public AttributeAssistantSuspendCommand()
        {
          

        }

        protected override void OnClick()
        {

            if (AAState._Suspend)
            {
                AAState._Suspend = false;
            }
            else
            {
                AAState._Suspend = true;
            }
           

        }
        protected override void Dispose(bool value)
        {
         
            base.Dispose(value);

        }
        protected override void OnUpdate()
        {
           
        }


    }
    public class AttributeAssistantSuspendOnCommand : ESRI.ArcGIS.Desktop.AddIns.Button
    {

        public AttributeAssistantSuspendOnCommand()
        {


        }

        protected override void OnClick()
        {

                AAState._Suspend = false;


        }
        protected override void Dispose(bool value)
        {
            base.Dispose(value);

        }
        protected override void OnUpdate()
        {

        }


    }
    public class AttributeAssistantSuspendOffCommand : ESRI.ArcGIS.Desktop.AddIns.Button
    {
     
        public AttributeAssistantSuspendOffCommand()
        {


        }

        protected override void OnClick()
        {

                AAState._Suspend = true;
     

        }
        protected override void Dispose(bool value)
        {
            base.Dispose(value);

        }
        protected override void OnUpdate()
        {

        }


    }
    public class RunChangeRulesCommand : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        private IEditor _editor;

        public RunChangeRulesCommand()
        {


            UID editorUID = new UIDClass();
            editorUID.Value = "esriEditor.editor";
            _editor = ArcMap.Application.FindExtensionByCLSID(editorUID) as IEditor;

            if (_editor != null)
            {

                return;
            }




        }

        protected override void OnClick()
        {
            if (AAState._PerformUpdates == false)
            {
                MessageBox.Show("Please turn on the attribute assistant before using this tool");
                return;

            }
            if (_editor.EditState == esriEditState.esriStateNotEditing)
            {
                MessageBox.Show("Please start editing to run this tool");
                return;

            }
            RunChangeRules();
        }

        protected override void OnUpdate()
        {

            if (AAState._PerformUpdates == false)
            {
                Enabled = false;

            }
            else
            {
                Enabled = true;
            }

        }
        //public void RunChangeRules()
        //{
        //    ICursor cursor = null; IFeatureCursor fCursor = null;


        //    try
        //    {
        //        //Get list of editable layers
        //        IEditor editor = _editor;
        //        IEditLayers eLayers = (IEditLayers)editor;
        //        long lastOID = -1;

        //        if (_editor.EditState != esriEditState.esriStateEditing)
        //            return;

        //        IMap map = editor.Map;
        //        IActiveView activeView = map as IActiveView;
        //        editor.StartOperation();

        //        if (map.SelectionCount > 0)
        //        {
        //            //If above threshold, prompt to cancel
        //            if ((map.SelectionCount > 1) &&
        //               (MessageBox.Show("Are you sure you wish to apply attribute assistant change rules for the selected " + map.SelectionCount + " features?", "Confirm", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No))
        //                return;

        //            bool test = false;

        //            //Get list of feature layers
        //            UID geoFeatureLayerID = new UIDClass();
        //            geoFeatureLayerID.Value = "{E156D7E5-22AF-11D3-9F99-00C04F6BC78E}";
        //            IEnumLayer enumLayer = map.get_Layers(((ESRI.ArcGIS.esriSystem.UID)geoFeatureLayerID), true);
        //            IFeatureLayer fLayer;
        //            IFeatureSelection fSel;
        //            ILayer layer;
        //            // Step through each geofeature layer in the map
        //            enumLayer.Reset();
        //            // Create an edit operation enabling undo/redo

        //            while ((layer = enumLayer.Next()) != null)
        //            {
        //                // Verify that this is a valid, visible layer and that this layer is editable
        //                fLayer = (IFeatureLayer)layer;
        //                if (fLayer.Valid && fLayer.Visible && eLayers.IsEditable(fLayer))
        //                {
        //                    // Verify that this layer has selected features  
        //                    IFeatureClass fc = fLayer.FeatureClass;
        //                    fSel = (IFeatureSelection)fLayer;

        //                    if ((fc != null) && (fSel.SelectionSet.Count > 0))
        //                    {
        //                        try
        //                        {

        //                            test = true;

        //                            fSel.SelectionSet.Search(null, false, out cursor);
        //                            fCursor = cursor as IFeatureCursor;
        //                            IFeature feat;
        //                            while ((feat = (IFeature)fCursor.NextFeature()) != null)
        //                            {
        //                                lastOID = feat.OID;
        //                                AAState.FeatureChange(feat as IObject);
        //                                //for (int i = 0; i < feat.Fields.FieldCount - 1; i++)
        //                                //{
        //                                //    if (feat.Fields.get_Field(i).Editable && fc.ShapeFieldName != feat.Fields.get_Field(i).Name)
        //                                //    {
        //                                //        feat.set_Value(i, feat.get_Value(i));
        //                                //        feat.Store();
        //                                //        break;
        //                                //    }
        //                                //}

        //                            }
        //                            if (feat != null)
        //                                Marshal.ReleaseComObject(feat);

        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            editor.AbortOperation();
        //                            MessageBox.Show(ex.Message + " \n" + fLayer.Name + ": " + lastOID, ex.Source);
        //                            return;
        //                        }


        //                    }

        //                }



        //            }




        //            //Alert the user know if no work was performed
        //            if (!(test))
        //                MessageBox.Show("None of the editable layers have selected features.", "No changes were made.");
        //            else
        //            {
        //                activeView.Refresh();
        //            }

        //        }
        //        IStandaloneTable stTable;

        //        ITableSelection tableSel;

        //        IStandaloneTableCollection stTableColl = (IStandaloneTableCollection)map;
        //        long count = stTableColl.StandaloneTableCount;
        //        if (count > 0)
        //        {
        //            for (int i = 0; i < count; i++)
        //            {

        //                stTable = stTableColl.get_StandaloneTable(i);
        //                tableSel = (ITableSelection)stTable;
        //                if (tableSel.SelectionSet.Count > 0)
        //                {
        //                    tableSel.SelectionSet.Search(null, false, out  cursor);
        //                    IRow pRow;
        //                    while ((pRow = (IRow)cursor.NextRow()) != null)
        //                    {
        //                        lastOID = pRow.OID;
        //                        AAState.FeatureChange(pRow as IObject);

        //                        //for (i = 0; i < pRow.Fields.FieldCount - 1; i++)
        //                        //{
        //                        //    if (pRow.Fields.get_Field(i).Editable)
        //                        //    {
        //                        //        pRow.set_Value(i, pRow.get_Value(i));
        //                        //        pRow.Store();
        //                        //        break;
        //                        //    }
        //                        //}

        //                    }
        //                    if (pRow != null)
        //                        Marshal.ReleaseComObject(pRow);


        //                }
        //            }

        //        }
        //        // Stop the edit operation 
        //        editor.StopOperation("Run Change Rules");

        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message + " \n" + "RunChangeRules", ex.Source);

        //        return;
        //    }
        //    finally
        //    {
        //        if (cursor != null)
        //            Marshal.ReleaseComObject(cursor);
        //        if (fCursor != null)
        //            Marshal.ReleaseComObject(fCursor);

        //    }
        //}
        public void RunChangeRules()
        {
            ICursor cursor = null; IFeatureCursor fCursor = null;
            bool ran = false;

            try
            {
                //Get list of editable layers
                IEditor editor = _editor;
                IEditLayers eLayers = (IEditLayers)editor;
                long lastOID = -1;
                string lastLay = "";

                if (_editor.EditState != esriEditState.esriStateEditing)
                    return;

                IMap map = editor.Map;
                IActiveView activeView = map as IActiveView;


                IStandaloneTable stTable;

                ITableSelection tableSel;

                IStandaloneTableCollection stTableColl = (IStandaloneTableCollection)map;

                long rowCount = stTableColl.StandaloneTableCount;
                long rowSelCount = 0;
                for (int i = 0; i < stTableColl.StandaloneTableCount; i++)
                {

                    stTable = stTableColl.get_StandaloneTable(i);
                    tableSel = (ITableSelection)stTable;
                    if (tableSel.SelectionSet != null)
                    {
                        rowSelCount = rowSelCount + tableSel.SelectionSet.Count;
                    }
                    

                }
                long featCount = map.SelectionCount;
                int totalCount = (Convert.ToInt32(rowSelCount) + Convert.ToInt32(featCount));


                if (totalCount >= 1)
                {

                    if (MessageBox.Show("Are you sure you wish to apply attribute assistant Change rules for the selected " + totalCount + " rows and features?",
                        "Confirm", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                      
                        ran = true;


                        editor.StartOperation();

                        //bool test = false;

                        //Get list of feature layers
                        UID geoFeatureLayerID = new UIDClass();
                        geoFeatureLayerID.Value = "{E156D7E5-22AF-11D3-9F99-00C04F6BC78E}";
                        IEnumLayer enumLayer = map.get_Layers(((ESRI.ArcGIS.esriSystem.UID)geoFeatureLayerID), true);
                        IFeatureLayer fLayer;
                        IFeatureSelection fSel;
                        ILayer layer;
                        // Step through each geofeature layer in the map
                        enumLayer.Reset();
                        // Create an edit operation enabling undo/redo
                        try
                        {
                         //   AAState.StopChangeMonitor();
                            while ((layer = enumLayer.Next()) != null)
                            {
                                // Verify that this is a valid, visible layer and that this layer is editable
                                fLayer = (IFeatureLayer)layer;
                                if (fLayer.Valid && eLayers.IsEditable(fLayer))//fLayer.Visible &&
                                {
                                    // Verify that this layer has selected features  
                                    IFeatureClass fc = fLayer.FeatureClass;
                                    fSel = (IFeatureSelection)fLayer;

                                    if ((fc != null) && (fSel.SelectionSet.Count > 0))
                                    {

                                        // test = true;

                                        //fSel.SelectionSet.Search(null, false, out cursor);
                                        //fCursor = cursor as IFeatureCursor;
                                        //IFeature feat;
                                        //while ((feat = (IFeature)fCursor.NextFeature()) != null)
                                        //{
                                        //    lastLay = fLayer.Name;


                                        //    lastOID = feat.OID;
                                        //    AAState.FeatureChange(feat as IObject);
                                        //    feat.Store();


                                        //}

                                        fSel.SelectionSet.Search(null, false, out cursor);
                                        fCursor = cursor as IFeatureCursor;
                                        IFeature feat;
                                        while ((feat = (IFeature)fCursor.NextFeature()) != null)
                                        {
                                            for (int i = 0; i < feat.Fields.FieldCount - 1; i++)
                                            {
                                                if (feat.Fields.get_Field(i).Editable && fc.ShapeFieldName != feat.Fields.get_Field(i).Name)
                                                {
                                                    feat.set_Value(i, feat.get_Value(i));
                                                    feat.Store();
                                                    break;
                                                }
                                            }

                                        }
                                        if (feat != null)
                                            Marshal.ReleaseComObject(feat);



                                    }

                                }

                            }
                           
                        }
                        catch (Exception ex)
                        {
                            editor.AbortOperation();
                            ran = false;
                            MessageBox.Show("RunChangeRule\n" + ex.Message + " \n" + lastLay + ": " + lastOID, ex.Source);
                            return;
                        }
                        finally
                        {
                          //  AAState.StartChangeMonitor();
                            try
                            {
                                // Stop the edit operation 
                                editor.StopOperation("Run Change Rules - Features");
                            }
                            catch (Exception ex)
                            { }

                        }







                        editor.StartOperation();
                        try
                        {
                            AAState.StopChangeMonitor();
                            for (int i = 0; i < stTableColl.StandaloneTableCount; i++)
                            {

                                stTable = stTableColl.get_StandaloneTable(i);
                                tableSel = (ITableSelection)stTable;
                                if (tableSel.SelectionSet != null)
                                {

                                    if (tableSel.SelectionSet.Count > 0)
                                    {
                                        tableSel.SelectionSet.Search(null, false, out  cursor);
                                        IRow pRow;
                                        while ((pRow = (IRow)cursor.NextRow()) != null)
                                        {
                                            lastOID = pRow.OID;
                                            lastLay = stTable.Name;
                                            IObject pObj = pRow as IObject;
                                            AAState.FeatureChange( pObj);
                                            pRow.Store();

                                        }
                                        if (pRow != null)
                                            Marshal.ReleaseComObject(pRow);


                                    }
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            editor.AbortOperation();
                            MessageBox.Show("RunChangeRules\n" + ex.Message + " \n" + lastLay + ": " + lastOID, ex.Source);
                            ran = false;

                            return;
                        }
                        finally
                        {
                            AAState.StartChangeMonitor();
                            try
                            {
                                // Stop the edit operation 
                                editor.StopOperation("Run Change Rules - Features");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("ERROR TURNING ON THE AA, Restart ArcMap");

                            }

                        }

                    }

                }
                else
                {
                    MessageBox.Show("Please select some features or rows to run this process.");

                }

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " \n" + "RunChangeRules", ex.Source);
                ran = false;

                return;
            }
            finally
            {
                if (ran)
                    MessageBox.Show("Process has completed successfully");

                if (cursor != null)
                    Marshal.ReleaseComObject(cursor);
                if (fCursor != null)
                    Marshal.ReleaseComObject(fCursor);

            }
        }


    }
    public class RunManualRulesCommand : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        private IEditor _editor;

        public RunManualRulesCommand()
        {


            UID editorUID = new UIDClass();
            editorUID.Value = "esriEditor.editor";
            _editor = ArcMap.Application.FindExtensionByCLSID(editorUID) as IEditor;

            if (_editor != null)
            {

                return;
            }




        }

        protected override void OnClick()
        {
            if (AAState._PerformUpdates == false)
            {
                MessageBox.Show("Please turn on the attribute assistant before using this tool");
                return;

            }
            if (_editor.EditState == esriEditState.esriStateNotEditing)
            {
                MessageBox.Show("Please start editing to run this tool");
                return;

            }
            RunManualRules();
        }

        protected override void OnUpdate()
        {

            if (AAState._PerformUpdates == false)
            {
                Enabled = false;

            }
            else
            {
                Enabled = true;
            }

        }
        public void RunManualRules()
        {
            ICursor cursor = null; IFeatureCursor fCursor = null;
            bool ran = false;

            try
            {
                //Get list of editable layers
                IEditor editor = _editor;
                IEditLayers eLayers = (IEditLayers)editor;
                long lastOID = -1;
                string lastLay = "";

                if (_editor.EditState != esriEditState.esriStateEditing)
                    return;

                IMap map = editor.Map;
                IActiveView activeView = map as IActiveView;


                IStandaloneTable stTable;

                ITableSelection tableSel;

                IStandaloneTableCollection stTableColl = (IStandaloneTableCollection)map;

                long rowCount = stTableColl.StandaloneTableCount;
                long rowSelCount = 0;
                for (int i = 0; i < stTableColl.StandaloneTableCount; i++)
                {

                    stTable = stTableColl.get_StandaloneTable(i);
                    tableSel = (ITableSelection)stTable;
                    if (tableSel.SelectionSet != null)
                    {
                        rowSelCount = rowSelCount + tableSel.SelectionSet.Count;
                    }

                }
                long featCount = map.SelectionCount;
                int totalCount = (Convert.ToInt32(rowSelCount) + Convert.ToInt32(featCount));


                if (totalCount >= 1)
                {
                    editor.StartOperation();

                    if (MessageBox.Show("Are you sure you wish to apply attribute assistant manual rules for the selected " + totalCount + " rows and features?",
                        "Confirm", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        ran = true;

                 
                        //bool test = false;

                        //Get list of feature layers
                        UID geoFeatureLayerID = new UIDClass();
                        geoFeatureLayerID.Value = "{E156D7E5-22AF-11D3-9F99-00C04F6BC78E}";
                        IEnumLayer enumLayer = map.get_Layers(((ESRI.ArcGIS.esriSystem.UID)geoFeatureLayerID), true);
                        IFeatureLayer fLayer;
                        IFeatureSelection fSel;
                        ILayer layer;
                        // Step through each geofeature layer in the map
                        enumLayer.Reset();
                        // Create an edit operation enabling undo/redo
                        try
                        {
                            while ((layer = enumLayer.Next()) != null)
                            {
                                // Verify that this is a valid, visible layer and that this layer is editable
                                fLayer = (IFeatureLayer)layer;
                                if (fLayer.Valid && eLayers.IsEditable(fLayer))//fLayer.Visible &&
                                {
                                    // Verify that this layer has selected features  
                                    IFeatureClass fc = fLayer.FeatureClass;
                                    fSel = (IFeatureSelection)fLayer;

                                    if ((fc != null) && (fSel.SelectionSet.Count > 0))
                                    {

                                        // test = true;

                                        fSel.SelectionSet.Search(null, false, out cursor);
                                        fCursor = cursor as IFeatureCursor;
                                        IFeature feat;
                                        while ((feat = (IFeature)fCursor.NextFeature()) != null)
                                        {
                                            lastLay = fLayer.Name;


                                            lastOID = feat.OID;
                                            IObject pObj = feat as IObject;

                                            AAState.FeatureManual(pObj);
                                            feat.Store();

                                        }
                                        if (feat != null)
                                            Marshal.ReleaseComObject(feat);



                                    }

                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            editor.AbortOperation();
                            ran = false;
                            MessageBox.Show("RunManualRules\n" + ex.Message + " \n" + lastLay + ": " + lastOID, ex.Source);
                            return;
                        }
                        finally
                        {
                            //try
                            //{
                            //    // Stop the edit operation 
                            //    editor.StopOperation("Run Manual Rules - Features");
                            //}
                            //catch (Exception ex)
                            //{ }

                        }







                    
                        try
                        {
                            for (int i = 0; i < stTableColl.StandaloneTableCount; i++)
                            {

                                stTable = stTableColl.get_StandaloneTable(i);
                                tableSel = (ITableSelection)stTable;
                                if (tableSel.SelectionSet != null)
                                {

                                    if (tableSel.SelectionSet.Count > 0)
                                    {
                                        tableSel.SelectionSet.Search(null, false, out  cursor);
                                        IRow pRow;
                                        while ((pRow = (IRow)cursor.NextRow()) != null)
                                        {
                                            lastOID = pRow.OID;
                                            lastLay = stTable.Name;
                                            IObject pObj = pRow as IObject;
                                            AAState.FeatureManual( pObj);
                                            pRow.Store();

                                        }
                                        if (pRow != null)
                                            Marshal.ReleaseComObject(pRow);


                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            editor.AbortOperation();
                            MessageBox.Show("RunManualRules\n" + ex.Message + " \n" + lastLay + ": " + lastOID, ex.Source);
                            ran = false;

                            return;
                        }
                        finally
                        {
                            //try
                            //{
                            //     Stop the edit operation 
                            //    editor.StopOperation("Run Manual Rules - Features");
                            //}
                            //catch (Exception ex)
                            //{ }

                        }
                        try
                        {
                            editor.StopOperation("Run Manual Rules - Features");

                        }
                        catch
                        {


                        }
                    }

                }
                else
                {
                    MessageBox.Show("Please select some features or rows to run this process.");
                    
                }

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " \n" + "RunManualRules", ex.Source);
                ran = false;

                return;
            }
            finally
            {
                if (ran)
                    MessageBox.Show("Process has completed successfully");

                if (cursor != null)
                    Marshal.ReleaseComObject(cursor);
                if (fCursor != null)
                    Marshal.ReleaseComObject(fCursor);

            }
        }

    }
}