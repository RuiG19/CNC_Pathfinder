using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CNC_Pathfinder.src.Utilities;

namespace CNC_Pathfinder.src.GCode
{
    public sealed class GCODE
    {

        #region Parameters and Structures

        public struct Function{
            public int LineNumber;
            public string cmd;
            public char func_type;
            public int func_index;
            public int ParameterCount;
            public List<Parameter> ParametersList;
        }

        public Function gcode;

        public struct Parameter
        {
            public char type;
            public double value;
            public string text;
        }

        #endregion

        #region Constructors

        public GCODE(string str)
        {
            gcode = parseString(str);

            /*try
            {
                gcode = parseString(str);
            }
            catch (Exception e)
            {
                throw e;
            }*/
        }

        #endregion

        #region Parser Functions

        public Function parseString(string str)
        {
            string[] elements_Split = str.Split(' ', '\n', '\r');
            int nElements = elements_Split.Length;
            if (nElements == 0)
            {
                throw new ArgumentNullException();
            }

            // remove empty strings (from excess spacing)
            int emptyStr_Count = 0;
            for(int s = 0; s < nElements; s++)
            {
                if(elements_Split[s].Length == 0)
                {
                    emptyStr_Count++;
                }
            }
            nElements -= emptyStr_Count;
            string[] elements = new string[nElements];
            int i = 0;
            for (int s = 0; s < (nElements + emptyStr_Count); s++)
            {
                if (elements_Split[s].Length != 0)
                {
                    elements[i] = elements_Split[s];
                    i++;
                }

            }

            i = 0;

            
            //
            Function gcode = new Function();
            gcode.cmd = str;


            if (elements[i][0] == LineNumber) // ignore line number
            {
                if(nElements == 1)
                {
                    throw new InvalidOperationException();
                }

                i++;
                String value_str = elements[i].Substring(1);
                int value = 0;

                try
                {
                    value = int.Parse(value_str);
                    gcode.LineNumber = value;
                }
                catch (Exception e)
                {

                }

            }



            if (!Validate_GCODE(elements[i]))
            {
                throw new InvalidOperationException();
            }
            
            gcode.func_type = elements[i][0];
            gcode.func_index = int.Parse(elements[i].Substring(1));
            
            i++;

            gcode.ParameterCount = nElements - i;

            if(nElements == 0)
            {
                return gcode;
            }

            gcode.ParametersList = new List<Parameter>();

            for (; i < nElements; i++)
            {
                if (!Validate_Parameter(elements[i][0])) // ignore invalid parameters
                {
                    gcode.ParameterCount--;
                    continue;
                }

                Parameter p = new Parameter();
                p.type = elements[i][0];

                if(elements[i].Length > 1)
                {
                    string value_str = elements[i].Substring(1);
                    double value = 0;

                    try
                    {
                        value = double.Parse(value_str, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo);
                    }
                    catch (Exception e)
                    {
                        gcode.ParameterCount--;
                        continue;
                    }

                    p.value = value;
                }


                gcode.ParametersList.Add(p);
            }

            return gcode;
        }

        public bool Validate_GCODE(string str)
        {
            if (str[0] != 'G' && str[0] != 'M') // Check if it's a valid function
            {
                return false;
            }

            string str_func_number = str.Substring(1);
            int func_number = -1;

            try
            {
                func_number = int.Parse(str_func_number);
            }
            catch(Exception e)
            {
                return false;
            }

            if (str[0] == 'G')
            {
                for (int i = 0; i < G_valid.Length; i++)
                {
                    if (func_number == G_valid[i])
                    {
                        return true;
                    }
                }
                return false;
            }

            if (str[0] == 'M')
            {
                for (int i = 0; i < M_valid.Length; i++)
                {
                    if (func_number == M_valid[i])
                    {
                        return true;
                    }
                }
                return false;
            }
            

            return false;
        }

        public bool Validate_Parameter(char str)
        {
            switch (str)
            {
                case TimeSeconds:
                case TimeMillis:    
                case CoordX:
                case CoordY:    
                case CoordZ:    
                case CoordI:    
                case CoordJ:    
                case CoordK:    
                case Feedrate:  
                case Radius:
                case param_E:
                case param_A:
                case param_B:
                case param_C:
                case param_D:
                case param_H:
                    return true;
                default:
                    return false;
            }

        }

        #endregion

        #region Auxiliary Functions

        public class Location
        {
            public double x { get; set; }
            public double y { get; set; }

            public Location(double x, double y)
            {
                this.x = x;
                this.y = y;
            }
        }

