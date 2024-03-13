using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CNC_Pathfinder.src.CNC;


namespace CNC_Pathfinder.src.Configurations
{
    class App_Configurations
    {

        public CNC_Properties CNC_Configurations { get; private set; }


        public App_Configurations(CNC_Properties CNC_Configurations, Action GUISettingsUpdate, Action<int, int, bool> GUICOMSettingsUpdate)
        {
            this.CNC_Configurations = CNC_Configurations;
            this.GUISettingsUpdate += new GUIUpdater(GUISettingsUpdate);
            this.GUICOMSettingsUpdate += new GUICOMSettings(GUICOMSettingsUpdate);
            LoadSettings();
        }

        private void LoadSettings()
        {
            if(GUISettingsUpdate == null || GUICOMSettingsUpdate == null)
            {
                throw new MissingMethodException();
            }

            this.CNC_Configurations.Axes_Step_Dimension.X = Properties.Settings.Default.CNC_Axes_Step_Dim_X; // !!
            this.CNC_Configurations.Axes_Step_Dimension.Y = Properties.Settings.Default.CNC_Axes_Step_Dim_Y; // !!
            this.CNC_Configurations.Axes_Step_Dimension.Z = Properties.Settings.Default.CNC_Axes_Step_Dim_Z;
            this.CNC_Configurations.Axes_Units_Dimension.X = Properties.Settings.Default.CNC_Axes_Units_Dim_X;
            this.CNC_Configurations.Axes_Units_Dimension.Y = Properties.Settings.Default.CNC_Axes_Units_Dim_Y;
            this.CNC_Configurations.Axes_Units_Dimension.Z = Properties.Settings.Default.CNC_Axes_Units_Dim_Z;
            this.CNC_Configurations.StepResolution.X = Properties.Settings.Default.CNC_Step_Resolution_X;
            this.CNC_Configurations.StepResolution.Y = Properties.Settings.Default.CNC_Step_Resolution_Y;
            this.CNC_Configurations.StepResolution.Z = Properties.Settings.Default.CNC_Step_Resolution_Z;

            this.CNC_Configurations.MinFeedrate = Properties.Settings.Default.CNC_Feedrate_Min; // !!
            this.CNC_Configurations.MaxFeedrate = Properties.Settings.Default.CNC_Feedrate_Max; // !!

            this.CNC_Configurations.laser_maxpower = Properties.Settings.Default.CNC_Laser_MaxPower; // !!

            this.CNC_Configurations._Units_Mode = (CNC_Properties.Units_Mode)Properties.Settings.Default.CNC_Units; // !!
            this.CNC_Configurations._Home_Position = (CNC_Properties.Home_Position)Properties.Settings.Default.CNC_InitialHomePosition; // !!
            this.CNC_Configurations._Feedrate_Mode = (CNC_Properties.Feedrate_Mode)Properties.Settings.Default.CNC_FeedrateMode;

            this.CNC_Configurations.CNC_Multiblock_BufferSize = Properties.Settings.Default.CNC_Multiblocks_Size;

            int baudrateIndex = Properties.Settings.Default.COM_Baudrate;
            int lineterminatorIndex = Properties.Settings.Default.COM_Lineterminator;
            bool waitforok = Properties.Settings.Default.COM_WaitForOK;

            this.CNC_Configurations._ToolType = (CNC_Properties.ToolType)Properties.Settings.Default.TP_ToolType;
            this.CNC_Configurations.laser_power_fill = Properties.Settings.Default.TP_Laser_Power_Cut;
            this.CNC_Configurations.laser_power_contour = Properties.Settings.Default.TP_Laser_Power_Contour;
            this.CNC_Configurations.laser_power_idle = Properties.Settings.Default.TP_Laser_Power_Idle;
            this.CNC_Configurations.drill_spindle = 0;//Properties.Settings.Default.TP_Drill_Speed;
            this.CNC_Configurations.drill_down_height = 0;// Properties.Settings.Default.TP_Drill_Down_UnitsHeight_Z;
            this.CNC_Configurations.drill_up_height = 0;// Properties.Settings.Default.TP_Drill_Up_UnitsHeight_Z;
            this.CNC_Configurations.RapidFeedrate = Properties.Settings.Default.TP_RapidFeedrate;
            this.CNC_Configurations.LinearFeedrate = Properties.Settings.Default.TP_LinearFeedrate;
            this.CNC_Configurations.ContourFeedrate = Properties.Settings.Default.TP_Contour_Feedrate;
            this.CNC_Configurations.tool_diameter_contour = Properties.Settings.Default.TP_ToolDiameter_Precision;
            this.CNC_Configurations.tool_diameter_fill = Properties.Settings.Default.TP_ToolDiameter_Roughing;

            this.CNC_Configurations.laser_height_precision = 0;//Properties.Settings.Default.TP_Laser_Height_Precision;
            this.CNC_Configurations.laser_height_roughing = 0;// Properties.Settings.Default.TP_Laser_Height_Roughing;
            
            GUISettingsUpdate(); // update home/current possition (just the number no the location) + numericboxes limits of feedrate and laser power + CNC panel ratio (X/Y)
            GUICOMSettingsUpdate(baudrateIndex, lineterminatorIndex, waitforok);
        }

