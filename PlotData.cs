using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;
using System.Reflection;
using System.IO;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using Plot_iNET_X.Analyser_Logic;


using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Plot_iNET_X
{
public partial class PlotData : Form
{

    private List<int> streamID;
    private byte[] frame=new byte[65536];
    private Dictionary<int, Dictionary<string, List<double>>> streamData;
    private Dictionary<int, Dictionary<string, FilteredPointList>> dataToPlot;
    private double[] singleData = null;
    private Dictionary<int, List<string>> channelsSelected;
    private UInt64 startPTP;

    private List<byte[]> frames;

    public PlotData()
    {
        InitializeComponent();
    }
    public PlotData(double[] data, List<int> streamlist)
    {
        #region Initialize_Globals
        try
        {
            streamID = streamlist;// new List<int> { 300 };
            channelsSelected = new Dictionary<int, List<string>>(Globals.channelsSelected);
            streamData = new Dictionary<int, Dictionary<string, List<double>>>(streamID.Count);
            dataToPlot = new Dictionary<int, Dictionary<string, FilteredPointList>>(streamID.Count);
            Globals.parError = new Dictionary<int, Dictionary<string, uint>>();
            Globals.totalErrors = 0;
            Globals.packetErrors = new Dictionary<int, Dictionary<string, uint>>();
            Globals.framesReceived = new Dictionary<int, int[]>();
            Globals.totalFrames = 0;
            foreach (int stream in Globals.limitPCAP.Keys)
            {
                dataToPlot[stream] = new Dictionary<string, FilteredPointList>();
                Globals.packetErrors[stream] = new Dictionary<string, uint>(){
            {"total",0}, //used for all parameters in that stream
            {"pktLost",0},
            {"SEQ",0}};
                Globals.parError[stream] = new Dictionary<string, uint>();
                Globals.framesReceived[stream] = new int[4] { 0, 0, 0, 0 };
                foreach (string parName in Globals.limitPCAP[stream].Keys)
                {
                    Globals.parError[stream][parName] = 0;
                }

                if (channelsSelected[stream].Count != 0)
                {
                    streamData[stream] = new Dictionary<string, List<double>>();
                    foreach (string parName in channelsSelected[stream])
                    {
                        streamData[stream][parName] = new List<double>();
                    }
                }

            }
        }
        catch (Exception exInit)
        {
            MessageBox.Show(String.Format("Crashed during globals initialization\nRead the rest of the crash report below\n\n\n{0}",
                            exInit.ToString()), "Packet parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion Initialize_Globals
     
        InitializeComponent();        
        singleData = data;
        zedGraphControl1.ContextMenuBuilder += new ZedGraphControl.ContextMenuBuilderEventHandler(Add_item_toMenu);
        zedGraphControl1.GraphPane.IsFontsScaled = false;
    }
    public PlotData(List<int> streamInput)
    {
    #region Initialize_Globals
        try
        {
            streamID = streamInput;
            this.channelsSelected = new Dictionary<int,List<string>>(Globals.channelsSelected);
            streamData = new Dictionary<int, Dictionary<string, List<double>>>(streamID.Count);
            dataToPlot = new Dictionary<int, Dictionary<string, FilteredPointList>>(streamID.Count);
            Globals.parError = new Dictionary<int, Dictionary<string, uint>>();
            Globals.totalErrors = 0;
            Globals.packetErrors = new Dictionary<int, Dictionary<string, uint>>();
            Globals.framesReceived = new Dictionary<int, int[]>();
            Globals.totalFrames = 0;
            int cntChannels=0;
            foreach (int stream in Globals.limitPCAP.Keys)
            {
                dataToPlot[stream] = new Dictionary<string, FilteredPointList>();
                Globals.packetErrors[stream] = new Dictionary<string, uint>(){
            {"total",0}, //used for all parameters in that stream
            {"pktLost",0},
            {"SEQ",0}};
                Globals.parError[stream] = new Dictionary<string, uint>();
                Globals.framesReceived[stream] = new int[4] { 0, 0, 0, 0 };
                foreach (string parName in Globals.limitPCAP[stream].Keys)
                {
                    Globals.parError[stream][parName] = 0;
                }

                if (channelsSelected[stream].Count != 0)
                {
                    streamData[stream] = new Dictionary<string, List<double>>();
                    foreach (string parName in channelsSelected[stream])
                    {
                        streamData[stream][parName] = new List<double>();//1000000);
                        cntChannels++;
                    }
                }

            }
            Globals.fileDump_list = new string[cntChannels];
            foreach(int stream in channelsSelected.Keys)
            {
                if (channelsSelected[stream].Count != 0)
                {
                    foreach (string parName in channelsSelected[stream])
                    {
                        Globals.fileDump_list[--cntChannels] = String.Format(@"{0}\{1}_{2}.dat", Globals.fileDump, stream, parName);
                    }
                }
            }
                
        }
        catch (Exception exInit)
        {
            MessageBox.Show(String.Format("Crashed during globals initialization\nRead the rest of the crash report below\n\n\n{0}",
                            exInit.ToString()), "Packet parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

    #endregion Initialize_Globals
            
        InitializeComponent();
        zedGraphControl1.ContextMenuBuilder += new ZedGraphControl.ContextMenuBuilderEventHandler(Add_item_toMenu);
        zedGraphControl1.GraphPane.IsFontsScaled = false;
    }

    public PlotData(List<int> streamInput, string option)
    {
        if (option != "dumpFile")
        {
            MessageBox.Show(String.Format("wrong option {0}", option));
            return;
        }
        #region Initialize_Globals
        try
        {
            this.streamID = streamInput;
            this.channelsSelected = new Dictionary<int, List<string>>(Globals.channelsSelected);
            this.streamData = new Dictionary<int, Dictionary<string, List<double>>>(streamID.Count);
            this.dataToPlot = new Dictionary<int, Dictionary<string, FilteredPointList>>(streamID.Count);
            Globals.parError = new Dictionary<int, Dictionary<string, uint>>();
            Globals.totalErrors = 0;
            Globals.packetErrors = new Dictionary<int, Dictionary<string, uint>>();
            Globals.framesReceived = new Dictionary<int, int[]>();
            Globals.totalFrames = 0;
            foreach (int stream in Globals.limitPCAP.Keys)
            {
                dataToPlot[stream] = new Dictionary<string, FilteredPointList>();
                Globals.packetErrors[stream] = new Dictionary<string, uint>(){
            {"total",0}, //used for all parameters in that stream
            {"pktLost",0},
            {"SEQ",0}};
                Globals.parError[stream] = new Dictionary<string, uint>();
                Globals.framesReceived[stream] = new int[4] { 0, 0, 0, 0 };
                foreach (string parName in Globals.limitPCAP[stream].Keys)
                {
                    Globals.parError[stream][parName] = 0;
                }

                if (channelsSelected[stream].Count != 0)
                {
                    streamData[stream] = new Dictionary<string, List<double>>();
                    foreach (string parName in channelsSelected[stream])
                    {
                        streamData[stream][parName] = new List<double>();
                    }
                }

            }
        }
        catch (Exception exInit)
        {
            MessageBox.Show(String.Format("Crashed during globals initialization\nRead the rest of the crash report below\n\n\n{0}",
                            exInit.ToString()), "Packet parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion Initialize_Globals

        InitializeComponent();
        zedGraphControl1.ContextMenuBuilder += new ZedGraphControl.ContextMenuBuilderEventHandler(Add_item_toMenu);
        zedGraphControl1.GraphPane.IsFontsScaled = false;
    }

    private void Form1_Resize(object sender, EventArgs e)
    {
        SetSize();
    }
    private void SetSize()
    {
        zedGraphControl1.Location = new Point(10, 10);
        // Leave a small margin around the outside of the control
        zedGraphControl1.Size = new Size(ClientRectangle.Width - 20,
                                ClientRectangle.Height - 20);
        foreach (int stream in dataToPlot.Keys)
        {
            foreach (string par in dataToPlot[stream].Keys)
            {
                dataToPlot[stream][par].SetBounds(zedGraphControl1.GraphPane.XAxis.Scale.Min, zedGraphControl1.GraphPane.XAxis.Scale.Max, (int)zedGraphControl1.GraphPane.Rect.Width);
            }
        }
    }

    #region ZedGRaph_Menu
    private void ZoomEvent(ZedGraphControl sender, ZoomState oldState, ZoomState newState)
    {
        // The maximum number of point to displayed is based on the width of the graphpane, and the visible range of the X axis
        foreach (int stream in dataToPlot.Keys)
        {
            foreach (string par in dataToPlot[stream].Keys)
            {
                dataToPlot[stream][par].SetBounds(sender.GraphPane.XAxis.Scale.Min, sender.GraphPane.XAxis.Scale.Max, (int)zedGraphControl1.GraphPane.Rect.Width);
            }
        }                    
        // This refreshes the graph when the button is released after a panning operation
        if (newState.Type == ZoomState.StateType.Pan)
            sender.Invalidate();
    }
    private void Add_item_toMenu(ZedGraphControl sender, ContextMenuStrip menuStrip, Point mousePt, ZedGraphControl.ContextMenuObjectState objState)
    {
        foreach (ToolStripMenuItem item in menuStrip.Items)
        {
            if ((string)item.Tag == "save_as")
            {
                menuStrip.Items.Remove(item);
                break;
            }
        }
        ToolStripMenuItem cm = new ToolStripMenuItem();
        cm.Tag = "hide_legend";
        cm.Name = "hide_legend";
        cm.Text = "Hide Legend";
        cm.Click += new EventHandler(HideLegend);
        menuStrip.Items.Add(cm);

        ToolStripMenuItem scroll = new ToolStripMenuItem();
        scroll.Tag = "toggle_scroll";
        scroll.Name = "toggle_scroll";
        scroll.Text = "Toggle Mouse Zoom / Scroll";
        scroll.Click += new EventHandler(ToggleZoom);
        menuStrip.Items.Add(scroll);            
    }
    private void ToggleZoom(object sender, EventArgs e)
    {
        //zedGraphControl1.ZoomButtons = MouseButtons.None;
        //zedGraphControl1.ZoomButtons2 = MouseButtons.None;
        //zedGraphControl1.ZoomStepFraction = 0;
        if (zedGraphControl1.PanModifierKeys == Keys.None)
        {
            zedGraphControl1.PanButtons = MouseButtons.Left;
            zedGraphControl1.PanModifierKeys = Keys.Control;

            zedGraphControl1.ZoomButtons = MouseButtons.Left;
            zedGraphControl1.ZoomModifierKeys = Keys.None;

        }
        else
        {
            zedGraphControl1.PanButtons = MouseButtons.Left;
            zedGraphControl1.PanModifierKeys = Keys.None;

            zedGraphControl1.ZoomButtons = MouseButtons.Left;
            zedGraphControl1.ZoomModifierKeys = Keys.Control;

        }


            //= !zedGraphControl1.IsEnableWheelZoom;
        zedGraphControl1.Refresh();
    }
    private void HideLegend(object sender, EventArgs e)
    {

        zedGraphControl1.GraphPane.Legend.IsVisible = !zedGraphControl1.GraphPane.Legend.IsVisible;
        //zedGraphControl1.GraphPane.CurveList.Clear();
        zedGraphControl1.Refresh();
    }
    #endregion ZedGRaph_Menu

    private void Form1_Load(object sender, EventArgs e)
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch sw2 = new System.Diagnostics.Stopwatch();
        sw.Start();
        //dataToPlot = new Dictionary<int, Dictionary<string, FilteredPointList>>(streamID.Count);
        if (Globals.useCompare)
        {
            sw2.Start();
            Globals.useCompare = false;
            double[] x = new double[singleData.Length];
            for(int i=0; i!=x.Length;i++)
            {
                x[i] = i;
            }
            dataToPlot[streamID.First()]["ComparedData"] = new FilteredPointList(x, singleData);
            singleData = null;
            x = null;
            sw2.Stop();
            CreateGraph(zedGraphControl1, streamID.First(), "plotCompare");
        }
        else if (Globals.filePCAP == null)
        {    
            sw2.Start();
            if (Globals.usePCAP)
            {
                if (Globals.fileSize > 1000) //save items to file if total size >1GB
                    iteratePcaps_Big();
                else
                {
                    iteratePcaps();
                }
            }
            else if(Globals.useDumpFiles)
            {
                LoadBinaryFiles();
            }
            sw2.Stop();
            foreach(int stream in streamID)
            {
                CreateGraph(zedGraphControl1, stream);
            }
        }
        else
        {
            Globals.filePCAP_list = new string[1]{Globals.filePCAP};
            sw2.Start();
            iteratePcaps();
            //iteratePcaps_Para();
            sw2.Stop();
            foreach (int stream in streamID)
            {
                CreateGraph(zedGraphControl1, stream);
            }
        }
        SetSize();
        sw.Stop();        
        if (Globals.showErrorSummary)
        {
            Thread logWindow = new Thread(() => new ErrorSummaryWindow().ShowDialog());
            logWindow.Priority = ThreadPriority.BelowNormal;
            logWindow.Name = "Error Summary Thread";
            logWindow.IsBackground = true;
            logWindow.Start();
        }
        frame = null;
        streamData = null;
        this.Text = String.Format("Total time for parsing ={0} | time to read PCAPs={1}", sw.Elapsed.ToString(), sw2.Elapsed.ToString());
        this.SuspendLayout();
        this.ResumeLayout(false);
    }



    private void LoadBinaryFiles()
    {
        foreach(int stream in streamID)
        {            
            foreach(string parName in channelsSelected[stream])
            {
                double[] y = LoadTempData(stream, parName);
                long size = y.LongLength;
                double[] x = new double[size];
                for (int e = 0; e != size;e++ )
                {
                    x[e] = e;
                }
                dataToPlot[stream][parName] = new FilteredPointList(x, y);
                x = null;
                y = null;
            }
        }
    }
    private void iteratePcaps_Big()
    {
        Dictionary<int, bool> firstTime = new Dictionary<int, bool>(streamData.Count);
        Dictionary<int,Dictionary<string, bool>>storedData = new Dictionary<int,Dictionary<string, bool>>(streamData.Count);
        foreach (int e in streamData.Keys)
        {
            firstTime[e] = true;
            storedData[e] = new Dictionary<string, bool>();
            foreach (string parName in streamData[e].Keys)
            {
                storedData[e][parName] = false;
            }
        }
        System.Diagnostics.Stopwatch save = new System.Diagnostics.Stopwatch();
        int fileCnt = Globals.filePCAP_list.Length;
        double[] dataTMP = null;
        foreach (string p in Globals.filePCAP_list)
        {
            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            //Dictionary<string, double[]> dataPcap_tmp = new Dictionary<string, double[]>();
            LogItems.addStreamInfo(String.Format("Starting to parse {0}", p));
            if (Globals.useDownsample) LoadDataDownSampled(p);
            else LoadData(p);
            foreach (int stream in streamData.Keys)
            {
                Dictionary<string, double[]> dataPcap_tmp = new Dictionary<string, double[]>();
                if (channelsSelected[stream].Count == 0) continue;
                foreach (string parName in streamData[stream].Keys)
                {
                    dataPcap_tmp[parName] = streamData[stream][parName].ToArray();
                    streamData[stream][parName].Clear();
                    //int len=0;
                    int previousLen = 0;
                    if (firstTime[stream]) previousLen = dataPcap_tmp[parName].Length;
                    else
                    {
                        if (fileCnt == 1)
                        {
                            dataTMP = LoadTempData(stream, parName);
                            if (storedData[stream][parName] == false) previousLen = dataToPlot[stream][parName].Count;
                            previousLen = previousLen + dataTMP.Length + dataPcap_tmp[parName].Length;                            
                        }
                        else if (storedData[stream][parName] == false)
                        {
                            previousLen = dataToPlot[stream][parName].Count;
                            previousLen = previousLen + dataPcap_tmp[parName].Length;
                        }
                        else previousLen = dataPcap_tmp[parName].Length;
                    }

                    uint cnt = 0;
                    double[] x = new double[previousLen];
                    double[] y = new double[previousLen];

                    /// Below Logic looks like crap
                    /// Possible errors (loosing last million in dat file) and ineficient code
                    /// TOFIX
                    if (firstTime[stream] == false)
                    {
                        if (fileCnt == 1)
                        {
                            int dataSize = dataTMP.Length;
                            //Buffer.BlockCopy(dataTMP, 0, y, dataSize, dataTMP.Length * sizeof(double));
                            //Array.Copy(dataTMP, 0, y, 0, dataTMP.Length);
                            //System.Collections.IEnumerator xEnum = dataTMP.GetEnumerator();
                            //x = Enumerable.Range(0,dataSize).Select(item => (double)item).ToArray<double>();
                            //cnt = (uint)dataSize-1;
                            for (int i = 0; i != dataSize; i++)
                            {
                                x[cnt] = cnt;
                                y[cnt] = dataTMP[i];
                                cnt++;
                            }                        
                        }
                        if (storedData[stream][parName] == false)                        
                        {
                            int dataSize = dataToPlot[stream][parName].Count;
                            for (int i = 0; i != dataSize; i++)
                            {
                                x[cnt] = cnt;
                                y[cnt] = dataToPlot[stream][parName][i].Y;
                                cnt++;
                            }
                        }
                        //previousLen = x.Length;                            
                        foreach (double dataItem in dataPcap_tmp[parName])
                        {
                            x[cnt] = cnt;
                            y[cnt] = dataItem;
                            cnt++;
                        }
                    }
                    else
                    {
                        foreach (double dataItem in dataPcap_tmp[parName])
                        {
                            x[cnt] = cnt;
                            y[cnt] = dataItem;
                            cnt++;
                        }
                    }
                    dataToPlot[stream][parName] = null;                 
                    
                    if (fileCnt==1)
                    {                        
                        dataToPlot[stream][parName] = new FilteredPointList(x, y);
                        dataTMP = null;
                    }
                    else if (cnt > 200000) //store into file if 200000 million points
                    {
                        //save.Start();
                        SaveTempData(stream, parName, y);
                        //save.Stop();
                        //MessageBox.Show(save.Elapsed.ToString());
                        //save.Restart();
                        storedData[stream][parName] = true;
                        //double[] data = LoadTempData(stream, parName);
                        //save.Stop();
                        //MessageBox.Show(save.Elapsed.ToString());
                    }
                    else
                    {
                        dataToPlot[stream][parName] = new FilteredPointList(x, y);
                        storedData[stream][parName] = false;
                    }
                    y = null;
                    //streamData[stream][parName].Clear();                    
                }
                //dataToPlot[parName] = dataTMP;
                firstTime[stream] = false;
                //streamData[stream].Clear();
            }
            fileCnt--;            
        }

    }

    private void iteratePcaps_Para()
    {
        Dictionary<int, bool> firstTime = new Dictionary<int, bool>(streamData.Count);
        Dictionary<int, Dictionary<string, bool>> storedData = new Dictionary<int, Dictionary<string, bool>>(streamData.Count);
        foreach (int e in streamData.Keys)
        {
            firstTime[e] = true;
            storedData[e] = new Dictionary<string, bool>();
            foreach (string parName in streamData[e].Keys)
            {
                storedData[e][parName] = false;
            }
        }
        System.Diagnostics.Stopwatch save = new System.Diagnostics.Stopwatch();
        int fileCnt = Globals.filePCAP_list.Length;
        double[] dataTMP = null;
        foreach (string p in Globals.filePCAP_list)
        {
            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            //Dictionary<string, double[]> dataPcap_tmp = new Dictionary<string, double[]>();
            LogItems.addStreamInfo(String.Format("Starting to parse {0}", p));
            if (Globals.useDownsample) LoadDataDownSampled(p);
            else LoadData(p);
            System.Threading.Tasks.Parallel.ForEach(streamData, streamPair =>
            {
                int stream = streamPair.Key;
                Dictionary<string, double[]> dataPcap_tmp = new Dictionary<string, double[]>();
                if (channelsSelected[stream].Count != 0)
                {
                    foreach (string parName in streamData[stream].Keys)
                    {
                        dataPcap_tmp[parName] = streamData[stream][parName].ToArray();
                        streamData[stream][parName].Clear();
                        //int len=0;
                        int previousLen = 0;
                        if (firstTime[stream]) previousLen = dataPcap_tmp[parName].Length;
                        else
                        {
                            if (fileCnt == 1)
                            {
                                dataTMP = LoadTempData(stream, parName);
                                if (storedData[stream][parName] == false) previousLen = dataToPlot[stream][parName].Count;
                                previousLen = previousLen + dataTMP.Length + dataPcap_tmp[parName].Length;
                            }
                            else if (storedData[stream][parName] == false)
                            {
                                previousLen = dataToPlot[stream][parName].Count;
                                previousLen = previousLen + dataPcap_tmp[parName].Length;
                            }
                            else previousLen = dataPcap_tmp[parName].Length;
                        }

                        uint cnt = 0;
                        double[] x = new double[previousLen];
                        double[] y = new double[previousLen];

                        /// Below Logic looks like crap
                        /// Possible errors (loosing last million in dat file) and ineficient code
                        /// TOFIX
                        if (firstTime[stream] == false)
                        {
                            if (fileCnt == 1)
                            {
                                int dataSize = dataTMP.Length;
                                //Buffer.BlockCopy(dataTMP, 0, y, dataSize, dataTMP.Length * sizeof(double));
                                //Array.Copy(dataTMP, 0, y, 0, dataTMP.Length);
                                //System.Collections.IEnumerator xEnum = dataTMP.GetEnumerator();
                                //x = Enumerable.Range(0,dataSize).Select(item => (double)item).ToArray<double>();
                                //cnt = (uint)dataSize-1;
                                for (int i = 0; i != dataSize; i++)
                                {
                                    x[cnt] = cnt;
                                    y[cnt] = dataTMP[i];
                                    cnt++;
                                }
                            }
                            if (storedData[stream][parName] == false)
                            {
                                int dataSize = dataToPlot[stream][parName].Count;
                                for (int i = 0; i != dataSize; i++)
                                {
                                    x[cnt] = cnt;
                                    y[cnt] = dataToPlot[stream][parName][i].Y;
                                    cnt++;
                                }
                            }
                            //previousLen = x.Length;                            
                            foreach (double dataItem in dataPcap_tmp[parName])
                            {
                                x[cnt] = cnt;
                                y[cnt] = dataItem;
                                cnt++;
                            }
                        }
                        else
                        {
                            foreach (double dataItem in dataPcap_tmp[parName])
                            {
                                x[cnt] = cnt;
                                y[cnt] = dataItem;
                                cnt++;
                            }
                        }
                        dataToPlot[stream][parName] = null;

                        if (fileCnt == 1)
                        {
                            dataToPlot[stream][parName] = new FilteredPointList(x, y);
                            dataTMP = null;
                        }
                        else if (cnt > 200000) //store into file if 200000 million points
                        {
                            //save.Start();
                            SaveTempData(stream, parName, y);
                            //save.Stop();
                            //MessageBox.Show(save.Elapsed.ToString());
                            //save.Restart();
                            storedData[stream][parName] = true;
                            //double[] data = LoadTempData(stream, parName);
                            //save.Stop();
                            //MessageBox.Show(save.Elapsed.ToString());
                        }
                        else
                        {
                            dataToPlot[stream][parName] = new FilteredPointList(x, y);
                            storedData[stream][parName] = false;
                        }
                        y = null;
                        //streamData[stream][parName].Clear();                    
                    }
                    //dataToPlot[parName] = dataTMP;
                    firstTime[stream] = false;
                    //streamData[stream].Clear();
                }
            });
            fileCnt--;
        }

    }
    private double[] LoadTempData(int stream, string parName)
    {
        BinaryReader Reader = null;
        string Name = Array.Find(Globals.fileDump_list, element => element.Contains(String.Format("{0}_{1}", stream, parName)));
        //string Name = String.Format(@"{0}\{1}_{2}.dat",Globals.fileDump,stream,parName);
        double[] data = null;
        try
        {
            Reader = new BinaryReader(File.Open(Name, FileMode.Open));
            int size = (int)(Reader.BaseStream.Length/8); //divide by 8 to get number of doubles? /Unsafe.            
            data = new double[size+1];             
            for (int i = 0; i != size; i++)
                data[i] = Reader.ReadDouble();
            Reader.Close();
        }
        catch (Exception e)
        {
            LogItems.addParsingError(String.Format("File not found or something..{0}\nsee below\n{1}", Name, e.Message));
            return null;
        }
        return data;       
    }
    private bool SaveTempData(int stream, string parName, double[] data)
    {
        BinaryWriter Writer = null;
        
        string Name = String.Format(@"{0}\{1}_{2}.dat", Globals.fileDump, stream, parName);

        try
        {
            // Create a new stream to write to the file
            FileStream fs = new FileStream(Name, FileMode.Append);
            Writer = new BinaryWriter(fs);
            //var data = streamData[stream][parName];
            // Writer raw data  
            int size = data.Length;
            for (int i = 0; i != size;i++ )
                Writer.Write(data[i]);
            Writer.Flush();
            Writer.Close();
        }
        catch (Exception e)
        {
            LogItems.addParsingError(String.Format("File not found or something..{0}\nsee below\n{1}", Name, e.Message));
            return false;        
        }
        return true;       

    }

    private void iteratePcaps()
    {
        Dictionary<int, bool> firstTime = new Dictionary<int, bool>(streamData.Count);
        foreach (int e in streamData.Keys) firstTime[e] = true;
        foreach (string p in Globals.filePCAP_list)
        {
            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            //Dictionary<string, double[]> dataPcap_tmp = new Dictionary<string, double[]>();
            LogItems.addStreamInfo(String.Format("Starting to parse {0}", p));

            LoadData(p);
            foreach (int stream in streamData.Keys)
            {
                Dictionary<string, double[]> dataPcap_tmp = new Dictionary<string, double[]>();
                if (channelsSelected[stream].Count==0) continue;
                foreach (string parName in streamData[stream].Keys)
                {
                    dataPcap_tmp[parName] = streamData[stream][parName].ToArray();
                    streamData[stream][parName].Clear();
                    //int len=0;
                    int previousLen = 0;
                    if (firstTime[stream]) previousLen = dataPcap_tmp[parName].Length;
                    else
                    {
                        previousLen = dataToPlot[stream][parName].Count;
                        previousLen = previousLen + dataPcap_tmp[parName].Length;
                    }
                    //RollingPointPairList dataTMP = new RollingPointPairList(len);
                    uint cnt = 0;
                    //double[] x = new double[dataPcap_tmp[parName].Length];
                    //double[] y = new double[dataPcap_tmp[parName].Length];
                    double[] x = new double[previousLen];
                    double[] y = new double[previousLen];

                    if (firstTime[stream] == false)
                    {
                        //dataTMP.Add(dataToPlot[parName]);
                        int dataSize = dataToPlot[stream][parName].Count;
                        for (int i = 0; i != dataSize; i++)
                        {
                            x[cnt] = cnt;
                            y[cnt] = dataToPlot[stream][parName][i].Y;
                            cnt++;
                        }
                        //previousLen = x.Length;                            
                        foreach (double dataItem in dataPcap_tmp[parName])
                        {
                            x[cnt] = cnt;
                            y[cnt] = dataItem;
                            cnt++;
                        }
                    }
                    else
                    {
                        foreach (double dataItem in dataPcap_tmp[parName])
                        {
                            x[cnt] = cnt;
                            y[cnt] = dataItem;
                            cnt++;
                        }
                    }
                    //if (firstTime == false)
                    //{                            
                    //    dataTMP.Add(dataToPlot[parName]);
                    //}
                    //dataTMP.Add(x, y);
                    dataToPlot[stream][parName] = null;
                    dataToPlot[stream][parName] = new FilteredPointList(x, y);
                    streamData[stream][parName].Clear();                    
                }
                    //dataToPlot[parName] = dataTMP;            
                firstTime[stream] = false;
                //streamData[stream].Clear();
            }
            
        }
    }

    #region CreatePlot

    private void CreateGraph(ZedGraphControl zgc,Dictionary<string, RollingPointPairList> dataToPlot, int streamID)
    {
        // get a reference to the GraphPane
        GraphPane myPane = zgc.GraphPane;
        // Set the Titles
        myPane.Title.Text = String.Format("Stream {0}", streamID);
        myPane.XAxis.Title.Text = "Time [packet #]";
        myPane.YAxis.Title.Text = "Value";

        List<Color> allColors = new List<Color>();
        var colors = getSomeColor.GetStaticPropertyBag(typeof(Color));

        foreach (KeyValuePair<string, object> colorPair in colors)
        {
            allColors.Add((Color)colorPair.Value);
        }
        int colCnt = allColors.Count() - 1;

        foreach (string param in dataToPlot.Keys)
        {
            LineItem myCurve = myPane.AddCurve(param, dataToPlot[param], getSomeColor.Blend(allColors[colCnt], Color.Black, 30));
            colCnt--;
        }
        myPane.XAxis.MajorGrid.IsVisible = true;
        myPane.YAxis.MajorGrid.IsVisible = true;
        // Tell ZedGraph to refigure the
        // axes since the data have changed
        zgc.AxisChange();            
    }
    private void CreateGraph(ZedGraphControl zgc, int streamID, string ratioName)
    {
        // get a reference to the GraphPane
        GraphPane myPane = zgc.GraphPane;


        // Set the Titles
        myPane.Title.Text = String.Format("Stream {0}", streamID);
        myPane.XAxis.Title.Text = "Time [packet #]";
        myPane.YAxis.Title.Text = "Value";

        List<Color> allColors = new List<Color>();
        var colors = getSomeColor.GetStaticPropertyBag(typeof(Color));

        foreach (KeyValuePair<string, object> colorPair in colors)
        {
            allColors.Add((Color)colorPair.Value);
        }


        int colCnt = allColors.Count() - 1;
        foreach (string param in dataToPlot[streamID].Keys)
        {
            LineItem myCurve = myPane.AddCurve(param, dataToPlot[streamID][param], getSomeColor.Blend(allColors[colCnt], Color.Black, 30));
            myCurve.Line.IsOptimizedDraw = true;
            myCurve.Symbol.IsVisible = false;
            myCurve.Line.IsAntiAlias = true;
            colCnt--;
        }


        myPane.XAxis.MajorGrid.IsVisible = true;
        myPane.YAxis.MajorGrid.IsVisible = true;
        // Tell ZedGraph to refigure the
        // axes since the data have changed            
        zgc.AxisChange();
    }
    private void CreateGraph(ZedGraphControl zgc, int streamID)
    {
        // get a reference to the GraphPane
        GraphPane myPane = zgc.GraphPane;
        // Set the Titles
        myPane.Title.Text = String.Format("Stream {0}", streamID);
        myPane.XAxis.Title.Text = "Time [packet #]";
        myPane.YAxis.Title.Text = "Value";

        List<Color> allColors = new List<Color>();
        var colors = getSomeColor.GetStaticPropertyBag(typeof(Color));

        foreach (KeyValuePair<string, object> colorPair in colors)
        {
            allColors.Add((Color)colorPair.Value);
        }


        int colCnt = allColors.Count() - 1;

        foreach (string param in dataToPlot[streamID].Keys)
        {
            LineItem myCurve = myPane.AddCurve(param, dataToPlot[streamID][param], getSomeColor.Blend(allColors[colCnt], Color.Black, 45));
            myCurve.Line.IsOptimizedDraw = true;
            myCurve.Symbol.IsVisible = false;
            myCurve.Line.IsAntiAlias = true;
            colCnt--;
        }

        myPane.XAxis.MajorGrid.IsVisible = true;
        myPane.YAxis.MajorGrid.IsVisible = true;
        // Tell ZedGraph to refigure the
        // axes since the data have changed            
        zgc.AxisChange();
    }
    #endregion CreatePlot


    /// <summary>
    /// experiment to parse packets in parallel
    /// </summary>
    /// <param name="pcapfile"></param>
    public void LoadDataParallel(string pcapfile)
    {
        Dictionary<int, Dictionary<string, double[]>> streamParameters = Globals.limitPCAP;
        try
        {
            int pktCnt = 0;
            frames = new List<byte[]>();
            using (PacketCommunicator communicator = new OfflinePacketDevice(pcapfile).Open(65536,                                  // portion of the packet to capture
                // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000))     // read timeout
            {
                //communicator.ReceiveSomePackets(out pktCnt, 10, parsePacketParrallel);
                communicator.ReceivePackets(0, getParallel);
                //communicator.ReceivePackets(0, parsePacketDownSampled);
                //communicator.ReceivePackets(0, parsePacketPTP);
            }

            //ThreadPool.GetMaxThreads(out threadCnt,out threadDone);
            ThreadPool.SetMaxThreads(50, 50);
            int threadCnt = 0;
            int threadDone = 0;
            ThreadPool.GetAvailableThreads(out threadCnt, out threadDone);


            if (frames.Count == 0) return;
            for (int i = 0; i != frames.Count - 1; i++)
            {
                ThreadPool.QueueUserWorkItem(state => parseFrame(frames[i], i));
            }
            //Parallel.For(0, frames.Count, i =>
            //    parseFrame(frames[i], i)
            //    );
            while (true)
            {
                ThreadPool.GetAvailableThreads(out threadCnt, out threadDone);

                if ((threadCnt == 50) && (threadDone == 50))
                { break; }
                Thread.Sleep(100);

            }
        }
        catch (Exception e)
        {
            MessageBox.Show(String.Format("Cannot open {0} or crashed during parsing, please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
                pcapfile, e.ToString()));
        }
    }
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LoadDataDownSampled(string pcapfile)
    {
        Dictionary<int, Dictionary<string, double[]>> streamParameters = Globals.limitPCAP;
        try
        {
            int pktCnt=0;
            frames = new List<byte[]>();
            using (PacketCommunicator communicator = new OfflinePacketDevice(pcapfile).Open(65536,                                  // portion of the packet to capture
                // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000))     // read timeout
            {
                //communicator.ReceiveSomePackets(out pktCnt, 10, parsePacketParrallel);
                communicator.ReceivePackets(0, parsePacketDownSampled);
                //communicator.ReceivePackets(0, parsePacketPTP);
            }
           
        }
        catch (Exception e)
        {
            MessageBox.Show(String.Format("Cannot open {0} or crashed during parsing, please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
                pcapfile, e.ToString()));
        }
    }
    public void LoadData(string pcapfile)    
    {
        Dictionary<int, Dictionary<string, double[]>> streamParameters = Globals.limitPCAP;
        try
        {
            int pktCnt;
            using (PacketCommunicator communicator = new OfflinePacketDevice(pcapfile).Open(65536,
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000))     // read timeout
            {
                communicator.ReceivePackets(0,parsePacket2);
                //communicator.ReceivePackets(0, parsePacketPTP);
                //communicator.ReceiveSomePackets(out pktCnt, -1, parsePacketParrallel);
            }
            //}
        }
        catch (Exception e)
        {
            MessageBox.Show(String.Format("Cannot open {0} or crashed during parsing, please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
                pcapfile, e.ToString()));
        }            
    }    

    /// <summary>
    /// Obsolete parsing method - using dictionary
    /// </summary>
    /// <param name="packet"></param>
    private void parsePacket(Packet packet)
    {
        int stream;
        if (packet.Length < 64)
        {
            return; //broken packet
        }
        if ((packet.Ethernet.EtherType == PcapDotNet.Packets.Ethernet.EthernetType.IpV4) && (packet.Ethernet.IpV4.Udp.Length != 0))
        {
            frame = packet.Ethernet.IpV4.Udp.Payload.ToArray();
            if (frame.Length < 28) return; //check correctness of UDP payload = -1;
            else stream = ((frame[Globals.inetStart + 4] << 24) + (frame[Globals.inetStart + 5] << 16) + (frame[Globals.inetStart + 6] << 8) + frame[Globals.inetStart + 7]);
            if (!streamID.Contains(stream)) //ignore streams not selected
            {
                //this shall never happen?
                //Globals.totalErrors++;
                //Save.LogError(String.Format("-1,{0},{1},Unexpected Stream ID received = {2},\n", Globals.totalFrames, Globals.totalErrors, streamID), -1);
                return;
            }
            if (frame.Length < Globals.streamLength[stream]) return;
            //MessageBox.Show(String.Format("{0} -- {1}", frame.Length, Globals.streamLength[stream])); // ignore broken iNET-X

            uint i = 0;
            uint parPos, parCnt, parOccurences, parPosTmp;
            uint parType = 0;
            double value=0.0;
            Dictionary<string, double[]> limit = Globals.limitPCAP[stream];
            Dictionary<string, limitPCAP_Derrived> limitDerrived = Globals.limitPCAP_derrived;

            foreach (string parName in channelsSelected[stream])
            {
                parPos = (uint)(limit[parName][4]);
                parCnt = (uint)(limit[parName][3]);
                parOccurences = (uint)(limit[parName][5]);
                parType = (uint)(limit[parName][6]);
                switch (parType) 
                {
                    case 0: //ANALOG
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            value = getValue.CalcValue(16, (double)((frame[parPos] << 8) + frame[++parPos]), limit[parName][7], limit[parName][8]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    //case 1:                
                    //    break;
                    case 2: //BCU Status                    
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);
                            value = (double)((frame[parPos] << 8) + frame[++parPos]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                                                        // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 3:     //BCD temp - BIT101
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);

                            value = getValue.CalcValue(16, (double)(getValue.bcd2int((frame[parPos] << 8) + frame[++parPos])), limit[parName][7], limit[parName][8]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {      
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 5: //Derrived parameter - TODO
                        //string sourceParameter = Globals.limitDerrived[parName][0];
                        string srcName = limitDerrived[parName].srcParameterName;
                        uint constNumber = 2;
                        if (limitDerrived[parName].const3 != null) constNumber = 3;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (constNumber>2)
                                //value = getValue.GetDerivedParameter(16, (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                                value = getValue.GetDerivedParameter((double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]), limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                            else
                                value = getValue.GetDerivedParameter(16, (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2);                                
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                        }
                        break;
                    case 6: //Concat --TODO to handle big parameters >16bit
                        string[] srcName_list = limitDerrived[parName].srcParametersName;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (srcName_list.Length == 2)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                value = getValue.getConcatedParameter(16, value1, value2);
                            }
                            else if (srcName_list.Length == 4)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                var par3Pos = (int)limit[srcName_list[2]][4] + parOccur * 2;
                                var value3 = getValue.getConcatedParameter(8, frame[par3Pos], frame[par3Pos + 1]);
                                var par4Pos = (int)limit[srcName_list[3]][4] + parOccur * 2;
                                var value4 = getValue.getConcatedParameter(8, frame[par4Pos], frame[par4Pos + 1]);
                                var top = getValue.getConcatedParameter(16, value1, value2);
                                var bottom = getValue.getConcatedParameter(16, value3, value4);
                                value = getValue.getConcatedParameter(32, top, bottom);
                            }
                        }
                        break;
                    default:
                            for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                            {
                                parPosTmp = (uint)(parPos + parOccur * 2);
                                value = (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]);
                            }
                        break;
                } i += 2;

                streamData[stream][parName].Add(value);
                }
            }
        Globals.totalFrames++;
    }


    /// <summary>
    /// Parse method using arrays -- mostly tested
    /// </summary>
    /// <param name="packet"></param>
    private void parsePacket2(Packet packet)
    {
        int stream;
        if (packet.Length < 64)
        {
            return; // No UDP header => broken packet
        }
        if ((packet.Ethernet.EtherType == PcapDotNet.Packets.Ethernet.EthernetType.IpV4) && (packet.Ethernet.IpV4.Udp.Length != 0))
        {
            frame = packet.Ethernet.IpV4.Udp.Payload.ToArray();
            if (frame.Length < 28) return; //iNET-X header broken
            else stream = (int)frame.ReadUInt(Globals.inetStart + 4, Endianity.Big);

            if (!streamID.Contains(stream)) //ignore streams not described in config CSV
            {                
                //Globals.totalErrors++;
                //Save.LogError(String.Format("-1,{0},{1},Unexpected Stream ID received = {2},\n", Globals.totalFrames, Globals.totalErrors, streamID), -1);
                return;
            }

            if (frame.Length < Globals.streamLength[stream]) // ignore uncomplete iNET-X payload
            {
                var msg = String.Format("Broken iNET-X Payload -> {0} != {1}", frame.Length, Globals.streamLength[stream]); //might be slowing down with a lot of errors
                LogItems.addParsingError(msg);
                return; 
            }        

            uint i = 0;
            uint parPos, parCnt, parOccurences, parPosTmp;
            uint parType = 0;
            double value = 0.0;
            //Dictionary<string, double[]> limit = Globals.limitPCAP[stream];
            //int streamCnt = (int)limit[limit.Keys.First()][1];
            //double[][] limitA = new double[Globals.limitArray[0].Length][];
            //limitA = Globals.limitArray[0];
            //Dictionary<string, limitPCAP_Derrived> limitDerrived = Globals.limitPCAP_derrived;

            Dictionary<string, double[]> limit = Globals.limitPCAP[stream]; //get parameters from this stream only.
            int streamCnt = (int)limit[limit.Keys.First()][1];  //get the stream position in the array.
            double[][] limitA = new double[Globals.limitArray[streamCnt].Length][]; //create local array lookup with a size of the stream
            limitA = Globals.limitArray[streamCnt];
            Dictionary<string, limitPCAP_Derrived> limitDerrived = Globals.limitPCAP_derrived; //to handle derrived params
            
            foreach (string parName in channelsSelected[stream])
            {                
                parCnt = (uint)(limit[parName][3]);
                parPos = (uint)(limitA[parCnt][4]);
                parOccurences = (uint)(limitA[parCnt][5]);
                parType = (uint)(limitA[parCnt][6]);
                switch (parType)
                {
                    case 0: //ANALOG
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            value = getValue.CalcValue(16, (double)((frame[parPos] << 8) + frame[++parPos]), limitA[parCnt][7], limitA[parCnt][8]);
                            if ((value > limitA[parCnt][9]) || (value < limitA[parCnt][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    //case 1:                
                    //    break;
                    case 2: //BCU Status                    
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);
                            value = (double)((frame[parPos] << 8) + frame[++parPos]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 3:     //BCD temp - BIT101 -- units does not seem correct -- TOFIX
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);

                            value = getValue.CalcValue(16, (double)(getValue.bcd2int((frame[parPos] << 8) + frame[++parPos])), limit[parName][7], limit[parName][8]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 5: //Derrived parameter - TODO
                        //string sourceParameter = Globals.limitDerrived[parName][0];
                        string srcName = limitDerrived[parName].srcParameterName;
                        uint constNumber = 2;
                        if (limitDerrived[parName].const3 != null) constNumber = 3;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (constNumber > 2)
                                //value = getValue.GetDerivedParameter(16, (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                                value = getValue.GetDerivedParameter((double)((frame[parPos] << 8) + frame[++parPos]), limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                            else
                                value = getValue.GetDerivedParameter(16, (double)((frame[parPos] << 8) + frame[++parPos]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 6: //Concat --TODO to handle big parameters >16bit
                        string[] srcName_list = limitDerrived[parName].srcParametersName;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (srcName_list.Length == 2)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                value = getValue.getConcatedParameter(16, value1, value2);
                            }
                            else if (srcName_list.Length == 4)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                var par3Pos = (int)limit[srcName_list[2]][4] + parOccur * 2;
                                var value3 = getValue.getConcatedParameter(8, frame[par3Pos], frame[par3Pos + 1]);
                                var par4Pos = (int)limit[srcName_list[3]][4] + parOccur * 2;
                                var value4 = getValue.getConcatedParameter(8, frame[par4Pos], frame[par4Pos + 1]);
                                var top = getValue.getConcatedParameter(16, value1, value2);
                                var bottom = getValue.getConcatedParameter(16, value3, value4);
                                value = getValue.getConcatedParameter(32, top, bottom);
                            }
                        }
                        break;
                    default: //to convert hex to decimal 0-65535
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            value = (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]);
                        }
                        break;
                } i += 2;
                streamData[stream][parName].Add(value);
            }
        }
        Globals.totalFrames++;
    }
    /// <summary>
    /// To parse every X packet
    /// </summary>
    /// <param name="packet"></param>
    private void parsePacketDownSampled(Packet packet)
    {
        int stream;
        if (packet.Length < 64)
        {
            return; // No UDP header => broken packet
        }
        if ((packet.Ethernet.EtherType == PcapDotNet.Packets.Ethernet.EthernetType.IpV4) && (packet.Ethernet.IpV4.Udp.Length != 0))
        {
            frame = packet.Ethernet.IpV4.Udp.Payload.ToArray();
            if (frame.Length < 28) return; //iNET-X header broken
            else stream = (int)frame.ReadUInt(Globals.inetStart + 4, Endianity.Big);

            if (!streamID.Contains(stream)) //ignore streams not described in config CSV
            {                
                //Globals.totalErrors++;
                //Save.LogError(String.Format("-1,{0},{1},Unexpected Stream ID received = {2},\n", Globals.totalFrames, Globals.totalErrors, streamID), -1);
                return;
            }

            if (frame.Length < Globals.streamLength[stream]) // ignore broken iNET-X payload
            {
                var msg = String.Format("Broken iNET-X Payload -> {0} != {1}", frame.Length, Globals.streamLength[stream]); //might be slowing down.
                LogItems.addParsingError(msg);
                return; 
            }

            Globals.sampleCnt--;
            if (Globals.sampleCnt == 0)
            {
                Globals.sampleCnt = 15;                
            }
            else
            {
                return;
            }

            uint i = 0;
            uint parPos, parCnt, parOccurences, parPosTmp;
            uint parType = 0;
            double value = 0.0;
            Dictionary<string, double[]> limit = Globals.limitPCAP[stream]; //get parameters from this stream only.
            int streamCnt = (int)limit[limit.Keys.First()][1];  //get the stream position in the array.
            double[][] limitA = new double[Globals.limitArray[streamCnt].Length][]; //create local array lookup with a size of the stream
            limitA = Globals.limitArray[streamCnt];
            Dictionary<string, limitPCAP_Derrived> limitDerrived = Globals.limitPCAP_derrived; //to handle derrived params

            foreach (string parName in channelsSelected[stream])
            {
                parCnt = (uint)(limit[parName][3]);
                parPos = (uint)(limitA[parCnt][4]);
                parOccurences = (uint)(limitA[parCnt][5]);
                parType = (uint)(limitA[parCnt][6]);
                switch (parType)
                {
                    case 0: //ANALOG
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            value = getValue.CalcValue(16, (double)((frame[parPos] << 8) + frame[++parPos]), limitA[parCnt][7], limitA[parCnt][8]);
                            if ((value > limitA[parCnt][9]) || (value < limitA[parCnt][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    //case 1:                
                    //    break;
                    case 2: //BCU Status                    
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);
                            value = (double)((frame[parPos] << 8) + frame[++parPos]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 3:     //BCD temp - BIT101 -- units does not seem correct -- TOFIX
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);

                            value = getValue.CalcValue(16, (double)(getValue.bcd2int((frame[parPos] << 8) + frame[++parPos])), limit[parName][7], limit[parName][8]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 5: //Derrived parameter - TODO
                        //string sourceParameter = Globals.limitDerrived[parName][0];
                        string srcName = limitDerrived[parName].srcParameterName;
                        uint constNumber = 2;
                        if (limitDerrived[parName].const3 != null) constNumber = 3;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (constNumber > 2)
                                //value = getValue.GetDerivedParameter(16, (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                                value = getValue.GetDerivedParameter((double)((frame[parPos] << 8) + frame[++parPos]), limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                            else
                                value = getValue.GetDerivedParameter(16, (double)((frame[parPos] << 8) + frame[++parPos]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 6: //Concat --TODO to handle big parameters >16bit
                        string[] srcName_list = limitDerrived[parName].srcParametersName;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (srcName_list.Length == 2)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                value = getValue.getConcatedParameter(16, value1, value2);
                            }
                            else if (srcName_list.Length == 4)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                var par3Pos = (int)limit[srcName_list[2]][4] + parOccur * 2;
                                var value3 = getValue.getConcatedParameter(8, frame[par3Pos], frame[par3Pos + 1]);
                                var par4Pos = (int)limit[srcName_list[3]][4] + parOccur * 2;
                                var value4 = getValue.getConcatedParameter(8, frame[par4Pos], frame[par4Pos + 1]);
                                var top = getValue.getConcatedParameter(16, value1, value2);
                                var bottom = getValue.getConcatedParameter(16, value3, value4);
                                value = getValue.getConcatedParameter(32, top, bottom);
                            }
                        }
                        break;
                    default:
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            value = (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]);
                        }
                        break;
                } i += 2;
                streamData[stream][parName].Add(value);          
            }
        }
        Globals.totalFrames++;
    }


    // Below are experiments

    //not efficient enough - paralell packets maybe.
    private void parsePacketParrallel(Packet packet)
    {
        int stream;
        if (packet.Length < 64)
        {
            return; //broken packet
        }
        if ((packet.Ethernet.EtherType == PcapDotNet.Packets.Ethernet.EthernetType.IpV4) && (packet.Ethernet.IpV4.Udp.Length != 0))
        {
            frame = packet.Ethernet.IpV4.Udp.Payload.ToArray();
            if (frame.Length < 28) return; //check correctness of UDP payload = -1;
            else stream = (int)frame.ReadUInt(Globals.inetStart + 4, Endianity.Big);//((frame[Globals.inetStart + 4] << 24) + (frame[Globals.inetStart + 5] << 16) + (frame[Globals.inetStart + 6] << 8) + frame[Globals.inetStart + 7]);
            if (!streamID.Contains(stream)) //ignore streams not selected
            {
                //this shall never happen?
                //Globals.totalErrors++;
                //Save.LogError(String.Format("-1,{0},{1},Unexpected Stream ID received = {2},\n", Globals.totalFrames, Globals.totalErrors, streamID), -1);
                return;
            }
            if (frame.Length < Globals.streamLength[stream]) return;
            //MessageBox.Show(String.Format("{0} -- {1}", frame.Length, Globals.streamLength[stream])); // ignore broken iNET-X

            uint i = 0;
            uint parPos, parCnt, parOccurences, parPosTmp;
            uint parType = 0;
            double value = 0.0;
            Dictionary<string, double[]> limit = Globals.limitPCAP[stream];
            int streamCnt = (int)limit[limit.Keys.First()][1];
            double[][] limitA = new double[Globals.limitArray[0].Length][];
            limitA = Globals.limitArray[0];
            Dictionary<string, limitPCAP_Derrived> limitDerrived = Globals.limitPCAP_derrived;
            foreach (string parName in channelsSelected[stream])
            {
                parCnt = (uint)(limit[parName][3]);
                parPos = (uint)(limitA[parCnt][4]);
                parOccurences = (uint)(limitA[parCnt][5]);
                parType = (uint)(limitA[parCnt][6]);
                switch (parType)
                {
                    case 0: //ANALOG
                        System.Threading.Tasks.Parallel.For(0, parOccurences-1, (pointX) =>
                        {
                            parPosTmp = (uint)(parPos + pointX * 2);
                            value = getValue.CalcValue(16, (double)((frame[parPosTmp] << 8) + frame[++parPosTmp]), limitA[parCnt][7], limitA[parCnt][8]);
                            if ((value > limitA[parCnt][9]) || (value < limitA[parCnt][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }                            
                        });
                        break;
                    //case 1:                
                    //    break;
                    case 2: //BCU Status                    
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);
                            value = (double)((frame[parPos] << 8) + frame[++parPos]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 3:     //BCD temp - BIT101 -- units does not seem correct -- TOFIX
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);

                            value = getValue.CalcValue(16, (double)(getValue.bcd2int((frame[parPos] << 8) + frame[++parPos])), limit[parName][7], limit[parName][8]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 5: //Derrived parameter - TODO
                        //string sourceParameter = Globals.limitDerrived[parName][0];
                        string srcName = limitDerrived[parName].srcParameterName;
                        uint constNumber = 2;
                        if (limitDerrived[parName].const3 != null) constNumber = 3;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (constNumber > 2)
                                //value = getValue.GetDerivedParameter(16, (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                                value = getValue.GetDerivedParameter((double)((frame[parPos] << 8) + frame[++parPos]), limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                            else
                                value = getValue.GetDerivedParameter(16, (double)((frame[parPos] << 8) + frame[++parPos]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 6: //Concat --TODO to handle big parameters >16bit
                        string[] srcName_list = limitDerrived[parName].srcParametersName;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (srcName_list.Length == 2)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                value = getValue.getConcatedParameter(16, value1, value2);
                            }
                            else if (srcName_list.Length == 4)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                var par3Pos = (int)limit[srcName_list[2]][4] + parOccur * 2;
                                var value3 = getValue.getConcatedParameter(8, frame[par3Pos], frame[par3Pos + 1]);
                                var par4Pos = (int)limit[srcName_list[3]][4] + parOccur * 2;
                                var value4 = getValue.getConcatedParameter(8, frame[par4Pos], frame[par4Pos + 1]);
                                var top = getValue.getConcatedParameter(16, value1, value2);
                                var bottom = getValue.getConcatedParameter(16, value3, value4);
                                value = getValue.getConcatedParameter(32, top, bottom);
                            }
                        }
                        break;
                    default:
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            value = (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]);
                        }
                        break;
                } i += 2;

                streamData[stream][parName].Add(value);
            }
        }
        Globals.totalFrames++;
    }
    //get all then parse in parra
    private void getParallel(Packet packet)
    {
        int stream;
        if (packet.Length < 64)
        {
            return; //broken packet
        }
        if ((packet.Ethernet.EtherType == PcapDotNet.Packets.Ethernet.EthernetType.IpV4) && (packet.Ethernet.IpV4.Udp.Length != 0))
        {
            frame = packet.Ethernet.IpV4.Udp.Payload.ToArray();
            if (frame.Length < 28) return; //check correctness of UDP payload = -1;
            else stream = (int)frame.ReadUInt(Globals.inetStart + 4, Endianity.Big);//((frame[Globals.inetStart + 4] << 24) + (frame[Globals.inetStart + 5] << 16) + (frame[Globals.inetStart + 6] << 8) + frame[Globals.inetStart + 7]);
            if (!streamID.Contains(stream)) //ignore streams not selected
            {
                //this shall never happen?
                //Globals.totalErrors++;
                //Save.LogError(String.Format("-1,{0},{1},Unexpected Stream ID received = {2},\n", Globals.totalFrames, Globals.totalErrors, streamID), -1);
                return;
            }
            if (frame.Length < Globals.streamLength[stream]) return;
            //MessageBox.Show(String.Format("{0} -- {1}", frame.Length, Globals.streamLength[stream])); // ignore broken iNET-X

            frames.Add(frame);
        }
    }

    private void parseFrame(byte[] frame, int frameCnt)
    {
            int stream = (int)frame.ReadUInt(Globals.inetStart + 4, Endianity.Big);
            uint i = 0;
            uint parPos, parCnt, parOccurences, parPosTmp;
            uint parType = 0;
            double value = 0.0;
            Dictionary<string, double[]> limit = Globals.limitPCAP[stream];
            int streamCnt = (int)limit[limit.Keys.First()][1];
            double[][] limitA = new double[Globals.limitArray[0].Length][];
            limitA = Globals.limitArray[0];
            Dictionary<string, limitPCAP_Derrived> limitDerrived = Globals.limitPCAP_derrived;
            foreach (string parName in channelsSelected[stream])
            {
                parCnt = (uint)(limit[parName][3]);
                parPos = (uint)(limitA[parCnt][4]);
                parOccurences = (uint)(limitA[parCnt][5]);
                parType = (uint)(limitA[parCnt][6]);
                switch (parType)
                {
                    case 0: //ANALOG
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + pointX * 2);
                            value = getValue.CalcValue(16, (double)((frame[parPos++] << 8) + frame[++parPos]), limitA[parCnt][7], limitA[parCnt][8]);
                            if ((value > limitA[parCnt][9]) || (value < limitA[parCnt][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                        }
                        parPos++;
                        break;
                    //case 1:                
                    //    break;
                    case 2: //BCU Status                    
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);
                            value = (double)((frame[parPos] << 8) + frame[++parPos]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 3:     //BCD temp - BIT101 -- units does not seem correct -- TOFIX
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);

                            value = getValue.CalcValue(16, (double)(getValue.bcd2int((frame[parPos] << 8) + frame[++parPos])), limit[parName][7], limit[parName][8]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 5: //Derrived parameter - TODO
                        //string sourceParameter = Globals.limitDerrived[parName][0];
                        string srcName = limitDerrived[parName].srcParameterName;
                        uint constNumber = 2;
                        if (limitDerrived[parName].const3 != null) constNumber = 3;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (constNumber > 2)
                                //value = getValue.GetDerivedParameter(16, (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                                value = getValue.GetDerivedParameter((double)((frame[parPos] << 8) + frame[++parPos]), limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                            else
                                value = getValue.GetDerivedParameter(16, (double)((frame[parPos] << 8) + frame[++parPos]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 6: //Concat --TODO to handle big parameters >16bit
                        string[] srcName_list = limitDerrived[parName].srcParametersName;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (srcName_list.Length == 2)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                value = getValue.getConcatedParameter(16, value1, value2);
                            }
                            else if (srcName_list.Length == 4)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                var par3Pos = (int)limit[srcName_list[2]][4] + parOccur * 2;
                                var value3 = getValue.getConcatedParameter(8, frame[par3Pos], frame[par3Pos + 1]);
                                var par4Pos = (int)limit[srcName_list[3]][4] + parOccur * 2;
                                var value4 = getValue.getConcatedParameter(8, frame[par4Pos], frame[par4Pos + 1]);
                                var top = getValue.getConcatedParameter(16, value1, value2);
                                var bottom = getValue.getConcatedParameter(16, value3, value4);
                                value = getValue.getConcatedParameter(32, top, bottom);
                            }
                        }
                        break;
                    default:
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            value = (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]);
                        }
                        break;
                } i += 2;
                streamData[stream][parName].Add(value);
            }
        
        Globals.totalFrames++;
    }
    
    //use PTP Time.
    private void parsePacketPTP(Packet packet)
    {
        int stream;
        if (packet.Length < 64)
        {
            return; //broken packet
        }
        if ((packet.Ethernet.EtherType == PcapDotNet.Packets.Ethernet.EthernetType.IpV4) && (packet.Ethernet.IpV4.Udp.Length != 0))
        {
            frame = packet.Ethernet.IpV4.Udp.Payload.ToArray();
            if (frame.Length < 28) return; //check correctness of UDP payload = -1;
            else stream = ((frame[Globals.inetStart + 4] << 24) + (frame[Globals.inetStart + 5] << 16) + (frame[Globals.inetStart + 6] << 8) + frame[Globals.inetStart + 7]);
            if (!streamID.Contains(stream)) //ignore streams not selected
            {
                //this shall never happen?
                //Globals.totalErrors++;
                //Save.LogError(String.Format("-1,{0},{1},Unexpected Stream ID received = {2},\n", Globals.totalFrames, Globals.totalErrors, streamID), -1);
                return;
            }
            if (frame.Length < Globals.streamLength[stream]) return;
            //MessageBox.Show(String.Format("{0} -- {1}", frame.Length, Globals.streamLength[stream])); // ignore broken iNET-X
            //getValue.getPTPTime(frame[16])
            UInt64 PTPSeconds;
            UInt64 PTPNanoseconds;
            PTPSeconds = frame.ReadUInt(16, Endianity.Big);
            PTPNanoseconds = frame.ReadUInt(20, Endianity.Big);

            MessageBox.Show(String.Format("{0} == {1}", PTPSeconds, PTPNanoseconds));
            uint i = 0;
            uint parPos, parCnt, parOccurences, parPosTmp;
            uint parType = 0;
            double value = 0.0;
            Dictionary<string, double[]> limit = Globals.limitPCAP[stream];
            int streamCnt = (int)limit[limit.Keys.First()][1];
            double[][] limitA = new double[Globals.limitArray[0].Length][];
            limitA = Globals.limitArray[0];
            Dictionary<string, limitPCAP_Derrived> limitDerrived = Globals.limitPCAP_derrived;
            foreach (string parName in channelsSelected[stream])
            {
                parCnt = (uint)(limit[parName][3]);
                parPos = (uint)(limitA[parCnt][4]);
                parOccurences = (uint)(limitA[parCnt][5]);
                parType = (uint)(limitA[parCnt][6]);
                switch (parType)
                {
                    case 0: //ANALOG
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            value = getValue.CalcValue(16, (double)((frame[parPos] << 8) + frame[++parPos]), limitA[parCnt][7], limitA[parCnt][8]);
                            if ((value > limitA[parCnt][9]) || (value < limitA[parCnt][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                */
                                //Globals.parError[stream][parName] += 1;
                                //Globals.packetErrors[stream]["total"]++;
                                //Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    //case 1:                
                    //    break;
                    case 2: //BCU Status                    
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);
                            value = (double)((frame[parPos] << 8) + frame[++parPos]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 3:     //BCD temp - BIT101
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //parPosTmp = (uint)(parPos + parOccur * 2);

                            value = getValue.CalcValue(16, (double)(getValue.bcd2int((frame[parPos] << 8) + frame[++parPos])), limit[parName][7], limit[parName][8]);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                            parPos++;
                        }
                        break;
                    case 5: //Derrived parameter - TODO
                        //string sourceParameter = Globals.limitDerrived[parName][0];
                        string srcName = limitDerrived[parName].srcParameterName;
                        uint constNumber = 2;
                        if (limitDerrived[parName].const3 != null) constNumber = 3;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (constNumber > 2)
                                //value = getValue.GetDerivedParameter(16, (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                                value = getValue.GetDerivedParameter((double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]), limitDerrived[parName].const1, limitDerrived[parName].const2, limitDerrived[parName].const3);
                            else
                                value = getValue.GetDerivedParameter(16, (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]), limit[srcName][7], limit[srcName][8], limitDerrived[parName].const1, limitDerrived[parName].const2);
                            if ((value > limit[parName][9]) || (value < limit[parName][10]))
                            {
                                // To do - handle error reporting
                                /* 
                                errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                    Globals.totalErrors,                            //total error count for all streams
                                    streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                    Globals.parError[streamID][parName],            //error count for parameter
                                    parName,                                        //parameter name
                                    value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                    parOccur);
                                    * */
                                Globals.parError[stream][parName] += 1;
                                Globals.packetErrors[stream]["total"]++;
                                Globals.totalErrors++;
                            }
                        }
                        break;
                    case 6: //Concat --TODO to handle big parameters >16bit
                        string[] srcName_list = limitDerrived[parName].srcParametersName;
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            if (srcName_list.Length == 2)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                value = getValue.getConcatedParameter(16, value1, value2);
                            }
                            else if (srcName_list.Length == 4)
                            {
                                var par1Pos = (int)limit[srcName_list[0]][4] + parOccur * 2;
                                var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                var par2Pos = (int)limit[srcName_list[1]][4] + parOccur * 2;
                                var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                var par3Pos = (int)limit[srcName_list[2]][4] + parOccur * 2;
                                var value3 = getValue.getConcatedParameter(8, frame[par3Pos], frame[par3Pos + 1]);
                                var par4Pos = (int)limit[srcName_list[3]][4] + parOccur * 2;
                                var value4 = getValue.getConcatedParameter(8, frame[par4Pos], frame[par4Pos + 1]);
                                var top = getValue.getConcatedParameter(16, value1, value2);
                                var bottom = getValue.getConcatedParameter(16, value3, value4);
                                value = getValue.getConcatedParameter(32, top, bottom);
                            }
                        }
                        break;
                    default:
                        for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                        {
                            parPosTmp = (uint)(parPos + parOccur * 2);
                            value = (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]);
                        }
                        break;
                } i += 2;

                streamData[stream][parName].Add(value);
            }
        }
        Globals.totalFrames++;
    }        
   
    
    
    
    

    private void ClosePlot(object sender, FormClosingEventArgs e)
    {          
        //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        dataToPlot.Clear();
        GC.WaitForFullGCComplete(500);
    }
}    
    public static class getSomeColor
    {
        public static Dictionary<string, object> GetStaticPropertyBag(Type t)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var map = new Dictionary<string, object>();
            foreach (var prop in t.GetProperties(flags))
            {
                map[prop.Name] = prop.GetValue(null, null);
            }
            return map;
        }

        public static Color Blend(this Color color, Color backColor, double amount)
        {
            byte r = (byte)((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte)((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte)((color.B * amount) + backColor.B * (1 - amount));
            return Color.FromArgb(r, g, b);
        }
    }
}
