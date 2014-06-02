using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plot_iNET_X.Analyser_Logic
{
    //calculation
    public static class getValue
    {
        //public static UInt64 getPTPTime(byte[] input)
        //{
        //    UInt64 value = 0;
        //    input.ReadUInt48(0, new Endianity());
        //    value = BitConverter.ToUInt64(input, 0);
        //    return value;
        //}


        public static long getConcatedParameter(int shift, int srcMSB, int srcLSB)
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
            value = ((getValue.CalcValue(16, srcInput, FSRmax, FSRmin) + con1) * con2) / con3;
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

}