        public static Location Convert_R_to_IJ(Location start, Location finish, double radius, bool clockwise)
        {
            bool less180deg = (radius > 0)? true : false;

            double r = Math.Abs(radius);
            double x1 = start.x;
            double y1 = start.y;
            double x2 = finish.x;
            double y2 = finish.y;

            Location c1 = new Location(0, 0);
            Location c2 = new Location(0, 0);


            double x3 = (x1 + x2) / 2;
            double y3 = (y1 + y2) / 2;
            double Q = Math.Sqrt( Math.Pow(x1-x2,2) + Math.Pow(y1 - y2, 2) );

            c1.x = x3 + Math.Sqrt(Math.Pow(r, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(y1 - y2) / Q;
            c1.y = y3 + Math.Sqrt(Math.Pow(r, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(x2 - x1) / Q;
            c2.x = x3 - Math.Sqrt(Math.Pow(r, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(y1 - y2) / Q;
            c2.y = y3 - Math.Sqrt(Math.Pow(r, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(x2 - x1) / Q;
            
            //c2.x =((double)x1 / 2 + x2 / 2 - (y1 * Math.Sqrt(-(-4 * Math.Pow(r, 2) + Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)) / (Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)))) / 2 + (y2 * Math.Sqrt(-(-4 * Math.Pow(r, 2) + Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)) / (Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)))) / 2);
            //c1.x = ((double)x1 / 2 + x2 / 2 + (y1 * Math.Sqrt(-(-4 * Math.Pow(r, 2) + Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)) / (Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)))) / 2 - (y2 * Math.Sqrt(-(-4 * Math.Pow(r, 2) + Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)) / (Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)))) / 2);
            
            //c2.y = ((double)y1 / 2 + y2 / 2 + (x1 * Math.Sqrt(-(-4 * Math.Pow(r, 2) + Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)) / (Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)))) / 2 - (x2 * Math.Sqrt(-(-4 * Math.Pow(r, 2) + Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)) / (Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)))) / 2);
            //c1.y = ((double)y1 / 2 + y2 / 2 - (x1 * Math.Sqrt(-(-4 * Math.Pow(r, 2) + Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)) / (Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)))) / 2 + (x2 * Math.Sqrt(-(-4 * Math.Pow(r, 2) + Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)) / (Math.Pow(x1, 2) - 2 * x1 * x2 + Math.Pow(x2, 2) + Math.Pow(y1, 2) - 2 * y1 * y2 + Math.Pow(y2, 2)))) / 2);

            Location Ref = new Location(r, 0);
            Location start_1 = new Location(start.x - c1.x, start.y - c1.y);
            Location finish_1 = new Location(finish.x - c1.x, finish.y - c1.y);
            Location start_2 = new Location(start.x - c2.x, start.y - c2.y);
            Location finish_2 = new Location(finish.x - c2.x, finish.y - c2.y);

            //float startAngle_1 = Math_Utilities.AngleBetween(start_1, Ref);
            //float finishAngle_1 = Math_Utilities.AngleBetween(finish_1, Ref);

            //float startAngle_2 = Math_Utilities.AngleBetween(start_2, Ref);
            //float finishAngle_2 = Math_Utilities.AngleBetween(finish_2, Ref);

            float sweepAngle_1 = Math_Utilities.AngleBetween_v2(start_1, finish_1); //finishAngle_1 - startAngle_1;
            float sweepAngle_2 = Math_Utilities.AngleBetween_v2(start_2, finish_2); // finishAngle_2 - startAngle_2;

            if (clockwise)
            {
                sweepAngle_1 = Math.Abs(360 - sweepAngle_1);
                sweepAngle_2 = Math.Abs(360 - sweepAngle_2);
            }


            if (less180deg)
            {
                if (sweepAngle_1 > sweepAngle_2)
                {
                    return c2;
                }
                else
                {
                    return c1;
                }
            }
            else
            {
                if (sweepAngle_1 > sweepAngle_2)
                {
                    return c1;
                }
                else
                {
                    return c2;
                }
            }
        }

        #endregion


        #region GCODE Commands

        #region ACK and other return messages

        public const string ack = "ok"; 
        public const string nack = "rs";
        public const string msg_overflow = "ov";
        public const string hardware_fault = "!!"; // cnc disconnects after this message
        public const string info_start = "//"; // at the start of a degub or information line

        public const string cnc_start = "start";
        public const string cnc_stop = "stop";

        public const string cnc_status_idle = "// st: idle";
        public const string cnc_status_busy = "// st: busy";

        public const string cnc_error_ok = "// er: 0";
        public const string cnc_error_syntax = "// er: 1";
        public const string cnc_error_motorOff = "// er: 2";

        public const string cnc_position_update = "// ps: "; // Xnnn Ynnn Znnn
        public const string cnc_definitions_update = "// df: "; // 

        #endregion

        #region Parameters

        public const char LineNumber = 'N';    // Number of Line.
        public const char TimeSeconds = 'S';    // Command parameter, such as time in seconds; temperatures; voltage to send to a motor
        public const char TimeMillis = 'P';    // Command parameter, such as time in milliseconds; proportional (Kp) in PID Tuning
        public const char CoordX = 'X';    // A X coordinate, usually to move to. This can be an Integer or Fractional number.
        public const char CoordY = 'Y';    // A Y coordinate, usually to move to. This can be an Integer or Fractional number.
        public const char CoordZ = 'Z';    // A Z coordinate, usually to move to. This can be an Integer or Fractional number.
        public const char CoordI = 'I';    // Parameter - X-offset in arc move. 
        public const char CoordJ = 'J';    // Parameter - Y-offset in arc move.
        public const char CoordK = 'K';    // Parameter - Z-offset in arc move.
        public const char Feedrate = 'F';    // Feedrate in mm per minute. (Speed of print head movement)
        public const char Radius = 'R';    // Parameter - Radius
        public const char Checksum = '*';   // Checksum. Used to check for communications errors.
        public const char param_E = 'E';
        public const char param_A = 'A';
        public const char param_B = 'B';
        public const char param_C = 'C';
        public const char param_D = 'D';
        public const char param_H = 'H';

        #endregion

        #region G-Codes

        public static int[] G_valid = { 0, 1, 2, 3, 4, 20, 21, 28, 90, 91, 92, 93, 94};
        
        public const string Rapid_Movement = "G00"; // [X Y Z F]
        public const string Linear_Movement = "G01"; // [X Y Z F]
        public const string Arc_CW_Movement = "G02"; // [X Y Z I J K F]
        public const string Arc_CCW_Movement = "G03"; // [X Y Z I J K F]

        public const string Dwell = "G04"; // [ P S ] Wait/delay  P - Milliseconds | S - Seconds 

        public const string Home_Movement = "G28"; // [  X Y Z]

        public const string Set_Positioning_Movement = "G92"; // [X Y Z F] Same as G90 + G00 ...

        public const string Arc_Plane_XY = "G17"; // [ ]
        public const string Arc_Plane_ZX = "G18"; // [ ]
        public const string Arc_Plane_ZY = "G19"; // [ ]

        public const string Units_Imperial = "G20"; // [ ]
        public const string Units_Metric = "G21"; // [ ]

        public const string Distance_Absolute = "G90"; // [ ]
        public const string Distance_Relative = "G91"; // [ ]

        public const string Feedrate_InverseTime = "G93"; // [ ]
        public const string Feedrate_UnitsPerMinute = "G94"; // [ ]

        public const string Set_Home_Minimum = "G161"; // [X Y Z]
        public const string Set_Home_Maximum = "G162"; // [X Y Z]

        #endregion

        #region M-Codes

        public static int[] M_valid = { 0, 1, 2, 3, 4, 5, 17, 18, 112, 114, 115, 117, 122, 260, 261, 262, 263, 264, 265, 266, 267, 270, 271, 272, 666 };

        public const string Program_Stop = "M00";
        public const string Program_Pause = "M01";
        public const string Program_End = "M02";
        public const string Tool_On = "M03";
        public const string Tool_On_ = "M04";
        public const string Tool_Off = "M05";

        public const string Motor_On = "M17";
        public const string Motor_Off = "M18";

        public const string Emergency_Stop = "M112";
        public const string Get_Current_Position = "M114";

        public const string Get_Last_Error = "M122";

        public const string CNC_Calibration = "M260";
        public const string Set_Home = "M261";
        public const string Set_Laser_Power = "M264";
        public const string Set_Laser_Freq_Pulses = "M265";
        public const string Laser_Mode = "M266";
        public const string Laser_Profile = "M267";

        public const string Set_CNC_Definitions = "M270";
        public const string Get_CNC_Definitions = "M271";
        public const string CNC_Status_Check = "M272";

        public const string CNC_Reset = "M666";

        #endregion

        #region Routines

        public const string TurnOff_Multiblocs = "M270 D0";
        public const string TurnOn_Multiblocs = "M270 D1";

        public const string SetMultiblocks_Size = "M270 E";

        public const string Set_Home_BottomLeft = "M261 S";
        public const string Set_Home_BottomRight = "M261 F";
        public const string Set_Home_TopLeft = "M261 E";
        public const string Set_Home_TopRight = "M261 P";
        public const string Set_Home_Center = "M261 K";

        #endregion

        #endregion
    }
}
