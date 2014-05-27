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

using System.Runtime.CompilerServices;

namespace Plot_iNET_X
{
    public partial class PlotData : Form
    {

        public static int streamID;
        public static byte[] frame =new byte[65536];
        public static Dictionary<string, List<double>> streamData;
        Dictionary<string, FilteredPointList> dataToPlot;

        public PlotData()
        {
            InitializeComponent();
        }
        public PlotData(int streamInput, bool isItList)
        {
            if (isItList==false) return;
            Globals.filePCAP = null;

            #region Initialize_Globals
            streamID = streamInput;
            streamData = new Dictionary<string, List<double>>();
            Globals.parError = new Dictionary<int, Dictionary<string, uint>>();
            Globals.totalErrors = 0;
            Globals.packetErrors = new Dictionary<int, Dictionary<string, uint>>();
            Globals.framesReceived = new Dictionary<int, int[]>();
            Globals.totalFrames = 0;
            foreach (int stream in Globals.limitPCAP.Keys)
            {
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
            }

            #endregion Initialize_Globals

            InitializeComponent();
            zedGraphControl1.ContextMenuBuilder += new ZedGraphControl.ContextMenuBuilderEventHandler(Add_item_toMenu);
            zedGraphControl1.GraphPane.IsFontsScaled = false;

        }
        public PlotData(int streamInput)
        {
#region Initialize_Globals
            streamID = streamInput;
            streamData = new Dictionary<string, List<double>>();


            Globals.parError = new Dictionary<int, Dictionary<string, uint>>();
            Globals.totalErrors = 0;
            Globals.packetErrors = new Dictionary<int, Dictionary<string, uint>>();
            Globals.framesReceived = new Dictionary<int, int[]>();
            Globals.totalFrames = 0;
            foreach (int stream in Globals.limitPCAP.Keys)
            {
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
            
        
        }

        #region ZedGRaph_Menu
        private void ZoomEvent(ZedGraphControl sender, ZoomState oldState, ZoomState newState)
        {
            // The maximum number of point to displayed is based on the width of the graphpane, and the visible range of the X axis
            foreach(string param in dataToPlot.Keys)
            {
                dataToPlot[param].SetBounds(sender.GraphPane.XAxis.Scale.Min, sender.GraphPane.XAxis.Scale.Max, (int)zedGraphControl1.GraphPane.Rect.Width);
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
            if (Globals.filePCAP ==null)
            {
                dataToPlot = new Dictionary<string, FilteredPointList>();
                bool firstTime = true;
                foreach( string p in Globals.filePCAP_list)
                {
                    GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced);
                    Dictionary <string, double[]> dataPcap_tmp = new Dictionary<string, double[]>();
                    LoadData(streamID, p);
                    
                    foreach(string parName in streamData.Keys)
                    {
                        dataPcap_tmp[parName] = streamData[parName].ToArray();          
                        //int len=0;
                        int previousLen =0;
                        if (firstTime) previousLen = dataPcap_tmp[parName].Length;
                        else
                        {                            
                            previousLen = dataToPlot[parName].Count;
                            previousLen = previousLen + dataPcap_tmp[parName].Length;
                        }                         
                        //RollingPointPairList dataTMP = new RollingPointPairList(len);
                        uint cnt=0;
                        //double[] x = new double[dataPcap_tmp[parName].Length];
                        //double[] y = new double[dataPcap_tmp[parName].Length];
                        double[] x = new double[previousLen];
                        double[] y = new double[previousLen];

                        if (firstTime == false)
                        {
                            //dataTMP.Add(dataToPlot[parName]);
                            for(int i=0; i!=dataToPlot[parName].Count;i++)
                            {
                                x[cnt] = cnt;
                                y[cnt] = dataToPlot[parName][i].Y;
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
                        dataToPlot[parName] = null;
                        dataToPlot[parName] = new FilteredPointList(x,y);
                        //dataToPlot[parName] = dataTMP;
                    }                    
                    firstTime = false;
                }
                sw2.Start();
                CreateGraph(zedGraphControl1, dataToPlot, streamID);
                sw2.Stop();
            }
                
            else CreateGraph(zedGraphControl1, LoadData(streamID), streamID);
            // Size the control to fill the form with a margin
            SetSize();
            sw.Stop();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            this.Text=String.Format("Total time for parsing ={0} | time to plot={1}",sw.Elapsed.ToString(),sw2.Elapsed.ToString());
        }
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
            //SetBounds(myPane.XAxis.Scale.Min, myPane.XAxis.Scale.Max, (int)zgc.GraphPane.Rect.Width);
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


        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void  LoadData(int streamID, string pcapfile)
        //Dictionary<string, RollingPointPairList> LoadData(int streamID, string pcapfile)
        {
            Dictionary<string, double[]> streamParameters = Globals.limitPCAP[streamID];
            streamData.Clear();
            try
            {
                //while (IsFileLocked(new FileInfo(Globals.filePCAP)))
                //{
                //    System.Threading.Thread.Sleep(100);
                //}
                OfflinePacketDevice selectedDevice = new OfflinePacketDevice(pcapfile);

                PacketCommunicator communicator = selectedDevice.Open(65536,                                  // portion of the packet to capture
                    // 65536 guarantees that the whole packet will be captured on all the link layers
                                        PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                        1000);                                  // read timeout
                communicator.ReceivePackets(0, parsePacket);
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Cannot open {0} or crashed during parsing, please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
                    pcapfile, e.ToString()));
            }

            //Dictionary<string, RollingPointPairList> DataOut = new Dictionary<string, RollingPointPairList>();
            //foreach (string param in streamData.Keys)
            //{

            //    if (!DataOut.ContainsKey(param))
            //    {
            //        DataOut[param] = new RollingPointPairList(streamData[param].Count);
            //        double[] x = new double[streamData[param].Count];
            //        double[] y = new double[streamData[param].Count];
            //        uint cnt = 0;
            //        foreach (double dataItem in streamData[param])
            //        {
            //            x[cnt] = cnt;
            //            y[cnt] = dataItem;
            //            cnt++;
            //        }
            //        DataOut[param].Add(x, y);
            //    }
            //    else
            //    {
            //        DataOut[param].Clear();
            //        DataOut[param] = new RollingPointPairList(streamData[param].Count);
            //        double[] x = new double[streamData[param].Count];
            //        double[] y = new double[streamData[param].Count];
            //        uint cnt = 0;
            //        foreach (double dataItem in streamData[param])
            //        {
            //            x[cnt] = cnt;
            //            y[cnt] = dataItem;
            //            cnt++;
            //        }
            //        DataOut[param].Add(x, y);

            //    }
            //    //DataOut[param] = new RollingPointPairList([555]);
            //    //DataOut[param] = streamData[param].ToArray();
            //}
            //return DataOut;
        }

        public static Dictionary<string, RollingPointPairList> LoadData(int streamID)
        {
            Dictionary<string, double[]> streamParameters = Globals.limitPCAP[streamID];
            streamData.Clear();
            try
            {
                //while (IsFileLocked(new FileInfo(Globals.filePCAP)))
                //{
                //    System.Threading.Thread.Sleep(100);
                //}
                OfflinePacketDevice selectedDevice = new OfflinePacketDevice(Globals.filePCAP);

                PacketCommunicator communicator = selectedDevice.Open(65536,                                  // portion of the packet to capture
                    // 65536 guarantees that the whole packet will be captured on all the link layers
                                        PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                        1000);                                  // read timeout
                communicator.ReceivePackets(0, parsePacket);
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Cannot open {0} or crashed during parsing, please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
                    Globals.filePCAP, e.ToString()), "Packet parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Dictionary<string, RollingPointPairList> DataOut = new Dictionary<string, RollingPointPairList>();
            foreach (string param in streamData.Keys)
            {

                if (!DataOut.ContainsKey(param))
                {
                    DataOut[param] = new RollingPointPairList(streamData[param].Count);
                    double[] x = new double[streamData[param].Count];
                    double[] y = new double[streamData[param].Count];
                    uint cnt = 0;
                    foreach (double dataItem in streamData[param])
                    {
                        x[cnt] = cnt;
                        y[cnt] = dataItem;
                        cnt++;
                    }
                    DataOut[param].Add(x,y);
                }
                else
                {
                    DataOut[param].Clear();
                    DataOut[param] = new RollingPointPairList(streamData[param].Count);
                    double[] x = new double[streamData[param].Count];
                    double[] y = new double[streamData[param].Count];
                    uint cnt = 0;
                    foreach (double dataItem in streamData[param])
                    {
                        x[cnt] = cnt;
                        y[cnt] = dataItem;
                        cnt++;
                    }
                    DataOut[param].Add(x, y);

                }
                //DataOut[param] = new RollingPointPairList([555]);
                //DataOut[param] = streamData[param].ToArray();
            }            
            return DataOut;
        }

        
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
                if (streamID != stream) //ignore streams not selected
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
                double value=0.0;
                Dictionary<string, double[]> limit = Globals.limitPCAP[stream];
                Dictionary<string, limitPCAP_Derrived> limitDerrived = Globals.limitPCAP_derrived;
                foreach (string parName in Globals.channelsSelected[stream])
                {
                    parPos = (uint)(limit[parName][4]);
                    parCnt = (uint)(limit[parName][3]);
                    parOccurences = (uint)(limit[parName][5]);
                    //if (Globals.channelsSelected[streamID].Contains(parName))
                    //{
                        if (!streamData.ContainsKey(parName))
                        {
                            streamData[parName] = new List<double>();
                        }
                            //streamData[param].Add(Convert.ToDouble(frame[(int)Globals.limitPCAP[streamID][param][4] + location]));
                        if (limit[parName][6] == 0) //ANALOG
                        {
                            for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                            {
                                parPosTmp =(uint) (parPos + parOccur * 2);
                                value = getValue.CalcValue(16, (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]), limit[parName][7], limit[parName][8]);
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
                            }
                        }
                        else if (limit[parName][6] == 3) //BCD temp - BIT101
                        {
                            for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                            {
                                parPosTmp = (uint)(parPos + parOccur * 2);
                                value = getValue.CalcValue(16, (double)(getValue.bcd2int((frame[parPos] << 8) + frame[parPos + 1])), limit[parName][7], limit[parName][8]);
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
                            }
                        }
                        else if (limit[parName][6] == 2) //BCU Status
                        {
                            for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                            {
                                parPosTmp = (uint)(parPos + parOccur * 2);
                                value = (double)((frame[parPos] << 8) + frame[parPos + 1]);
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
                        }
                        else if (limit[parName][6]==5) //derrived parameter 
                        {
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
                        }
                        else if (limit[parName][6]==6) //derrived parameter concated
                        {
                            string[] srcName = limitDerrived[parName].srcParametersName;

                            for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                            {
                                //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
                                parPosTmp = (uint)(parPos + parOccur * 2);
                                if (srcName.Length == 2)
                                {
                                    var par1Pos = (int)limit[srcName[0]][4] + parOccur*2;
                                    var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                    var par2Pos = (int)limit[srcName[1]][4] + parOccur * 2;
                                    var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                    value = getValue.getConcatedParameter(16, value1,value2); 
                                }
                                else if (srcName.Length == 4)
                                {
                                    var par1Pos = (int)limit[srcName[0]][4] + parOccur * 2;
                                    var value1 = getValue.getConcatedParameter(8, frame[par1Pos], frame[par1Pos + 1]);
                                    var par2Pos = (int)limit[srcName[1]][4] + parOccur * 2;
                                    var value2 = getValue.getConcatedParameter(8, frame[par2Pos], frame[par2Pos + 1]);
                                    var par3Pos = (int)limit[srcName[2]][4] + parOccur * 2;
                                    var value3 = getValue.getConcatedParameter(8, frame[par3Pos], frame[par3Pos + 1]);
                                    var par4Pos = (int)limit[srcName[3]][4] + parOccur * 2;
                                    var value4 = getValue.getConcatedParameter(8, frame[par4Pos], frame[par4Pos + 1]);
                                    var top = getValue.getConcatedParameter(16, value1, value2);
                                    var bottom = getValue.getConcatedParameter(16, value3, value4);
                                    
                                    value = getValue.getConcatedParameter(32, top, bottom);
                                }

                                //if ((value > limit[parName][9]) || (value < limit[parName][10]))
                                //{
                                //    // To do - handle error reporting
                                //    Globals.parError[stream][parName] += 1;
                                //    Globals.packetErrors[stream]["total"]++;
                                //    Globals.totalErrors++;
                                //}

                            }
                        }
                        else
                        {
                            for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                            {
                                parPosTmp = (uint)(parPos + parOccur * 2);
                                value = (double)((frame[parPosTmp] << 8) + frame[parPosTmp + 1]);
                            } //generate some methods for BCU report etc..
                        } i += 2;
                        streamData[parName].Add(value);
                    }
                }
            Globals.totalFrames++;
        }
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
    public static class Garbage
    {
        

    }
    public static class ErrorReporting
    {
        //TO DO
    }