        public void Save(int baudrateIndex, int lineterminatorIndex, bool waitforok)
        {

            Properties.Settings.Default.CNC_Axes_Step_Dim_X = this.CNC_Configurations.Axes_Step_Dimension.X;
            Properties.Settings.Default.CNC_Axes_Step_Dim_Y = this.CNC_Configurations.Axes_Step_Dimension.Y;
            Properties.Settings.Default.CNC_Axes_Step_Dim_Z = this.CNC_Configurations.Axes_Step_Dimension.Z;
            Properties.Settings.Default.CNC_Axes_Units_Dim_X = this.CNC_Configurations.Axes_Units_Dimension.X;
            Properties.Settings.Default.CNC_Axes_Units_Dim_Y = this.CNC_Configurations.Axes_Units_Dimension.Y;
            Properties.Settings.Default.CNC_Axes_Units_Dim_Z = this.CNC_Configurations.Axes_Units_Dimension.Z;
            Properties.Settings.Default.CNC_Step_Resolution_X = this.CNC_Configurations.StepResolution.X;
            Properties.Settings.Default.CNC_Step_Resolution_Y = this.CNC_Configurations.StepResolution.Y;
            Properties.Settings.Default.CNC_Step_Resolution_Z = this.CNC_Configurations.StepResolution.Z;

            Properties.Settings.Default.CNC_Feedrate_Min = this.CNC_Configurations.MinFeedrate;
            Properties.Settings.Default.CNC_Feedrate_Max = this.CNC_Configurations.MaxFeedrate;

            Properties.Settings.Default.CNC_Laser_MaxPower = this.CNC_Configurations.laser_maxpower;

            Properties.Settings.Default.CNC_Units = (int)this.CNC_Configurations._Units_Mode;
            Properties.Settings.Default.CNC_InitialHomePosition = (int)this.CNC_Configurations._Home_Position;
            Properties.Settings.Default.CNC_FeedrateMode = (int)this.CNC_Configurations._Feedrate_Mode;

            Properties.Settings.Default.CNC_Multiblocks_Size = this.CNC_Configurations.CNC_Multiblock_BufferSize;

            Properties.Settings.Default.COM_Baudrate = baudrateIndex;
            Properties.Settings.Default.COM_Lineterminator = lineterminatorIndex;
            Properties.Settings.Default.COM_WaitForOK = waitforok;

            Properties.Settings.Default.TP_ToolType = (int)this.CNC_Configurations._ToolType;
            Properties.Settings.Default.TP_Laser_Power_Cut = this.CNC_Configurations.laser_power_fill;
            Properties.Settings.Default.TP_Laser_Power_Contour = this.CNC_Configurations.laser_power_contour;
            Properties.Settings.Default.TP_Laser_Power_Idle = this.CNC_Configurations.laser_power_idle;
            //Properties.Settings.Default.TP_Drill_Speed = this.CNC_Configurations.drill_spindle;
            //Properties.Settings.Default.TP_Drill_Down_UnitsHeight_Z = this.CNC_Configurations.drill_down_height;
            //Properties.Settings.Default.TP_Drill_Up_UnitsHeight_Z = this.CNC_Configurations.drill_up_height;
            Properties.Settings.Default.TP_RapidFeedrate = this.CNC_Configurations.RapidFeedrate;
            Properties.Settings.Default.TP_LinearFeedrate = this.CNC_Configurations.LinearFeedrate;
            Properties.Settings.Default.TP_Contour_Feedrate = this.CNC_Configurations.ContourFeedrate;
            Properties.Settings.Default.TP_ToolDiameter_Precision = this.CNC_Configurations.tool_diameter_contour;
            Properties.Settings.Default.TP_ToolDiameter_Roughing = this.CNC_Configurations.tool_diameter_fill;
            //Properties.Settings.Default.TP_Laser_Height_Precision = this.CNC_Configurations.laser_height_precision;
            //Properties.Settings.Default.TP_Laser_Height_Roughing = this.CNC_Configurations.laser_height_roughing;

            Properties.Settings.Default.Save();
        }

        public void Cancel()
        {
            GUISettingsUpdate();
        }

        // update home/current possition (just the number no the location) + numericboxes limits of feedrate and laser power + CNC panel ratio (X/Y)
        public delegate void GUIUpdater();
        public GUIUpdater GUISettingsUpdate;

        public delegate void GUICOMSettings(int baudrateIndex, int lineterminatorIndex, bool waitforok);
        public GUICOMSettings GUICOMSettingsUpdate;

    }
}
