using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System.IO;
using System.Xml;
using System.Threading;
using System.Reflection;

namespace GraphicalPacketCheck
{

    public class Globals
    {
        //file names
        public static string errorFile="";//{ get; set; }
        public static uint errorFileCnt { get; set; }
        public static Dictionary<int, string> outputFile { get; set; }
        public static uint outputFileCnt { get; set; }
        
        
        //Queue
        //public static Dictionary<int, Packet[]> saveQueue { get; set; }       
        public static Dictionary<int, List<string>> saveQueue { get; set; }

        //Threads - not used.
        public static Thread QueueHandle { get; set; }

        //Timer
        public static System.Timers.Timer AcquisitionTimer { get; set; }


        //GUI element pointers
        public static Label[] dataHolders { get; set; }
        public static Dictionary<int, Label[]> dataNameHoldersDict { get; set; } // pointers to parameter NAME labels - indicators.
        public static Dictionary<int, Label[]> dataHoldersDict { get; set; } // pointers to Parameter DATA labels
        public static Dictionary<int, Label[]> summaryHoldersDict { get; set; }// pointers to summary labels

        //PCAP
        public static PacketDevice selectedDevice { get; set; }
        public static PacketCommunicator communicator { get; set; }

        //statistics
        public static Dictionary<int, Dictionary<string, double[]>> limitPCAP { get; set; }
        public static Dictionary<int, Dictionary<string, uint>> parError { get; set; }
        public static int totalErrors { get; set; }
        public static Dictionary<int, double> streamRate { get; set; }
        public static Dictionary<int, uint> streamLength { get; set; }
        public static int allstreamsRate { get; set; } //sum all streamrates
        //public static Dictionary<ushort, uint> groupError { get; set; }
        public static Dictionary<int, Dictionary<string, uint>> packetErrors { get; set; }
        public static Dictionary<int, int[]> framesReceived { get; set; }
        public static int totalFrames { get; set; }

        
        public static IFormatProvider stringFormat { get; set; }
        // default values
        public const int inetStart = 0;// { get; set; } Start of iNET-X header
        public static int streamID = -1;
        public static int NetworkCardID = -1;
        public static string sourceIP = null;




        public static int digitsNumber { get; set; }
        public static int streamSaveRate { get; set; }