    public static class getValue
    {
        public static long getConcatedParameter(int shift,int srcMSB, int srcLSB)
        {
            //value = Convert.ToInt64(String.Format("{0}{1}", srcMSB, srcLSB));
            var value = ((long)srcMSB << shift) + srcLSB;
            return value;
        }

        public static long getConcatedParameter(int shift, long srcMSB, long srcLSB)
        {
            //value = Convert.ToInt64(String.Format("{0}{1}", srcMSB, srcLSB));
            var value = ((long)srcMSB << shift) + srcLSB;
            return value;
        }


        public static double GetDerivedParameter(int bits, double srcInput, double FSRmax, double FSRmin, double con1, double con2)
        {
            //const * P_KAD_ADC_109_B_S1_0_AnalogIn(0) + const2
            double value;
            value = con1 * getValue.CalcValue(16, srcInput, FSRmax, FSRmin) + con2;
            return value;
        }
        public static double GetDerivedParameter(double srcInput, double con1, double con2, double con3)
        {
            //input of non converted parameter
            double value;
            value = (srcInput + con1) * con2 / con3;
            return value;
        }
        public static double GetDerivedParameter(int bits, double srcInput, double FSRmax, double FSRmin, double con1, double con2, double con3)
        {
            //-34055)*1200/2777
            double value;
            value = ((getValue.CalcValue(16, srcInput, FSRmax, FSRmin)+con1)*con2)/con3;
            return value;
        }
        public static double GetTDCVolt(double input) //finish this later if needed
        {
            double voltage = 0.0;
            return voltage;
        }
        public static double CalcValue(int bits, double input, double FSRmax, double FSRmin)
        {
            double result = input * (FSRmax - FSRmin) / (Math.Pow(2, bits) - 1) + FSRmin;
            return result;
        }
        public static int bcd2int(int bcd)
        {
            return int.Parse(bcd.ToString("X"));
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
