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

namespace Plot_iNET_X
{
public partial class PlotData : Form
{

    public static List<int> streamID;
    public static byte[] frame =new byte[65536];
    public static Dictionary<int, Dictionary<string, List<double>>> streamData;
    public static Dictionary<int, Dictionary<string, FilteredPointList>> dataToPlot;

    public PlotData()
    {
        InitializeComponent();
    }
    public PlotData(List<int> streamInput)
    {
    #region Initialize_Globals
        try
        {
            streamID = streamInput;
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

                if (Globals.channelsSelected[stream].Count != 0)
                {
                    streamData[stream] = new Dictionary<string, List<double>>();
                    foreach (string parName in Globals.channelsSelected[stream])
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
            streamID = streamInput;
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

                if (Globals.channelsSelected[stream].Count != 0)
                {
                    streamData[stream] = new Dictionary<string, List<double>>();
                    foreach (string parName in Globals.channelsSelected[stream])
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
        if (Globals.filePCAP == null)
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
                CreateGraph(zedGraphControl1, dataToPlot[stream], stream);
            }
        }
        else
        {
            Globals.filePCAP_list = new string[1]{Globals.filePCAP};
            sw2.Start();
            iteratePcaps();
            sw2.Stop();
            foreach (int stream in streamID)
            {
                CreateGraph(zedGraphControl1, dataToPlot[stream], stream);
            }
        }
        SetSize();
        sw.Stop();        
        if (Globals.showErrorSummary)
        {
            Thread logWindow = new Thread(() => new ErrorSummaryWindow().ShowDialog());
            logWindow.Priority = ThreadPriority.BelowNormal;
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
            foreach(string parName in Globals.channelsSelected[stream])
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

    private static void iteratePcaps_Big()
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
            LoadData(p);
            foreach (int stream in streamData.Keys)
            {
                Dictionary<string, double[]> dataPcap_tmp = new Dictionary<string, double[]>();
                if (Globals.channelsSelected[stream].Count == 0) continue;
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

                    if (firstTime[stream] == false)
                    {
                        if (fileCnt == 1)
                        {
                            int dataSize = dataTMP.Length;
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
                    else if (cnt > 1000000) //store into file if 1 million points
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
    private static double[] LoadTempData(int stream, string parName)
    {
        BinaryReader Reader = null;        
        string Name = String.Format(@"{0}\{1}_{2}.dat",Globals.fileDump,stream,parName);
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
    private static bool SaveTempData(int stream, string parName, double[] data)
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

    private static void iteratePcaps()
    {
        Dictionary<int, bool> firstTime = new Dictionary<int, bool>(streamData.Count);
        foreach (int e in streamData.Keys) firstTime[e] = true;
        foreach (string p in Globals.filePCAP_list)
        {
            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            //Dictionary<string, double[]> dataPcap_tmp = new Dictionary<string, double[]>();
            LoadData(p);
            foreach (int stream in streamData.Keys)
            {
                Dictionary<string, double[]> dataPcap_tmp = new Dictionary<string, double[]>();
                if (Globals.channelsSelected[stream].Count==0) continue;
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

    private static void CreateGraph(ZedGraphControl zgc,Dictionary<string, RollingPointPairList> dataToPlot, int streamID)
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
    private static void CreateGraph(ZedGraphControl zgc, Dictionary<string, FilteredPointList> dataToPlot, int streamID)
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
    private static void CreateGraph(ZedGraphControl zgc, int streamID)
    {
        // get a reference to the GraphPane
        GraphPane myPane = zgc.GraphPane;

        // Set the Titles
        myPane.Title.Text = String.Format("Stream {0}",streamID);

        Dictionary<string, double[]> dataToPlot = new Dictionary<string, double[]>();
        //dataToPlot = LoadData(String.Format("{0}{1}.csv", Globals.outputFile[streamID], Globals.outputFileCnt.ToString().PadLeft(4,'0')), streamID);
        dataToPlot = null;
        myPane.XAxis.Title.Text = "Time [packet #]";
        myPane.YAxis.Title.Text = "Value";

        int dataItems = dataToPlot.First().Value.Length;
        Dictionary <string, PointPairList> dataZed = new Dictionary <string, PointPairList>(dataItems);

        double[] X = new double[dataItems];

        for(int i=0; i!=dataItems;i ++)
        {
            X[i] = (double)i;
        }
        foreach (string param in dataToPlot.Keys)
        {
            dataZed[param] = new PointPairList(X ,dataToPlot[param]);
        }
                        
        List<Color> allColors = new List<Color>();
        var colors = getSomeColor.GetStaticPropertyBag(typeof(Color));

        foreach (KeyValuePair<string, object> colorPair in colors)
        {
            allColors.Add((Color)colorPair.Value);
        }


        int colCnt = allColors.Count()-1;

        foreach (string param in dataZed.Keys)
        {
            LineItem myCurve = myPane.AddCurve(param, dataZed[param], getSomeColor.Blend(allColors[colCnt], Color.Black, 30));
            colCnt--;
        }

        myPane.XAxis.MajorGrid.IsVisible = true;
        myPane.YAxis.MajorGrid.IsVisible = true;
        // Tell ZedGraph to refigure the
        // axes since the data have changed
        zgc.AxisChange();
    }
    private void CreateGraph(ZedGraphControl zgc)
    {
        // get a reference to the GraphPane
        GraphPane myPane = zgc.GraphPane;
        // Set the Titles
        myPane.Title.Text = "Data";
        myPane.XAxis.Title.Text = "Time [packet #]";
        myPane.YAxis.Title.Text = "Value";

        // Make up some data arrays based on the Sine function
        double x, y1, y2;
        PointPairList list1 = new PointPairList();
        PointPairList list2 = new PointPairList();
        for (int i = 0; i < 2000; i++)
        {
            x = (double)i + 5;
            y1 = (1.5 + i * 0.2) * (5 + Math.Sin((double)i * 0.2));
            y2 = 3.0 * (1.5 + Math.Sin((double)i * 0.2));
            list1.Add(x, y1);
            list2.Add(x, y2);
        }

        //list1.AddRange(yData);
        //list1.
        // Generate a red curve with diamond
        // symbols, and "Porsche" in the legend
        LineItem myCurve = myPane.AddCurve("Porsche",
                list1, Color.Red, SymbolType.Diamond);

        // Generate a blue curve with circle
        // symbols, and "Piper" in the legend
        LineItem myCurve2 = myPane.AddCurve("Piper",
                list2, Color.Blue, SymbolType.Circle);

        // Tell ZedGraph to refigure the
        // axes since the data have changed
        zgc.AxisChange();
    }

    #endregion CreatePlot




    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LoadData(string pcapfile)
    //Dictionary<string, RollingPointPairList> LoadData(int streamID, string pcapfile)
    {
        Dictionary<int, Dictionary<string, double[]>> streamParameters = Globals.limitPCAP;
        //streamData.Clear();  
        try
        {
            //while (IsFileLocked(new FileInfo(Globals.filePCAP)))
            //{
            //    System.Threading.Thread.Sleep(100);
            //}
            //OfflinePacketDevice selectedDevice = new OfflinePacketDevice(pcapfile);
            //string pcapfileMapped="mapped pcap file";
            //using (System.IO.MemoryMappedFiles.MemoryMappedFile pcapfileMapped_obj =  System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(pcapfile, FileMode.Open, pcapfileMapped, 0))//, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read))
            ////pcapfileS = new System.IO.MemoryMappedFiles.MemoryMappedFile();
            //using (System.IO.MemoryMappedFiles.MemoryMappedViewAccessor pcapFileAccess = pcapfileMapped_obj.CreateViewAccessor())
                
            //{
                // MemoryMappedViewAccessor pcapFileAccess = pcapfileMapped_obj.CreateViewAccessor();
                using (PacketCommunicator communicator = new OfflinePacketDevice(pcapfile).Open(65536,                                  // portion of the packet to capture
                    // 65536 guarantees that the whole packet will be captured on all the link layers
                                        PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                        1000))     // read timeout
                {
                    communicator.ReceivePackets(0,parsePacket2);
                }
            //}
        }
        catch (Exception e)
        {
            MessageBox.Show(String.Format("Cannot open {0} or crashed during parsing, please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
                pcapfile, e.ToString()));
        }            
    }
    //public static Dictionary<string, RollingPointPairList> LoadData(int streamID)
    //{
    //    Dictionary<string, double[]> streamParameters = Globals.limitPCAP[streamID];
    //    streamData.Clear();
    //    try
    //    {
    //        //while (IsFileLocked(new FileInfo(Globals.filePCAP)))
    //        //{
    //        //    System.Threading.Thread.Sleep(100);
    //        //}
    //        OfflinePacketDevice selectedDevice = new OfflinePacketDevice(Globals.filePCAP);

    //        PacketCommunicator communicator = selectedDevice.Open(65536,                                  // portion of the packet to capture
    //            // 65536 guarantees that the whole packet will be captured on all the link layers
    //                                PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
    //                                1000);                                  // read timeout
    //        communicator.ReceivePackets(0, parsePacket);
    //    }
    //    catch (Exception e)
    //    {
    //        MessageBox.Show(String.Format("Cannot open {0} or crashed during parsing, please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
    //            Globals.filePCAP, e.ToString()), "Packet parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //    }

    //    Dictionary<string, RollingPointPairList> DataOut = new Dictionary<string, RollingPointPairList>();
        
    //    foreach (string param in streamData.Keys)
    //    {

    //        if (!DataOut.ContainsKey(param))
    //        {
    //            DataOut[param] = new RollingPointPairList(streamData[param].Count);
    //            double[] x = new double[streamData[param].Count];
    //            double[] y = new double[streamData[param].Count];
    //            uint cnt = 0;
    //            foreach (double dataItem in streamData[param])
    //            {
    //                x[cnt] = cnt;
    //                y[cnt] = dataItem;
    //                cnt++;
    //            }
    //            DataOut[param].Add(x,y);
    //        }
    //        else
    //        {
    //            DataOut[param].Clear();
    //            DataOut[param] = new RollingPointPairList(streamData[param].Count);
    //            double[] x = new double[streamData[param].Count];
    //            double[] y = new double[streamData[param].Count];
    //            uint cnt = 0;
    //            foreach (double dataItem in streamData[param])
    //            {
    //                x[cnt] = cnt;
    //                y[cnt] = dataItem;
    //                cnt++;
    //            }
    //            DataOut[param].Add(x, y);

    //        }
    //        //DataOut[param] = new RollingPointPairList([555]);
    //        //DataOut[param] = streamData[param].ToArray();
    //    }            
    //    return DataOut;
    //}             


    private static void parsePacket(Packet packet)
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
            foreach (string parName in Globals.channelsSelected[stream])
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


    //parse method using arrays -- TODO fix other types than analog.
    private static void parsePacket2(Packet packet)
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
            else stream = (int)frame.ReadUInt(Globals.inetStart + 4,Endianity.Big);//((frame[Globals.inetStart + 4] << 24) + (frame[Globals.inetStart + 5] << 16) + (frame[Globals.inetStart + 6] << 8) + frame[Globals.inetStart + 7]);
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
            foreach (string parName in Globals.channelsSelected[stream])
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


    //use PTP Time.
    private static void parsePacketPTP(Packet packet)
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
            UInt64 PTP1;
            UInt64 PTP2;
            PTP1 = frame.ReadUInt(16, Endianity.Big);
            PTP2 = frame.ReadUInt(20, Endianity.Big);

            MessageBox.Show(String.Format("{0} == {1}",PTP1,PTP2));
            uint i = 0;
            uint parPos, parCnt, parOccurences, parPosTmp;
            uint parType = 0;
            double value = 0.0;
            Dictionary<string, double[]> limit = Globals.limitPCAP[stream];
            int streamCnt = (int)limit[limit.Keys.First()][1];
            double[][] limitA = new double[Globals.limitArray[0].Length][];
            limitA = Globals.limitArray[0];
            Dictionary<string, limitPCAP_Derrived> limitDerrived = Globals.limitPCAP_derrived;
            foreach (string parName in Globals.channelsSelected[stream])
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
   
    
    
    
    
    //Check file access
    public static bool IsFileLocked(FileInfo file)
    {
        FileStream stream = null;

        try
        {
            stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch (IOException)
        {
            return true;
        }
        finally
        {
            if (stream != null)
                stream.Close();
        }
        return false;
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
