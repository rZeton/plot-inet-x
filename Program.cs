using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Plot_iNET_X
{
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
            Application.Run(new Form1());
        }
    }
    public class Globals
    {
        public static string errorFile;
        public static int errorFileCnt;
        public static Dictionary<int, uint> streamLength;
        public static string filePCAP { get; set; }
        public static string[] filePCAP_list { get; set; }
        public static string limitfile { get; set; }

        //statistics
        public static int totalFrames { get; set; }
        public static Dictionary<int, int[]> framesReceived { get; set; }
        //stats error counters
        public static Dictionary<int, Dictionary<string, uint>> parError { get; set; }
        public static int totalErrors { get; set; }
        public static Dictionary<int, Dictionary<string, uint>> packetErrors { get; set; }


        public static int streamID = -1;
        public const int inetStart = 0; //0 for udps


        public static Dictionary<string, limitPCAP_Derrived> limitPCAP_derrived {get;set;}//; //Dictionary<String,double[]>> limitPCAP_derrived {get;set;}
        public static Dictionary<int, Dictionary<string, double[]>> limitPCAP { get; set; }
        public static Dictionary<int, List<string>> channelsSelected { get; set; }

        
    }

    public class limitPCAP_Derrived
    {
        //limitPCAP_Derrived class to hold data
        public int streamID   {get;set;}
        public string srcParameterName {get;set;}
        public string[] srcParametersName { get; set; }
        public double const1 {get;set;}
        public double const2 {get;set;}
        public double const3 { get; set; }

        public double[] getConstants()
        {
            return new double[]{const1, const2};
        }


    }
    #region XidML loading
    public class Configure
    {
        public static void SaveLimits(string configFile, string limitsFile)
        {
            System.IO.StreamWriter limitsPtr = new System.IO.StreamWriter(limitsFile);
            //(@"" + configFile.Substring(0, configFile.LastIndexOf('.')) + "_limits.csv"));
            //string cfg; // structure as follows==>  streamID, parTotal, parameter{parCnt, FSRmin[], FSRmax[]}
            if (System.IO.File.Exists(configFile))
            {
                System.IO.FileStream configFilePtr = new System.IO.FileStream(configFile, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                XmlTextReader reader = new XmlTextReader(configFilePtr);
                int pktCnt = 0;
                limitsPtr.WriteLine("Stream ID, Stream #, Stream Rate, Parameter Number, Parameter Offset, Parameter Occurrences, Parameter Name, Parameter Type [0 - analog; 1 - bitvector; 5 - derrived], Range Maximum, Range Minimum, Limit Maximum, Limit Minimum,Derrived Source [streamID;ParameterName], Constants [y1;y2], X");
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

                                    limitsPtr.Write("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{8},{9},,,\n", streamID, pktCnt, pktRate, parCnt, parOffset, parOccurr, parName, FSR[0], FSR[1], FSR[2]);
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
            System.IO.FileStream configParPtr = new System.IO.FileStream(configFile, System.IO.FileMode.Open, System.IO.FileAccess.Read);
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
}
