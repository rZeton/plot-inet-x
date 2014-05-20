using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System.Threading;

namespace GraphicalPacketCheck
{
    public partial class PacketCheck : Form
    {
        public selectDevice selectObj;
        public selectIP IPobj;
        public PlotData GraphPlot;
        public static List_Parameter_Names listObj;
        public static MethodInvoker logThis,UpdateStatusBar,UpdateSummary;
        public selectChannel selChanObj;

        public PacketCheck()
        {
            Globals.digitsNumber = 7;
            InitializeComponent();
            SelectNIC();
            Globals.AcquisitionTimer = new System.Timers.Timer(5000);
            Globals.AcquisitionTimer.Enabled = false;
            Globals.AcquisitionTimer.Elapsed += new System.Timers.ElapsedEventHandler(AcquisitionTimer_Click);
            //MeasureIntCasts();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            GetDataHoldersThread();
            if (Globals.errorFile == "") Globals.errorFile = "error";
            if (Globals.outputFile == null)
            {
                Globals.outputFile = new Dictionary<int, string>(Globals.limitPCAP.Keys.Count);
                foreach (int streamID in Globals.limitPCAP.Keys)
                {
                    Globals.outputFile[streamID] = String.Format("Stream_{0}_", streamID);
                }
            
            }
            if (Globals.streamSaveRate == 0) Globals.streamSaveRate = 3600;
            OpenPcapPort();
            this.button2.Dispose();
            this.toolStripDropDownButton1.Dispose();
            this.toolStripDropDownButton2.Dispose();
            Globals.AcquisitionTimer.Enabled = true;
        }                 //CONNECT
        public void OpenPcapPort()
        {
            #region Initialize Globals
            Globals.channelsSelected = new Dictionary<int, List<string>>();
            Globals.parError = new Dictionary<int, Dictionary<string, uint>>();
            Globals.streamRate = new Dictionary<int, double>();           
            Globals.totalErrors = 0;
            Globals.packetErrors = new Dictionary<int, Dictionary<string, uint>>();
            Globals.framesReceived = new Dictionary<int, int[]>();
            Globals.totalFrames = 0;
            Globals.saveQueue = new Dictionary<int, List<string>>();
            foreach (int stream in Globals.limitPCAP.Keys)
            {
                Globals.channelsSelected[stream] = new List<string>();
                Globals.packetErrors[stream] = new Dictionary<string, uint>(){
                {"total",0}, //used for all parameters in that stream
                {"pktLost",0},
                {"SEQ",0}};
                Globals.streamRate[stream] = Globals.limitPCAP[stream][Globals.limitPCAP[stream].Keys.First()][2];
                Globals.allstreamsRate += Convert.ToInt16(Globals.streamRate[stream]);
                Globals.parError[stream] = new Dictionary<string, uint>();
                Globals.framesReceived[stream] = new int[4] { 0, 0, 0,0 };
                Globals.saveQueue[stream] = new List<string>();
                foreach (string parName in Globals.limitPCAP[stream].Keys)
                {
                    Globals.parError[stream][parName] = 0;
                }
            } //count parameters, add error counter for each

            //Globals.Timer = new System.Diagnostics.Stopwatch[2]; Globals.Timer[0] = new System.Diagnostics.Stopwatch(); Globals.Timer[1] = new System.Diagnostics.Stopwatch();
            #endregion Initialize Globals
            Save.LogError(new List<string>(), -1);
            PacketDevice selectedDevice = Globals.selectedDevice;
            using (PacketCommunicator communicator =
                selectedDevice.Open(65536,                                  // portion of the packet to capture
                // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.NoCaptureLocal,//Promiscuous, // promiscuous mode
                                    1000))                                  // read timeout
            {
                if (selectedDevice.Addresses.Count == 0)
                {
                    //this.textBox1.Text = String.Format("{0} is down.. Power Up Chassis or check your connection", selectedDevice.Description);
                    System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("ipconfig.exe");
                    // The following commands are needed to redirect the standard output.
                    // This means that it will be redirected to the Process.StandardOutput StreamReader.
                    procStartInfo.RedirectStandardOutput = true;
                    procStartInfo.UseShellExecute = false;
                    // Do not create the black window.
                    procStartInfo.CreateNoWindow = true;
                    // Now we create a process, assign its ProcessStartInfo and start it
                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.StartInfo = procStartInfo;
                    proc.Start();
                    // Get the output into a string
                    string result = proc.StandardOutput.ReadToEnd();
                    this.textBox1.Text = result;
                    MessageBox.Show(String.Format("{0} is down..\n Power Up Chassis or check your connection\n\n{1} ", selectedDevice.Description, result));
                    this.button2.Visible =true;
                    return;
                }
                this.textBox1.Text = String.Format("Listening on " + selectedDevice.Description + "...");
                this.Refresh();
                //Globals.inetStart = 0; // use this only if looking at UDP packet, currently only payload, changed to constant                       

                try
                {
                    backgroundWorker1.RunWorkerAsync();
                }
                catch (Exception e)
                {
                    MessageBox.Show(String.Format("PCPAP Error:\n{0}\n", e.ToString(), e.Data));
                }
            }

        }


        #region Drawingitems


        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Globals.digitsNumber = 6;
            //ChangeDigits();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            Globals.digitsNumber = 9;
            //ChangeDigits();
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            Globals.digitsNumber = 16;
            //ChangeDigits();
        }
        private void ChangeDigits()
        {
            foreach (int stream in Globals.dataHoldersDict.Keys)
            {
                foreach (Control tab in this.streamParameterTabControl.Controls)
                {
                    if ((tab is TabPage) && (tab.Name == stream.ToString()))
                    {
                        tab.ResumeLayout(true);
                        foreach (Label datlab in Globals.dataHoldersDict[stream])
                        {

                            datlab.Text = String.Format("{0}", datlab.Text.PadRight(Globals.digitsNumber, '-'));
                            datlab.Size = datlab.PreferredSize;
                        }
                        foreach (Control ctr in tab.Controls)
                        {
                            if (ctr is FlowLayoutPanel)
                            {
                                foreach (TableLayoutPanel flow in ctr.Controls)
                                {
                                    flow.ResumeLayout(true);
                                    flow.Size = flow.PreferredSize;
                                    flow.SuspendLayout();
                                    flow.ResumeLayout(false);
                                }
                            }
                        }
                        tab.Size = tab.PreferredSize;
                        tab.SuspendLayout();
                        tab.ResumeLayout(false);
                    }
                }
            }
        }

