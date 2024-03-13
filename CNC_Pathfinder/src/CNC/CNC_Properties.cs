using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CNC_Pathfinder.src.CNC
{
    /// <summary>
    /// Structure for all the CNC properties.
    /// </summary>
    [Serializable]
    public class CNC_Properties
    {
        public CNC_Properties Clone()
        {
            //CNC_Properties copy = new CNC_Properties();
            //CNC_Properties copy = (CNC_Properties)this.MemberwiseClone();

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, this);
                ms.Position = 0;
                return (CNC_Properties)formatter.Deserialize(ms);
            }


            /*
            try
            {
                copy._Coordinates_Mode = this._Coordinates_Mode;
                copy._Units_Mode = this._Units_Mode;
                copy._Arc_Plane_Mode = this._Arc_Plane_Mode;
                copy._Feedrate_Mode = this._Feedrate_Mode;
                copy._Home_Position = this._Home_Position;

                copy.MinFeedrate = this.MinFeedrate;
                copy.MaxFeedrate = this.MaxFeedrate;

                copy.LinearFeedrate = this.LinearFeedrate;
                copy.RapidFeedrate = this.RapidFeedrate;

                copy.CNC_Multiblocks_Active = this.CNC_Multiblocks_Active;
                copy.CNC_Multiblock_BufferSize = this.CNC_Multiblock_BufferSize;

                copy.Axes_Step_Dimension = this.Axes_Step_Dimension;
                copy.Axes_Units_Dimension = this.Axes_Units_Dimension;

                copy.StepResolution = this.StepResolution;

                copy.current_location = this.current_location;

                copy.toolActive = this.toolActive;

                copy.motorsActive = this.motorsActive;

                copy.Costume_Home_Position = this.Costume_Home_Position;

                copy.Current_Feedrate = this.Current_Feedrate;

                copy.Current_LaserPower = this.Current_LaserPower;
            }
            catch(Exception e)
            {
                return null;
            }
            */
            //return copy;
        }

        #region Enum Types

        /// <summary>
        /// Defines possible types for the CNC tool.
        /// </summary>
        public enum ToolType
        {
            Laser = 0,
            Drill = 1
        }

        /// <summary>
        /// Defines possible coordinate system types.
        /// </summary>
        public enum Coordinates_Mode
        {
            Relative = 0,
            Absolute = 1
        }

        /// <summary>
        /// Defines possible unit system modes.
        /// </summary>
        public enum Units_Mode
        {
            Metric = 0,
            Imperial = 1
        }

        /// <summary>
        /// Defines the possible arc planes to describe an arc movement.
        /// </summary>
        public enum Arc_Plane_Mode
        {
            Plane_XY = 0,
            Plane_ZX = 1,
            Plane_ZY = 2
        }

        /// <summary>
        /// Defines the possible feedrate modes.
        /// </summary>
        public enum Feedrate_Mode
        {
            UnitsPerMinute = 0,
            Frequency = 1

        }

        /// <summary>
        /// Defines possible positions for the home(origin) location.
        /// </summary>
        public enum Home_Position
        {
            Center = 0,
            BottomLeft = 1,
            BottomRight = 2,
            TopLeft = 3,
            TopRight = 4,
            Costume = 5
        }

        #endregion

        #region Inner Structures

        /// <summary>
        /// 3 axes coordinate (Integer) system.
        /// </summary>
        [Serializable]
        public class Coordinates
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }

            public Coordinates()
            {
                this.X = 0;
                this.Y = 0;
                this.Z = 0;
            }

            public Coordinates(int X, int Y, int Z)
            {
                this.X = X;
                this.Y = Y;
                this.Z = Z;
            }
        }


        /// <summary>
        /// 3 axes coordinate (Double) system.
        /// </summary>
        [Serializable]
        public class CoordinatesD
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public CoordinatesD()
            {
                this.X = 0;
                this.Y = 0;
                this.Z = 0;
            }

            public CoordinatesD(double X, double Y, double Z)
            {
                this.X = X;
                this.Y = Y;
                this.Z = Z;
            }
        }

        #endregion

        #region Properties

        #region CNC Machine sub-properties

        public const double inch_to_mm = 25.4;

        public Coordinates_Mode _Coordinates_Mode { get; set; } /// <seealso cref="CNC_Properties.Coordinates_Mode"/>
        public Units_Mode _Units_Mode { get; set; } /// <seealso cref="CNC_Properties.Units_Mode"/>
        public Arc_Plane_Mode _Arc_Plane_Mode { get; set; } /// <seealso cref="CNC_Properties.Arc_Plane_Mode"/>
        public Feedrate_Mode _Feedrate_Mode { get; set; } /// <seealso cref="CNC_Properties.Feedrate_Mode"/>
        public Home_Position _Home_Position { get; set; } /// <seealso cref="CNC_Properties.Home_Position"/

        public Coordinates_Mode TP_Coordinates_Mode { get; set; } // for toolpath only
        public Units_Mode TP_Units_Mode { get; set; } // for toolpath only


        public double MinFeedrate { get; set; } /// In mm/min.
        public double MaxFeedrate { get; set; } /// In mm/min.

        public double LinearFeedrate { get; set; } /// In selected units/min. (For Toolpathing)
        public double RapidFeedrate { get; set; } /// In selected units/min. (For Toolpathing)
        public double ContourFeedrate { get; set; } /// In selected units/min. (For Toolpathing)

        public bool CNC_Multiblocks_Active { get; set; }
        public int CNC_Multiblock_BufferSize { get; set; } /// CNC multiblock buffer size.

        public Coordinates Axes_Step_Dimension { get; set; } /// In steps.
        public CoordinatesD Axes_Units_Dimension { get; set; } /// In mm. 

        public CoordinatesD StepResolution { get; set; } /// Steps/mm (for each axes)

        #endregion

        #region Tool sub-properties

        public ToolType _ToolType { get; set; } /// <seealso cref="CNC_Properties.ToolType"/>  
        public double tool_diameter_contour { get; set; } /// In selected units.
        public double tool_diameter_fill { get; set; } /// In selected units.
                
        // -- Laser tool sub-properties

        public int laser_maxpower { get; set; } /// In mili watts.

        public int laser_power_fill { get; set; } /// In mili watts.
        public int laser_power_contour { get; set; }
        public int laser_power_idle { get; set; } /// In mili watts. 


        public double laser_height_precision { get; set; } /// In selected units. !DEPRECATED!
        public double laser_height_roughing { get; set; } /// In selected units. !DEPRECATED!

        // ----- Tool Pathing sub-properties
        public int laser_number_of_passes { get; set; } /// Number of passes in the same location. !DEPRECATED!

        // -- Drill tool sub-properties

        public double drill_down_height { get; set; } /// In selected units. !DEPRECATED!
        public double drill_up_height { get; set; } /// In selected units. !DEPRECATED!
        public int drill_spindle { get; set; } /// !DEPRECATED!

        #endregion

        #region Real-Time properties

        public bool waitForOk { get; set; }

        public CoordinatesD current_location { get; set; } /// In selected units.

        public bool toolActive { get; set; }

        public bool motorsActive { get; set; }

        public Coordinates Costume_Home_Position { get; set; } /// In steps (absolute measure from the bottom left)

        public double Current_Feedrate { get; set; }

        public double Current_LaserPower { get; set; }

        #endregion

        #endregion
    }
}