        public static Dictionary<int, List<string>> channelsSelected { get; set; }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PacketCheck());
        }

    }
    
      #region XidML loading  
    public class Configure
    {
        public static void SaveLimits(string configFile, string limitsFile)
        {
            StreamWriter limitsPtr = new StreamWriter(limitsFile);
            //(@"" + configFile.Substring(0, configFile.LastIndexOf('.')) + "_limits.csv"));
            //string cfg; // structure as follows==>  streamID, parTotal, parameter{parCnt, FSRmin[], FSRmax[]}
            if (File.Exists(configFile))
            {
                FileStream configFilePtr = new FileStream(configFile, FileMode.Open, FileAccess.Read);
                XmlTextReader reader = new XmlTextReader(configFilePtr);
                int pktCnt = 0;
                limitsPtr.WriteLine("Stream ID, Stream #, Stream Rate, Parameter Number, Parameter Offset, Parameter Occurrences, Parameter Name, Parameter Type [0 - analog 1 - bitvector], Range Maximum, Range Minimum, Limit Maximum, Limit Minimum,X");
                string parName = "";
                double parOffset = 0;
                double parOccurr = 1;
                while (reader.Read())
                {
                    if ((reader.IsStartElement()) && (reader.Name == "iNET-X"))
                    {
                        int parCnt = 0;
                        reader.ReadToFollowing("PackageRate");
                        int pktRate = reader.ReadElementContentAsInt();
                        reader.ReadToFollowing("StreamID");
                        int streamID = Convert.ToInt32(reader.ReadElementContentAsString(), 16);
                        reader.ReadToFollowing("Port"); reader.ReadToFollowing("Value"); Console.WriteLine(reader.Value);
                        int port = Convert.ToInt16(reader.ReadElementContentAsString());
                        MessageBox.Show(String.Format("#{3} Stream ID is: {0} on Port: {2} with rate of {1} Hz\n Press any key to continue..", streamID, pktRate, port, pktCnt));                        
                        while (reader.Read())
                        {
                            //while ((reader.IsStartElement()) && (reader.Name != "iNET-X"))
                            //{
                            switch (reader.Name)
                            {                              
                                case "ParameterReference":
                                    parName = reader.ReadElementContentAsString();
                                    break;
                                //else if ((reader.IsStartElement()) && (reader.Name == "Offset_Bytes"))
                                case "Offset_Bytes":
                                    //reader.ReadToFollowing("Offset_Bytes");
                                    parOffset = reader.ReadElementContentAsDouble();
                                    break;
                                case "Occurrences":
                                    parOccurr = reader.ReadElementContentAsDouble();
                                    List<double> FSR = GetParameterFSR(configFile, parName);

                                    limitsPtr.Write("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{8},{9}\n", streamID, pktCnt, pktRate, parCnt, parOffset, parOccurr, parName, FSR[0], FSR[1], FSR[2]);
                                    //Console.WriteLine("Parameter {0}: {1}", parCnt, parName);
                                    parCnt++;
                                    break;
                            }
                            if (reader.LocalName == "iNET-X")
                            {
                                //Console.WriteLine(reader.LocalName);
                                break;
                            }
                        }
                        //Console.WriteLine("END OF STREAM");
                        pktCnt++;

                    }
                }
                reader.Close();
                limitsPtr.Close();
                configFilePtr.Close();
            }
            else { MessageBox.Show(String.Format("The file {0} could not be located", configFile)); }
            MessageBox.Show(String.Format("Configuration file created. \n\n Please open {0} in Excel or other CSV editor.\n Provide your pass/fail criteria for parameters and then click OK afterwards", limitsFile));
        }
        private static List<double> GetParameterFSR(string configFile, string parName)
        {
            FileStream configParPtr = new FileStream(configFile, FileMode.Open, FileAccess.Read);
            using (XmlTextReader reader = new XmlTextReader(configParPtr))
            {
                reader.ReadToFollowing("ParameterSet");
                while (reader.ReadToFollowing("Parameter"))
                {
                    reader.MoveToFirstAttribute();
                    if (reader.Value == parName)
                    {
                        List<double> FSR = new List<double>();
                        reader.ReadToFollowing("BaseUnit");
                        string parameterUnit = reader.ReadElementContentAsString();
                        reader.ReadToFollowing("DataFormat");
                        if (parameterUnit == "BitVector")//Status registers show in binary form
                        {
                            FSR.Add(2);
                            FSR.Add(65535);
                            FSR.Add(0);
                        }
                        else if (parameterUnit == "Count") //to handle counters
                        {
                            FSR.Add(1);
                            FSR.Add(65535);
                            FSR.Add(0);
                        }
                        else
                        {
                            string parameterFormat = reader.ReadElementContentAsString();
                            if (parameterFormat == "OffsetBinary") //check if its data parameter or status register
                            {
                                FSR.Add(0);
                                reader.ReadToFollowing("RangeMaximum");
                                FSR.Add(reader.ReadElementContentAsDouble());
                                //List<double> FSR = new List<double> { reader.ReadElementContentAsDouble() };
                                reader.ReadToFollowing("RangeMinimum");
                                FSR.Add(reader.ReadElementContentAsDouble());
                                //Console.WriteLine("Parameter {0} Max: {1}, Min: {2} ", parName,FSR[0], FSR[1]);
                            }
                            else if (parameterFormat == "BinaryCodedDecimal")
                            {
                                if (parameterUnit == "Celsius")
                                {
                                    FSR.Add(3);
                                    reader.ReadToFollowing("RangeMaximum");
                                    FSR.Add(reader.ReadElementContentAsDouble());
                                    //List<double> FSR = new List<double> { reader.ReadElementContentAsDouble() };
                                    reader.ReadToFollowing("RangeMinimum");
                                    FSR.Add(reader.ReadElementContentAsDouble());
                                }
                                else
                                {
                                    FSR.Add(4);
                                    FSR.Add(65535);
                                    FSR.Add(0);
                                }
                            }
                            else
                                {
                                    FSR.Add(4);
                                    FSR.Add(65535);
                                    FSR.Add(0);
                                }
                            
                        }
                        reader.Close();
                        configParPtr.Close();
                        return FSR;
                    }
                }
            }
            return null;
        }



    }
      #endregion XidML loading
    public class Save
    {       
        public static void SaveFrame(byte[] frame, DateTime timestamp, int stream)
        {
            //int lenghth = packet.Length;           
            //byte[] frameArray = packet.ToArray().Skip(28).Take(Globals.limitPCAP[stream].Keys.Count * 2).ToArray();// packet.ToString();//packet.ToHexadecimalString();            

            //IEnumerable<byte> copyFrame = frame.Skip(28).Take(10);
            System.Text.StringBuilder frameMSG = new System.Text.StringBuilder();
            Dictionary<string, double[]> limit = Globals.limitPCAP[stream];
            int parPos;
            int parCnt;
            double dataTmp;
            if (frame.Length < Globals.streamLength[stream])
            {
                return;
            }
            foreach (string parName in limit.Keys)
            {
                parPos = (int)(limit[parName][4]);
                parCnt = (int)(limit[parName][3]);
                if (limit[parName][6] == 0) //check TYPE of DATA - it is offset or bitVector etc
                {
                    dataTmp = PacketCheck.CalcValue(16, (double)((frame[parPos] << 8) + frame[parPos + 1]), limit[parName][7], limit[parName][8]);
                }
                else if (limit[parName][6] == 3) //BCD temp - BIT101
                {
                    dataTmp = PacketCheck.CalcValue(16, (double)(PacketCheck.bcd2int((frame[parPos] << 8) + frame[parPos + 1])), limit[parName][7], limit[parName][8]);
                }
                else { dataTmp = (double)((frame[parPos] << 8) + frame[parPos + 1]); } //generate some methods for BCU report etc..
                //i += 2;
                frameMSG.AppendFormat("{0},", dataTmp);
            }

            string dataToSave = frameMSG.ToString();
            string time = timestamp.ToString("yyyy-MM-dd hh:mm:ss.fffff");
            int frameNumber = Globals.framesReceived[stream][0];
            string fileName = String.Format("{0}{1}.csv", Globals.outputFile[stream], Globals.outputFileCnt.ToString().PadLeft(4, '0'));
            if (Globals.framesReceived[stream][0]==0)//(Globals.totalFrames == 1)
            {
                Globals.outputFileCnt = 0;
                //fileS = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                using (StreamWriter dataStream =File.CreateText(fileName))//Globals.outputFile[stream] + Globals.outputFileCnt.ToString().PadLeft(4, '0') + ".csv"))
                //using (StreamWriter dataStream = new StreamWriter(fileS)) 
                {
                    dataStream.WriteLine("Stream ID,Frame #, Network Interface Timestamp,Frame,");
                    dataStream.Write("{0},{1},{2},{3}\n", stream ,frameNumber , time, dataToSave);
                    dataStream.Flush();
                    dataStream.Close();
                }
            }
            else if ((new FileInfo((Globals.outputFile[stream] + Globals.outputFileCnt.ToString().PadLeft(4, '0') + ".csv"))).Length > 160000000) //160 MB
            {
                Globals.outputFileCnt++;
                fileName = String.Format("{0}{1}.csv", Globals.outputFile[stream], Globals.outputFileCnt.ToString().PadLeft(4, '0'));
                using (StreamWriter dataStream = File.CreateText(fileName)) //   .AppendText (errorPtr))
                {
                    dataStream.WriteLine("Stream ID,Frame #, Network Interface Timestamp,Frame,");
                    dataStream.Write("{0},{1},{2},{3}\n", stream, frameNumber, time, dataToSave);
                    dataStream.Flush();
                    dataStream.Close();
                }
            }
            else
            {
                using (StreamWriter dataStream = File.AppendText(fileName))
                //using (StreamWriter dataStream = File.AppendText(Globals.outputFile[stream] + Globals.outputFileCnt.ToString().PadLeft(4, '0') + ".csv")) //   .AppendText (errorPtr))
                {
                    dataStream.Write("{0},{1},{2},{3}\n", stream, frameNumber, time, dataToSave);
                    dataStream.Flush();
                    dataStream.Close();
                }
            }
        }
        
        public static void LogError(List<string> errorMSG_Queue, int stream)//, string parName,ushort group)
        {
            //FileInfo
            if (((Globals.totalErrors == 1)||(Globals.totalFrames==1)||(Globals.totalErrors==0))&&(Globals.errorFileCnt==0))
            {
                Globals.errorFileCnt = 0;
                using (StreamWriter errorStream = File.CreateText(Globals.errorFile + Globals.errorFileCnt.ToString().PadLeft(4, '0') + ".csv"))
                {
                    errorStream.WriteLine("Error #,StreamID,Frame,Parameter Error #, , , ,");
                    foreach(string errorMSG in errorMSG_Queue) errorStream.Write(errorMSG);
                    errorStream.Flush();
                    errorStream.Close();
                    return;
                }
                
            }
            //File.Open("MyFile.txt", FileMode.Open, FileAccess.Read, FileShare.None))

            string fileName = Globals.errorFile + Globals.errorFileCnt.ToString().PadLeft(4, '0') + ".csv";
            if ((new FileInfo(fileName)).Length > 100000000)
            {
                Globals.errorFileCnt++;
                using (StreamWriter errorStream = File.CreateText(Globals.errorFile + Globals.errorFileCnt.ToString().PadLeft(4, '0') + ".csv")) //   .AppendText (errorPtr))
                {
                    errorStream.WriteLine("Error #,StreamID,Frame,Parameter Error #, , , ,");
                    foreach (string errorMSG in errorMSG_Queue) errorStream.Write(errorMSG);
                    errorStream.Flush();
                    errorStream.Close();
                }
            }
            else
            {
                using (StreamWriter errorStream = File.AppendText(fileName)) //   .AppendText (errorPtr))
                {
                    foreach (string errorMSG in errorMSG_Queue) errorStream.Write(errorMSG);
                    errorStream.Flush();
                    errorStream.Close();
                }
            }
        }

        public static void LogError(string errorMSG, string errorType, int stream)
        {
            if (Globals.totalErrors == 0)
            {
                Globals.errorFileCnt = 0;
                using (StreamWriter errorStream = File.CreateText(Globals.errorFile + Globals.errorFileCnt.ToString().PadLeft(4, '0') + ".csv"))
                {
                    errorStream.WriteLine("StreamID, Frame, Error #, , , ,");
                    errorStream.Write(errorMSG + "\n");
                    errorStream.Flush();
                    errorStream.Close();
                }
            }
            else if ((new FileInfo((Globals.errorFile + Globals.errorFileCnt.ToString().PadLeft(4, '0') + ".csv"))).Length > 100000000)
            {
                Globals.errorFileCnt++;
                using (StreamWriter errorStream = File.CreateText(Globals.errorFile + Globals.errorFileCnt.ToString().PadLeft(4, '0') + ".csv")) //   .AppendText (errorPtr))
                {
                    errorStream.WriteLine("StreamID, Frame, Error #, , , ,");
                    errorStream.Write(errorMSG + "\n");
                    errorStream.Flush();
                    errorStream.Close();
                }
            }
            else
            {
                using (StreamWriter errorStream = File.AppendText(Globals.errorFile + Globals.errorFileCnt.ToString().PadLeft(4, '0') + ".csv")) //   .AppendText (errorPtr))
                {
                    errorStream.Write(errorMSG + "\n");
                    errorStream.Flush();
                    errorStream.Close();
                }
            }
            Globals.totalErrors++;
            Globals.packetErrors[stream][errorType]++;
        }
    }
        
    
}