        private void DrawParametersThread()
        {
            this.streamParametersFlow0.Dispose();
            this.tabPage1.Dispose();
            this.tabStream0.Dispose();
            //if (Globals.streamID != -1) { this.streamParametersFlow0.Controls.Clear(); }
            try
            {
                TabPage[] tabStream = new TabPage[Globals.limitPCAP.Keys.Count];
                TabPage[] tabSummary = new TabPage[Globals.limitPCAP.Keys.Count];
                int pktCnt = 0;
                foreach (int stream in Globals.limitPCAP.Keys)
                {
                    //string[] parametersList = new string[] { Globals.limitPCAP[stream].Keys};
                    List<string> parametersList = new List<string>(Globals.limitPCAP[stream].Keys);
                    double[] data = new double[Globals.limitPCAP[stream].Count];
                    Label[] dataHolders = new Label[data.Length];
                    Label[] dataLabels = new Label[data.Length];
                    TableLayoutPanel[] dataColumns = new TableLayoutPanel[data.Length / 30 + 1];
                    FlowLayoutPanel flow = new FlowLayoutPanel();
                    flow.FlowDirection = FlowDirection.LeftToRight;
                    tabStream[pktCnt] = new TabPage();
                    for (int i = 0; i != data.Length; i++)
                    {
                        int whichColumn = i / 30;
                        dataLabels[i] = new Label();
                        dataLabels[i].Name = "DataLabel" + i;
                        dataLabels[i].AutoSize = false;
                        //dataLabels[i].Text = String.Format("P{0}", i.ToString().PadLeft(3, '0'));     // Changed to parameter name as ACLS has XX123 convention
                        dataLabels[i].Text = String.Format("{0}", parametersList[i].ToString());
                        dataLabels[i].Font = new Font(dataLabels[i].Font.FontFamily, 8, dataLabels[i].Font.Style);

                        dataLabels[i].Size = dataLabels[i].PreferredSize;
                        dataHolders[i] = new Label();
                        dataHolders[i].Name = i.ToString();
                        dataHolders[i].AutoSize = false;
                        dataHolders[i].Text = String.Format("{0}", data[i].ToString().PadLeft(Globals.digitsNumber, '0'));
                        dataHolders[i].Font = new Font(dataLabels[i].Font.FontFamily, 8, dataLabels[i].Font.Style);
                        //Globals.dataHolders[i].TextAlign = ContentAlignment.BottomLeft;
                        dataHolders[i].Size = dataHolders[i].PreferredSize;

                        if (i % 30 == 0)
                        {
                            dataColumns[whichColumn] = new TableLayoutPanel();
                            dataColumns[whichColumn].ColumnCount = 2;
                            dataColumns[whichColumn].RowCount = 30;
                        }
                        dataColumns[whichColumn].Controls.Add(dataLabels[i]);
                        dataColumns[whichColumn].Controls.Add(dataHolders[i]);
                        dataColumns[whichColumn].Size = dataColumns[whichColumn].PreferredSize;
                    }
                    for (int i = 0; i != dataColumns.Length; i++)
                    {
                        flow.Controls.Add(dataColumns[i]);
                    }
                    flow.SuspendLayout();
                    flow.ResumeLayout(false);
                    //tabStream[pktCnt].AutoScroll = true;
                    //tabStream[pktCnt].AutoScrollPosition = new System.Drawing.Point(349, 0);
                    flow.Size = flow.PreferredSize;
                    tabStream[pktCnt].Controls.Add(flow);
                    tabStream[pktCnt].Name = stream.ToString();
                    tabStream[pktCnt].Text = String.Format("ID={0}", stream);
                    tabStream[pktCnt].Size = tabStream[pktCnt].PreferredSize;
                    //create summary tab
                    tabSummary[pktCnt] = new TabPage(String.Format("ID={0}", stream));
                    tabSummary[pktCnt].Name = stream.ToString();
                    tabStream[pktCnt].AutoScroll = true;
                    Label[] summaryLabel = new Label[8];
                    Panel sumpanel = new Panel();
                    for (int i = 0; i != summaryLabel.Length; i++) summaryLabel[i] = new Label();
                    summaryLabel[0].Text = "Lost Packets";
                    summaryLabel[1].Text = "Sequence Errors";
                    summaryLabel[2].Text = "Parameter Errors";
                    summaryLabel[3].Text = "Frames Received";// +stream.ToString();
                    summaryLabel[0].Name = "l";
                    summaryLabel[1].Name = "l";
                    summaryLabel[2].Name = "l";
                    summaryLabel[3].Name = "l";
                    summaryLabel[0].Location = new System.Drawing.Point(6, 3);
                    summaryLabel[1].Location = new System.Drawing.Point(6, 16);
                    summaryLabel[2].Location = new System.Drawing.Point(6, 29);
                    summaryLabel[3].Location = new System.Drawing.Point(6, 42);
                    //summary dataHolders
                    summaryLabel[4].Text = "".PadLeft(10, '0');
                    summaryLabel[5].Text = "".PadLeft(10, '0');
                    summaryLabel[6].Text = "".PadLeft(10, '0');
                    summaryLabel[7].Text = "".PadLeft(10, '0');
                    summaryLabel[4].Name = "Lost Packets";
                    summaryLabel[5].Name = "Sequence Errors";
                    summaryLabel[6].Name = "Parameter Errors";
                    summaryLabel[7].Name = "Frames Received";
                    summaryLabel[4].Location = new System.Drawing.Point(98, 3);
                    summaryLabel[5].Location = new System.Drawing.Point(98, 16);
                    summaryLabel[6].Location = new System.Drawing.Point(98, 29);
                    summaryLabel[7].Location = new System.Drawing.Point(98, 42);
                    summaryLabel[4].BackColor = System.Drawing.Color.Red;
                    summaryLabel[5].BackColor = System.Drawing.Color.Red;
                    summaryLabel[6].BackColor = System.Drawing.Color.Red;
                    summaryLabel[7].BackColor = System.Drawing.Color.White;
                    summaryLabel[4].ForeColor = System.Drawing.Color.White;
                    summaryLabel[5].ForeColor = System.Drawing.Color.White;
                    summaryLabel[6].ForeColor = System.Drawing.Color.White;
                    summaryLabel[7].ForeColor = System.Drawing.Color.Black;
                    for (int i = 0; i != summaryLabel.Length; i++)
                    {
                        summaryLabel[i].Size = summaryLabel[i].PreferredSize;
                        sumpanel.Controls.Add(summaryLabel[i]);
                    }
                    sumpanel.SuspendLayout(); sumpanel.ResumeLayout(false);
                    tabSummary[pktCnt].Controls.Add(sumpanel);
                    tabSummary[pktCnt].SuspendLayout();
                    tabSummary[pktCnt].ResumeLayout(false);
                    pktCnt++;
                }
                foreach (TabPage stream in tabStream)
                {
                    this.streamParameterTabControl.Controls.Add(stream);
                }
                foreach (TabPage stream in tabSummary)
                {
                    this.streamSummaryTab.Controls.Add(stream);
                }
                this.streamSummaryTab.SuspendLayout(); this.streamSummaryTab.ResumeLayout(false);
                this.streamParameterTabControl.SuspendLayout(); this.streamParameterTabControl.ResumeLayout(false);
                this.streamParameterTabControl.Size = this.streamParameterTabControl.PreferredSize;
                //this.Size = this.PreferredSize;
                this.SuspendLayout();
                this.ResumeLayout(false);
                this.Refresh();
                // for future use // DrawParametersList();
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("No Stream ID was selected or StreamID={0} is not correct, check your limits file.\n\n{1}", Globals.streamID, e.Message.ToString()));
            }
        }
        private void GetDataHoldersThread()
        {
            DrawParametersThread();
            Globals.dataHoldersDict = new Dictionary<int, Label[]>(Globals.limitPCAP.Keys.Count);
            Globals.dataNameHoldersDict = new Dictionary<int, Label[]>(Globals.limitPCAP.Keys.Count);
            Globals.summaryHoldersDict = new Dictionary<int, Label[]>(Globals.limitPCAP.Keys.Count);
            foreach (int stream in Globals.limitPCAP.Keys)
            {
                Globals.dataHoldersDict[stream] = new Label[Globals.limitPCAP[stream].Count];
                Globals.dataNameHoldersDict[stream] = new Label[Globals.limitPCAP[stream].Count];
                Globals.summaryHoldersDict[stream] = new Label[4];
                int[] i = { 0, 0 };
                foreach (Control tab in this.streamParameterTabControl.Controls)
                {
                    if ((tab is TabPage) && (tab.Name == stream.ToString()))
                    {
                        foreach (Control flow in tab.Controls)
                        {
                            if (flow is FlowLayoutPanel)
                            {
                                foreach (Control col in flow.Controls)
                                {
                                    if (col is TableLayoutPanel)
                                    {
                                        foreach (Control txt in col.Controls)
                                        {
                                            if (txt is Label)
                                            {
                                                if (!txt.Name.Contains('a'))
                                                {
                                                    Label parLabel = txt as Label;
                                                    Globals.dataHoldersDict[stream][i[0]] = parLabel;
                                                    i[0]++;
                                                }
                                                else
                                                {
                                                    Label parLabel = txt as Label;
                                                    Globals.dataNameHoldersDict[stream][i[1]] = parLabel;
                                                    i[1]++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                int x = 0;
                foreach (Control tab in this.streamSummaryTab.Controls)
                {
                    if ((tab is TabPage) && (tab.Name == stream.ToString()))
                    {
                        foreach (Control panel in tab.Controls)
                        {
                            if (panel is Panel)
                            {
                                foreach (Control lab in panel.Controls)
                                {
                                    if ((lab is Label) && (lab.Name != "l"))
                                    {
                                        Label summaryLab = lab as Label;
                                        Globals.summaryHoldersDict[stream][x] = summaryLab;
                                        x++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion Drawingitems

        #region DATA Evaluation
        private void AnalyseDataThread(byte[] frame, int streamID)
        {
            int i = 0;
            double value = 0;
            //int streamID = Globals.streamID;
            Dictionary<string, double[]> limit = Globals.limitPCAP[streamID];
            StringBuilder errorMSG = new StringBuilder();
            if (frame.Length < Globals.streamLength[streamID])
            {
                errorMSG.AppendFormat("{0},{1},{2}, Packet is too short ,{3}, Shall be,{4},\n", streamID, Globals.framesReceived[streamID][0], Globals.totalErrors,
                    frame.Length, Globals.streamLength[streamID]);
                Globals.packetErrors[streamID]["pktLost"] += 1;
                Globals.totalErrors++;
                Globals.saveQueue[streamID].Add(errorMSG.ToString());
                return;
            }

            Globals.framesReceived[streamID][1] = ((frame[Globals.inetStart + 8] << 24) + (frame[Globals.inetStart + 9] << 16) + (frame[Globals.inetStart + 10] << 8) + frame[Globals.inetStart + 11]);
            if (Globals.framesReceived[streamID][1] != (Globals.framesReceived[streamID][2] + 1))
            {
                errorMSG.AppendFormat("{4},{0},{1}, Wrong sequence number detected,{2}, where previous SEQ was,{3},\n", Globals.framesReceived[streamID][0], Globals.totalErrors,
                    Globals.framesReceived[streamID][1], Globals.framesReceived[streamID][2], streamID);
                //Globals.packetErrors[streamID]["pktLost"] += (uint)Math.Abs(Globals.framesReceived[streamID][1] - (Globals.framesReceived[streamID][2] + 1));
                Globals.packetErrors[streamID]["SEQ"]++;
                Globals.totalErrors++;
            }
            Globals.framesReceived[streamID][2] = Globals.framesReceived[streamID][1];
            int parPos, parCnt, parOccurences,parPosTmp;
            string OtherValue;
            foreach (string parName in limit.Keys)
            {
                parPos = (int)(limit[parName][4]);
                parCnt = (int)(limit[parName][3]); //initial start+ occurences
                parOccurences = (int)(limit[parName][5]);
                if (limit[parName][6] == 0) //ANALOG
                {
                    for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                    {
                        parPosTmp = parPos + parOccur * 2;
                        value = CalcValue(16, (double)((frame[parPos] << 8) + frame[parPos + 1]), limit[parName][7], limit[parName][8]);
                        if ((value > limit[parName][9]) || (value < limit[parName][10]))
                        {
                            errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                Globals.totalErrors,                            //total error count for all streams
                                streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                Globals.parError[streamID][parName],            //error count for parameter
                                parName,                                        //parameter name
                                value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                parOccur);                     
                            Globals.parError[streamID][parName] += 1;
                            Globals.packetErrors[streamID]["total"]++;
                            Globals.totalErrors++;
                        }
                    }
                }
                else if (limit[parName][6] == 3) //BCD temp - BIT101
                {
                    for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                    {
                        parPosTmp = parPos + parOccur * 2;
                        value = CalcValue(16, (double)(bcd2int((frame[parPos] << 8) + frame[parPos + 1])), limit[parName][7], limit[parName][8]);
                        if ((value > limit[parName][9]) || (value < limit[parName][10]))
                        {
                            errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                Globals.totalErrors,                            //total error count for all streams
                                streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                Globals.parError[streamID][parName],            //error count for parameter
                                parName,                                        //parameter name
                                value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                parOccur);                     
                            Globals.parError[streamID][parName] += 1;
                            Globals.packetErrors[streamID]["total"]++;
                            Globals.totalErrors++;
                        }
                    }
                }
                else if (limit[parName][6] == 2) //BCU Status
                {
                    for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                    {
                        parPosTmp = parPos + parOccur * 2;
                        value = (double)((frame[parPos] << 8) + frame[parPos + 1]);
                        if ((value > limit[parName][9]) || (value < limit[parName][10]))
                        {
                            errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                Globals.totalErrors,                            //total error count for all streams
                                streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                Globals.parError[streamID][parName],            //error count for parameter
                                parName,                                        //parameter name
                                value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                parOccur);
                            Globals.parError[streamID][parName] += 1;
                            Globals.packetErrors[streamID]["total"]++;
                            Globals.totalErrors++;
                        }
                    }
                }
                else
                {
                    for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                    {
                        parPosTmp = parPos + parOccur * 2; value = (double)((frame[parPos] << 8) + frame[parPos + 1]);
                    } //generate some methods for BCU report etc..
                } i += 2;
            }
            //Save.LogError(errorMSG.ToString(), streamID);
            Globals.saveQueue[streamID].Add(errorMSG.ToString());
        }
        private void UpdateParametersThread(byte[] frame, int streamID)
        {
            int i = 0;
            double value = 0;
            //int streamID = Globals.streamID;
            Dictionary<string, double[]> limit = Globals.limitPCAP[streamID];
            StringBuilder errorMSG = new StringBuilder();
            if (frame.Length < Globals.streamLength[streamID])
            {
                errorMSG.AppendFormat("{0},{1},{2}, Packet is too short ,{3}, Shall be,{4},\n", streamID, Globals.framesReceived[streamID][0], Globals.totalErrors,
                    frame.Length, Globals.streamLength[streamID]);
                //errorMSG.AppendFormat(Globals.stringFormat, "{0},{1},{2}, Packet is too short ,{3}, Shall be,{4},\n", streamID, Globals.framesReceived[streamID][0], Globals.totalErrors,
                //    frame.Length, Globals.streamLength[streamID]);
                Globals.packetErrors[streamID]["pktLost"] += 1;
                Globals.totalErrors++;
                Globals.saveQueue[streamID].Add(errorMSG.ToString());
                return;
            }
            Globals.framesReceived[streamID][1] = ((frame[Globals.inetStart + 8] << 24) + (frame[Globals.inetStart + 9] << 16) + (frame[Globals.inetStart + 10] << 8) + frame[Globals.inetStart + 11]);
            if (Globals.framesReceived[streamID][1] != ++Globals.framesReceived[streamID][2])
            {
                errorMSG.AppendFormat("{0},{1},{2}, Wrong sequence no. ,{3},{4},\n", streamID, Globals.framesReceived[streamID][0], Globals.totalErrors,
                    Globals.framesReceived[streamID][1], Globals.framesReceived[streamID][2]);
                //Globals.packetErrors[streamID]["pktLost"] += (uint)Math.Abs(Globals.framesReceived[streamID][1] - (Globals.framesReceived[streamID][2] + 1));
                Globals.packetErrors[streamID]["SEQ"]++;
                Globals.totalErrors++;
            }
            Globals.framesReceived[streamID][2] = Globals.framesReceived[streamID][1];
            int parPos;
            int parPosTmp;
            int parCnt;
            int parOccurences;// = 0;
            foreach (string parName in limit.Keys)
            {
                parPos = (int)(limit[parName][4]);
                parCnt = (int)(limit[parName][3]);
                parOccurences = (int)(limit[parName][5]);
                if (limit[parName][6] == 0) //check if it is offset or bitVector
                {
                    for (int parOccur = 0; parOccur < parOccurences; parOccur++)
                    {
                        parPosTmp = parPos + parOccur * 2;
                        value = CalcValue(16, (double)((frame[parPos] << 8) + frame[parPos + 1]), limit[parName][7], limit[parName][8]);
                        if (parOccur == 0)
                        {
                            logThis = delegate
                            {
                                Globals.dataHoldersDict[streamID][parCnt].Text = value.ToString().PadRight(16, ' ');
                                if (Globals.parError[streamID][parName] != 0) Globals.dataNameHoldersDict[streamID][parCnt].BackColor = Color.Red;
                            };
                                Globals.dataHoldersDict[streamID][parCnt].Invoke(logThis);
                        }
                        if ((value > limit[parName][9]) || (value < limit[parName][10]))
                        {
                            if (parOccur == 0)
                            {
                                logThis = delegate
                                {
                                    Globals.dataHoldersDict[streamID][parCnt].BackColor = Color.Red;
                                };
                                Globals.dataHoldersDict[streamID][parCnt].Invoke(logThis);
                            }
                            errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                Globals.totalErrors,                            //total error count for all streams
                                streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                Globals.parError[streamID][parName],            //error count for parameter
                                parName,                                        //parameter name
                                value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                parOccur);
                            Globals.parError[streamID][parName] += 1;
                            Globals.packetErrors[streamID]["total"]++;
                            Globals.totalErrors++;
                        }
                        else
                        {
                            if (parOccur == 0)
                            {
                                logThis = delegate { Globals.dataHoldersDict[streamID][parCnt].BackColor = Color.LightGreen; };
                                Globals.dataHoldersDict[streamID][parCnt].Invoke(logThis);
                            }
                        }
                    }
                }
                else if (limit[parName][6] == 2)
                {
                    string OtherValue = Convert.ToString(frame[parPos], 2).PadLeft(8, '0') + Convert.ToString(frame[parPos + 1], 2).PadLeft(8, '0');
                    logThis = delegate
                    {
                        Globals.dataHoldersDict[streamID][parCnt].Text = OtherValue.ToString().PadLeft(6, '0');
                        Globals.dataHoldersDict[streamID][parCnt].BackColor = Color.YellowGreen;
                    };
                    Globals.dataHoldersDict[streamID][parCnt].Invoke(logThis);
                    for (int parOccur = 0; parOccur != parOccurences; parOccur++)
                    {
                        parPosTmp = parPos + parOccur * 2;
                        value = (double)((frame[parPos] << 8) + frame[parPos + 1]);
                        if ((value > limit[parName][9]) || (value < limit[parName][10]))
                        {
                            errorMSG.AppendFormat("{0},{1},{2},{3}, Value of ,{4}, = ,{5},it should be between,{7},{6},occurence=,{8},\n ",
                                Globals.totalErrors,                            //total error count for all streams
                                streamID, Globals.framesReceived[streamID][0],  //stream ID, frames received per stream
                                Globals.parError[streamID][parName],            //error count for parameter
                                parName,                                        //parameter name
                                value, limit[parName][10], limit[parName][9],   //current parameter value, limit max, limit min 
                                parOccur);
                        }
                    }
                }
                else
                {
                    string OtherValue = ((frame[parPos] << 8) + frame[parPos + 1]).ToString();
                    logThis = delegate
                    {
                        Globals.dataHoldersDict[streamID][parCnt].Text = OtherValue.ToString().PadLeft(6, '0');
                        Globals.dataHoldersDict[streamID][parCnt].BackColor = Color.Gainsboro;
                    };
                    Globals.dataHoldersDict[streamID][parCnt].Invoke(logThis);

                } //generate some methods for BCU report etc..
                i += 2;
            }
            //Save.LogError(errorMSG.ToString(), streamID);
            Globals.saveQueue[streamID].Add(errorMSG.ToString());

            //Update the summary
            UpdateStatusBar = delegate
            {
                this.totalFramesReceivedLabel.Text = Globals.totalFrames.ToString().PadLeft(16, '0');
                this.totalErrorsLabel.Text = (Globals.totalErrors).ToString().PadLeft(10, '0');
            };
            this.statusBar.Invoke(UpdateStatusBar);
            UpdateSummary = delegate
            {
                Globals.summaryHoldersDict[streamID][2].Text = Globals.packetErrors[streamID]["total"].ToString().PadLeft(10, '0');
                Globals.summaryHoldersDict[streamID][0].Text = Globals.packetErrors[streamID]["pktLost"].ToString().PadLeft(10, '0');
                Globals.summaryHoldersDict[streamID][1].Text = Globals.packetErrors[streamID]["SEQ"].ToString().PadLeft(10, '0');
                Globals.summaryHoldersDict[streamID][3].Text = Globals.framesReceived[streamID][0].ToString().PadLeft(10, '0');
            };
            this.streamSummaryTab.Invoke(UpdateSummary);
        }

        public static double GetTDCVolt(double input) //finish this later if needed
        {
            double voltage=0.0;
            return voltage;
        }
        public static double CalcValue(int bits, double input, double FSRmax, double FSRmin)
        {
            double result= input * (FSRmax - FSRmin) / (Math.Pow(2, bits) - 1) + FSRmin;         
            return result;
        }
        public static int bcd2int(int bcd)
        {
            return int.Parse(bcd.ToString("X"));
        }
        #endregion DATA Evaluation


        #region ThreadVersion
        //Listener Section
        private void backgroundWorker1_DoWork_1(object sender, DoWorkEventArgs e)
        {
            getPacketEvent();
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
          //  object result = e.Result;

        }

        public void getPacketEvent()
        {
            byte[] frame = new byte[65536];
            int streamID;           
            Dictionary<int,int> streamRate = new Dictionary<int,int>(Globals.streamRate.Keys.Count);
            Dictionary<int,int> streamRatePlot = new Dictionary<int,int>(Globals.streamRate.Keys.Count);
            
            foreach (int stream in Globals.streamRate.Keys)
            {
                streamRate[stream] = (Int16)(Globals.streamRate[stream]);
                streamRatePlot[stream] = streamRate[stream]*Globals.streamSaveRate;
            }
            
            PacketCommunicator communicator = Globals.selectedDevice.Open(65536,                                  // portion of the packet to capture
                // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.NoCaptureLocal,//Promiscuous, // promiscuous mode
                                    1000);                                  // read timeout
            var query = from packet in communicator.ReceivePackets(-1) //number of packets prior to analyse, use stream rate of 1. par (acquisition for 1 sec prior to analyse)
                        where ((packet.Ethernet.EtherType == PcapDotNet.Packets.Ethernet.EthernetType.IpV4) && (packet.Ethernet.IpV4.Udp.Length != 0))//&&(packet.Ethernet.IpV4.Source.ToString() == "192.168.28.1"))
                        //filter packets by not empty and from specified source
                        select new { packet.Timestamp, packet.Ethernet.IpV4.Udp.Payload }; //get payload and timestamp from Network Card 

            foreach (var Payload in query)
            {              
                frame = Payload.Payload.ToArray();
                if (frame.Length < 28) streamID = -1;
                else streamID = ((frame[Globals.inetStart + 4] << 24) + (frame[Globals.inetStart + 5] << 16) + (frame[Globals.inetStart + 6] << 8) + frame[Globals.inetStart + 7]);
                if (!Globals.limitPCAP.ContainsKey(streamID)) //ignore streams not selected
                {
                    //this shall never happen?
                    //Globals.totalErrors++;
                    //Save.LogError(String.Format("-1,{0},{1},Unexpected Stream ID received = {2},\n", Globals.totalFrames, Globals.totalErrors, streamID), -1);
                    continue;
                }
                else
                {
                    Globals.totalFrames++;
                    if (Globals.framesReceived[streamID][0] == 0) //first time frame arrives to a stream
                    {
                        Globals.framesReceived[streamID][2] = ((frame[Globals.inetStart + 8] << 24) + (frame[Globals.inetStart + 9] << 16)
                            + (frame[Globals.inetStart + 10] << 8) + frame[Globals.inetStart + 11]) - 1;
                        UpdateParametersThread(frame, streamID);
                        
                        Save.SaveFrame(frame, Payload.Timestamp, streamID); // Save all frames from known streams change to binary form
                    }
                    else
                    {
                        if (Globals.framesReceived[streamID][0] % streamRate[streamID] == 0) //update labels for each stream every second
                        {
                            UpdateParametersThread(frame, streamID);
                        }
                        else
                        {
                            AnalyseDataThread(frame, streamID);
                        }

                        if (Globals.framesReceived[streamID][0] % streamRatePlot[streamID] == 0)
                        {
                            Save.SaveFrame(frame, Payload.Timestamp, streamID); // Save all frames from known streams change to binary form
                        }
                    }
                    Globals.framesReceived[streamID][0]++;
                }
            }
            
        }

        //End of Listener

        //not in use atm, for future to have reference with respect to range and limits in second window.
        private void DrawParametersList()
        {
            try
            {

                TabPage[] tabStream = new TabPage[Globals.limitPCAP.Keys.Count];
                int pktCnt = 0;
                foreach (int stream in Globals.limitPCAP.Keys)
                {
                    double[] data = new double[Globals.limitPCAP[stream].Count];
                    Label[] dataHolders = new Label[data.Length];
                    Label[] dataLabels = new Label[data.Length];
                    TableLayoutPanel[] dataColumns = new TableLayoutPanel[data.Length / 30 + 1];
                    FlowLayoutPanel flow = new FlowLayoutPanel();
                    flow.FlowDirection = FlowDirection.LeftToRight;
                    tabStream[pktCnt] = new TabPage();
                    for (int i = 0; i != data.Length; i++)
                    {
                        int whichColumn = i / 30;
                        dataLabels[i] = new Label();
                        dataLabels[i].Name = "DataLabel" + i;
                        dataLabels[i].AutoSize = false;
                        dataLabels[i].Text = String.Format("P{0}", i.ToString().PadLeft(3, '0'));
                        dataLabels[i].Font = new Font(dataLabels[i].Font.FontFamily, 8, dataLabels[i].Font.Style);
                        //dataLabels[i].TextAlign = ContentAlignment.BottomLeft;
                        dataLabels[i].Size = dataLabels[i].PreferredSize;
                        dataHolders[i] = new Label();
                        dataHolders[i].Name = i.ToString();
                        dataHolders[i].AutoSize = false;
                        dataHolders[i].Text = String.Format("{0}", data[i].ToString().PadLeft(6, '0'));
                        dataHolders[i].Font = new Font(dataLabels[i].Font.FontFamily, 8, dataLabels[i].Font.Style);
                        //Globals.dataHolders[i].TextAlign = ContentAlignment.BottomLeft;
                        dataHolders[i].Size = dataHolders[i].PreferredSize;

                        if (i % 30 == 0)
                        {
                            dataColumns[whichColumn] = new TableLayoutPanel();
                            dataColumns[whichColumn].ColumnCount = 2;
                            dataColumns[whichColumn].RowCount = 30;
                        }
                        dataColumns[whichColumn].Controls.Add(dataLabels[i]);
                        dataColumns[whichColumn].Controls.Add(dataHolders[i]);
                        dataColumns[whichColumn].Size = dataColumns[whichColumn].PreferredSize;
                    }
                    for (int i = 0; i != dataColumns.Length; i++)
                    {
                        flow.Controls.Add(dataColumns[i]);
                    }
                    flow.SuspendLayout();
                    flow.ResumeLayout(false);

                    flow.Size = flow.PreferredSize;
                    tabStream[pktCnt].Controls.Add(flow);
                    tabStream[pktCnt].Name = stream.ToString();
                    tabStream[pktCnt].Text = String.Format("ID={0}", stream);
                    tabStream[pktCnt].Size = tabStream[pktCnt].PreferredSize;
                    pktCnt++;
                }

                listObj = new List_Parameter_Names();
                listObj.Text = "Details of parameters";
                TabControl tabsForDataNames = new TabControl();

                foreach (TabPage stream in tabStream)
                {
                    tabsForDataNames.Controls.Add(stream);
                }
                tabsForDataNames.Size = tabsForDataNames.PreferredSize;
                listObj.Controls.Add(tabsForDataNames);
                listObj.AutoScroll = true;
                listObj.Size = listObj.PreferredSize;
                listObj.SuspendLayout();
                listObj.ResumeLayout(false);
                listObj.Refresh();
                listObj.Show();
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("No Stream ID was selected or StreamID={0} is not correct, check your limits file.\n\n{1}", Globals.streamID, e.Message.ToString()));
            }
        }
        #endregion ThreadVersion

        
        #region Output Files Operations
        private void button5_Click(object sender, EventArgs e)
        {
            OpenFile("error");
            //OpenFile("output");
            this.button2.Visible = true;
            this.button5.Visible = false;
        }
        #endregion Output Files Operations
        #region configLoad
        private static void OpenFile(string type)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            SaveFileDialog output = new SaveFileDialog();
            output.CheckFileExists = false;
            // Set filter options and filter index.
            switch (type)
            {
                case ("XidML"):
                    openFileDialog1.Filter = "Configuration Input (.xidml)|*.xidml|All Files (*.*)|*.*";
                    break;
                case ("limit"):
                    openFileDialog1.Filter = "Configuration Output(.csv)|*.csv|All Files (*.*)|*.*";
                    break;
                case("error"):
                    output.Filter = "Error Log Output(.csv)|*.csv|All Files (*.*)|*.*";
                    output.FilterIndex = 2;
                    output.Title = "Select Error Log location and name";
                    DialogResult resulterror = output.ShowDialog();
                    if (output.FileName != "")
                    {
                        Globals.errorFile = output.FileName.ToString();
                        Globals.errorFileCnt = 0;
                    }
                    else { Globals.errorFile = "error"; }
                    if (resulterror == DialogResult.Cancel)  { Globals.errorFile = "error"; }
                    break;
                //case ("output"):
                //    output.Filter = "Data Log Output(.csv)|*.csv|All Files (*.*)|*.*";
                //    output.FilterIndex = 2;
                //    output.Title = "Select Data Log location and name";
                //    DialogResult resultdata = output.ShowDialog();
                //    if (output.FileName != "")
                //    {
                //        Globals.outputFile = output.FileName.ToString();
                //        Globals.outputFileCnt = 0;
                //    }
                //    else { Globals.outputFile = "data"; }
                //    if (resultdata == DialogResult.Cancel){ Globals.outputFile = "dataOut"; }
                //    break;
            }
            if ((type != "limit") && (type != "XidML")) return;
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Multiselect = false;
            openFileDialog1.Title = "Select " + type;
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                switch (type)
                {
                    case ("limit"):
                        Globals.limitPCAP = LoadLimitsPCAP(openFileDialog1.FileName.ToString());//"limits.csv");
                        break;
                    case ("XidML"):
                        output.Filter = "Configuration Out (.csv)|*.csv|All Files (*.*)|*.*";
                        output.FilterIndex = 1;
                        output.Title = "Select Output limit file";
                        DialogResult outres = output.ShowDialog();
                        if (output.FileName != "")
                        {
                            Configure.SaveLimits(openFileDialog1.FileName.ToString(), output.FileName.ToString());
                        }
                        if (outres == DialogResult.Cancel) { Configure.SaveLimits(openFileDialog1.FileName.ToString(), "Limits_File.csv"); }
                        break;
                }
            }
            else Globals.limitPCAP = null;
        }
        private void button3_Click(object sender, EventArgs e)//open prepared csv with limits.
        {
            OpenFile("limit");
            if (Globals.limitPCAP == null) return;
            int[] streamLength = new int[50];
            int i = 0;
            this.textBox1.Text = String.Format("File Loaded, following streams were found:\n");
            foreach (int stream in Globals.limitPCAP.Keys)
            {
                streamLength[i] = Globals.limitPCAP[stream].Count;
                this.textBox1.AppendText(String.Format("Stream ID={0} with {1} parameters.\n", stream, Globals.limitPCAP[stream].Count));
            }
            this.button3.Visible = false;
            this.button1.Visible = false;
            this.button5.Visible = true;
            this.button2.Visible = true;
            //DrawStreams();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFile("XidML");
            OpenFile("limit");
            if (Globals.limitPCAP == null) return;
            int[] streamLength = new int[50];
            int i = 0;
            this.textBox1.Text = String.Format("File Loaded, following streams were found:\n");
            foreach (int stream in Globals.limitPCAP.Keys)
            {
                streamLength[i] = Globals.limitPCAP[stream].Count;
                this.textBox1.AppendText(String.Format("Stream ID={0} with {1} parameters.\n", stream, Globals.limitPCAP[stream].Count));
            }
            this.button3.Visible = false;
            this.button5.Visible = true;
            this.button1.Visible = false;
            this.button2.Visible = true;
            //DrawStreams();
        }
        public static Dictionary<int, Dictionary<string, double[]>> LoadLimitsPCAP(string limitFile)
        {
            /*  Structure of Dictionary:
            *   KEY Stream No. ==
            *                   ==> KEY Parameter name ====
            *                                             ==> parameter data in array
            * 
            * */
            Dictionary<int, Dictionary<string, double[]>> limit = new Dictionary<int, Dictionary<string, double[]>>();
            Dictionary<int, uint> streamLength = new Dictionary<int,uint>();
            string line = null;
            int lineCnt = 0;            
            try {
                System.IO.StreamReader limitStream = new System.IO.StreamReader(limitFile); 
                while (limitStream.Peek() >= 0)
                {
                    line = limitStream.ReadLine();
                    if (lineCnt != 0) //ignore name line 
                    //structure of file shall be as follows: Packet #,Stream ID, Stream Rate, Parameter Number,Parameter Offset,Parameter Name,Parameter Type,Range Maximum,Range Minimum,Limit Maximum,Limit Minimum
                    {
                        string[] parameters = line.Split(',');  // readline
                        double[] data = new double[]{
                            Convert.ToDouble(parameters[0]),    //0 - Stream ID	
                            Convert.ToDouble(parameters[1]),    //1 - Packet Number 
                            Convert.ToDouble(parameters[2]),    //2 - stream rate in Hz
                            Convert.ToDouble(parameters[3]),    //3 - Parameter Number
	                        Convert.ToDouble(parameters[4]),    //4 - Parameter Offset
                            Convert.ToDouble(parameters[5]),    //5 - Parameter Occurences in stream
                            Convert.ToDouble(parameters[7]),    //6 - Parameter Type - 0 = Data / 1= BitVector
                            Convert.ToDouble(parameters[8]),    //7 - Range Maximum 0 = Vector
                            Convert.ToDouble(parameters[9]),    //8 - Range Minimum 0 = Vector
                            Convert.ToDouble(parameters[10]),    //9 - Limit Maximum	 -- to be added manually
                            Convert.ToDouble(parameters[11]),   //10	- Limit Minimum  -- to be added manually
                            };
                        if (!limit.ContainsKey(Convert.ToInt32(parameters[0])))
                        {
                            limit.Add(Convert.ToInt32(parameters[0]), new Dictionary<string, double[]>(){
                                                                            {parameters[6], data}   //add parameter name with relevant data.
                                                                        });
                        }
                        else
                        {
                            limit[Convert.ToInt32(parameters[0])][parameters[6]] = data;          //add parameter name with relevant data.                        
                        }
                    }
                    lineCnt++;
                }

                foreach (int stream in limit.Keys)
                {
                    streamLength[stream] = 0;
                    foreach (string par in limit[stream].Keys)
                    {
                        if (limit[stream][par][4] > streamLength[stream])
                        {
                            streamLength[stream] = (uint)(limit[stream][par][4]+(uint)limit[stream][par][5]*2);
                        }
                    }
                }
                Globals.streamLength = streamLength;
            
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Cannot open {0}, please make sure that file is not in use by other program\n{1}", limitFile, e.Message));
            }
            return limit;
        }

        #endregion configLoad
        #region PCAP NIC Selection
        private void SelectNIC(object sender, EventArgs e)
        {
            FlowLayoutPanel panel = selectObj.Controls[0] as FlowLayoutPanel;
            foreach (Control child in panel.Controls)
            {
                if (child is RadioButton)
                {
                    RadioButton radio = child as RadioButton;
                    if (radio != null)
                    {
                        if (radio.Checked)
                        {
                            Globals.NetworkCardID = Convert.ToInt32(radio.Name);
                            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
                            Globals.selectedDevice = allDevices[Globals.NetworkCardID];
                            this.textBox1.Text = String.Format("Network Card ID = {0} selected.", radio.Name.ToString());
                            //MessageBox.Show(String.Format("Network Card ID = {0} selected.", radio.Name.ToString()));
                            break;
                        }
                    }
                }
            }
            //this.textBox1.AppendText(sender.ToString()+e.ToString());
            selectObj.Close();
            selectObj.Dispose();
            SelectIP();
        }
        private void SelectNIC()
        {
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            if (Globals.NetworkCardID == -1)
            {
                RadioButton[] selectDeviceBtn = new RadioButton[allDevices.Count];
                Label[] selectDeviceName = new Label[allDevices.Count];

                if (allDevices.Count == 0)
                {
                    MessageBox.Show("No interfaces found! Make sure WinPcap is installed.");
                    return;
                }
                // Print the list
                for (int i = 0; i != allDevices.Count; i++)
                {
                    LivePacketDevice device = allDevices[i];
                    selectDeviceBtn[i] = new RadioButton();
                    selectDeviceBtn[i].Click += new EventHandler(this.SelectNIC);
                    selectDeviceName[i] = new Label();
                    if (device.Description != null)
                    {
                        selectDeviceName[i].Text = (String.Format("{0}. ({1}) ", i, device.Description));
                        selectDeviceBtn[i].Name = i.ToString();
                    }
                    else { selectDeviceName[i].Text = (" (No description available)"); }
                    if (device.Addresses.Count != 0) { selectDeviceName[i].Text = selectDeviceName[i].Text + String.Format("\n Parameters => {0}\n", device.Addresses[0]); }
                    selectDeviceName[i].Size = selectDeviceName[i].PreferredSize;
                }
                selectObj = new selectDevice();
                FlowLayoutPanel selectDevicePanel = new FlowLayoutPanel();
                Label selectDeviceInfo = new Label();
                selectDeviceInfo.Text = "Select your host interface from Network Cards listed below:";
                selectDeviceInfo.Size = selectDeviceInfo.PreferredSize;
                selectObj.Text = "Select Network Device";
                selectDevicePanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
                selectDevicePanel.Controls.Add(selectDeviceInfo);
                for (int i = 0; i != selectDeviceBtn.Length; i++)
                {
                    selectDevicePanel.Controls.Add(selectDeviceName[i]);
                    selectDevicePanel.Controls.Add(selectDeviceBtn[i]);
                }
                selectDevicePanel.Size = selectDevicePanel.PreferredSize;
                selectObj.Controls.Add(selectDevicePanel);
                selectObj.Size = selectObj.PreferredSize;
                selectObj.Show();
            }

        }
        private void selectIPClick(object sender, EventArgs e)
        {
            FlowLayoutPanel panel = IPobj.Controls[0] as FlowLayoutPanel;
            foreach (Control child in panel.Controls)
            {
                if (child is TextBox)
                {
                    TextBox txt = child as TextBox;
                    if (txt.Text != "")
                    {
                        Globals.sourceIP = txt.Text.ToString();
                        MessageBox.Show(String.Format("Source IP = {0} selected.", txt.Text.ToString()));
                        break;
                    }
                    else
                    {
                        //Globals.sourceIP = "192.168.2.2"; //default
                        //MessageBox.Show(String.Format("Default IP = {0} selected.", Globals.sourceIP));
                        Globals.sourceIP = null;

                        break;
                    }
                }
            }
            IPobj.Close();
            IPobj.Dispose();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }
        private void SelectIP()
        {
            if (Globals.sourceIP != null) //disabled 
            {
                TextBox inputIP = new TextBox();
                Label inputIPLabel = new Label();
                FlowLayoutPanel panel = new FlowLayoutPanel();
                panel.FlowDirection = FlowDirection.TopDown;
                inputIPLabel.Text = "Please specify IP Adress of source of packets in 192.168.2.1 format\nLeave blank for no filtering";
                inputIPLabel.Size = inputIPLabel.PreferredSize;
                inputIP.Size = inputIPLabel.Size;
                Button btnOK = new Button();
                btnOK.Text = "OK";
                btnOK.Click += new EventHandler(selectIPClick);
                panel.Controls.AddRange(new Control[] { inputIPLabel, inputIP, btnOK });
                panel.SuspendLayout();
                panel.ResumeLayout(false);
                panel.Size = panel.PreferredSize;
                IPobj = new selectIP();
                IPobj.Text = "Please select IP Adress of the source";
                IPobj.Controls.Add(panel);
                IPobj.Size = IPobj.PreferredSize;
                IPobj.Show();
            }
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }
        #endregion PCAP NIC Selection       

        private void toolStripStatusLabel5_Click(object sender, EventArgs e)
        {
            DialogResult result1 = MessageBox.Show("Are you sure you want to reset all stats??","Important Question",  MessageBoxButtons.YesNo);
            if (result1 == DialogResult.Yes)
            {
                Globals.totalErrors = 0;
                Dictionary<int, Dictionary<string,uint>> pktError = Globals.packetErrors;
                Dictionary<int, Dictionary<string, uint>> parError = Globals.parError;
                List<int> listPkt = Globals.packetErrors.Keys.ToList();
                foreach (int pkt in listPkt)
                {
                    pktError[pkt] = pktError[pkt].ToDictionary(p => p.Key, p =>(uint) 0);
                    parError[pkt] = parError[pkt].ToDictionary(p => p.Key, p =>(uint) 0);                   
                }
                Globals.packetErrors = pktError;
                Globals.parError = parError;
            }

        }

        private void AcquisitionTimer_Click(object sender, EventArgs e)
        {          
            Dictionary<int,double> streamRate = Globals.streamRate;
            Dictionary<int,int[]> framesReceived = Globals.framesReceived;
            //ushort newErrors = 0;
            StringBuilder errorMSG = new StringBuilder();
            List<string> errorQueue = new List<string>();   
            foreach (int stream in framesReceived.Keys)
            {
                             
                int current = framesReceived[stream][0];
                int last = framesReceived[stream][3];
                if (current == last)
                {
                    errorMSG.AppendFormat("{0},{1}, No new packets from this stream in last 5 seconds, {2},\n",Globals.totalErrors, stream, DateTime.Now);
                    //errorMSG.AppendFormat(IFormatProvider
                    Globals.packetErrors[stream]["pktLost"] += (uint)streamRate[stream]*5;
                    Globals.totalErrors += 1;
                    //System.Media.SystemSounds.Beep.Play();
                    //System.Media.SystemSounds.Asterisk.Play();
                    System.Media.SystemSounds.Exclamation.Play();
                    //System.Media.SystemSounds.Question.Play();
                    //System.Media.SystemSounds.Hand.Play();
                }
                Globals.framesReceived[stream][3] = current;
                UpdateSummary = delegate
                {
                    Globals.summaryHoldersDict[stream][2].Text = Globals.packetErrors[stream]["total"].ToString().PadLeft(10, '0');
                    Globals.summaryHoldersDict[stream][0].Text = Globals.packetErrors[stream]["pktLost"].ToString().PadLeft(10, '0');
                    //Globals.summaryHoldersDict[stream][1].Text = Globals.packetErrors[stream]["SEQ"].ToString().PadLeft(10, '0');
                    //Globals.summaryHoldersDict[stream][3].Text = Globals.framesReceived[stream][0].ToString().PadLeft(10, '0');
                };
                this.streamSummaryTab.Invoke(UpdateSummary);                
                errorQueue = Globals.saveQueue[stream].ToList();                
                Globals.saveQueue[stream].Clear();
                errorQueue.Add(errorMSG.ToString());
                Save.LogError(errorQueue, stream);
                errorQueue.Clear();
                errorMSG.Clear();
            }
            //Globals.totalErrors += newErrors;
            UpdateStatusBar = delegate
            {
                this.totalFramesReceivedLabel.Text = Globals.totalFrames.ToString().PadLeft(16, '0');
                this.totalErrorsLabel.Text = (Globals.totalErrors).ToString().PadLeft(10, '0');
            };
            this.statusBar.Invoke(UpdateStatusBar);

        }

        private void toolStripStatusLabel6_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.ExecutablePath, "/restart" + System.Diagnostics.Process.GetCurrentProcess().Id);//Application.ExecutablePath);
            Application.Exit();
        }

        #region PLOTDATASAVED
        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            Globals.streamSaveRate = 1;
        }
        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            Globals.streamSaveRate = 2;
        }
        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            Globals.streamSaveRate = 10;
        }
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Globals.streamSaveRate = 60;
        }
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            Globals.streamSaveRate = 600;
        }
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            Globals.streamSaveRate = 3600;
        }

        private void toolStripStatusLabel8_Click(object sender, EventArgs e)
        {
            Thread plot = new Thread(new ThreadStart(GetNewGraph));
            plot.SetApartmentState(ApartmentState.STA);
            plot.Start();
        }
        
        private void GetNewGraph()
        {
            try
            {                
                selChanObj = new selectChannel();
                //start new thread to plot the selected channels
                foreach (int stream in Globals.channelsSelected.Keys)
                {
                    if (Globals.channelsSelected[stream].Count != 0)
                    {
                        GraphPlot = new PlotData(stream);
                        GraphPlot.SuspendLayout();
                        GraphPlot.ResumeLayout(false);
                        GraphPlot.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        #endregion PLOTDATASAVED


        #region RUBBISH
        //private void DrawStreams()
        //{
        //    foreach (int stream in Globals.limitPCAP.Keys)
        //    {
        //        Label streamLabel = new Label();
        //        RadioButton streamBtn = new RadioButton();
        //        streamBtn.Size = streamBtn.PreferredSize;
        //        streamLabel.Text = String.Format("ID={0}", stream);
        //        streamLabel.Size = streamLabel.PreferredSize;
        //        streamBtn.Name = stream.ToString();
        //        streamBtn.Click += new EventHandler(this.SelectStream);
        //        this.flowLayoutPanel2.Controls.Add(streamBtn);
        //        this.flowLayoutPanel2.Controls.Add(streamLabel);

        //    }
        //    this.flowLayoutPanel2.SuspendLayout();
        //    this.flowLayoutPanel2.ResumeLayout(false);
        //    this.button2.Visible = true;
        //}
        //private void SelectStream(object sender, EventArgs e)
        //{
        //    foreach (Control child in this.flowLayoutPanel2.Controls)
        //    {
        //        if (child is RadioButton)
        //        {
        //            RadioButton radio = child as RadioButton;
        //            if (radio != null)
        //            {
        //                if (radio.Checked)
        //                {
        //                    Globals.streamID = Convert.ToInt32(radio.Name);
        //                    MessageBox.Show(String.Format("Stream ID = {0} selected.", radio.Name.ToString()));
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    this.button2.Visible = true;
        //}
        //private void MyTimer_Tick(object sender, EventArgs e)
        //{
        //    //MessageBox.Show("The form will now be closed.", "Time Elapsed");
        //    //this.Close();
        //    UpdateParameters(Globals.data);
        //}
        //private void UpdateParameters(byte[] frame)
        //{
        //    int i = 0;
        //    double value = 0;
        //    int streamID = Globals.streamID;
        //    Dictionary<string, double[]> limit = Globals.limitPCAP[streamID];
        //    StringBuilder errorMSG = new StringBuilder();
        //    Globals.framesReceived[streamID][1] = ((frame[Globals.inetStart + 8] << 24) + (frame[Globals.inetStart + 9] << 16) + (frame[Globals.inetStart + 10] << 8) + frame[Globals.inetStart + 11]);
        //    if (Globals.framesReceived[streamID][1] != ++Globals.framesReceived[streamID][2])
        //    {
        //        errorMSG.AppendFormat("{4},{0},{1}, Wrong sequence no. ,{2},{3},\n", Globals.framesReceived[streamID][0], Globals.totalErrors,
        //            Globals.framesReceived[streamID][1], Globals.framesReceived[streamID][2], streamID);
        //        Globals.packetErrors[streamID]["pktLost"] += (uint)Math.Abs(Globals.framesReceived[streamID][1] - (Globals.framesReceived[streamID][2] + 1));
        //        Globals.packetErrors[streamID]["SEQ"]++;
        //        Globals.totalErrors++;
        //    }
        //    Globals.framesReceived[streamID][2] = Globals.framesReceived[streamID][1];
        //    int parPos;
        //    foreach (string parName in limit.Keys)
        //    {
        //        parPos = Convert.ToInt16(limit[parName][4]);
        //        if (limit[parName][5] == 0) //check if it is offset or bitVector
        //        {
        //            value = CalcValue(16, Convert.ToDouble((frame[parPos] << 8) + frame[parPos + 1]), limit[parName][6], limit[parName][7]);
        //            Globals.dataHolders[i / 2].Text = value.ToString().PadLeft(6, '0');
        //            if ((value > limit[parName][8]) || (value < limit[parName][9]))
        //            {
        //                Globals.dataHolders[i / 2].BackColor = Color.Red;
        //                errorMSG.AppendFormat("{6},{5},{4},Value of,{0}, = ,{1},it should be between,{2},{3},\n ",
        //                 parName, value, limit[parName][9], limit[parName][8], Globals.totalErrors, Globals.framesReceived[streamID][0], streamID); //CSV format
        //                Save.LogError(errorMSG.ToString(), streamID);
        //                Globals.parError[streamID][parName] += 1;
        //                //Globals.totalErrors++;
        //                this.totalErrorsLabel.Text = (++Globals.totalErrors).ToString().PadLeft(6, '0');
        //            }
        //            else Globals.dataHolders[i / 2].BackColor = Color.LightGreen;
        //        }
        //        else
        //        {
        //            value = Convert.ToDouble((frame[parPos] << 8) + frame[parPos + 1]);
        //            Globals.dataHolders[i / 2].BackColor = Color.Gainsboro;
        //        } //generate some methods for BCU report etc..
        //        i += 2;
        //    }
        //    this.totalFramesReceivedLabel.Text = Globals.framesReceived[streamID][0].ToString().PadLeft(8, '0');
        //}
        //private void AnalyseData(byte[] frame)
        //{
        //    int i = 0;
        //    double value = 0;
        //    int streamID = Globals.streamID;
        //    Dictionary<string, double[]> limit = Globals.limitPCAP[streamID];
        //    StringBuilder errorMSG = new StringBuilder();

        //    Globals.framesReceived[streamID][1] = ((frame[Globals.inetStart + 8] << 24) + (frame[Globals.inetStart + 9] << 16) + (frame[Globals.inetStart + 10] << 8) + frame[Globals.inetStart + 11]);
        //    if (Globals.framesReceived[streamID][1] != (Globals.framesReceived[streamID][2] + 1))
        //    {
        //        errorMSG.AppendFormat("{4},{0},{1}, Wrong sequence number detected,{2}, where previous SEQ was,{3},\n", Globals.framesReceived[streamID][0], Globals.totalErrors,
        //            Globals.framesReceived[streamID][1], Globals.framesReceived[streamID][2], streamID);
        //        Globals.packetErrors[streamID]["pktLost"] += (uint)Math.Abs(Globals.framesReceived[streamID][1] - (Globals.framesReceived[streamID][2] + 1));
        //        Globals.packetErrors[streamID]["SEQ"]++;
        //        Globals.totalErrors++;
        //    }
        //    Globals.framesReceived[streamID][2] = Globals.framesReceived[streamID][1];
        //    int parPos;
        //    foreach (string parName in limit.Keys)
        //    {
        //        parPos = Convert.ToInt16(limit[parName][4]);
        //        //Globals.dataHolders[i / 2].Text = value.ToString().PadLeft(6, '0');
        //        if (limit[parName][5] == 0) //check if it is offset or bitVector
        //        {
        //            value = CalcValue(16, Convert.ToDouble((frame[parPos] << 8) + frame[parPos + 1]), limit[parName][6], limit[parName][7]);
        //            if ((value > limit[parName][8]) || (value < limit[parName][9]))
        //            {
        //                errorMSG.AppendFormat("{6},{5},{4},Value of,{0}, = ,{1},it should be between,{2},{3},\n ",
        //                        parName, value, limit[parName][9], limit[parName][8], Globals.totalErrors, Globals.framesReceived[streamID][0], streamID); //CSV format                    
        //                Globals.parError[streamID][parName] += 1;
        //                Globals.totalErrors++;
        //            }
        //        }
        //        else { value = Convert.ToDouble((frame[parPos] << 8) + frame[parPos + 1]); } //generate some methods for BCU report etc..
        //        i += 2;
        //    }
        //    Save.LogError(errorMSG.ToString(), streamID);


        //}      
        //private void DrawGUI()
        //{
        //    if (Globals.streamID != -1) { this.streamParametersFlow0.Controls.Clear(); }
        //    try
        //    {
        //        double[] data = new double[Globals.limitPCAP[Globals.streamID].Count];
        //        Label[] dataHolders = new Label[data.Length];
        //        Label[] dataLabels = new Label[data.Length];
        //        //FlowLayoutPanel[] dataColumns = new FlowLayoutPanel[data.Length/20];
        //        //FlowLayoutPanel[] labelColumns = new FlowLayoutPanel[data.Length / 20];
        //        TableLayoutPanel[] dataColumns = new TableLayoutPanel[data.Length / 30 + 1];
        //        this.streamParametersFlow0.FlowDirection = FlowDirection.LeftToRight;
        //        for (int i = 0; i != data.Length; i++)
        //        {
        //            int whichColumn = i / 30;
        //            dataLabels[i] = new Label();
        //            dataLabels[i].Name = "DataLabel" + i;
        //            dataLabels[i].AutoSize = false;
        //            dataLabels[i].Text = String.Format("P{0}", i.ToString().PadLeft(3, '0'));
        //            dataLabels[i].Font = new Font(dataLabels[i].Font.FontFamily, 8, dataLabels[i].Font.Style);
        //            //dataLabels[i].TextAlign = ContentAlignment.BottomLeft;
        //            dataLabels[i].Size = dataLabels[i].PreferredSize;

        //            dataHolders[i] = new Label();
        //            dataHolders[i].Name = i.ToString();
        //            dataHolders[i].AutoSize = false;
        //            dataHolders[i].Text = String.Format("{0}", data[i].ToString().PadLeft(6, '0'));
        //            dataHolders[i].Font = new Font(dataLabels[i].Font.FontFamily, 8, dataLabels[i].Font.Style);
        //            //Globals.dataHolders[i].TextAlign = ContentAlignment.BottomLeft;
        //            dataHolders[i].Size = dataHolders[i].PreferredSize;

        //            if (i % 30 == 0)
        //            {
        //                dataColumns[whichColumn] = new TableLayoutPanel();
        //                dataColumns[whichColumn].ColumnCount = 2;
        //                dataColumns[whichColumn].RowCount = 30;
        //            }
        //            dataColumns[whichColumn].Controls.Add(dataLabels[i]);
        //            dataColumns[whichColumn].Controls.Add(dataHolders[i]);
        //            dataColumns[whichColumn].Size = dataColumns[whichColumn].PreferredSize;
        //        }
        //        for (int i = 0; i != dataColumns.Length; i++)
        //        {
        //            this.streamParametersFlow0.Controls.Add(dataColumns[i]);
        //        }
        //        this.streamParametersFlow0.Size = this.streamParametersFlow0.PreferredSize;
        //        this.Size = this.PreferredSize;
        //        this.streamParametersFlow0.SuspendLayout();
        //        this.streamParametersFlow0.ResumeLayout(false);
        //        this.Refresh();
        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show(String.Format("No Stream ID was selected or StreamID={0} is not correct, check your limits file.\n\n{1}", Globals.streamID, e.Message.ToString()));
        //    }
        //}
        //private void GetDataHolders()
        //{
        //    DrawGUI();
        //    Globals.dataHolders = new Label[Globals.limitPCAP[Globals.streamID].Count];
        //    int i = 0;
        //    foreach (Control ctr in this.streamParametersFlow0.Controls)
        //    {
        //        if (ctr is TableLayoutPanel)
        //        {
        //            foreach (Control txt in ctr.Controls)
        //            {
        //                if ((txt is Label) && (!txt.Name.Contains('a')))
        //                {
        //                    //textBox1.AppendText(String.Format("{0} = {1} - {2}\n", txt.Name.ToString(), txt.Text, e));
        //                    Label parLabel = txt as Label;
        //                    Globals.dataHolders[i] = parLabel;
        //                    i++;
        //                    //txt.Text = a++.ToString().PadLeft(4, '0');

        //                }
        //            }
        //        }
        //    }
        //}
        //public void getPacket()
        //{

        //    PacketDevice selectedDevice = Globals.selectedDevice;
        //    using (PacketCommunicator communicator =
        //        selectedDevice.Open(65536,                                  // portion of the packet to capture
        //        // 65536 guarantees that the whole packet will be captured on all the link layers
        //                            PacketDeviceOpenAttributes.NoCaptureLocal,//Promiscuous, // promiscuous mode
        //                            1000))                                  // read timeout
        //    {
        //        using (BerkeleyPacketFilter filter = communicator.CreateFilter("udp"))
        //        {
        //            // Set the filter                    
        //            communicator.SetFilter(filter);
        //        }
        //        while (selectedDevice.Addresses.Count == 0)
        //        {
        //            this.textBox1.Text = String.Format("{0} is down.. Power Up Chassis or check your connection", selectedDevice.Description);
        //        }
        //        this.textBox1.Text = String.Format("Listening on " + selectedDevice.Description + "...");
        //        this.Refresh();

        //        // start the capture
        //        Globals.inetStart = 0; // use this only if looking at UDP packet, currently only payload                            

        //        try
        //        {
        //            int frameRate = Convert.ToInt32(Globals.streamRate[Convert.ToInt32(Globals.streamID)]);//how often update data on screen in seconds, based on stream rate of first item
        //            int frameClrRate = frameRate * 5; // update GUI every 5 seconds 
        //            int streamID;
        //            byte[] frame = new byte[65536];
        //            //double averageTime = 0; //for debug timimings
        //            #region Receive packets PCAP
        //            while (true)
        //            {
        //                if (Globals.framesReceived[Globals.streamID][0] % frameClrRate == 0) this.Refresh();
        //                this.textBox1.ResetText();
        //                var query = from packet in communicator.ReceivePackets(frameRate) //number of packets prior to analyse, use stream rate of 1. par (acquisition for 1 sec prior to analyse)
        //                            where ((packet.Ethernet.IpV4.Udp.Length != 0) && (packet.Ethernet.IpV4.Source.ToString() == Globals.sourceIP))
        //                            //filter packets by not empty and from specified source
        //                            select new { packet.Timestamp, packet.Ethernet.IpV4.Udp.Payload }; //get payload and timestamp from Network Card 

        //                foreach (var Payload in query)
        //                {

        //                    frame = Payload.Payload.ToArray();
        //                    streamID = ((frame[Globals.inetStart + 4] << 24) + (frame[Globals.inetStart + 5] << 16) + (frame[Globals.inetStart + 6] << 8) + frame[Globals.inetStart + 7]);

        //                    if (streamID != Globals.streamID) //ignore streams not selected
        //                    {
        //                        //Globals.totalErrors++;
        //                        //LogError("Corupted packet or unexpected Stream ID received\n",streamID);
        //                        //this.textBox1.AppendText(String.Format("Stream {0} is not described in limits file ", streamID));
        //                        continue;
        //                    }
        //                    Save.SaveFrame(Payload.Payload, Payload.Timestamp, streamID); // Save all frames
        //                    //Globals.Timer[1].Restart();                            
        //                    //Console.WriteLine("# Test time {0:00}:{1:00}:{2:00}.{3:00}",
        //                    //Globals.Timer[0].Elapsed.Hours, Globals.Timer[0].Elapsed.Minutes, Globals.Timer[0].Elapsed.Seconds, Globals.Timer[0].Elapsed.Milliseconds / 10);
        //                    if (Globals.framesReceived[streamID][0] == 0)
        //                    {
        //                        //byte[] frame = new byte[65536];
        //                        //frame = Payload.Payload.ToArray();
        //                        Globals.framesReceived[streamID][2] = ((frame[Globals.inetStart + 8] << 24) + (frame[Globals.inetStart + 9] << 16)
        //                            + (frame[Globals.inetStart + 10] << 8) + frame[Globals.inetStart + 11]) - 1;
        //                        this.textBox1.Text = String.Format("= Listening to stream: {0} on {1} ", Globals.streamID, Globals.selectedDevice.Addresses[0].Address);
        //                        UpdateParameters(frame);
        //                    }
        //                    else
        //                    {
        //                        if (Globals.framesReceived[streamID][0] % 100 == 0) //updated labels every 100 frames received
        //                        {
        //                            UpdateParameters(frame);
        //                            this.textBox1.Text = String.Format("= Listening to stream: {0} on {1} ", Globals.streamID, Globals.selectedDevice.Addresses[0].Address);
        //                        }
        //                        else
        //                        {
        //                            AnalyseData(frame);
        //                        }
        //                        //this.textBox1.AppendText(Payload.Payload.ToHexadecimalString());                                
        //                    }
        //                    // Console.WriteLine("\n# Test time {0:00}:{1:00}:{2:00}.{3:00}",
        //                    //Globals.Timer[0].Elapsed.Hours, Globals.Timer[0].Elapsed.Minutes, Globals.Timer[0].Elapsed.Seconds, Globals.Timer[0].Elapsed.Milliseconds / 10);
        //                    //Console.WriteLine(Payload.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " iNET-X data: " + Payload.Payload.ToHexadecimalString()); //Timestamp and payload
        //                    //averageTime += Globals.Timer[1].ElapsedMilliseconds;
        //                    // Console.WriteLine(Globals.Timer[1].ElapsedMilliseconds + "|" + averageTime / GlobalsStream.totalFrames);
        //                    Globals.framesReceived[streamID][0]++;
        //                    this.textBox1.Text = String.Format("\n\n{0}|{1}", communicator.TotalStatistics.PacketsDroppedByInterface, communicator.TotalStatistics.PacketsCaptured);
        //                    this.Refresh();
        //                }
        //                Globals.totalFrames += frameRate;
        //            }
        //            #endregion Receive packets PCAP
        //        }
        //        catch (Exception e)
        //        {
        //            MessageBox.Show(String.Format("PCPAP Error:\n{0}\n", e.ToString(), e.Data));
        //        }
        //    }
        //}
        #endregion RUBBISH
     
    }
    public partial class selectDevice : Form
    {
        public int getNIC { get; set; }
    }
    public partial class selectIP : Form
    {
        public string sourceIP { get; set; }
    }

    public partial class selectChannel : Form
    {
        public string[] selectedChannel { get; set; }

        public selectChannel()
        {          
            try
            {
                TabControl streamParameterTabControl = new TabControl();
                int maximumX = 0;
                int maximumY = 0;
                TabPage[] tabStream = new TabPage[Globals.limitPCAP.Keys.Count];
                int pktCnt = 0;
                foreach (int stream in Globals.limitPCAP.Keys)
                {
                    List<string> parametersList = new List<string>(Globals.limitPCAP[stream].Keys);
                    double[] data = new double[Globals.limitPCAP[stream].Count];
                    CheckBox[] dataSelect = new CheckBox[data.Length];
                    Label[] dataLabels = new Label[data.Length];
                    TableLayoutPanel[] dataColumns = new TableLayoutPanel[data.Length / 20 + 1];
                    FlowLayoutPanel flow = new FlowLayoutPanel();
                    flow.FlowDirection = FlowDirection.LeftToRight;
                    tabStream[pktCnt] = new TabPage();
                    for (int i = 0; i != data.Length; i++)
                    {
                        int whichColumn = i / 20;
                        dataLabels[i] = new Label();
                        dataLabels[i].Name = i.ToString();
                        dataLabels[i].AutoSize = false;
                        //dataLabels[i].Text = String.Format("P{0}", i.ToString().PadLeft(3, '0'));     // Changed to parameter name as ACLS has XX123 convention
                        dataLabels[i].Text = String.Format("{0}", parametersList[i].ToString());
                        dataLabels[i].Font = new Font(dataLabels[i].Font.FontFamily, 8, dataLabels[i].Font.Style);

                        dataLabels[i].Size = dataLabels[i].PreferredSize;
                        dataSelect[i] = new CheckBox();
                        dataSelect[i].Name = String.Format("{0}", parametersList[i].ToString());
                        dataSelect[i].AutoSize = false;
                        dataSelect[i].Font = new Font(dataLabels[i].Font.FontFamily, 8, dataLabels[i].Font.Style);
                        //Globals.dataHolders[i].TextAlign = ContentAlignment.BottomLeft;
                        dataSelect[i].Size = dataSelect[i].PreferredSize;

                        if (i % 20 == 0)
                        {
                            dataColumns[whichColumn] = new TableLayoutPanel();
                            dataColumns[whichColumn].ColumnCount = 2;
                            dataColumns[whichColumn].RowCount = 20;
                        }
                        dataColumns[whichColumn].Controls.Add(dataLabels[i]);
                        dataColumns[whichColumn].Controls.Add(dataSelect[i]);
                        dataColumns[whichColumn].Size = dataColumns[whichColumn].PreferredSize;
                        if (dataColumns[whichColumn].Size.Height > maximumY) maximumY = dataColumns[whichColumn].Size.Height;
                        //if (tabStream[pktCnt].Size.Width > maximumX) maximumX = tabStream[pktCnt].Size.Width;
                    }
                    for (int i = 0; i != dataColumns.Length; i++)
                    {
                        flow.Controls.Add(dataColumns[i]);
                    }
                    flow.SuspendLayout();
                    flow.ResumeLayout(false);
                    //tabStream[pktCnt].AutoScroll = true;
                    //tabStream[pktCnt].AutoScrollPosition = new System.Drawing.Point(349, 0);
                    flow.Size = flow.PreferredSize;
                    tabStream[pktCnt].Controls.Add(flow);
                    tabStream[pktCnt].Name = stream.ToString();
                    tabStream[pktCnt].Text = String.Format("ID={0}", stream);
                    tabStream[pktCnt].Size = tabStream[pktCnt].PreferredSize;
                    if (tabStream[pktCnt].Size.Height > maximumY) maximumY = tabStream[pktCnt].Size.Height;
                    if (tabStream[pktCnt].Size.Width > maximumX) maximumX = tabStream[pktCnt].Size.Width;
                    pktCnt++;
                }
                foreach (TabPage stream in tabStream)
                {
                    streamParameterTabControl.Controls.Add(stream);
                }
                streamParameterTabControl.SuspendLayout(); streamParameterTabControl.ResumeLayout(false);
                streamParameterTabControl.Size = new Size(maximumX+5,maximumY+15);//streamParameterTabControl.PreferredSize;
                //this.Size = this.PreferredSize;
                FlowLayoutPanel selectionFlow = new FlowLayoutPanel();
                selectionFlow.FlowDirection = FlowDirection.LeftToRight;
                selectionFlow.Controls.Add(streamParameterTabControl);
                //streamParameterTabControl.Dock = DockStyle.Fill;                
                Button btnOK = new Button();
                btnOK.Text = "Draw";
                btnOK.Click += new EventHandler(selectChannelClick);
                selectionFlow.Controls.Add(btnOK);
                Button btnAll = new Button();
                btnAll.Text = "All";
                btnAll.Click += new EventHandler(selectAllClick);
                selectionFlow.Controls.Add(btnAll);

                selectionFlow.Size = selectionFlow.PreferredSize;
                selectionFlow.SuspendLayout();
                selectionFlow.ResumeLayout(false);
                this.Controls.Add(selectionFlow);
                this.Size = this.PreferredSize;//new Size(maximumX + 50, maximumY + 45);
                this.SuspendLayout();
                this.ResumeLayout(false);
                this.ShowDialog();
                this.Refresh();
                // for future use // DrawParametersList();
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("No Stream ID was selected or StreamID={0} is not correct, check your limits file.\n\n{1}", Globals.streamID, e.Message.ToString()));
            }
        }
        private void selectChannelClick(object sender, EventArgs e)
        {
            getSelectedChannel();
        }
        private void getSelectedChannel()
        {
            Dictionary<int, List<string>> channels = new Dictionary<int, List<string>>(Globals.limitPCAP.Keys.Count);
            
            foreach (int stream in Globals.limitPCAP.Keys)
            {
                Globals.channelsSelected[stream].Clear();
                channels[stream] = new List<string>();// new String[Globals.limitPCAP[stream].Count];

                int i = 0;
                foreach (Control panel in this.Controls)
                {
                    if (panel is FlowLayoutPanel)
                    {
                        foreach (Control ctr in panel.Controls)
                        {
                            if (ctr is TabControl)
                            {
                                foreach (Control tab in ctr.Controls)
                                {
                                    if ((tab is TabPage) && (tab.Name == stream.ToString()))
                                    {
                                        foreach (Control flow in tab.Controls)
                                        {
                                            if (flow is FlowLayoutPanel)
                                            {
                                                foreach (Control col in flow.Controls)
                                                {
                                                    if (col is TableLayoutPanel)
                                                    {
                                                        foreach (Control tickBox in col.Controls)
                                                        {
                                                            if (tickBox is CheckBox)
                                                            {
                                                                CheckBox tickStat = tickBox as CheckBox;
                                                                if (tickStat.Checked)
                                                                {
                                                                    channels[stream].Add(tickStat.Name);
                                                                    i++;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Globals.channelsSelected = channels;
            this.Close();
            this.Dispose();

        }

        private void selectAllClick(object sender, EventArgs e)
        {
            getAllChannels();
        }

        private void getAllChannels()
        {
            Dictionary<int, List<string>> channels = new Dictionary<int, List<string>>(Globals.limitPCAP.Keys.Count);
           
            foreach (int stream in Globals.limitPCAP.Keys)
            {
                Globals.channelsSelected[stream].Clear();
                channels[stream] = new List<string>();// new String[Globals.limitPCAP[stream].Count];

                int i = 0;
                foreach (string parName in Globals.limitPCAP[stream].Keys)
                {
                    channels[stream].Add(parName);
                    i++;
                }
            }
            Globals.channelsSelected = channels;
            this.Close();
            this.Dispose();
        }
    }

    public class ThermocoupleCalc
    {
        //double[,] c = new double[,]{new double[]{0.000000000000E+00,
        // 0.394501280250E-01,
        // 0.236223735980E-04,
        //-0.328589067840E-06,
        //-0.499048287770E-08,
        //-0.675090591730E-10,
        //-0.574103274280E-12,
        //-0.310888728940E-14,
        //-0.104516093650E-16,
        //-0.198892668780E-19,
        //-0.163226974860E-22},new double[]
        //{-0.176004136860E-01,
        // 0.389212049750E-01,
        // 0.185587700320E-04,
        //-0.994575928740E-07,
        // 0.318409457190E-09,
        //-0.560728448890E-12,
        // 0.560750590590E-15,
        //-0.320207200030E-18,
        // 0.971511471520E-22,
        //-0.121047212750E-25}
        //};
        double[,] t_range = new double[,] { { -270.0, 0.0, 1372.0 } };

        double[,] a = new double[,]{{0.118597600000E+00,
        -0.118343200000E-03,
         0.126968600000E+03}};

        double[,] emf_range = new double[,] { { -5.891, 0.0, 20.644, 54.886 } };
        double[,] d = new double[,] {{         0.0000000E+00,
         2.5173462E+01,
        -1.1662878E+00,
        -1.0833638E+00,
        -8.9773540E-01,
        -3.7342377E-01,
        -8.6632643E-02,
        -1.0450598E-02,
        -5.1920577E-04,
         0.0000000E+00},{
         0.000000E+00,
         2.508355E+01,
         7.860106E-02,
        -2.503131E-01,
         8.315270E-02,
        -1.228034E-02,
         9.804036E-04,
        -4.413030E-05,
         1.057734E-06,
        -1.052755E-08},{
                -1.318058E+02,
         4.830222E+01,
        -1.646031E+00,
         5.464731E-02,
        -9.650715E-04,
         8.802193E-06,
        -3.110810E-08,
         0.000000E+00,
         0.000000E+00,
         0.000000E+00}};




    public double GetVoltage(string tc_type, double input, string input_unit, string output_unit = "mV")
        {
            double voltOut;
            double temperature;
            bool return_emf;
            double emf;

            //if (input_unit == "K"){
            //    temperature = x - 273.15;
            //    if (output_unit == "mV"){
            //        return_emf = true;
            //    }
            if (input_unit == "C")
            {
                temperature = input;
                if (output_unit == "mV")
                {
                    return_emf = true;
                }
                else if (input_unit == "mV")
                {
                    emf = input;
                    return_emf = false;
                }
            }
                /*
      //        if (return_emf)
      //        {
      //            try{
      //                // Create emf array if temperature is an array
      //                iterator = iter(temperature);
      //                t= temperature
      //                v = len(temperature)*[0.0]
      //            catch Exception e
      //            {                    
      //                t = [temperature]
      //                v = [0.0]
      //            for i in range(len(t)) :
      //                if   (t[i] >= t_range[tc_type][0] and t[i] < t_range[tc_type][1]) :
      //                    temperature_range = 0
      //                elif (t[i] >= t_range[tc_type][1] and t[i] < t_range[tc_type][2]) :
      //                    temperature_range = 1
      //                elif (t[i] >= t_range[tc_type][2] and t[i] < t_range[tc_type][3]) :
      //                    temperature_range = 2
      //                else :
      //                    return(("Temperature ({0:f} {1:s}) is outside allowed range for "+
      //                           "Type {2:s} thermocouple").format(t[i],input_unit,tc_type))
      //                for j in range(len(c[tc_type][temperature_range])) :
      //                    v[i] += c[tc_type][temperature_range][j]*t[i]**j
      //                v[i] += a[tc_type][0]*math.exp(
      //                    a[tc_type][1]*(t[i]-a[tc_type][2])**2)
      //                if output_unit == "V" :
      //                    v[i] = v[i]/1000.0
      //            if len(v) == 1 :
      //                // Return a value, not an array, if temperature was not an array
      //                return v[0]
      //            else :
      //                return v
      //            }
            
        


 
      //for k in emf_range.keys():
      //    if tc_type == k :
      //        invalid_thermocouple = False
      //if invalid_thermocouple :
      //    return("'{0:s}' is not a recognized theromocouple type".format(tc_type))

      //// Given temperature, return emf
    

      //// Given emf, return temperature
      //else :
      //    try:
      //        // Create emf array if temperature is an array
      //        iterator = iter(emf)
      //        v= emf
      //        t = len(emf)*[0.0]
      //    except TypeError:
      //        v = [emf]
      //        t = [0.0]
      //    for i in range(len(v)) :
      //        if v[i] > emf_range[tc_type][0] and v[i] < emf_range[tc_type][1] :
      //            emf_range = 0
      //        elif v[i] >= emf_range[tc_type][1] and v[i] < emf_range[tc_type][2] :
      //            emf_range = 1
      //        elif v[i] >= emf_range[tc_type][2] and v[i] < emf_range[tc_type][3] :
      //            emf_range = 2
      //        else :
      //            return(("EMF ({0:f} {1:s}) is outside allowed range for "+
      //                   "Type {2:s} thermocouple").format(v[i],input_unit,tc_type)) 
      //        for j in range(len(d[tc_type][emf_range])) :
      //            t[i] += d[tc_type][emf_range][j]*v[i]**j
      //        // Note that temperature is returning in Kelvin, not Celsius
      //        if output_unit == "K" :
      //            t[i] += 273.15
      //        elif output_unit == "F" :
      //            t[i] = t[i]*9./5. + 32.0
      //    if len(t) == 1 :
      //        // Return a value, not an array, if emf was not an array
      //        return t[0];
      //    else :
      //        return t;            
                 */
                return 0.0;
            

        }
    }
    public partial class List_Parameter_Names : Form
    {
        public Dictionary<int, Dictionary<string, double[]>> list_Parameter_Names { get; set; }
    }

}

