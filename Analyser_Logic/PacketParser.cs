using PcapDotNet.Core;
using PcapDotNet.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Plot_iNET_X.Analyser_Logic
{
   public static class DataComparer
   {
       public static Double Correlation(Double[] Xs, Double[] Ys)
       {
           Double sumX = 0;
           Double sumX2 = 0;
           Double sumY = 0;
           Double sumY2 = 0;
           Double sumXY = 0;

           int n = Xs.Length < Ys.Length ? Xs.Length : Ys.Length;

           for (int i = 0; i < n; ++i)
           {
               Double x = Xs[i];
               Double y = Ys[i];

               sumX += x;
               sumX2 += x * x;
               sumY += y;
               sumY2 += y * y;
               sumXY += x * y;
           }

           Double stdX = Math.Sqrt(sumX2 / n - sumX * sumX / n / n);
           Double stdY = Math.Sqrt(sumY2 / n - sumY * sumY / n / n);
           Double covariance = (sumXY / n - sumX * sumY / n / n);

           return covariance / stdX / stdY;
       }
       public static double[] getRatio(double[] param1, double[] param2)
       {
           int n = param1.Length < param2.Length ? param1.Length : param2.Length;
           double[] Out = new double[n];
           for (int i = 0; i != Out.Length; i++)
           {
               Out[i] = param1[i] / param2[i];
           }
           return Out;
       }


       public static double[] LoadTempData(int stream, string parName)
       {
           BinaryReader Reader = null;
           string Name = Array.Find(Globals.fileDump_list, element => element.Contains(String.Format("{0}_{1}", stream,parName)));
           //string Name = String.Format(@"{0}\{1}_{2}.dat", Globals.fileDump, stream, parName);
           
           double[] data = null;
           try
           {
               Reader = new BinaryReader(File.Open(Name, FileMode.Open));
               int size = (int)(Reader.BaseStream.Length / 8); //divide by 8 to get number of doubles? /Unsafe.            
               data = new double[size + 1];
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
   }



    public static class PacketParser
    {
        private static List<int> streamID {get;set;}       
        public static Dictionary<int, Dictionary<string, List<double>>> streamData;
        private static readonly SemaphoreSlim _sl = new SemaphoreSlim(initialCount: 1);
        //public PacketParser(List<int> streams)
        //{
        //    streamID = streams;
        //}
        //parse method using arrays -- TODO fix other types than analog.
        public static void parsePacket2(Packet packet)
        {
            int stream;
            if (packet.Length < 64)
            {
                return; //broken packet
            }
            if ((packet.Ethernet.EtherType == PcapDotNet.Packets.Ethernet.EthernetType.IpV4) && (packet.Ethernet.IpV4.Udp.Length != 0))
            {
                byte[] frame = packet.Ethernet.IpV4.Udp.Payload.ToArray();
                if (frame.Length < 28) return; //check correctness of UDP payload = -1;
                else stream = (int)frame.ReadUInt(Globals.inetStart + 4, Endianity.Big);//((frame[Globals.inetStart + 4] << 24) + (frame[Globals.inetStart + 5] << 16) + (frame[Globals.inetStart + 6] << 8) + frame[Globals.inetStart + 7]);
                //if (!streamID.Contains(stream)) //ignore streams not selected
                //{
                //    //this shall never happen?
                //    //Globals.totalErrors++;
                //    //Save.LogError(String.Format("-1,{0},{1},Unexpected Stream ID received = {2},\n", Globals.totalFrames, Globals.totalErrors, streamID), -1);
                //    return;
                //}
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


        public static void getUDPPayloads()
        {
            int size= Globals.filePCAP_list.Length;
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            /*
            * Parallel could be a HDD killer for big lists
            * 
            * 
            */
           //Parallel.For(0, size, (i) =>
           //{
           //    LoadData(Globals.filePCAP_list[i], i);
           //});

            for (int i = 0; i != size; i++)
            {
                LoadData(Globals.filePCAP_list[i]);
            }
            sw.Stop();
            var msg = String.Format("it took {0} to get UDP payloads",sw.Elapsed.ToString());
            LogItems.addStreamInfo(msg);            
        }
        

        public static void LoadData(string pcapfile)
        {
            
            Dictionary<int, Dictionary<string, double[]>> streamParameters = Globals.limitPCAP;
            //try
            //{           
            int pktCnt;
                using (PacketCommunicator communicator = new OfflinePacketDevice(pcapfile).Open(65536,
                                        PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                        1000))     // read timeout
                {
                    communicator.ReceiveSomePackets(out pktCnt,-1, extractUDP);
                        //communicator.ReceiveSomePackets((0, extractUDP);
                }

                //}
            //}
            //catch (Exception e)
            //{
            //    var msg = (String.Format("Cannot open {0} or crashed during parsing, please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
            //        pcapfile, e.ToString()));
            //    LogItems.addParsingError(msg);
            //}
        }

        private static void extractUDP(Packet packet)
        {
            int stream;
            if (packet.Length < 64)
            {
                return; //broken packet
            }
            if ((packet.Ethernet.EtherType == PcapDotNet.Packets.Ethernet.EthernetType.IpV4) && (packet.Ethernet.IpV4.Udp.Length != 0))
            {
                byte[] frame = packet.Ethernet.IpV4.Udp.Payload.ToArray();
                if (frame.Length < 28) return; //check correctness of UDP payload = -1;
                else stream = (int)frame.ReadUInt(Globals.inetStart + 4, Endianity.Big);
                
                if (Globals.channelsSelected.ContainsKey(stream))
                {
                    SaveUDP(frame,stream);
                }
            }            
        }

        private static void SaveUDP(byte[] frame, int stream)
        {
            string dump = Globals.fileDump;
            BinaryWriter Writer = null;
            string Name = String.Format(@"{0}\Stream_{1}.dat", dump, stream);
           //FileStream fs = null;
            //try
            //{
                // Create a new stream to write to the file
                //fs = new FileStream(Name, FileMode.Append);
            //}
            //catch (Exception e)
            //{
            //    LogItems.addParsingError(String.Format("File not found or something..{0}\nsee below\n{1}", Name, e.Message));
            //}
            using (FileStream fs = new FileStream(Name, FileMode.Append,FileAccess.Write,FileShare.None))
            {
                Writer = new BinaryWriter(fs);
                //Writer.Write(frame);
                for (int i = 0; i != frame.Length; i++)
                    Writer.Write(frame[i]);
                Writer.Flush();
                Writer.Close();
            }            
        }   
    }
    
}
