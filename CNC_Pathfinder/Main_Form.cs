using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Media;
using System.Globalization;
using System.Threading;
using Svg;

// Costum namespaces
using CNC_Pathfinder.src.Communication;
using CNC_Pathfinder.src.GCode;
using CNC_Pathfinder.src.CNC;
using CNC_Pathfinder.src.Toolpathing;
using CNC_Pathfinder.src.Configurations;

namespace CNC_Pathfinder
{
    public partial class Main_Form : Form
    {
        #region Parameters

        public enum Main_Form_Menu
        {
            Control_Panel = 0,
            Image_Toolpathing = 1,
            Configurations = 2
        }

        public enum Control_Panel_State
        {
            disconnect = 0,
            manual_control = 1,
            printing_play = 2,
            priting_pause = 3,
            printing_resume = 4
        }

        private Control_Panel_State ControlPanelState = Control_Panel_State.manual_control;

        private CNC Machine;

        private IMG_Manager Toolpathing_Manager;

        private App_Configurations CNC_Settings;

        private FormWindowState LastWindowState = FormWindowState.Minimized;

        //
        private NumberFormatInfo nfi = new NumberFormatInfo();

        private static string[] lineTerminators = new string[] { "", "\n", "\r", "\r\n" };
        private static string[] lineTerminatorsCombobox = new string[] { "None", "New Line \'\\n\'", "Carriage Return \'\\r\'", "Both \'\\r\\n\'" };

        private static string[] TP_BMP_Mode_Only = new string[] { "BMP" };
        private static string[] TP_BMP_SVG_Mode = new string[] { "BMP", "SVG", "SVG (Edges Only)" };

        private static int[] baudrateList = new int[] { 4800, 9600, 14400, 19200, 28800, 38400, 56000, 57600, 115200 };

        private Color _LightGray = Color.FromArgb(148,148,148);
        private Color _DarkGray = Color.FromArgb(117,117,117);
        private Color _LightBlue = Color.FromArgb(0, 122, 204);
        private Color _DarkBlue = Color.FromArgb(0, 99, 177);

        // File parameters

        private OpenFileDialog FileBrowser = new OpenFileDialog();

        #endregion

        #region Constructor

        public Main_Form()
        {
            InitializeComponent();

            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = "";
            
            FileBrowser.Filter = "gcode files|*.gcode;*.GCODE;*.tap;*.TAP;*.nc;*.NC|All files (*.*)|*.*";
            FileBrowser.FilterIndex = 2;

            InitializeManagers();
            InitializeMyComponents();

            CNC_Settings = new App_Configurations(Machine.Configurations, Config_GUI_Update, Config_COM_Update);

            InitializeGUIProperties();

            Toolpathing_Manager.DisplayMode(IMG_Manager.ImageDisplay.NoImage);

        }

        #endregion

        #region Init Functions

        private void InitializeGUIProperties()
        {
            //
            // General Initializations
            //


            setMenu(Main_Form_Menu.Control_Panel);
            setControlPanel(Control_Panel_State.disconnect);
            // Start Form Maximized
            this.Size = this.MinimumSize;
            //WindowState = FormWindowState.Maximized;

            //label_App_Build_Version.Text = "Build: v" + Application.ProductVersion;


            //
            Application.DoEvents();
        }

        private void InitializeManagers()
        {
            Machine = new CNC();
            Machine.Disconnected += new EventHandler(onCNCDisconnect);
            Machine.MessageReceived += new EventHandler(onMsgReceived);
            Machine.MessageSent += new EventHandler(onMsgSent);
            Machine.updatePosition += new CNC.cnc_updatePosition(updateCurrentPositionChange);
            Machine.updateConfigurations += new CNC.cnc_updateConfigurations(updateConfigurations);
            Machine.printProgress += new CNC.cnc_printProgress(fileProgress);
            Machine.printWarning += new CNC.cnc_printWarning(textBox_cmd_monitor_write);
            Machine.backgroundworker_finished += new CNC.cnc_backgroundworker_finished(cnc_backgroundwork_finished);
            Machine.backgroundworker_draw += new CNC.sim_backgroundworker_draw(File_Sim_Draw);
            Machine.backgroundworker_sim_finished += new CNC.sim_backgroundworker_finished(File_sim_finished);
            Machine.BusyStateChange += new CNC.CNC_BusyStateChange(CNC_BusyStateChanged);

            Toolpathing_Manager = new IMG_Manager(Machine.Configurations);
            Toolpathing_Manager.DisplayImage += new IMG_Manager.ImageDisplayer(DisplayImage);
            Toolpathing_Manager.DisplayMode += new IMG_Manager.setDisplayMode(DisplayMode);
            Toolpathing_Manager.ProgressUpdate += new IMG_Manager.TPProgressUpdate(TP_UpdateProgress);

            
            //CNC_Settings.GUISettingsUpdate += new App_Configurations.GUIUpdater(Config_GUI_Update);
            //CNC_Settings.GUICOMSettingsUpdate += new App_Configurations.GUICOMSettings(Config_COM_Update);
                        
            Application.DoEvents();
        }

        private void InitializeMyComponents()
        {
            comboBox_Configuration_LineTerminator.DataSource = lineTerminatorsCombobox;
            //comboBox_Configuration_LineTerminator.SelectedIndex = 1;

            comboBox_Configuration_baudrate.DataSource = baudrateList;
            //comboBox_Configuration_baudrate.SelectedItem = 115200;

            
            update_COMPort_List();

            //checkBox_waitForCNCAck.Checked = Machine.Configurations.waitForOk;
            /*
            numericUpDown_ControlPanel_distance.Controls[0].Visible = false;
            numericUpDown_ControlPanel_feedrate.Controls[0].Visible = false;
            numericUpDown_ControlPanel_laser.Controls[0].Visible = false;

            numericUpDown_TP_img_width_pp.Controls[0].Visible = false;
            numericUpDown_TP_img_height_pp.Controls[0].Visible = false;

            numericUpDown_Config_Axes_Units_Dim_X.Controls[0].Visible = false;
            numericUpDown_Config_Axes_Steps_Dim_X.Controls[0].Visible = false;
            numericUpDown_Config_Axes_Units_Dim_Y.Controls[0].Visible = false;
            numericUpDown_Config_Axes_Steps_Dim_Y.Controls[0].Visible = false;
            numericUpDown_Config_Axes_Units_Dim_Z.Controls[0].Visible = false;
            numericUpDown_Config_Axes_Steps_Dim_Z.Controls[0].Visible = false;
            numericUpDown_Config_Feedrate_Min.Controls[0].Visible = false;
            numericUpDown_Config_Feedrate_Max.Controls[0].Visible = false;
            numericUpDown_Config_LaserPower_Max.Controls[0].Visible = false;
            numericUpDown_Config_Multiblocks_Size.Controls[0].Visible = false;

            numericUpDown_Config_Feedrate_Rapid.Controls[0].Visible = false;
            numericUpDown_Config_Feedrate_Linear.Controls[0].Visible = false;
            numericUpDown_Config_Tool_Diam_Rough.Controls[0].Visible = false;
            numericUpDown_Config_Tool_Diam_Precision.Controls[0].Visible = false;

            numericUpDown_Config_Laser_Power_Cut.Controls[0].Visible = false;
            numericUpDown_Config_Laser_Power_Idle.Controls[0].Visible = false;
            numericUpDown_Config_Laser_height_rough.Controls[0].Visible = false;
            numericUpDown_Config_Laser_height_precision.Controls[0].Visible = false;

            numericUpDown_Config_Drill_Down_height.Controls[0].Visible = false;
            numericUpDown_Config_Drill_Up_height.Controls[0].Visible = false;
            numericUpDown_Config_Drill_Speed.Controls[0].Visible = false;
            */

            toolTip_main_form_tt.SetToolTip(button_ControlPanel_update_COM_Ports, "Update COM Ports List");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_Reset_CNC, "Restart CNC");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_z_minus, "Z-");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_z_plus, "Z+");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_y_plus, "Y+");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_y_minus, "Y-");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_x_plus, "X+");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_x_minus, "X-");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_home_all,"Move all axes to Home Position");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_home_x, "Move X axis to Home Position");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_home_y, "Move Y axis to Home Position");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_home_z, "Move Z axis to Home Position");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_SetHome, "Set Current Position as Home");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_updatePosition, "Update Current Position");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_Motor_OnOff, "Turn the Motors On/Off");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_laser, "Turn the Laser On/Off");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_Load_File, "Load a gcode file");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_Close_File, "Close the gcode file");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_Start_File, "Start");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_Resume_File, "Resume");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_Pause_File, "Pause");
            toolTip_main_form_tt.SetToolTip(button_ControlPanel_Cancel_File, "Stop");



            comboBox_TP_ED_List.DataSource = new string[] { "SXOR", "Robert's Cross", "Perwitt", "Sobel" };
            comboBox_TP_ED_List.SelectedIndex = 3;
            comboBox_TP_RemoveColor.DataSource = new string[] { "Black", "White" };
            comboBox_TP_RemoveColor.SelectedIndex = 0;
            comboBox_TP_PathType.DataSource = new string[] { "Zig-Zag" , "Spiral"};
            comboBox_TP_PathType.SelectedIndex = 0;
            comboBox_TP_Img_Selection.DataSource = new string[] { "All Paths", "Tool Only" };

            comboBox_TP_Mode.DataSource = TP_BMP_Mode_Only;
            comboBox_TP_Mode.SelectedIndex = 0;

            comboBox_Config_Units.DataSource = new string[] {"Metric" , "Imperial"};
            comboBox_Config_HomePosition.DataSource = new string[] { "Center", "Bottom Left", "Bottom Right", " Top Left", "Top Right"};
            //comboBox_Config_ToolType.DataSource = new string[] {"Laser", "Drill" };


            
            //button_AdvancedOptions.Text = "Show Advanced Options";

            Application.DoEvents();
        }

        #endregion


        //region GUI Control Functions

        private void Main_Form_Resize(object sender, EventArgs e)
        {
            // Control Panel

            // XYZ labels
            int label_size = (panel_XYZ_labels.Size.Width - 6) / 3;
            label_ControlPanel_X_value.Size = new Size(label_size, label_ControlPanel_X_value.Size.Height);
            label_ControlPanel_Y_value.Size = new Size(label_size, label_ControlPanel_Y_value.Size.Height);
            label_ControlPanel_Z_value.Size = new Size(label_size, label_ControlPanel_Z_value.Size.Height);

            label_ControlPanel_Y_value.Location = new Point(label_size + 6, label_ControlPanel_Y_value.Location.Y);
            label_ControlPanel_Z_value.Location = new Point(label_ControlPanel_Y_value.Location.X + label_size + 6, label_ControlPanel_Z_value.Location.Y);

            // panel_XY_plane + panel_XY_baseFrame
            Resize_XY_Panel();


            // When window state changes
            if (WindowState != LastWindowState)
            {
                LastWindowState = WindowState;

                updateCurrentPositionChange(Machine.Configurations.current_location.X, Machine.Configurations.current_location.Y, Machine.Configurations.current_location.Z);

            }
        }

        private void Resize_XY_Panel()
        {
            // panel_XY_plane + panel_XY_baseFrame
            int x_size = 0, y_size = 0, x_loc = 0, y_loc = 0;
            double ratio = (double)Machine.Configurations.Axes_Units_Dimension.X / Machine.Configurations.Axes_Units_Dimension.Y;

            if (Machine.Configurations.Axes_Units_Dimension.X > Machine.Configurations.Axes_Units_Dimension.Y)
            {
                x_size = panel_XY_baseFrame.Size.Width;
                y_size = Convert.ToInt32(Math.Round(panel_XY_baseFrame.Size.Width / ratio));
                x_loc = -1;
                y_loc = Convert.ToInt32(Math.Round((double)(panel_XY_baseFrame.Size.Height - y_size) / 2));
                if (y_loc <= 0)
                {
                    y_size = panel_XY_baseFrame.Size.Height;
                    x_size = Convert.ToInt32(Math.Round(panel_XY_baseFrame.Size.Height * ratio));
                    y_loc = -1;
                    x_loc = Convert.ToInt32(Math.Round((double)(panel_XY_baseFrame.Size.Width - x_size) / 2));
                }
            }
            else
            {
                y_size = panel_XY_baseFrame.Size.Height;
                x_size = Convert.ToInt32(Math.Round(panel_XY_baseFrame.Size.Height * ratio));
                y_loc = -1;
                x_loc = Convert.ToInt32(Math.Round((double)(panel_XY_baseFrame.Size.Width - x_size) / 2));
                if (x_loc <= 0)
                {
                    x_size = panel_XY_baseFrame.Size.Width;
                    y_size = Convert.ToInt32(Math.Round(panel_XY_baseFrame.Size.Width / ratio));
                    x_loc = -1;
                    y_loc = Convert.ToInt32(Math.Round((double)(panel_XY_baseFrame.Size.Height - y_size) / 2));
                }
            }

            panel_XY_plane.Size = new System.Drawing.Size(x_size, y_size);
            panel_XY_plane.Location = new System.Drawing.Point(x_loc, y_loc);
        }

        private void Main_Form_ResizeEnd(object sender, EventArgs e)
        {
            updateCurrentPositionChange(Machine.Configurations.current_location.X, Machine.Configurations.current_location.Y, Machine.Configurations.current_location.Z);
        }

        #region Menus Control

        private void setMenu(Main_Form_Menu menu)
        {
            switch (menu)
            {
                case Main_Form_Menu.Control_Panel:
                    // buttons
                    checkBox_Menu_ControlPanel.Checked = true;
                    checkBox_Menu_ImageToolpathing.Checked = false;
                    checkBox_Menu_Configurations.Checked = false;

                    //panels
                    panel_Menu_ControlPanel.Visible = true;
                    panel_Menu_ImageToolpathing.Visible = false;
                    panel_Menu_Configurations.Visible = false;

                    this.AcceptButton = button_ControlPanel_cmd_send;
                    update_COMPort_List();

                    break;
                case Main_Form_Menu.Image_Toolpathing:
                    // buttons
                    checkBox_Menu_ControlPanel.Checked = false;
                    checkBox_Menu_ImageToolpathing.Checked = true;
                    checkBox_Menu_Configurations.Checked = false;

                    //panels
                    panel_Menu_ControlPanel.Visible = false;
                    panel_Menu_ImageToolpathing.Visible = true;
                    panel_Menu_Configurations.Visible = false;

                    this.AcceptButton = null;

                    break;
                case Main_Form_Menu.Configurations:
                    // buttons
                    checkBox_Menu_ControlPanel.Checked = false;
                    checkBox_Menu_ImageToolpathing.Checked = false;
                    checkBox_Menu_Configurations.Checked = true;

                    //panels
                    panel_Menu_ControlPanel.Visible = false;
                    panel_Menu_ImageToolpathing.Visible = false;
                    panel_Menu_Configurations.Visible = true;

                    this.AcceptButton = null;

                    break;
                default:
                    throw new InvalidOperationException();

            }
        }

        private void checkBox_Menu_ControlPanel_onClick(object sender, EventArgs e)
        {
            setMenu(Main_Form_Menu.Control_Panel);
        }

        private void checkBox_Menu_ImageToolpathing_onClick(object sender, EventArgs e)
        {
            setMenu(Main_Form_Menu.Image_Toolpathing);
        }

        private void checkBox_Menu_Configurations_onClick(object sender, EventArgs e)
        {
            setMenu(Main_Form_Menu.Configurations);
        }



        #endregion

        #region Control_Panel

        // GUI On Off Elements

        private delegate void setControlPanel_Callback(Control_Panel_State state);

        private void setControlPanel(Control_Panel_State state)
        {
            if (this.button_ControlPanel_DisConnect.InvokeRequired)
            {
                setControlPanel_Callback d = new setControlPanel_Callback(setControlPanel);
                this.button_ControlPanel_DisConnect.Invoke(d, new object[] { state });
                return;
            }

            if (ControlPanelState == state)
            {
                return;
            }
            ControlPanelState = state;

            if(state != Control_Panel_State.disconnect)
            {
                updateCurrentPositionChange(Machine.Configurations.current_location.X, Machine.Configurations.current_location.Y, Machine.Configurations.current_location.Z);
                updateConfigurations();
            }
            

            switch (state)
            {
                case Control_Panel_State.disconnect:
                    button_ControlPanel_DisConnect.Text = "Connect";
                    button_ControlPanel_DisConnect.Enabled = true;
                    button_ControlPanel_DisConnect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    button_ControlPanel_DisConnect.Size = new Size(127,26);

                    comboBox_ControlPanel_COM_Ports.Enabled = true;
                    button_ControlPanel_update_COM_Ports.Enabled = true;
                    button_ControlPanel_update_COM_Ports.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    button_ControlPanel_Reset_CNC.Enabled = false;
                    button_ControlPanel_Reset_CNC.BackColor = _LightGray;

                    button_ControlPanel_x_plus.Enabled = false;
                    button_ControlPanel_x_minus.Enabled = false;
                    button_ControlPanel_y_plus.Enabled = false;
                    button_ControlPanel_y_minus.Enabled = false;
                    button_ControlPanel_z_plus.Enabled = false;
                    button_ControlPanel_z_minus.Enabled = false;
                    button_ControlPanel_x_minus_y_plus.Enabled = false;
                    button_ControlPanel_x_minus_y_minus.Enabled = false;
                    button_ControlPanel_x_plus_y_plus.Enabled = false;
                    button_ControlPanel_x_plus_y_minus.Enabled = false;
                    button_ControlPanel_x_plus.BackColor = _LightGray;
                    button_ControlPanel_x_minus.BackColor = _LightGray;
                    button_ControlPanel_y_plus.BackColor = _LightGray;
                    button_ControlPanel_y_minus.BackColor = _LightGray;
                    button_ControlPanel_z_plus.BackColor = _DarkGray;
                    button_ControlPanel_z_minus.BackColor = _DarkGray;
                    button_ControlPanel_x_minus_y_plus.BackColor = _LightGray;
                    button_ControlPanel_x_minus_y_minus.BackColor = _LightGray;
                    button_ControlPanel_x_plus_y_plus.BackColor = _LightGray;
                    button_ControlPanel_x_plus_y_minus.BackColor = _LightGray;
                    try
                    {
                        pictureBox_control_button.Image.Dispose();
                    }
                    finally
                    {
                        pictureBox_control_button.Image = new Bitmap(CNC_Pathfinder.Properties.Resources.control_btn_pic_gray);
                    }
                    button_ControlPanel_home_all.Enabled = false;
                    button_ControlPanel_home_x.Enabled = false;
                    button_ControlPanel_home_y.Enabled = false;
                    button_ControlPanel_home_z.Enabled = false;
                    button_ControlPanel_home_all.BackColor = _LightGray;
                    button_ControlPanel_home_x.BackColor = _LightGray;
                    button_ControlPanel_home_y.BackColor = _LightGray;
                    button_ControlPanel_home_z.BackColor = _LightGray;
                    button_ControlPanel_Stop.Enabled = false;
                    button_ControlPanel_Stop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
                    try
                    {
                        button_ControlPanel_Stop.BackgroundImage.Dispose();
                    }
                    finally
                    {
                        button_ControlPanel_Stop.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.emergency_stop_btn_gray);
                    }
                    try
                    {
                        pictureBox_EmergencyStop.Image.Dispose();
                    }
                    finally
                    {
                        pictureBox_EmergencyStop.Image = new Bitmap(CNC_Pathfinder.Properties.Resources.emergency_stop_pic_gray);
                    }
                    button_ControlPanel_Motor_OnOff.Enabled = false;
                    button_ControlPanel_Motor_OnOff.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));

                    trackBar_ControlPanel_distance.Enabled = false;
                    numericUpDown_ControlPanel_distance.Enabled = false;
                    trackBar_ControlPanel_feedrate.Enabled = false;
                    numericUpDown_ControlPanel_feedrate.Enabled = false;
                    trackBar_ControlPanel_laser.Enabled = false;
                    numericUpDown_ControlPanel_laser.Enabled = false;
                    button_ControlPanel_laser.Enabled = false;
                    button_ControlPanel_laser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
                    label_ControlPanel_distance.Enabled = false;
                    label_ControlPanel_feedrate.Enabled = false;
                    label_ControlPanel_laser.Enabled = false;

                    button_ControlPanel_Load_File.Enabled = false;
                    button_ControlPanel_Close_File.Enabled = false;
                    button_ControlPanel_Start_File.Enabled = false;
                    button_ControlPanel_Pause_File.Enabled = false; 
                    button_ControlPanel_Cancel_File.Enabled = false;
                    button_ControlPanel_Resume_File.Enabled = false;
                    button_ControlPanel_Load_File.BackColor = _LightGray;
                    button_ControlPanel_Close_File.BackColor = _LightGray;
                    button_ControlPanel_Start_File.BackColor = _LightGray;
                    button_ControlPanel_Pause_File.BackColor = _LightGray;
                    button_ControlPanel_Cancel_File.BackColor = _LightGray;
                    button_ControlPanel_Resume_File.BackColor = _LightGray;
                    button_ControlPanel_Start_File.Visible = true;
                    button_ControlPanel_Resume_File.Visible = false;

                    progressBar_ControlPanel_file_progress.Enabled = false;
                    progressBar_ControlPanel_file_progress.Value = 0;
                    label_ControlPanel_file_progress.Text = "0%";
                    label_ControlPanel_file_progress.Enabled = false;
                    File_Stopwatch_stop();
                    File_Stopwatch_resetCount();
                    label_ControlPanel_file_time.Enabled = false;
                    button_ControlPanel_cmd_send.Enabled = false;
                    button_ControlPanel_cmd_send.BackColor = _LightGray;
                    label_FILE.Enabled = false;
                    label_File_Name.Enabled = false;
                    label_File_Name.Text = "(none)";
                    toolTip_main_form_tt.SetToolTip(label_File_Name, "(none)");
                    CloseFile();


                    textBox_ControlPanel_cmd_monitor.Clear();
                    textBox_ControlPanel_cmd_monitor.Enabled = false;
                    textBox_ControlPanel_cmd_send.Clear();
                    textBox_ControlPanel_cmd_send.Enabled = false;
                    label_ControlPanel_X_value.Text = "X: 0";
                    label_ControlPanel_Y_value.Text = "Y: 0";
                    label_ControlPanel_Z_value.Text = "Z: 0";
                    label_ControlPanel_X_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
                    label_ControlPanel_Y_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
                    label_ControlPanel_Z_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));

                    button_ControlPanel_SetHome.Enabled = false;
                    button_ControlPanel_updatePosition.Enabled = false;
                    button_ControlPanel_SetHome.BackColor = _LightGray;
                    button_ControlPanel_updatePosition.BackColor = _LightGray;

                    panel_XY_cursor.Visible = false;
                    panel_Z_cursor.Visible = false;

                    try
                    {
                        button_ControlPanel_laser.BackgroundImage.Dispose();
                    }
                    finally
                    {
                        button_ControlPanel_laser.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.laser_off);
                    }

                    pictureBox_Warning_LaserOn.Visible = false;

                    break;
                case Control_Panel_State.manual_control:
                    button_ControlPanel_DisConnect.Text = "Disconnect";
                    button_ControlPanel_DisConnect.Enabled = true;
                    button_ControlPanel_DisConnect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    button_ControlPanel_DisConnect.Size = new Size(102, 26);

                    comboBox_ControlPanel_COM_Ports.Enabled = false;
                    button_ControlPanel_update_COM_Ports.Enabled = false;
                    button_ControlPanel_update_COM_Ports.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
                    button_ControlPanel_Reset_CNC.Enabled = true;
                    button_ControlPanel_Reset_CNC.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(163)))), ((int)(((byte)(11)))));

                    button_ControlPanel_x_plus.Enabled = true;
                    button_ControlPanel_x_minus.Enabled = true;
                    button_ControlPanel_y_plus.Enabled = true;
                    button_ControlPanel_y_minus.Enabled = true;
                    button_ControlPanel_z_plus.Enabled = true;
                    button_ControlPanel_z_minus.Enabled = true;
                    button_ControlPanel_x_minus_y_plus.Enabled = true;
                    button_ControlPanel_x_minus_y_minus.Enabled = true;
                    button_ControlPanel_x_plus_y_plus.Enabled = true;
                    button_ControlPanel_x_plus_y_minus.Enabled = true;
                    button_ControlPanel_x_plus.BackColor = _LightBlue;
                    button_ControlPanel_x_minus.BackColor = _LightBlue;
                    button_ControlPanel_y_plus.BackColor = _LightBlue;
                    button_ControlPanel_y_minus.BackColor = _LightBlue;
                    button_ControlPanel_z_plus.BackColor = _DarkBlue;
                    button_ControlPanel_z_minus.BackColor = _DarkBlue;
                    button_ControlPanel_x_minus_y_plus.BackColor = _LightBlue;
                    button_ControlPanel_x_minus_y_minus.BackColor = _LightBlue;
                    button_ControlPanel_x_plus_y_plus.BackColor = _LightBlue;
                    button_ControlPanel_x_plus_y_minus.BackColor = _LightBlue;
                    try
                    {
                        pictureBox_control_button.Image.Dispose();
                    }
                    finally
                    {
                        pictureBox_control_button.Image = new Bitmap(CNC_Pathfinder.Properties.Resources.control_btn_pic);
                    }
                    button_ControlPanel_home_all.Enabled = true;
                    button_ControlPanel_home_x.Enabled = true;
                    button_ControlPanel_home_y.Enabled = true;
                    button_ControlPanel_home_z.Enabled = true;
                    button_ControlPanel_home_all.BackColor = _LightBlue;
                    button_ControlPanel_home_x.BackColor = _LightBlue;
                    button_ControlPanel_home_y.BackColor = _LightBlue;
                    button_ControlPanel_home_z.BackColor = _LightBlue;
                    button_ControlPanel_Stop.Enabled = true;
                    button_ControlPanel_Stop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    try
                    {
                        button_ControlPanel_Stop.BackgroundImage.Dispose();
                    }
                    finally
                    {
                        button_ControlPanel_Stop.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.emergency_stop_btn);
                    }
                    try
                    {
                        pictureBox_EmergencyStop.Image.Dispose();
                    }
                    finally
                    {
                        pictureBox_EmergencyStop.Image = new Bitmap(CNC_Pathfinder.Properties.Resources.emergency_stop_pic);
                    }
                    button_ControlPanel_Motor_OnOff.Enabled = true;
                    button_ControlPanel_Motor_OnOff.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));

                    trackBar_ControlPanel_distance.Enabled = true;
                    numericUpDown_ControlPanel_distance.Enabled = true;

                    trackBar_ControlPanel_feedrate.Enabled = true;
                    numericUpDown_ControlPanel_feedrate.Enabled = true;
                    trackBar_ControlPanel_laser.Enabled = true;
                    numericUpDown_ControlPanel_laser.Enabled = true;
                    //updateConfigurations();
                    button_ControlPanel_laser.Enabled = true;
                    button_ControlPanel_laser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    label_ControlPanel_distance.Enabled = true;
                    label_ControlPanel_feedrate.Enabled = true;
                    label_ControlPanel_laser.Enabled = true;

                    button_ControlPanel_Load_File.Enabled = true;
                    button_ControlPanel_Close_File.Enabled = false;
                    button_ControlPanel_Start_File.Enabled = false;
                    button_ControlPanel_Pause_File.Enabled = false;
                    button_ControlPanel_Cancel_File.Enabled = false;
                    button_ControlPanel_Resume_File.Enabled = false;
                    button_ControlPanel_Load_File.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    button_ControlPanel_Close_File.BackColor = _LightGray;
                    button_ControlPanel_Start_File.BackColor = _LightGray;
                    button_ControlPanel_Pause_File.BackColor = _LightGray;
                    button_ControlPanel_Cancel_File.BackColor = _LightGray;
                    button_ControlPanel_Resume_File.BackColor = _LightGray;
                    button_ControlPanel_Start_File.Visible = true;
                    button_ControlPanel_Resume_File.Visible = false;
                    
                    progressBar_ControlPanel_file_progress.Enabled = false;
                    progressBar_ControlPanel_file_progress.Value = 0;
                    label_ControlPanel_file_progress.Text = "0%";
                    label_ControlPanel_file_progress.Enabled = true;
                    File_Stopwatch_stop();
                    File_Stopwatch_resetCount();
                    label_ControlPanel_file_time.Enabled = true;
                    button_ControlPanel_cmd_send.Enabled = true;
                    button_ControlPanel_cmd_send.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    label_FILE.Enabled = true;
                    label_File_Name.Enabled = true;
                    label_File_Name.Text = "(none)";
                    toolTip_main_form_tt.SetToolTip(label_File_Name, "(none)");

                    //textBox_ControlPanel_cmd_monitor.Clear();
                    textBox_ControlPanel_cmd_monitor.Enabled = true;
                    //textBox_ControlPanel_cmd_send.Clear();
                    textBox_ControlPanel_cmd_send.Enabled = true;
                    //label_ControlPanel_X_value.Text = "X: 0";
                    //label_ControlPanel_Y_value.Text = "Y: 0";
                    //label_ControlPanel_Z_value.Text = "Z: 0";
                    label_ControlPanel_X_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    label_ControlPanel_Y_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    label_ControlPanel_Z_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));

                    button_ControlPanel_SetHome.Enabled = true;
                    button_ControlPanel_updatePosition.Enabled = true;
                    button_ControlPanel_SetHome.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    button_ControlPanel_updatePosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));

                    panel_XY_cursor.Visible = true;
                    panel_Z_cursor.Visible = true;

                    break;
                case Control_Panel_State.printing_play:

                    button_ControlPanel_DisConnect.Text = "Disconnect";
                    button_ControlPanel_DisConnect.Enabled = true;
                    button_ControlPanel_DisConnect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    button_ControlPanel_DisConnect.Size = new Size(102, 26);

                    comboBox_ControlPanel_COM_Ports.Enabled = false;
                    button_ControlPanel_update_COM_Ports.Enabled = false;
                    button_ControlPanel_update_COM_Ports.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
                    button_ControlPanel_Reset_CNC.Enabled = false;
                    button_ControlPanel_Reset_CNC.BackColor = _LightGray;

                    button_ControlPanel_x_plus.Enabled = true;
                    button_ControlPanel_x_minus.Enabled = true;
                    button_ControlPanel_y_plus.Enabled = true;
                    button_ControlPanel_y_minus.Enabled = true;
                    button_ControlPanel_z_plus.Enabled = true;
                    button_ControlPanel_z_minus.Enabled = true;
                    button_ControlPanel_x_minus_y_plus.Enabled = true;
                    button_ControlPanel_x_minus_y_minus.Enabled = true;
                    button_ControlPanel_x_plus_y_plus.Enabled = true;
                    button_ControlPanel_x_plus_y_minus.Enabled = true;
                    button_ControlPanel_x_plus.BackColor = _LightBlue;
                    button_ControlPanel_x_minus.BackColor = _LightBlue;
                    button_ControlPanel_y_plus.BackColor = _LightBlue;
                    button_ControlPanel_y_minus.BackColor = _LightBlue;
                    button_ControlPanel_z_plus.BackColor = _DarkBlue;
                    button_ControlPanel_z_minus.BackColor = _DarkBlue;
                    button_ControlPanel_x_minus_y_plus.BackColor = _LightBlue;
                    button_ControlPanel_x_minus_y_minus.BackColor = _LightBlue;
                    button_ControlPanel_x_plus_y_plus.BackColor = _LightBlue;
                    button_ControlPanel_x_plus_y_minus.BackColor = _LightBlue;
                    try
                    {
                        pictureBox_control_button.Image.Dispose();
                    }
                    finally
                    {
                        pictureBox_control_button.Image = new Bitmap(CNC_Pathfinder.Properties.Resources.control_btn_pic);
                    }
                    button_ControlPanel_home_all.Enabled = true;
                    button_ControlPanel_home_x.Enabled = true;
                    button_ControlPanel_home_y.Enabled = true;
                    button_ControlPanel_home_z.Enabled = true;
                    button_ControlPanel_home_all.BackColor = _LightBlue;
                    button_ControlPanel_home_x.BackColor = _LightBlue;
                    button_ControlPanel_home_y.BackColor = _LightBlue;
                    button_ControlPanel_home_z.BackColor = _LightBlue;
                    button_ControlPanel_Stop.Enabled = true;
                    button_ControlPanel_Stop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    try
                    {
                        button_ControlPanel_Stop.BackgroundImage.Dispose();
                    }
                    finally
                    {
                        button_ControlPanel_Stop.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.emergency_stop_btn);
                    }
                    try
                    {
                        pictureBox_EmergencyStop.Image.Dispose();
                    }
                    finally
                    {
                        pictureBox_EmergencyStop.Image = new Bitmap(CNC_Pathfinder.Properties.Resources.emergency_stop_pic);
                    }
                    button_ControlPanel_Motor_OnOff.Enabled = true;
                    button_ControlPanel_Motor_OnOff.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));

                    trackBar_ControlPanel_distance.Enabled = true;
                    numericUpDown_ControlPanel_distance.Enabled = true;
                    trackBar_ControlPanel_feedrate.Enabled = true;
                    numericUpDown_ControlPanel_feedrate.Enabled = true;
                    trackBar_ControlPanel_laser.Enabled = true;
                    numericUpDown_ControlPanel_laser.Enabled = true;
                    button_ControlPanel_laser.Enabled = true;
                    button_ControlPanel_laser.BackColor = _LightBlue;
                    label_ControlPanel_distance.Enabled = true;
                    label_ControlPanel_feedrate.Enabled = true;
                    label_ControlPanel_laser.Enabled = true;

                    button_ControlPanel_Load_File.Enabled = false;
                    button_ControlPanel_Close_File.Enabled = true;
                    button_ControlPanel_Start_File.Enabled = true;
                    button_ControlPanel_Pause_File.Enabled = false;
                    button_ControlPanel_Cancel_File.Enabled = false;
                    button_ControlPanel_Resume_File.Enabled = false;
                    button_ControlPanel_Load_File.BackColor = _LightGray;
                    button_ControlPanel_Close_File.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    button_ControlPanel_Start_File.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(163)))), ((int)(((byte)(11)))));
                    button_ControlPanel_Pause_File.BackColor = _LightGray;
                    button_ControlPanel_Cancel_File.BackColor = _LightGray;
                    button_ControlPanel_Resume_File.BackColor = _LightGray;
                    button_ControlPanel_Start_File.Visible = true;
                    button_ControlPanel_Resume_File.Visible = false;

                    progressBar_ControlPanel_file_progress.Enabled = false;
                    //progressBar_ControlPanel_file_progress.Value = 0;
                    //label_ControlPanel_file_progress.Text = "0%";
                    label_ControlPanel_file_progress.Enabled = true;
                    //label_ControlPanel_file_time.Text = "00:00";
                    label_ControlPanel_file_time.Enabled = true;
                    button_ControlPanel_cmd_send.Enabled = true;
                    button_ControlPanel_cmd_send.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    label_FILE.Enabled = true;
                    label_File_Name.Enabled = true;

                    //textBox_ControlPanel_cmd_monitor.Clear();
                    textBox_ControlPanel_cmd_monitor.Enabled = true;
                    //textBox_ControlPanel_cmd_send.Clear();
                    textBox_ControlPanel_cmd_send.Enabled = true;
                    //label_ControlPanel_X_value.Text = "X: 0";
                    //label_ControlPanel_Y_value.Text = "Y: 0";
                    //label_ControlPanel_Z_value.Text = "Z: 0";
                    label_ControlPanel_X_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    label_ControlPanel_Y_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    label_ControlPanel_Z_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));

                    button_ControlPanel_SetHome.Enabled = true;
                    button_ControlPanel_updatePosition.Enabled = true;
                    button_ControlPanel_SetHome.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    button_ControlPanel_updatePosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));

                    panel_XY_cursor.Visible = true;
                    panel_Z_cursor.Visible = true;

                    break;
                case Control_Panel_State.priting_pause:

                    button_ControlPanel_DisConnect.Text = "Disconnect";
                    button_ControlPanel_DisConnect.Enabled = false;
                    button_ControlPanel_DisConnect.BackColor = _LightGray;
                    button_ControlPanel_DisConnect.Size = new Size(102, 26);

                    comboBox_ControlPanel_COM_Ports.Enabled = false;
                    button_ControlPanel_update_COM_Ports.Enabled = false;
                    button_ControlPanel_update_COM_Ports.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
                    button_ControlPanel_Reset_CNC.Enabled = false;
                    button_ControlPanel_Reset_CNC.BackColor = _LightGray;

                    button_ControlPanel_x_plus.Enabled = false;
                    button_ControlPanel_x_minus.Enabled = false;
                    button_ControlPanel_y_plus.Enabled = false;
                    button_ControlPanel_y_minus.Enabled = false;
                    button_ControlPanel_z_plus.Enabled = false;
                    button_ControlPanel_z_minus.Enabled = false;
                    button_ControlPanel_x_minus_y_plus.Enabled = false;
                    button_ControlPanel_x_minus_y_minus.Enabled = false;
                    button_ControlPanel_x_plus_y_plus.Enabled = false;
                    button_ControlPanel_x_plus_y_minus.Enabled = false;
                    button_ControlPanel_x_plus.BackColor = _LightGray;
                    button_ControlPanel_x_minus.BackColor = _LightGray;
                    button_ControlPanel_y_plus.BackColor = _LightGray;
                    button_ControlPanel_y_minus.BackColor = _LightGray;
                    button_ControlPanel_z_plus.BackColor = _DarkGray;
                    button_ControlPanel_z_minus.BackColor = _DarkGray;
                    button_ControlPanel_x_minus_y_plus.BackColor = _LightGray;
                    button_ControlPanel_x_minus_y_minus.BackColor = _LightGray;
                    button_ControlPanel_x_plus_y_plus.BackColor = _LightGray;
                    button_ControlPanel_x_plus_y_minus.BackColor = _LightGray;
                    try
                    {
                        pictureBox_control_button.Image.Dispose();
                    }
                    finally
                    {
                        pictureBox_control_button.Image = new Bitmap(CNC_Pathfinder.Properties.Resources.control_btn_pic_gray);
                    }
                    button_ControlPanel_home_all.Enabled = false;
                    button_ControlPanel_home_x.Enabled = false;
                    button_ControlPanel_home_y.Enabled = false;
                    button_ControlPanel_home_z.Enabled = false;
                    button_ControlPanel_home_all.BackColor = _LightGray;
                    button_ControlPanel_home_x.BackColor = _LightGray;
                    button_ControlPanel_home_y.BackColor = _LightGray;
                    button_ControlPanel_home_z.BackColor = _LightGray;
                    button_ControlPanel_Stop.Enabled = true;
                    button_ControlPanel_Stop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    try
                    {
                        button_ControlPanel_Stop.BackgroundImage.Dispose();
                    }
                    finally
                    {
                        button_ControlPanel_Stop.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.emergency_stop_btn);
                    }
                    try
                    {
                        pictureBox_EmergencyStop.Image.Dispose();
                    }
                    finally
                    {
                        pictureBox_EmergencyStop.Image = new Bitmap(CNC_Pathfinder.Properties.Resources.emergency_stop_pic);
                    }
                    button_ControlPanel_Motor_OnOff.Enabled = false;
                    button_ControlPanel_Motor_OnOff.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));

                    trackBar_ControlPanel_distance.Enabled = false;
                    numericUpDown_ControlPanel_distance.Enabled = false;
                    trackBar_ControlPanel_feedrate.Enabled = false;
                    numericUpDown_ControlPanel_feedrate.Enabled = false;
                    trackBar_ControlPanel_laser.Enabled = false;
                    numericUpDown_ControlPanel_laser.Enabled = false;
                    button_ControlPanel_laser.Enabled = false;
                    button_ControlPanel_laser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
                    label_ControlPanel_distance.Enabled = true;
                    label_ControlPanel_feedrate.Enabled = true;
                    label_ControlPanel_laser.Enabled = true;

                    button_ControlPanel_Load_File.Enabled = false;
                    button_ControlPanel_Close_File.Enabled = false;
                    button_ControlPanel_Start_File.Enabled = false;
                    button_ControlPanel_Pause_File.Enabled = true;
                    button_ControlPanel_Cancel_File.Enabled = true;
                    button_ControlPanel_Resume_File.Enabled = false;
                    button_ControlPanel_Load_File.BackColor = _LightGray;
                    button_ControlPanel_Close_File.BackColor = _LightGray;
                    button_ControlPanel_Start_File.BackColor = _LightGray;
                    button_ControlPanel_Pause_File.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(155)))), ((int)(((byte)(5)))));
                    button_ControlPanel_Cancel_File.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    button_ControlPanel_Resume_File.BackColor = _LightGray;
                    button_ControlPanel_Start_File.Visible = false;
                    button_ControlPanel_Resume_File.Visible = true;

                    progressBar_ControlPanel_file_progress.Enabled = true;
                    //progressBar_ControlPanel_file_progress.Value = 0;
                    //label_ControlPanel_file_progress.Text = "0%";
                    label_ControlPanel_file_progress.Enabled = true;
                    //label_ControlPanel_file_time.Text = "00:00";
                    label_ControlPanel_file_time.Enabled = true;
                    button_ControlPanel_cmd_send.Enabled = false;
                    button_ControlPanel_cmd_send.BackColor = _LightGray;
                    label_FILE.Enabled = true;
                    label_File_Name.Enabled = true;

                    //textBox_ControlPanel_cmd_monitor.Clear();
                    textBox_ControlPanel_cmd_monitor.Enabled = true;
                    //textBox_ControlPanel_cmd_send.Clear();
                    textBox_ControlPanel_cmd_send.Enabled = false;
                    //label_ControlPanel_X_value.Text = "X: 0";
                    //label_ControlPanel_Y_value.Text = "Y: 0";
                    //label_ControlPanel_Z_value.Text = "Z: 0";
                    label_ControlPanel_X_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    label_ControlPanel_Y_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    label_ControlPanel_Z_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));

                    button_ControlPanel_SetHome.Enabled = false;
                    button_ControlPanel_updatePosition.Enabled = false;
                    button_ControlPanel_SetHome.BackColor = _LightGray;
                    button_ControlPanel_updatePosition.BackColor = _LightGray;

                    panel_XY_cursor.Visible = true;
                    panel_Z_cursor.Visible = true;

                    break;
                case Control_Panel_State.printing_resume:

                    button_ControlPanel_DisConnect.Text = "Disconnect";
                    button_ControlPanel_DisConnect.Enabled = false;
                    button_ControlPanel_DisConnect.BackColor = _LightGray;
                    button_ControlPanel_DisConnect.Size = new Size(102, 26);

                    comboBox_ControlPanel_COM_Ports.Enabled = false;
                    button_ControlPanel_update_COM_Ports.Enabled = false;
                    button_ControlPanel_update_COM_Ports.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
                    button_ControlPanel_Reset_CNC.Enabled = false;
                    button_ControlPanel_Reset_CNC.BackColor = _LightGray;

                    button_ControlPanel_x_plus.Enabled = false;
                    button_ControlPanel_x_minus.Enabled = false;
                    button_ControlPanel_y_plus.Enabled = false;
                    button_ControlPanel_y_minus.Enabled = false;
                    button_ControlPanel_z_plus.Enabled = false;
                    button_ControlPanel_z_minus.Enabled = false;
                    button_ControlPanel_x_minus_y_plus.Enabled = false;
                    button_ControlPanel_x_minus_y_minus.Enabled = false;
                    button_ControlPanel_x_plus_y_plus.Enabled = false;
                    button_ControlPanel_x_plus_y_minus.Enabled = false;
                    button_ControlPanel_x_plus.BackColor = _LightGray;
                    button_ControlPanel_x_minus.BackColor = _LightGray;
                    button_ControlPanel_y_plus.BackColor = _LightGray;
                    button_ControlPanel_y_minus.BackColor = _LightGray;
                    button_ControlPanel_z_plus.BackColor = _DarkGray;
                    button_ControlPanel_z_minus.BackColor = _DarkGray;
                    button_ControlPanel_x_minus_y_plus.BackColor = _LightGray;
                    button_ControlPanel_x_minus_y_minus.BackColor = _LightGray;
                    button_ControlPanel_x_plus_y_plus.BackColor = _LightGray;
                    button_ControlPanel_x_plus_y_minus.BackColor = _LightGray;
                    try
                    {
                        pictureBox_control_button.Image.Dispose();
                    }
                    finally
                    {
                        pictureBox_control_button.Image = new Bitmap(CNC_Pathfinder.Properties.Resources.control_btn_pic_gray);
                    }
                    button_ControlPanel_home_all.Enabled = false;
                    button_ControlPanel_home_x.Enabled = false;
                    button_ControlPanel_home_y.Enabled = false;
                    button_ControlPanel_home_z.Enabled = false;
                    button_ControlPanel_home_all.BackColor = _LightGray;
                    button_ControlPanel_home_x.BackColor = _LightGray;
                    button_ControlPanel_home_y.BackColor = _LightGray;
                    button_ControlPanel_home_z.BackColor = _LightGray;
                    button_ControlPanel_Stop.Enabled = true;
                    button_ControlPanel_Stop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    try
                    {
                        button_ControlPanel_Stop.BackgroundImage.Dispose();
                    }
                    finally
                    {
                        button_ControlPanel_Stop.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.emergency_stop_btn);
                    }
                    try
                    {
                        pictureBox_EmergencyStop.Image.Dispose();
                    }
                    finally
                    {
                        pictureBox_EmergencyStop.Image = new Bitmap(CNC_Pathfinder.Properties.Resources.emergency_stop_pic);
                    }
                    button_ControlPanel_Motor_OnOff.Enabled = false;
                    button_ControlPanel_Motor_OnOff.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));

                    trackBar_ControlPanel_distance.Enabled = false;
                    numericUpDown_ControlPanel_distance.Enabled = false;
                    trackBar_ControlPanel_feedrate.Enabled = false;
                    numericUpDown_ControlPanel_feedrate.Enabled = false;
                    trackBar_ControlPanel_laser.Enabled = false;
                    numericUpDown_ControlPanel_laser.Enabled = false;
                    button_ControlPanel_laser.Enabled = false;
                    button_ControlPanel_laser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
                    label_ControlPanel_distance.Enabled = true;
                    label_ControlPanel_feedrate.Enabled = true;
                    label_ControlPanel_laser.Enabled = true;

                    button_ControlPanel_Load_File.Enabled = false;
                    button_ControlPanel_Close_File.Enabled = true;
                    button_ControlPanel_Start_File.Enabled = false;
                    button_ControlPanel_Pause_File.Enabled = false;
                    button_ControlPanel_Cancel_File.Enabled = true;
                    button_ControlPanel_Resume_File.Enabled = true;
                    button_ControlPanel_Load_File.BackColor = _LightGray;
                    button_ControlPanel_Close_File.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    button_ControlPanel_Start_File.BackColor = _LightGray;
                    button_ControlPanel_Pause_File.BackColor = _LightGray;
                    button_ControlPanel_Cancel_File.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    button_ControlPanel_Resume_File.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(163)))), ((int)(((byte)(11)))));
                    button_ControlPanel_Start_File.Visible = false;
                    button_ControlPanel_Resume_File.Visible = true;

                    progressBar_ControlPanel_file_progress.Enabled = true;
                    //progressBar_ControlPanel_file_progress.Value = 0;
                    //label_ControlPanel_file_progress.Text = "0%";
                    label_ControlPanel_file_progress.Enabled = true;
                    //label_ControlPanel_file_time.Text = "00:00";
                    label_ControlPanel_file_time.Enabled = true;
                    button_ControlPanel_cmd_send.Enabled = false;
                    button_ControlPanel_cmd_send.BackColor = _LightGray;
                    label_FILE.Enabled = true;
                    label_File_Name.Enabled = true;

                    //textBox_ControlPanel_cmd_monitor.Clear();
                    textBox_ControlPanel_cmd_monitor.Enabled = true;
                    //textBox_ControlPanel_cmd_send.Clear();
                    textBox_ControlPanel_cmd_send.Enabled = false;
                    //label_ControlPanel_X_value.Text = "X: 0";
                    //label_ControlPanel_Y_value.Text = "Y: 0";
                    //label_ControlPanel_Z_value.Text = "Z: 0";
                    label_ControlPanel_X_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    label_ControlPanel_Y_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
                    label_ControlPanel_Z_value.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));

                    button_ControlPanel_SetHome.Enabled = false;
                    button_ControlPanel_updatePosition.Enabled = false;
                    button_ControlPanel_SetHome.BackColor = _LightGray;
                    button_ControlPanel_updatePosition.BackColor = _LightGray;

                    panel_XY_cursor.Visible = true;
                    panel_Z_cursor.Visible = true;

                    break;
            }
            
        }

        // Serial COM TextBox control

        private void button_ControlPanel_cmd_send_Click(object sender, EventArgs e)
        {
            // send cmd
            if (Machine.Connection_Status)
            {
                Machine.sendCMD(textBox_ControlPanel_cmd_send.Text); 
            }
            else
            {
                Machine.Disconnect();
            }
            // clear textBox_ControlPanel_cmd_send txtbox
            textBox_ControlPanel_cmd_send.Clear();
            textBox_ControlPanel_cmd_send.Focus();
        }

        private delegate void textBox_cmd_monitor_Callback(string text);

        private void textBox_cmd_monitor_write(string text)
        {
            if(text == null || text.Length == 0)
            {
                return;
            }

            if (this.textBox_ControlPanel_cmd_monitor.InvokeRequired)
            {
                //textBox_cmd_monitor_Callback d = new textBox_cmd_monitor_Callback(textBox_cmd_monitor_write);
                //this.textBox_ControlPanel_cmd_monitor.Invoke(d, new object[] { text });
                //Application.DoEvents();
                textBox_ControlPanel_cmd_monitor.Invoke((ThreadStart)(() => textBox_cmd_monitor_write(text)));
                return;
            }

            if(text.Length > 2 && (text[text.Length - 1] == '\n'))
            {
                if(text[text.Length - 2] == '\r')
                {
                    textBox_ControlPanel_cmd_monitor.AppendText(text);
                }
                else
                {

                    text = text.Replace('\n', '\r');
                    text += '\n';
                    textBox_ControlPanel_cmd_monitor.AppendText(text);
                }
            }
            else if(text.Length > 1)
            {
                if(text[text.Length - 1] == '\r')
                {
                    textBox_ControlPanel_cmd_monitor.AppendText(text + '\n');
                }
                else
                {
                    textBox_ControlPanel_cmd_monitor.AppendText(text + "\r\n");
                }
                
            }
            else
            {
                textBox_ControlPanel_cmd_monitor.AppendText(text + "\r\n");
            }

            Application.DoEvents();
        }

        private void onMsgReceived(object sender, EventArgs e)
        {
            string msg = (e as COM_Port.MessageEventArgs).msg;
            if (msg == null)
            {
                return;
            }
            textBox_cmd_monitor_write(msg);
        }

        private void onMsgSent(object sender, EventArgs e)
        {
            string msg = (e as COM_Port.MessageEventArgs).msg;
            if (msg == null)
            {
                return;
            }
            
            textBox_cmd_monitor_write(msg);
        }

        // CNC update interfaces


        private delegate void updateCurrentPositionChange_callbck(double x, double y, double z);

        private void updateCurrentPositionChange(double x, double y, double z)
        {
            if (label_ControlPanel_X_value.InvokeRequired)
            {
                updateCurrentPositionChange_callbck d = new updateCurrentPositionChange_callbck(updateCurrentPositionChange);
                this.label_ControlPanel_X_value.Invoke(d, new object[] { x, y, z });
                Application.DoEvents();
                return;
            }

            // limit the number of digits to be displayed
            string _x = x.ToString(nfi).Length < 8 ? x.ToString(nfi) : x.ToString(nfi).Substring(0, 8);
            string _y = y.ToString(nfi).Length < 8 ? y.ToString(nfi) : y.ToString(nfi).Substring(0, 8);
            string _z = z.ToString(nfi).Length < 8 ? z.ToString(nfi) : z.ToString(nfi).Substring(0, 8);

            // update labels
            label_ControlPanel_X_value.Text = "X: " + _x;
            label_ControlPanel_Y_value.Text = "Y: " + _y;
            label_ControlPanel_Z_value.Text = "Z: " + _z;

            // update cursors
            int location_x = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Convert.ToInt32(Math.Round(x * Machine.Configurations.StepResolution.X)) : Convert.ToInt32(Math.Round(x * CNC_Properties.inch_to_mm * Machine.Configurations.StepResolution.X));
            int location_y = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Convert.ToInt32(Math.Round(y * Machine.Configurations.StepResolution.Y)) : Convert.ToInt32(Math.Round(y * CNC_Properties.inch_to_mm * Machine.Configurations.StepResolution.Y));
            int location_z = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Convert.ToInt32(Math.Round(z * Machine.Configurations.StepResolution.Z)) : Convert.ToInt32(Math.Round(z * CNC_Properties.inch_to_mm * Machine.Configurations.StepResolution.Z));

            switch (Machine.Configurations._Home_Position)
            {
                case CNC_Properties.Home_Position.BottomLeft:
                    location_z += Machine.Configurations.Axes_Step_Dimension.Z / 2;
                    break;
                case CNC_Properties.Home_Position.BottomRight:
                    location_x += Machine.Configurations.Axes_Step_Dimension.X;
                    location_z += Machine.Configurations.Axes_Step_Dimension.Z / 2;
                    break;
                case CNC_Properties.Home_Position.Center:
                    location_x += Machine.Configurations.Axes_Step_Dimension.X / 2;
                    location_y += Machine.Configurations.Axes_Step_Dimension.Y / 2;
                    location_z += Machine.Configurations.Axes_Step_Dimension.Z / 2;
                    break;
                case CNC_Properties.Home_Position.TopLeft:
                    location_y += Machine.Configurations.Axes_Step_Dimension.Y;
                    location_z += Machine.Configurations.Axes_Step_Dimension.Z / 2;
                    break;
                case CNC_Properties.Home_Position.TopRight:
                    location_x += Machine.Configurations.Axes_Step_Dimension.X;
                    location_y += Machine.Configurations.Axes_Step_Dimension.Y;
                    location_z += Machine.Configurations.Axes_Step_Dimension.Z / 2;
                    break;
                case CNC_Properties.Home_Position.Costume:
                    location_x += Machine.Configurations.Costume_Home_Position.X;
                    location_y += Machine.Configurations.Costume_Home_Position.Y;
                    location_z += Machine.Configurations.Costume_Home_Position.Z;
                    break;
            }

            double relative_x = (double)location_x / Machine.Configurations.Axes_Step_Dimension.X;
            double relative_y = 1 - (double)location_y / Machine.Configurations.Axes_Step_Dimension.Y;
            double relative_z = 1 - (double)location_z / Machine.Configurations.Axes_Step_Dimension.Z;

            int new_x = Convert.ToInt32(Math.Round((panel_XY_plane.Size.Width - 2) * relative_x - 4));
            int new_y = Convert.ToInt32(Math.Round((panel_XY_plane.Size.Height - 2) * relative_y - 4));
            int new_z = Convert.ToInt32(Math.Round((panel_Z_plane.Size.Height-8) * relative_z));

            if(new_x > panel_XY_plane.Size.Width)
            {
                new_x = panel_XY_plane.Size.Width;
            }
            else if (new_x < - 4)
            {
                new_x = - 4;
            }

            if (new_y > panel_XY_plane.Size.Height)
            {
                new_y = panel_XY_plane.Size.Height;
            }
            else if (new_y < -4)
            {
                new_y =  -4;
            }

            if (new_z + 8 > panel_Z_plane.Size.Height)
            {
                new_z = panel_Z_plane.Size.Height - 8;
            }
            else if (new_z < 0)
            {
                new_z = 0;
            }

            panel_XY_cursor.Location = new Point(new_x, new_y);
            panel_Z_cursor.Location = new Point(panel_Z_cursor.Location.X, new_z);
        }

        private delegate void updateConfigurations_callbck();

        private void updateConfigurations()
        {
            if (numericUpDown_ControlPanel_feedrate.InvokeRequired)
            {
                updateConfigurations_callbck d = new updateConfigurations_callbck(updateConfigurations);
                this.numericUpDown_ControlPanel_feedrate.Invoke(d, new object[] {  });
                Application.DoEvents();
                return;
            }
            
            // MOTORS
            if (Machine.Configurations.motorsActive)
            {
                if(ControlPanelState == Control_Panel_State.priting_pause || ControlPanelState == Control_Panel_State.printing_resume || ControlPanelState == Control_Panel_State.disconnect)
                {
                    trackBar_ControlPanel_feedrate.Enabled = false;
                    numericUpDown_ControlPanel_feedrate.Enabled = false;
                }
                else
                {
                    trackBar_ControlPanel_feedrate.Enabled = true;
                    numericUpDown_ControlPanel_feedrate.Enabled = true;
                }


                try
                {
                    button_ControlPanel_Motor_OnOff.BackgroundImage.Dispose();
                }
                finally
                {
                    button_ControlPanel_Motor_OnOff.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.motor_on);
                }
            }
            else
            {
                //trackBar_ControlPanel_feedrate.Enabled = false;
                //numericUpDown_ControlPanel_feedrate.Enabled = false;
                try
                {
                    button_ControlPanel_Motor_OnOff.BackgroundImage.Dispose();
                }
                finally
                {
                    button_ControlPanel_Motor_OnOff.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.motor_off);
                }

            }

            Decimal fr = Convert.ToDecimal(Machine.Configurations.Current_Feedrate);
            if(fr > numericUpDown_ControlPanel_feedrate.Maximum)
            {
                fr = numericUpDown_ControlPanel_feedrate.Maximum;
            }
            else if(fr < numericUpDown_ControlPanel_feedrate.Minimum)
            {
                fr = numericUpDown_ControlPanel_feedrate.Minimum;
            }
            numericUpDown_ControlPanel_feedrate.Value = fr;

            // LASER
            if (Machine.Configurations.toolActive)
            {

                if (ControlPanelState == Control_Panel_State.priting_pause || ControlPanelState == Control_Panel_State.printing_resume || ControlPanelState == Control_Panel_State.disconnect)
                {
                    trackBar_ControlPanel_laser.Enabled = false;
                    numericUpDown_ControlPanel_laser.Enabled = false;
                }
                else
                {
                    trackBar_ControlPanel_laser.Enabled = true;
                    numericUpDown_ControlPanel_laser.Enabled = true;
                }


                try
                {
                    button_ControlPanel_laser.BackgroundImage.Dispose();
                }
                finally
                {
                    button_ControlPanel_laser.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.laser_on);
                }

                pictureBox_Warning_LaserOn.Visible = true;
            }
            else
            {
                //trackBar_ControlPanel_laser.Enabled = false;
                //numericUpDown_ControlPanel_laser.Enabled = false;

                try
                {
                    button_ControlPanel_laser.BackgroundImage.Dispose();
                }
                finally
                {
                    button_ControlPanel_laser.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.laser_off);
                }

                pictureBox_Warning_LaserOn.Visible = false;
            }

            Decimal lp = Convert.ToDecimal(Machine.Configurations.Current_LaserPower);
            if(lp > numericUpDown_ControlPanel_laser.Maximum)
            {
                lp = numericUpDown_ControlPanel_laser.Maximum;
            }
            else if(lp < numericUpDown_ControlPanel_laser.Minimum)
            {
                lp = numericUpDown_ControlPanel_laser.Minimum;
            }

            numericUpDown_ControlPanel_laser.Value = lp;

            // UNITS
            if (Machine.Configurations._Feedrate_Mode == CNC_Properties.Feedrate_Mode.UnitsPerMinute)
            {
                if (Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric)
                {
                    label_ControlPanel_distance.Text = "Distance [mm]:";
                    label_ControlPanel_feedrate.Text = "Feedrate [mm/min]";
                }
                else
                {
                    label_ControlPanel_distance.Text = "Distance [inch]:";
                    label_ControlPanel_feedrate.Text = "Feedrate [inch/min]";
                }
            }
            else
            {
                label_ControlPanel_feedrate.Text = "Feedrate [RPM]";
            }

            label_ConfigXDim.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Dimension [mm]:" : "Dimension [inch]:";
            label_ConfigYDim.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Dimension [mm]:" : "Dimension [inch]:";
            label_ConfigZDim.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Dimension [mm]:" : "Dimension [inch]:";

            label_ConfigXRes.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Resolution [step/mm]:" : "Resolution [step/inch]:";
            label_ConfigYRes.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Resolution [step/mm]:" : "Resolution [step/inch]:";
            label_ConfigZRes.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Resolution [step/mm]:" : "Resolution [step/inch]:";

            label_ConfigMaxFeed.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Max. Feedrate [mm/min]:" : "Max. Feedrate [inch/min]:";
            label_ConfigMinFeed.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Min. Feedrate [mm/min]:" : "Min. Feedrate [inch/min]:";

            comboBox_Config_Units.SelectedItem = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Metric" : "Imperial";

        }

        private delegate void update_COMPort_List_callbck();

        private void update_COMPort_List()
        {
            if (comboBox_ControlPanel_COM_Ports.InvokeRequired)
            {
                update_COMPort_List_callbck d = new update_COMPort_List_callbck(update_COMPort_List);
                this.comboBox_ControlPanel_COM_Ports.Invoke(d, new object[] { });
                Application.DoEvents();
                return;
            }

            string[] com_list = Machine.checkSerialPortsAvailable();
            if (com_list.Length == 0)
            {
                comboBox_ControlPanel_COM_Ports.SelectedItem = null;
                comboBox_ControlPanel_COM_Ports.DataSource = null;
            }
            else
            {
                comboBox_ControlPanel_COM_Ports.DataSource = Machine.checkSerialPortsAvailable();
                comboBox_ControlPanel_COM_Ports.SelectedIndex = 0;
            }
        }

        // Manual Control Buttons & Connection

        private void button_ControlPanel_DisConnect_Click(object sender, EventArgs e)
        {
            if(ControlPanelState == Control_Panel_State.disconnect) // Conect pressed
            {
                if (comboBox_ControlPanel_COM_Ports.SelectedItem == null)
                {
                    SystemSounds.Asterisk.Play();
                    return;
                }

                Thread.Sleep(500);

                if (Machine.Connect((string)comboBox_ControlPanel_COM_Ports.SelectedItem, (int)comboBox_Configuration_baudrate.SelectedItem, lineTerminators[comboBox_Configuration_LineTerminator.SelectedIndex]))
                {
                    
                    // send configurations to cnc
                    Machine.sendConfigurations();
                    // update feedrate and laser trackbars and numerics
                    trackBar_ControlPanel_feedrate.Maximum = Convert.ToInt32(Machine.Configurations.MaxFeedrate);
                    trackBar_ControlPanel_feedrate.Minimum = Convert.ToInt32(Machine.Configurations.MinFeedrate) + 1;
                    numericUpDown_ControlPanel_feedrate.Maximum = Convert.ToInt32(Machine.Configurations.MaxFeedrate);
                    numericUpDown_ControlPanel_feedrate.Minimum = Convert.ToInt32(Machine.Configurations.MinFeedrate) + 1;

                    trackBar_ControlPanel_laser.Maximum = Convert.ToInt32(Machine.Configurations.laser_maxpower);
                    trackBar_ControlPanel_laser.Minimum = 0;
                    numericUpDown_ControlPanel_laser.Maximum = Convert.ToInt32(Machine.Configurations.laser_maxpower);
                    numericUpDown_ControlPanel_laser.Minimum = 0;
                    // connected state
                    setControlPanel(Control_Panel_State.manual_control);

                }
            }
            else // disconnect pressed
            {
                //if (Machine.isBusy())
                //{
                //    Machine.Cancel();
                //}
                
                Machine.Disconnect();
            }
        }

        private void onCNCDisconnect(object sender, EventArgs e)
        {
            if (Machine.isBusy())
            {
                Machine.Cancel();
            }
            if (ControlPanelState != Control_Panel_State.disconnect) // if not disconnected
            {
                setControlPanel(Control_Panel_State.disconnect);
            }

            update_COMPort_List();
        }

        private void button_ControlPanel_update_COM_Ports_Click(object sender, EventArgs e)
        {
            update_COMPort_List();
        }

        private void button_ControlPanel_updatePosition_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                Machine.GetCurrentPositio();
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_SetHome_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                Machine.SetHome();
                if(ControlPanelState == Control_Panel_State.printing_play)
                {
                    Machine.simulateFile(File, error_log_file_path);
                }
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_home_all_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                Machine.Home();
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_home_x_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                Machine.Home(true,false,false);
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_home_y_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                Machine.Home(false, true, false);
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_home_z_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                Machine.Home(false, false, true);
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_x_minus_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                double move_value = Convert.ToDouble(numericUpDown_ControlPanel_distance.Value);
                /*
                if (checkBox_ControlPanel_step_001.Checked)
                {
                    move_value = 0.01;
                }
                else if (checkBox_ControlPanel_step_01.Checked)
                {
                    move_value = 0.1;
                }
                else if (checkBox_ControlPanel_step_1.Checked)
                {
                    move_value = 1;
                }
                else if (checkBox_ControlPanel_step_10.Checked)
                {
                    move_value = 10;
                }
                */
                if (Machine.Configurations.toolActive)
                {
                    Machine.Move(-move_value, 0,0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value), Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
                }
                else
                {
                    Machine.Move(-move_value, 0, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value));
                }
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_x_plus_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                double move_value = Convert.ToDouble(numericUpDown_ControlPanel_distance.Value);

                /*
                if (checkBox_ControlPanel_step_001.Checked)
                {
                    move_value = 0.01;
                }
                else if (checkBox_ControlPanel_step_01.Checked)
                {
                    move_value = 0.1;
                }
                else if (checkBox_ControlPanel_step_1.Checked)
                {
                    move_value = 1;
                }
                else if (checkBox_ControlPanel_step_10.Checked)
                {
                    move_value = 10;
                }*/

                if (Machine.Configurations.toolActive)
                {
                    Machine.Move(move_value, 0, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value), Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
                }
                else
                {
                    Machine.Move(move_value, 0, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value));
                }
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_y_minus_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                double move_value = Convert.ToDouble(numericUpDown_ControlPanel_distance.Value);

                /*
                if (checkBox_ControlPanel_step_001.Checked)
                {
                    move_value = 0.01;
                }
                else if (checkBox_ControlPanel_step_01.Checked)
                {
                    move_value = 0.1;
                }
                else if (checkBox_ControlPanel_step_1.Checked)
                {
                    move_value = 1;
                }
                else if (checkBox_ControlPanel_step_10.Checked)
                {
                    move_value = 10;
                }*/

                if (Machine.Configurations.toolActive)
                {
                    Machine.Move(0, -move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value), Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
                }
                else
                {
                    Machine.Move(0, -move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value));
                }
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_y_plus_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                double move_value = Convert.ToDouble(numericUpDown_ControlPanel_distance.Value);

                /*
                if (checkBox_ControlPanel_step_001.Checked)
                {
                    move_value = 0.01;
                }
                else if (checkBox_ControlPanel_step_01.Checked)
                {
                    move_value = 0.1;
                }
                else if (checkBox_ControlPanel_step_1.Checked)
                {
                    move_value = 1;
                }
                else if (checkBox_ControlPanel_step_10.Checked)
                {
                    move_value = 10;
                }*/

                if (Machine.Configurations.toolActive)
                {
                    Machine.Move(0, move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value), Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
                }
                else
                {
                    Machine.Move(0, move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value));
                }
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_z_minus_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                double move_value = Convert.ToDouble(numericUpDown_ControlPanel_distance.Value);

                /*
                if (checkBox_ControlPanel_step_001.Checked)
                {
                    move_value = 0.01;
                }
                else if (checkBox_ControlPanel_step_01.Checked)
                {
                    move_value = 0.1;
                }
                else if (checkBox_ControlPanel_step_1.Checked)
                {
                    move_value = 1;
                }
                else if (checkBox_ControlPanel_step_10.Checked)
                {
                    move_value = 10;
                }
                */

                if (Machine.Configurations.toolActive)
                {
                    Machine.Move(0, 0, -move_value, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value), Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
                }
                else
                {
                    Machine.Move(0, 0, -move_value, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value));
                }
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_z_plus_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                double move_value = Convert.ToDouble(numericUpDown_ControlPanel_distance.Value);

                /*
                if (checkBox_ControlPanel_step_001.Checked)
                {
                    move_value = 0.01;
                }
                else if (checkBox_ControlPanel_step_01.Checked)
                {
                    move_value = 0.1;
                }
                else if (checkBox_ControlPanel_step_1.Checked)
                {
                    move_value = 1;
                }
                else if (checkBox_ControlPanel_step_10.Checked)
                {
                    move_value = 10;
                } */

                if (Machine.Configurations.toolActive)
                {
                    Machine.Move(0, 0, move_value, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value), Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
                }
                else
                {
                    Machine.Move(0, 0, move_value, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value));
                }
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_x_minus_y_minus_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                double move_value = Convert.ToDouble(numericUpDown_ControlPanel_distance.Value);

                /*
                if (checkBox_ControlPanel_step_001.Checked)
                {
                    move_value = 0.01;
                }
                else if (checkBox_ControlPanel_step_01.Checked)
                {
                    move_value = 0.1;
                }
                else if (checkBox_ControlPanel_step_1.Checked)
                {
                    move_value = 1;
                }
                else if (checkBox_ControlPanel_step_10.Checked)
                {
                    move_value = 10;
                } */

                if (Machine.Configurations.toolActive)
                {
                    Machine.Move(-move_value, -move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value), Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
                }
                else
                {
                    Machine.Move(-move_value, -move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value));
                }
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_x_minus_y_plus_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                double move_value = Convert.ToDouble(numericUpDown_ControlPanel_distance.Value);

                /*
                if (checkBox_ControlPanel_step_001.Checked)
                {
                    move_value = 0.01;
                }
                else if (checkBox_ControlPanel_step_01.Checked)
                {
                    move_value = 0.1;
                }
                else if (checkBox_ControlPanel_step_1.Checked)
                {
                    move_value = 1;
                }
                else if (checkBox_ControlPanel_step_10.Checked)
                {
                    move_value = 10;
                }
                */
                if (Machine.Configurations.toolActive)
                {
                    Machine.Move(-move_value, move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value), Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
                }
                else
                {
                    Machine.Move(-move_value, move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value));
                }
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_x_plus_y_minus_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                double move_value = Convert.ToDouble(numericUpDown_ControlPanel_distance.Value);

                /*
                if (checkBox_ControlPanel_step_001.Checked)
                {
                    move_value = 0.01;
                }
                else if (checkBox_ControlPanel_step_01.Checked)
                {
                    move_value = 0.1;
                }
                else if (checkBox_ControlPanel_step_1.Checked)
                {
                    move_value = 1;
                }
                else if (checkBox_ControlPanel_step_10.Checked)
                {
                    move_value = 10;
                }*/

                if (Machine.Configurations.toolActive)
                {
                    Machine.Move(move_value, -move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value), Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
                }
                else
                {
                    Machine.Move(move_value, -move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value));
                }
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_x_plus_y_plus_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                double move_value = Convert.ToDouble(numericUpDown_ControlPanel_distance.Value);
                /*
                if (checkBox_ControlPanel_step_001.Checked)
                {
                    move_value = 0.01;
                }
                else if (checkBox_ControlPanel_step_01.Checked)
                {
                    move_value = 0.1;
                }
                else if (checkBox_ControlPanel_step_1.Checked)
                {
                    move_value = 1;
                }
                else if (checkBox_ControlPanel_step_10.Checked)
                {
                    move_value = 10;
                } */

                if (Machine.Configurations.toolActive)
                {
                    Machine.Move(move_value, move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value), Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
                }
                else
                {
                    Machine.Move(move_value, move_value, 0, Convert.ToDouble(numericUpDown_ControlPanel_feedrate.Value));
                }
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_Motor_OnOff_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                Machine.toggleMotors();
                //updateConfigurations();
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_laser_Click(object sender, EventArgs e)
        {
            if (Machine.Connection_Status)
            {
                Machine.toggleTool();
                //updateConfigurations();
            }
            else
            {
                Machine.Disconnect();
            }
        }

        private void button_ControlPanel_Stop_Click(object sender, EventArgs e)
        {
            Machine.Cancel();
            setControlPanel(Control_Panel_State.manual_control);
            /*
            if (ControlPanelState != Control_Panel_State.manual_control)
            {
                setControlPanel(Control_Panel_State.printing_play);
            }*/
            CloseFile();
        }

        private void button_ControlPanel_Reset_CNC_Click(object sender, EventArgs e)
        {
            Machine.Reset();
        }

        private void button_ControlPanel_Recalibrate_Click(object sender, EventArgs e)
        {
            Machine.Calibrate();
        }

        private void trackBar_ControlPanel_distance_Scroll(object sender, EventArgs e)
        {
            if (Convert.ToInt32(Math.Round(numericUpDown_ControlPanel_distance.Value)) != trackBar_ControlPanel_distance.Value)
            {
                numericUpDown_ControlPanel_distance.Value = trackBar_ControlPanel_distance.Value;
            }
        }

        private void numericUpDown_ControlPanel_distance_ValueChanged(object sender, EventArgs e)
        {
            if (Convert.ToInt32(Math.Round(numericUpDown_ControlPanel_distance.Value)) != trackBar_ControlPanel_distance.Value)
            {
                trackBar_ControlPanel_distance.Value = Convert.ToInt32(Math.Round(numericUpDown_ControlPanel_distance.Value));
            }
        }

        private void numericUpDown_ControlPanel_feedrate_ValueChanged(object sender, EventArgs e)
        {
            if(Convert.ToInt32(Math.Round(numericUpDown_ControlPanel_feedrate.Value)) != trackBar_ControlPanel_feedrate.Value)
            {
                trackBar_ControlPanel_feedrate.Value = Convert.ToInt32(Math.Round(numericUpDown_ControlPanel_feedrate.Value));
            }
        }

        private void trackBar_ControlPanel_feedrate_Scroll(object sender, EventArgs e)
        {
            if (Convert.ToInt32(Math.Round(numericUpDown_ControlPanel_feedrate.Value)) != trackBar_ControlPanel_feedrate.Value)
            {
                numericUpDown_ControlPanel_feedrate.Value = trackBar_ControlPanel_feedrate.Value;
            }
        }

        private void numericUpDown_ControlPanel_laser_ValueChanged(object sender, EventArgs e)
        {
            if (Convert.ToInt32(Math.Round(numericUpDown_ControlPanel_laser.Value)) != trackBar_ControlPanel_laser.Value)
            {
                trackBar_ControlPanel_laser.Value = Convert.ToInt32(Math.Round(numericUpDown_ControlPanel_laser.Value));
                Machine.setLaserPower(Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
            }
        }

        private void trackBar_ControlPanel_laser_Scroll(object sender, EventArgs e)
        {
            if (Convert.ToInt32(Math.Round(numericUpDown_ControlPanel_laser.Value)) != trackBar_ControlPanel_laser.Value)
            {
                numericUpDown_ControlPanel_laser.Value = trackBar_ControlPanel_laser.Value;
                
            }
        }

        private void trackBar_ControlPanel_laser_MouseUp(object sender, MouseEventArgs e)
        {
            Machine.setLaserPower(Convert.ToDouble(numericUpDown_ControlPanel_laser.Value));
        }


        // Button paint events

        private void btn_onPaint_X_minus(object sender, PaintEventArgs e)
        {

            GraphicsPath btn_path = new GraphicsPath(/*FillMode.Winding*/);
            btn_path.AddArc(new Rectangle(0, 0, 160, 160), 202.5F, -45F);
            btn_path.AddArc(new Rectangle(40, 40, 80, 80), 157.5F, 45F);

            // Convert the GraphicsPath into a Region.
            Region polygon_region = new Region(btn_path);

            // Constrain the button to the region.
            (sender as Button).Region = polygon_region;

            // Make the button big enough to hold the whole region.
            (sender as Button).SetBounds(
                (sender as Button).Location.X,
               (sender as Button).Location.Y,
                160, 160);// 50, 160);
        }

        private void btn_onPaint_X_minus_Y_minus(object sender, PaintEventArgs e)
        {

            GraphicsPath btn_path = new GraphicsPath(/*FillMode.Winding*/);
            btn_path.AddArc(new Rectangle(0, 0, 160, 160), 157.5F, -45.5F); // 157.5F, -45F);
            btn_path.AddArc(new Rectangle(40, 40, 80, 80), 112F, 45.5F); // 112.5F, 45F

            // Convert the GraphicsPath into a Region.
            Region polygon_region = new Region(btn_path);

            // Constrain the button to the region.
            (sender as Button).Region = polygon_region;

            // Make the button big enough to hold the whole region.
            (sender as Button).SetBounds(
                (sender as Button).Location.X,
               (sender as Button).Location.Y,
                160, 160);//200, 250);
        }

        private void btn_onPaint_X_minus_Y_plus(object sender, PaintEventArgs e)
        {

            GraphicsPath btn_path = new GraphicsPath(/*FillMode.Winding*/);
            btn_path.AddArc(new Rectangle(0, 0, 160, 160), 247.5F, -45F);
            btn_path.AddArc(new Rectangle(40, 40, 80, 80), 202.5F, 45F);

            // Convert the GraphicsPath into a Region.
            Region polygon_region = new Region(btn_path);

            // Constrain the button to the region.
            (sender as Button).Region = polygon_region;

            // Make the button big enough to hold the whole region.
            (sender as Button).SetBounds(
                (sender as Button).Location.X,
               (sender as Button).Location.Y,
                160, 160);//200, 250);
        }

        private void btn_onPaint_Y_plus(object sender, PaintEventArgs e)
        {

            GraphicsPath btn_path = new GraphicsPath(/*FillMode.Winding*/);
            btn_path.AddArc(new Rectangle(0, 0, 160, 160), 292.5F, -45F);
            btn_path.AddArc(new Rectangle(40, 40, 80, 80), 247.5F, 45F);

            // Convert the GraphicsPath into a Region.
            Region polygon_region = new Region(btn_path);

            // Constrain the button to the region.
            (sender as Button).Region = polygon_region;

            // Make the button big enough to hold the whole region.
            (sender as Button).SetBounds(
                (sender as Button).Location.X,
               (sender as Button).Location.Y,
                160, 160);//200, 50);
        }

        private void btn_onPaint_Y_plus_X_plus(object sender, PaintEventArgs e)
        {

            GraphicsPath btn_path = new GraphicsPath(/*FillMode.Winding*/);
            btn_path.AddArc(new Rectangle(0, 0, 160, 160), 338F, -45.5F); // (0, 0, 160, 160), 337.5F, -45F);
            btn_path.AddArc(new Rectangle(40, 40, 80, 80), 292.5F, 46F); // (40, 40, 80, 80), 292.5F, 45F);

            // Convert the GraphicsPath into a Region.
            Region polygon_region = new Region(btn_path);

            // Constrain the button to the region.
            (sender as Button).Region = polygon_region;

            // Make the button big enough to hold the whole region.
            (sender as Button).SetBounds(
                (sender as Button).Location.X,
               (sender as Button).Location.Y,
                160, 160);//200, 250);
        }

        private void btn_onPaint_X_plus(object sender, PaintEventArgs e)
        {

            GraphicsPath btn_path = new GraphicsPath(/*FillMode.Winding*/);
            btn_path.AddArc(new Rectangle(1, 0, 159, 160), 22.5F, -45F); // (0, 0, 160, 160), 22.5F, -45F);
            btn_path.AddArc(new Rectangle(41, 40, 79, 80), 337.5F, 45F); // (40, 40, 80, 80), 337.5F, 45F);

            // Convert the GraphicsPath into a Region.
            Region polygon_region = new Region(btn_path);

            // Constrain the button to the region.
            (sender as Button).Region = polygon_region;

            // Make the button big enough to hold the whole region.
            (sender as Button).SetBounds(
                (sender as Button).Location.X,
               (sender as Button).Location.Y,
                160, 160);//360, 200);
        }

        private void btn_onPaint_Y_minus_X_plus(object sender, PaintEventArgs e)
        {

            GraphicsPath btn_path = new GraphicsPath(/*FillMode.Winding*/);
            btn_path.AddArc(new Rectangle(0, 0, 160, 160), 68F, -46F); // 22.5F, 45F);
            btn_path.AddArc(new Rectangle(40, 40, 80, 80), 22F, 46F); // 22.5F, 45F);

            // Convert the GraphicsPath into a Region.
            Region polygon_region = new Region(btn_path);

            // Constrain the button to the region.
            (sender as Button).Region = polygon_region;

            // Make the button big enough to hold the whole region.
            (sender as Button).SetBounds(
                (sender as Button).Location.X,
               (sender as Button).Location.Y,
                160, 160);//360, 200);
        }

        private void btn_onPaint_Y_minus(object sender, PaintEventArgs e)
        {

            GraphicsPath btn_path = new GraphicsPath();

            btn_path.AddArc(new Rectangle(0, 1, 160, 159), 112.5F, -45F); //(0, 0, 160, 160), 112.5F, -45F);
            btn_path.AddArc(new Rectangle(40, 41, 80, 79), 67.5F, 45F); // (40, 40, 80, 80), 67.5F, 45F);

            // Convert the GraphicsPath into a Region.
            Region polygon_region = new Region(btn_path);

            // Constrain the button to the region.
            (sender as Button).Region = polygon_region;

            // Make the button big enough to hold the whole region.
            (sender as Button).SetBounds(
                (sender as Button).Location.X,
               (sender as Button).Location.Y,
                160, 160);//210, 360);


        }

        private void btn_onPaint_Z_plus(object sender, PaintEventArgs e)
        {

            GraphicsPath btn_path = new GraphicsPath(/*FillMode.Winding*/);
            btn_path.AddArc(new Rectangle(0, 0, 80, 80), 0F, -180F);

            // Convert the GraphicsPath into a Region.
            Region polygon_region = new Region(btn_path);

            // Constrain the button to the region.
            (sender as Button).Region = polygon_region;

            // Make the button big enough to hold the whole region.
            (sender as Button).SetBounds(
                (sender as Button).Location.X,
               (sender as Button).Location.Y,
                80, 80);//88, 40);
        }

        private void btn_onPaint_Z_minus(object sender, PaintEventArgs e)
        {

            GraphicsPath btn_path = new GraphicsPath(/*FillMode.Winding*/);
            btn_path.AddArc(new Rectangle(0, 0, 80, 80), 0F, 180F);

            // Convert the GraphicsPath into a Region.
            Region polygon_region = new Region(btn_path);

            // Constrain the button to the region.
            (sender as Button).Region = polygon_region;
            
            
            // Make the button big enough to hold the whole region.
            (sender as Button).SetBounds(
                (sender as Button).Location.X,
               (sender as Button).Location.Y,
                80, 80);//88, 115);
        }


        // File related functions/parameters

        private string error_log_file_path;

        private string[] File;
        private System.Timers.Timer File_Stopwatch;
        private int File_Stopwatch_seconds = 0;
        private int File_Stopwatch_minutes = 0;
        private int File_Stopwatch_hours = 0;
        

        private void CloseFile()
        {
            File = new string[] { };

            label_File_Name.Text = "(none)";
            
            try
            {
                panel_XY_plane.BackgroundImage.Dispose();
            }
            catch(Exception e)
            {
                
            }

            try
            {
                panel_XY_plane.BackgroundImage = null;
            }
            catch(Exception e)
            {

            }
        }

        private void File_sim_finished()
        {
            setControlPanel(Control_Panel_State.printing_play);
        }

        private void button_ControlPanel_Load_File_Click(object sender, EventArgs e)
        {

            if (FileBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Cursor.Current = Cursors.WaitCursor;

                string FilePath = FileBrowser.FileName;

                error_log_file_path = FilePath + ".log";

                string[] subfolders = FilePath.Split('\\');

                int FileLines = 0;

                string FileName = subfolders[subfolders.Length - 1];
                //FileName_label.Text = FileName;
                //ToolTipoFileName.SetToolTip(FileName_label, FileName);

                label_File_Name.Text = FileName;
                toolTip_main_form_tt.SetToolTip(label_File_Name, FileName);

                StringBuilder fileline = new StringBuilder();
                //StringBuilder file = new StringBuilder();

                List<string> file_StringList = new List<string>();

                try
                {
                    StreamReader fileStream;
                    if ((fileStream = System.IO.File.OpenText(FilePath)) != null)
                    {
                        using (fileStream)
                        {

                            while (!fileStream.EndOfStream)
                            {
                                fileline.Append(fileStream.ReadLine());

                                if (fileline.Length == 0)
                                {
                                    continue;
                                }
                                if (fileline[0] == 'G' || fileline[0] == 'M' || fileline[0] == 'N')
                                {
                                    FileLines++;
                                    //file.Append(fileline).AppendLine();

                                    file_StringList.Add(fileline.ToString() + '\n');
                                }
                                fileline.Clear();

                            }


                            File = file_StringList.ToArray();

                            //TextBox.AppendText(file.ToString());

                        }
                    }
                }
                catch (Exception err)
                {
                    CloseFile();
                    Cursor.Current = Cursors.Default;
                    return;
                }

            }
            else
            {
                CloseFile();
                Cursor.Current = Cursors.Default;
                return;
            }

            if (File.Length == 0)
            {
                CloseFile();
                Cursor.Current = Cursors.Default;
                //setControlPanel(Control_Panel_State.manual_control);
                return;
            }

            //CurrentLine = 0;
            //SelectLine(CurrentLine);
            //BTN_update(BTN_State.Play);
            Cursor.Current = Cursors.Default;

            // call simulation
            Machine.simulateFile(File, error_log_file_path);


            // when sim finish it will swap : setControlPanel(Control_Panel_State.printing_play); on a delegate function (File_sim_finished)
        }

        private void button_ControlPanel_Start_File_Click(object sender, EventArgs e)
        {
            try
            {
                Machine.printFile(File);
                setControlPanel(Control_Panel_State.priting_pause);
                File_Stopwatch_start();
                fileProgress(0);
            }
            catch (Exception exc)
            {
                setControlPanel(Control_Panel_State.manual_control);
            }

        }

        private void button_ControlPanel_Pause_File_Click(object sender, EventArgs e)
        {
            Machine.Pause();
            setControlPanel(Control_Panel_State.printing_resume);
            File_Stopwatch_pause();
        }

        private void button_ControlPanel_Cancel_File_Click(object sender, EventArgs e)
        {
            Machine.Cancel();
            setControlPanel(Control_Panel_State.printing_play);
            File_Stopwatch_stop();
        }

        private void button_ControlPanel_Resume_File_Click(object sender, EventArgs e)
        {
            Machine.Resume();
            setControlPanel(Control_Panel_State.priting_pause);
            File_Stopwatch_resume();
        }

        private void button_ControlPanel_Close_File_Click(object sender, EventArgs e)
        {
            CloseFile();

            // verificar necessidade de program_end

            setControlPanel(Control_Panel_State.manual_control);
            Application.DoEvents();

        }

        private delegate void fileProgress_callback(int p);

        private void fileProgress(int p)
        {
            if (ControlPanelState == Control_Panel_State.disconnect || ControlPanelState == Control_Panel_State.manual_control)
            {
                return;
            }

            if (label_ControlPanel_file_progress.InvokeRequired)
            {
                fileProgress_callback d = new fileProgress_callback(fileProgress);
                this.label_ControlPanel_file_progress.Invoke(d, new object[] { p });
                Application.DoEvents();
                return;
            }

            label_ControlPanel_file_progress.Text = p + "%";
            progressBar_ControlPanel_file_progress.Value = p;
        }

        private void cnc_backgroundwork_finished()
        {
            if (ControlPanelState == Control_Panel_State.priting_pause)
            {
                setControlPanel(Control_Panel_State.printing_play);
                File_Stopwatch_stop();
            }
        }

        private delegate void writeTime_callback(string time);

        private void writeTime(string time)
        {
            if (label_ControlPanel_file_time.InvokeRequired)
            {
                writeTime_callback d = new writeTime_callback(writeTime);
                this.label_ControlPanel_file_time.Invoke(d, new object[] { time });
                Application.DoEvents();
                return;
            }

            label_ControlPanel_file_time.Text = time;
        }

        private void File_Stopwatch_resetCount()
        {
            File_Stopwatch_seconds = 0;
            File_Stopwatch_minutes = 0;
            File_Stopwatch_hours = 0;

            writeTime("00:00:00");
        }

        private void File_Stopwatch_start()
        {
            File_Stopwatch_resetCount();
            File_Stopwatch = new System.Timers.Timer(1000);
            File_Stopwatch.Elapsed += File_Stopwatch_event;
            File_Stopwatch.Start();
        }

        private void File_Stopwatch_stop()
        {
            try
            {
                File_Stopwatch.Dispose();
            }
            catch (Exception e)
            {

            }

        }

        private void File_Stopwatch_pause()
        {
            try
            {
                File_Stopwatch.Stop();
            }
            catch (Exception e)
            {

            }
        }

        private void File_Stopwatch_resume()
        {
            try
            {
                File_Stopwatch.Start();
            }
            catch (Exception e)
            {

            }
        }

        private void File_Stopwatch_event(object source, System.Timers.ElapsedEventArgs e)
        {
            File_Stopwatch_seconds++;
            if(File_Stopwatch_seconds >= 60)
            {
                File_Stopwatch_seconds = 0;
                File_Stopwatch_minutes++;
            }
            if(File_Stopwatch_minutes >= 60)
            {
                File_Stopwatch_minutes = 0;
                File_Stopwatch_hours++;
            }

            string sec = File_Stopwatch_seconds > 9? File_Stopwatch_seconds.ToString() : "0" + File_Stopwatch_seconds.ToString();
            string min = File_Stopwatch_minutes > 9 ? File_Stopwatch_minutes.ToString() : "0" + File_Stopwatch_minutes.ToString();
            string hour = File_Stopwatch_hours > 9 ? File_Stopwatch_hours.ToString() : "0" + File_Stopwatch_hours.ToString();

            writeTime(hour + ":" + min + ":" + sec);

            try
            {
                File_Stopwatch.Start();
            }
            catch (Exception err)
            {

            }
        }


        private delegate void File_Sim_Draw_callback(Bitmap img);

        private void File_Sim_Draw(Bitmap img)
        {
            if (panel_XY_plane.InvokeRequired)
            {
                File_Sim_Draw_callback d = new File_Sim_Draw_callback(File_Sim_Draw);
                this.panel_XY_plane.Invoke(d, new object[] { img });
                Application.DoEvents();
                return;
            }

            /*
            try
            {
                panel_XY_plane.BackgroundImage.Dispose();
            }
            catch(Exception err)
            {

            }*/

            img.RotateFlip(RotateFlipType.Rotate180FlipX);
            panel_XY_plane.BackgroundImage = img;
            
        } 

        #endregion

        // Image functions

        private delegate void DisplayMode_callback(IMG_Manager.ImageDisplay mode);

        private void DisplayMode(IMG_Manager.ImageDisplay mode)
        {
            if (button_TP_ShowColorIMG.InvokeRequired)
            {
                DisplayMode_callback d = new DisplayMode_callback(DisplayMode);
                this.button_TP_ShowColorIMG.Invoke(d, new object[] { mode });
                Application.DoEvents();
                return;
            }
                        
            switch (mode)
            {
                case IMG_Manager.ImageDisplay.NoImage:
                    // Image Options
                    button_TP_LoadIMG.Enabled = true;
                    button_TP_CloseIMG.Enabled = false;
                    button_SaveImg.Enabled = false;
                    label_TP_FileName.Text = "(none)";

                    button_TP_ShowColorIMG.Enabled = false;
                    button_TP_ShowBWIMG.Enabled = false;
                    button_TP_ApplyEdge.Enabled = false;
                    trackBar_TP_ShowBWIMG.Enabled = false;
                    trackBar_TP_EdgeDepth.Enabled = false;
                    comboBox_TP_ED_List.Enabled = false;

                    //comboBox_TP_ED_List.SelectedIndex = 3;

                    button_TP_ResetIMG.Enabled = false;
                    button_TP_ApplyInvert.Enabled = false;
                    button_TP_ApplySharp.Enabled = false;
                    button_TP_ApplyBlur.Enabled = false;

                    // - colors 
                    button_TP_LoadIMG.BackColor = _LightBlue;

                    button_TP_CloseIMG.BackColor = _LightGray;
                    button_SaveImg.BackColor = _LightGray;
                    button_TP_ShowColorIMG.BackColor = _LightGray;
                    button_TP_ShowBWIMG.BackColor = _LightGray;
                    button_TP_ApplyEdge.BackColor = _LightGray;
                    button_TP_ResetIMG.BackColor = _LightGray;
                    button_TP_ApplyInvert.BackColor = _LightGray;
                    button_TP_ApplySharp.BackColor = _LightGray;
                    button_TP_ApplyBlur.BackColor = _LightGray;

                    // Toolpathing Options
                    comboBox_TP_Mode.Enabled = false;
                    numericUpDown_TP_img_width_pp.Enabled = false;
                    numericUpDown_TP_img_height_pp.Enabled = false;
                    comboBox_TP_RemoveColor.Enabled = false;
                    comboBox_TP_PathType.Enabled = false;

                    label_TP_txt_TPMODE.Enabled = false;
                    label_TP_txt_HD.Enabled = false;
                    label_TP_txt_VD.Enabled = false;
                    label_TP_img_width_pp.Enabled = false;
                    label_TP_img_height_pp.Enabled = false;
                    label_TP_img_width_pp.Text = "0";
                    label_TP_img_height_pp.Text = "0";
                    label_TP_txt_HD_pp.Enabled = false;
                    label_TP_txt_VD_pp.Enabled = false;
                    label_TP_txt_HD_mm.Enabled = false;
                    label_TP_txt_VD_mm.Enabled = false;
                    label_TP_txt_RmvColor.Enabled = false;
                    label_TP_txt_TPType.Enabled = false;

                    progressBar_TP_Progress.Enabled = false;
                    label_TP_Progress.Enabled = false;
                    progressBar_TP_Progress.Value = 0;
                    label_TP_Progress.Text = "0%";
                    label_TP_txt_Status.Enabled = false;
                    label_TP_Status.Enabled = false;
                    label_TP_Status.Text = "Idle";

                    button_TP_Start.Enabled = false;
                    button_TP_Cancel.Enabled = false;

                    button_TP_ShowToolpath.Enabled = false;
                    comboBox_TP_Img_Selection.Enabled = false;
                    //button_TP_ShowToolpath.Visible = false;

                    // - colors 
                    button_TP_Start.BackColor = _LightGray;
                    button_TP_Cancel.BackColor = _LightGray;
                    button_TP_ShowToolpath.BackColor = _LightGray;

                    break;
                case IMG_Manager.ImageDisplay.Color:
                    // Image Options
                    button_TP_LoadIMG.Enabled = false;
                    button_TP_CloseIMG.Enabled = true;
                    button_SaveImg.Enabled = true;

                    button_TP_ShowColorIMG.Enabled = false;
                    button_TP_ShowBWIMG.Enabled = true;
                    button_TP_ApplyEdge.Enabled = true;
                    trackBar_TP_ShowBWIMG.Enabled = true;
                    //trackBar_TP_EdgeDepth.Enabled = true;
                    comboBox_TP_ED_List.Enabled = true;

                    button_TP_ResetIMG.Enabled = true;
                    button_TP_ApplyInvert.Enabled = true;
                    button_TP_ApplySharp.Enabled = true;
                    button_TP_ApplyBlur.Enabled = true;

                    // colors 
                    button_TP_LoadIMG.BackColor = _LightGray;

                    button_TP_CloseIMG.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    button_SaveImg.BackColor = _LightBlue;
                    button_TP_ShowColorIMG.BackColor = _LightGray;
                    button_TP_ShowBWIMG.BackColor = _LightBlue;
                    button_TP_ApplyEdge.BackColor = _LightBlue;
                    button_TP_ResetIMG.BackColor = _LightBlue;
                    button_TP_ApplyInvert.BackColor = _LightBlue;
                    button_TP_ApplySharp.BackColor = _LightBlue;
                    button_TP_ApplyBlur.BackColor = _LightBlue;

                    // Toolpathing Options
                    comboBox_TP_Mode.Enabled = true;
                    numericUpDown_TP_img_width_pp.Enabled = true;
                    numericUpDown_TP_img_height_pp.Enabled = true;
                    comboBox_TP_RemoveColor.Enabled = true;
                    comboBox_TP_PathType.Enabled = true;

                    label_TP_txt_TPMODE.Enabled = true;
                    label_TP_txt_HD.Enabled = true;
                    label_TP_txt_VD.Enabled = true;
                    label_TP_img_width_pp.Enabled = true;
                    label_TP_img_height_pp.Enabled = true;
                    //label_TP_img_width_pp.Text = "0";
                    //label_TP_img_height_pp.Text = "0";
                    label_TP_txt_HD_pp.Enabled = true;
                    label_TP_txt_VD_pp.Enabled = true;
                    label_TP_txt_HD_mm.Enabled = true;
                    label_TP_txt_VD_mm.Enabled = true;
                    label_TP_txt_RmvColor.Enabled = true;
                    label_TP_txt_TPType.Enabled = true;

                    progressBar_TP_Progress.Enabled = false;
                    label_TP_Progress.Enabled = false;
                    progressBar_TP_Progress.Value = 0;
                    label_TP_Progress.Text = "0%";
                    label_TP_txt_Status.Enabled = false;
                    label_TP_Status.Enabled = false;
                    label_TP_Status.Text = "Idle";

                    button_TP_Start.Enabled = false; // !!
                    button_TP_Cancel.Enabled = false;

                    button_TP_ShowToolpath.Enabled = Toolpathing_Manager.ToolPathAvailable;
                    comboBox_TP_Img_Selection.Enabled = Toolpathing_Manager.ToolPathAvailable;
                    //button_TP_ShowToolpath.Visible = Toolpathing_Manager.ToolPathAvailable;

                    // - colors 
                    button_TP_Start.BackColor = _LightGray; // !!
                    button_TP_Cancel.BackColor = _LightGray;
                    button_TP_ShowToolpath.BackColor = Toolpathing_Manager.ToolPathAvailable ? _LightBlue : _LightGray;
                    break;
                case IMG_Manager.ImageDisplay.BW:
                    // Image Options
                    button_TP_LoadIMG.Enabled = false;
                    button_TP_CloseIMG.Enabled = true;
                    button_SaveImg.Enabled = true;

                    button_TP_ShowColorIMG.Enabled = true;
                    button_TP_ShowBWIMG.Enabled = false;
                    button_TP_ApplyEdge.Enabled = true;
                    trackBar_TP_ShowBWIMG.Enabled = true;
                    //trackBar_TP_EdgeDepth.Enabled = true;
                    comboBox_TP_ED_List.Enabled = true;

                    button_TP_ResetIMG.Enabled = true;
                    button_TP_ApplyInvert.Enabled = true;
                    button_TP_ApplySharp.Enabled = true;
                    button_TP_ApplyBlur.Enabled = true;

                    // colors 
                    button_TP_LoadIMG.BackColor = _LightGray;

                    button_TP_CloseIMG.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    button_SaveImg.BackColor = _LightBlue;
                    button_TP_ShowColorIMG.BackColor = _LightBlue;
                    button_TP_ShowBWIMG.BackColor = _LightGray;
                    button_TP_ApplyEdge.BackColor = _LightBlue;
                    button_TP_ResetIMG.BackColor = _LightBlue;
                    button_TP_ApplyInvert.BackColor = _LightBlue;
                    button_TP_ApplySharp.BackColor = _LightBlue;
                    button_TP_ApplyBlur.BackColor = _LightBlue;

                    // Toolpathing Options
                    comboBox_TP_Mode.Enabled = true;
                    numericUpDown_TP_img_width_pp.Enabled = true;
                    numericUpDown_TP_img_height_pp.Enabled = true;
                    comboBox_TP_RemoveColor.Enabled = true;
                    comboBox_TP_PathType.Enabled = true;

                    label_TP_txt_TPMODE.Enabled = true;
                    label_TP_txt_HD.Enabled = true;
                    label_TP_txt_VD.Enabled = true;
                    label_TP_img_width_pp.Enabled = true;
                    label_TP_img_height_pp.Enabled = true;
                    //label_TP_img_width_pp.Text = "0";
                    //label_TP_img_height_pp.Text = "0";
                    label_TP_txt_HD_pp.Enabled = true;
                    label_TP_txt_VD_pp.Enabled = true;
                    label_TP_txt_HD_mm.Enabled = true;
                    label_TP_txt_VD_mm.Enabled = true;
                    label_TP_txt_RmvColor.Enabled = true;
                    label_TP_txt_TPType.Enabled = true;

                    progressBar_TP_Progress.Enabled = false;
                    label_TP_Progress.Enabled = false;
                    progressBar_TP_Progress.Value = 0;
                    label_TP_Progress.Text = "0%";
                    label_TP_txt_Status.Enabled = false;
                    label_TP_Status.Enabled = false;
                    label_TP_Status.Text = "Idle";

                    button_TP_Start.Enabled = true;
                    button_TP_Cancel.Enabled = false;

                    button_TP_ShowToolpath.Enabled = Toolpathing_Manager.ToolPathAvailable;
                    comboBox_TP_Img_Selection.Enabled = Toolpathing_Manager.ToolPathAvailable;
                    //button_TP_ShowToolpath.Visible = Toolpathing_Manager.ToolPathAvailable;

                    // - colors 
                    button_TP_Start.BackColor = _LightBlue;
                    button_TP_Cancel.BackColor = _LightGray;
                    button_TP_ShowToolpath.BackColor = Toolpathing_Manager.ToolPathAvailable? _LightBlue : _LightGray;
                    break;
                case IMG_Manager.ImageDisplay.ED:
                    // Image Options
                    button_TP_LoadIMG.Enabled = false;
                    button_TP_CloseIMG.Enabled = true;
                    button_SaveImg.Enabled = true;

                    button_TP_ShowColorIMG.Enabled = true;
                    button_TP_ShowBWIMG.Enabled = true;
                    button_TP_ApplyEdge.Enabled = false;
                    trackBar_TP_ShowBWIMG.Enabled = true;
                    if(comboBox_TP_ED_List.SelectedIndex != 0)
                    {
                        trackBar_TP_EdgeDepth.Enabled = true;
                    }
                    comboBox_TP_ED_List.Enabled = true;

                    button_TP_ResetIMG.Enabled = true;
                    button_TP_ApplyInvert.Enabled = true;
                    button_TP_ApplySharp.Enabled = true;
                    button_TP_ApplyBlur.Enabled = true;

                    // colors 
                    button_TP_LoadIMG.BackColor = _LightGray;

                    button_TP_CloseIMG.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    button_SaveImg.BackColor = _LightBlue;
                    button_TP_ShowColorIMG.BackColor = _LightBlue;
                    button_TP_ShowBWIMG.BackColor = _LightBlue;
                    button_TP_ApplyEdge.BackColor = _LightGray;
                    button_TP_ResetIMG.BackColor = _LightBlue;
                    button_TP_ApplyInvert.BackColor = _LightBlue;
                    button_TP_ApplySharp.BackColor = _LightBlue;
                    button_TP_ApplyBlur.BackColor = _LightBlue;

                    // Toolpathing Options
                    comboBox_TP_Mode.Enabled = true;
                    numericUpDown_TP_img_width_pp.Enabled = true;
                    numericUpDown_TP_img_height_pp.Enabled = true;
                    comboBox_TP_RemoveColor.Enabled = true;
                    comboBox_TP_PathType.Enabled = true;

                    label_TP_txt_TPMODE.Enabled = true;
                    label_TP_txt_HD.Enabled = true;
                    label_TP_txt_VD.Enabled = true;
                    label_TP_img_width_pp.Enabled = true;
                    label_TP_img_height_pp.Enabled = true;
                    //label_TP_img_width_pp.Text = "0";
                    //label_TP_img_height_pp.Text = "0";
                    label_TP_txt_HD_pp.Enabled = true;
                    label_TP_txt_VD_pp.Enabled = true;
                    label_TP_txt_HD_mm.Enabled = true;
                    label_TP_txt_VD_mm.Enabled = true;
                    label_TP_txt_RmvColor.Enabled = true;
                    label_TP_txt_TPType.Enabled = true;

                    progressBar_TP_Progress.Enabled = false;
                    label_TP_Progress.Enabled = false;
                    progressBar_TP_Progress.Value = 0;
                    label_TP_Progress.Text = "0%";
                    label_TP_txt_Status.Enabled = false;
                    label_TP_Status.Enabled = false;
                    label_TP_Status.Text = "Idle";

                    button_TP_Start.Enabled = true;
                    button_TP_Cancel.Enabled = false;

                    button_TP_ShowToolpath.Enabled = Toolpathing_Manager.ToolPathAvailable;
                    comboBox_TP_Img_Selection.Enabled = Toolpathing_Manager.ToolPathAvailable;
                    //button_TP_ShowToolpath.Visible = Toolpathing_Manager.ToolPathAvailable;

                    // - colors 
                    button_TP_Start.BackColor = _LightBlue;
                    button_TP_Cancel.BackColor = _LightGray;
                    button_TP_ShowToolpath.BackColor = Toolpathing_Manager.ToolPathAvailable? _LightBlue : _LightGray;
                    break;
                case IMG_Manager.ImageDisplay.Processing: // 
                    // Image Options
                    button_TP_LoadIMG.Enabled = false;
                    button_TP_CloseIMG.Enabled = false;
                    button_SaveImg.Enabled = false;

                    button_TP_ShowColorIMG.Enabled = false;
                    button_TP_ShowBWIMG.Enabled = false;
                    button_TP_ApplyEdge.Enabled = false;
                    trackBar_TP_ShowBWIMG.Enabled = false;
                    trackBar_TP_EdgeDepth.Enabled = false;
                    comboBox_TP_ED_List.Enabled = false;


                    button_TP_ResetIMG.Enabled = false;
                    button_TP_ApplyInvert.Enabled = false;
                    button_TP_ApplySharp.Enabled = false;
                    button_TP_ApplyBlur.Enabled = false;

                    // colors 
                    button_TP_LoadIMG.BackColor = _LightGray;
                    button_TP_CloseIMG.BackColor = _LightGray;
                    button_SaveImg.BackColor = _LightGray;

                    button_TP_ShowColorIMG.BackColor = _LightBlue;
                    button_TP_ShowColorIMG.BackColor = _LightGray;
                    button_TP_ShowBWIMG.BackColor = _LightGray;
                    button_TP_ApplyEdge.BackColor = _LightGray;
                    button_TP_ResetIMG.BackColor = _LightGray;
                    button_TP_ApplyInvert.BackColor = _LightGray;
                    button_TP_ApplySharp.BackColor = _LightGray;
                    button_TP_ApplyBlur.BackColor = _LightGray;

                    // Toolpathing Options
                    comboBox_TP_Mode.Enabled = false;
                    numericUpDown_TP_img_width_pp.Enabled = false;
                    numericUpDown_TP_img_height_pp.Enabled = false;
                    comboBox_TP_RemoveColor.Enabled = false;
                    comboBox_TP_PathType.Enabled = false;

                    label_TP_txt_TPMODE.Enabled = false;
                    label_TP_txt_HD.Enabled = false;
                    label_TP_txt_VD.Enabled = false;
                    label_TP_img_width_pp.Enabled = false;
                    label_TP_img_height_pp.Enabled = false;
                    //label_TP_img_width_pp.Text = "0";
                    //label_TP_img_height_pp.Text = "0";
                    label_TP_txt_HD_pp.Enabled = false;
                    label_TP_txt_VD_pp.Enabled = false;
                    label_TP_txt_HD_mm.Enabled = false;
                    label_TP_txt_VD_mm.Enabled = false;
                    label_TP_txt_RmvColor.Enabled = false;
                    label_TP_txt_TPType.Enabled = false;

                    progressBar_TP_Progress.Enabled = true;
                    label_TP_Progress.Enabled = true;
                    //progressBar_TP_Progress.Value = 0;
                    //label_TP_Progress.Text = "0%";
                    label_TP_txt_Status.Enabled = true;
                    label_TP_Status.Enabled = true;
                    label_TP_Status.Text = "Idle";

                    button_TP_Start.Enabled = false;
                    button_TP_Cancel.Enabled = true;

                    button_TP_ShowToolpath.Enabled = false;
                    comboBox_TP_Img_Selection.Enabled = false;
                    comboBox_TP_Img_Selection.SelectedIndex = 0;
                    //button_TP_ShowToolpath.Visible = false;

                    // - colors 
                    button_TP_Start.BackColor = _LightGray;
                    button_TP_Cancel.BackColor = _LightBlue;
                    button_TP_ShowToolpath.BackColor = _LightGray;
                    break;
                case IMG_Manager.ImageDisplay.Toolpath:
                    // Image Options
                    button_TP_LoadIMG.Enabled = false;
                    button_TP_CloseIMG.Enabled = true;
                    button_SaveImg.Enabled = true;

                    button_TP_ShowColorIMG.Enabled = true;
                    button_TP_ShowBWIMG.Enabled = true;
                    button_TP_ApplyEdge.Enabled = true;
                    trackBar_TP_ShowBWIMG.Enabled = true;
                    //trackBar_TP_EdgeDepth.Enabled = true;
                    comboBox_TP_ED_List.Enabled = true;

                    button_TP_ResetIMG.Enabled = true;
                    button_TP_ApplyInvert.Enabled = true;
                    button_TP_ApplySharp.Enabled = true;
                    button_TP_ApplyBlur.Enabled = true;

                    // colors 
                    button_TP_LoadIMG.BackColor = _LightGray;

                    button_TP_CloseIMG.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
                    button_SaveImg.BackColor = _LightBlue;
                    button_TP_ShowColorIMG.BackColor = _LightBlue;
                    button_TP_ShowBWIMG.BackColor = _LightBlue;
                    button_TP_ApplyEdge.BackColor = _LightBlue;
                    button_TP_ResetIMG.BackColor = _LightBlue;
                    button_TP_ApplyInvert.BackColor = _LightBlue;
                    button_TP_ApplySharp.BackColor = _LightBlue;
                    button_TP_ApplyBlur.BackColor = _LightBlue;

                    // Toolpathing Options
                    comboBox_TP_Mode.Enabled = true;
                    numericUpDown_TP_img_width_pp.Enabled = true;
                    numericUpDown_TP_img_height_pp.Enabled = true;
                    comboBox_TP_RemoveColor.Enabled = true;
                    comboBox_TP_PathType.Enabled = true;

                    label_TP_txt_TPMODE.Enabled = true;
                    label_TP_txt_HD.Enabled = true;
                    label_TP_txt_VD.Enabled = true;
                    label_TP_img_width_pp.Enabled = true;
                    label_TP_img_height_pp.Enabled = true;
                    //label_TP_img_width_pp.Text = "0";
                    //label_TP_img_height_pp.Text = "0";
                    label_TP_txt_HD_pp.Enabled = true;
                    label_TP_txt_VD_pp.Enabled = true;
                    label_TP_txt_HD_mm.Enabled = true;
                    label_TP_txt_VD_mm.Enabled = true;
                    label_TP_txt_RmvColor.Enabled = true;
                    label_TP_txt_TPType.Enabled = true;

                    progressBar_TP_Progress.Enabled = false;
                    label_TP_Progress.Enabled = false;
                    progressBar_TP_Progress.Value = 0;
                    label_TP_Progress.Text = "0%";
                    label_TP_txt_Status.Enabled = false;
                    label_TP_Status.Enabled = false;
                    label_TP_Status.Text = "Idle";

                    button_TP_Start.Enabled = false;
                    button_TP_Cancel.Enabled = false;

                    button_TP_ShowToolpath.Enabled = false;
                    comboBox_TP_Img_Selection.Enabled = Toolpathing_Manager.ToolPathAvailable;
                    //button_TP_ShowToolpath.Visible = Toolpathing_Manager.ToolPathAvailable;

                    // - colors 
                    button_TP_Start.BackColor = _LightGray;
                    button_TP_Cancel.BackColor = _LightGray;
                    button_TP_ShowToolpath.BackColor = _LightGray;
                    break;
            }
        }

        private delegate void DisplayImage_callback(Bitmap p);

        private void DisplayImage(Bitmap img)
        {
            if (pictureBox_IMG_Toolpathing.InvokeRequired)
            {
                DisplayImage_callback d = new DisplayImage_callback(DisplayImage);
                this.pictureBox_IMG_Toolpathing.Invoke(d, new object[] { img });
                Application.DoEvents();
                return;
            }

            try
            {
                pictureBox_IMG_Toolpathing.Image.Dispose();
            }
            catch (Exception e)
            {

            }

            if (img == null)
            {
                pictureBox_IMG_Toolpathing.Image = null;
                return;
            }


            // interpolar imagens pequenas

            if(img.Width * 4 < pictureBox_IMG_Toolpathing.Width)
            {
                double aspectRatio = (double)img.Width / (double)img.Height;

                int newWidth = pictureBox_IMG_Toolpathing.Width;
                int newHeight = Convert.ToInt32(Math.Round((double)newWidth / aspectRatio));

                int ratio =  Convert.ToInt32(Math.Round((double)newWidth / img.Width));

                Bitmap Bmp = new Bitmap(newWidth, newHeight);

                using (var b = new SolidBrush(Color.White))
                using (Bitmap aux = new Bitmap(img.Width + 1, img.Height + 1)) // aux img to correct the first line and column compression on NearestNeighbor
                using (Graphics gfx_aux = Graphics.FromImage(aux))
                using (Graphics gfx = Graphics.FromImage(Bmp))
                {
                    gfx_aux.FillRectangle(b, 0, 0, aux.Width, aux.Height);
                    gfx_aux.DrawImage(img, 1, 1, img.Width, img.Height);
                    // Turn off anti-aliasing and draw an exact copy
                    gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    gfx.FillRectangle(b, 0, 0, newWidth, newWidth);
                    gfx.DrawImage(aux, 0, 0, Bmp.Width, Bmp.Height);
                }
                pictureBox_IMG_Toolpathing.Image = Bmp;
            }
            else
            {
                try
                {
                    pictureBox_IMG_Toolpathing.Image = (Bitmap)img.Clone();
                }
                catch (Exception e)
                {

                }
            }

            

        }

        private void button_TP_LoadIMG_Click(object sender, EventArgs e)
        {
            if (Toolpathing_Manager.LoadImage())
            {
                label_TP_FileName.Text = Toolpathing_Manager.filename;

                label_TP_img_width_pp.Text = Toolpathing_Manager.Img_Size.Width.ToString();
                label_TP_img_height_pp.Text = Toolpathing_Manager.Img_Size.Height.ToString();

                numericUpDown_TP_img_width_pp.Value = Convert.ToDecimal(Toolpathing_Manager.IMG_Width);
                numericUpDown_TP_img_height_pp.Value = Convert.ToDecimal(Toolpathing_Manager.IMG_Height);

                if (Toolpathing_Manager.svg_image)
                {
                    comboBox_TP_Mode.DataSource = TP_BMP_SVG_Mode;
                }
                else
                {
                    comboBox_TP_Mode.DataSource = TP_BMP_Mode_Only;
                }

            }
        }

        private void button_TP_CloseIMG_Click(object sender, EventArgs e)
        {
            if (Toolpathing_Manager.CloseImage())
            {
                label_TP_FileName.Text = "(none)";
            }
        }

        private void button_SaveImg_Click(object sender, EventArgs e)
        {
            if (Toolpathing_Manager.ImageAvailable)
            {
                SaveFileDialog FileBrowser = new SaveFileDialog();
                FileBrowser.Filter = "Image files|*.bmp;*.BMP;*.png;*.PNG;*.svg;*.SVG|All files (*.*)|*.*";
                FileBrowser.FilterIndex = 2;
                FileBrowser.Title = "Open an Image File";

                string filepath;
                FileBrowser.FileName = Toolpathing_Manager + "_new.bmp";
                if (FileBrowser.ShowDialog() == DialogResult.OK)
                {
                    filepath = FileBrowser.FileName;
                    pictureBox_IMG_Toolpathing.Image.Save(filepath);

                }



            }
        }

        private void button_SaveImg_MouseEnter(object sender, EventArgs e)
        {
            if (Toolpathing_Manager.ImageAvailable)
            {
                try
                {
                    button_SaveImg.BackgroundImage.Dispose();
                }
                finally
                {
                    button_SaveImg.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.save_file_2);
                }
            }

        }

        private void button_SaveImg_MouseLeave(object sender, EventArgs e)
        {

            try
            {
                button_SaveImg.BackgroundImage.Dispose();
            }
            finally
            {
                button_SaveImg.BackgroundImage = new Bitmap(CNC_Pathfinder.Properties.Resources.save_file_1);
            }
        }


        private void button_TP_ResetIMG_Click(object sender, EventArgs e)
        {
            Toolpathing_Manager.resetToOriginalImage();
            numericUpDown_TP_img_height_pp.Value = Convert.ToDecimal(Toolpathing_Manager.IMG_Height);
            numericUpDown_TP_img_width_pp.Value = Convert.ToDecimal(Toolpathing_Manager.IMG_Width);
        }

        private void button_TP_ShowColorIMG_Click(object sender, EventArgs e)
        {
            Toolpathing_Manager.ShowColorImage();
        }

        private void button_TP_ShowBWIMG_Click(object sender, EventArgs e)
        {
            Toolpathing_Manager.ShowBlackwhite(trackBar_TP_ShowBWIMG.Value);
        }

        private void trackBar_TP_ShowBWIMG_Scroll(object sender, EventArgs e)
        {
            Toolpathing_Manager.ShowBlackwhite(trackBar_TP_ShowBWIMG.Value);
        }

        private void button_TP_ApplySharp_Click(object sender, EventArgs e)
        {
            Toolpathing_Manager.ApplySharp();
        }

        private void button_TP_ApplyBlur_Click(object sender, EventArgs e)
        {
            Toolpathing_Manager.ApplyBlur();
        }

        private void button_TP_ApplyInvert_Click(object sender, EventArgs e)
        {
            Toolpathing_Manager.ApplyNegative();
        }

        private void button_TP_ApplyEdge_Click(object sender, EventArgs e)
        {
            if (Toolpathing_Manager.EdgeAvailable)
            {
                switch (comboBox_TP_ED_List.SelectedIndex)
                {
                    case 0: // SXOR
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.SXOR, trackBar_TP_EdgeDepth.Value);
                        break;
                    case 1: // RC
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.RobertsCross, trackBar_TP_EdgeDepth.Value);
                        break;
                    case 2: // Perwitt
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.Perwitt, trackBar_TP_EdgeDepth.Value);
                        break;
                    case 3: // Sobel
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.Sobel, trackBar_TP_EdgeDepth.Value);
                        break;
                }
            }
            else
            {
                switch (comboBox_TP_ED_List.SelectedIndex)
                {
                    case 0: // SXOR
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.SXOR, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                    case 1: // RC
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.RobertsCross, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                    case 2: // Perwitt
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.Perwitt, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                    case 3: // Sobel
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.Sobel, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                }
            }



            
        }

        private void trackBar_TP_EdgeDepth_Scroll(object sender, EventArgs e)
        {
            if (Toolpathing_Manager.EdgeAvailable)
            {
                switch (comboBox_TP_ED_List.SelectedIndex)
                {
                    case 0: // SXOR
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.SXOR ,trackBar_TP_EdgeDepth.Value);
                        break;
                    case 1: // RC
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.RobertsCross, trackBar_TP_EdgeDepth.Value);
                        break;
                    case 2: // Perwitt
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.Perwitt, trackBar_TP_EdgeDepth.Value);
                        break;
                    case 3: // Sobel
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.Sobel, trackBar_TP_EdgeDepth.Value);
                        break;
                }
            }
            else
            {
                switch (comboBox_TP_ED_List.SelectedIndex)
                {
                    case 0: // SXOR
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.SXOR, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                    case 1: // RC
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.RobertsCross, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                    case 2: // Perwitt
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.Perwitt, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                    case 3: // Sobel
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.Sobel, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                }
            }
        }

        private void comboBox_TP_ED_List_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox_TP_ED_List.SelectedIndex)
            {
                case 0: // SXOR
                    trackBar_TP_EdgeDepth.Maximum = 0;
                    trackBar_TP_EdgeDepth.Value = 0;
                    trackBar_TP_EdgeDepth.Enabled = false;
                    break;
                case 1: // RC
                    trackBar_TP_EdgeDepth.Maximum = 1;
                    if (trackBar_TP_EdgeDepth.Value > 1)
                    {
                        trackBar_TP_EdgeDepth.Value = 1;
                    }
                    trackBar_TP_EdgeDepth.Enabled = true;
                    break;
                case 2: // Perwitt
                    trackBar_TP_EdgeDepth.Maximum = 3;
                    if (trackBar_TP_EdgeDepth.Value > 3)
                    {
                        trackBar_TP_EdgeDepth.Value = 3;
                    }
                    trackBar_TP_EdgeDepth.Enabled = true;
                    break;
                case 3: // Sobel
                    trackBar_TP_EdgeDepth.Maximum = 2;
                    if (trackBar_TP_EdgeDepth.Value > 2)
                    {
                        trackBar_TP_EdgeDepth.Value = 2;
                    }
                    trackBar_TP_EdgeDepth.Enabled = true;
                    break;
            }

            if (Toolpathing_Manager.EdgeAvailable)
            {
                switch (comboBox_TP_ED_List.SelectedIndex)
                {
                    case 0: // SXOR
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.SXOR, 0);
                        break;
                    case 1: // RC
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.RobertsCross, trackBar_TP_EdgeDepth.Value);
                        break;
                    case 2: // Perwitt
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.Perwitt, trackBar_TP_EdgeDepth.Value);
                        break;
                    case 3: // Sobel
                        Toolpathing_Manager.ShowEdgeImage(IMG_Manager.EdgeDetectionType.Sobel, trackBar_TP_EdgeDepth.Value);
                        break;
                }
            }
            else
            {
                switch (comboBox_TP_ED_List.SelectedIndex)
                {
                    case 0: // SXOR
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.SXOR, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                    case 1: // RC
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.RobertsCross, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                    case 2: // Perwitt
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.Perwitt, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                    case 3: // Sobel
                        Toolpathing_Manager.ApplyEdge(IMG_Manager.EdgeDetectionType.Sobel, trackBar_TP_EdgeDepth.Value, trackBar_TP_ShowBWIMG.Value);
                        break;
                }
            }


        }

        private void button_TP_Start_Click(object sender, EventArgs e)
        {
            bool RemoveWhite = comboBox_TP_RemoveColor.SelectedIndex == 0 ? false : true;
            IMG_ToolPathing.ToolPathingType PathType;
            switch (comboBox_TP_PathType.SelectedIndex) {
                case 0:
                    PathType = IMG_ToolPathing.ToolPathingType.ZigZag;
                    break;
                case 1:
                    PathType = IMG_ToolPathing.ToolPathingType.Spiral;
                    break;
                case 2:
                    PathType = IMG_ToolPathing.ToolPathingType.SpiralHighQuality; // not implemented
                    break;
                default:
                    PathType = IMG_ToolPathing.ToolPathingType.Spiral;
                    break;

            }

            switch (comboBox_TP_Mode.SelectedIndex)
            {
                case 0: // BMP
                    Toolpathing_Manager.Start(RemoveWhite, PathType);
                    break;
                case 1: // SVG
                    Toolpathing_Manager.StartSVG(RemoveWhite, (trackBar_TP_ShowBWIMG.Value + 1) * 10, PathType, false);
                    break;
                case 2: // SVG Edge Only
                    Toolpathing_Manager.StartSVG(RemoveWhite, (trackBar_TP_ShowBWIMG.Value + 1) * 10, PathType, true);
                    break;
                
            }

            
        }

        private void button_TP_Cancel_Click(object sender, EventArgs e)
        {
            Toolpathing_Manager.Cancel();
        }

        private void button_TP_ShowToolpath_Click(object sender, EventArgs e)
        {
            if (Toolpathing_Manager.ToolPathAvailable)
            {
                if (comboBox_TP_Img_Selection.SelectedIndex == 0)
                {
                    Toolpathing_Manager.ShowToolPathImage(true);
                }
                else
                {
                    Toolpathing_Manager.ShowToolPathImage(false);
                }
            }
        }

        private void comboBox_TP_Img_Selection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Toolpathing_Manager.ToolPathAvailable)
            {
                if (comboBox_TP_Img_Selection.SelectedIndex == 0)
                {
                    Toolpathing_Manager.ShowToolPathImage(true);
                }
                else
                {
                    Toolpathing_Manager.ShowToolPathImage(false);
                }
            }
        }

        private void numericUpDown_TP_img_width_pp_ValueChanged(object sender, EventArgs e)
        {
            if(Math.Round(Toolpathing_Manager.IMG_Width,3) != Math.Round(Convert.ToDouble(numericUpDown_TP_img_width_pp.Value),3))
            {
                double change_ratio = Convert.ToDouble(numericUpDown_TP_img_width_pp.Value) / Toolpathing_Manager.IMG_Width;

                Toolpathing_Manager.IMG_Width = Convert.ToDouble(numericUpDown_TP_img_width_pp.Value);

                Toolpathing_Manager.IMG_Height *= change_ratio;
                numericUpDown_TP_img_height_pp.Value = Convert.ToDecimal(Toolpathing_Manager.IMG_Height);
            }

        }

        private void numericUpDown_TP_img_height_pp_ValueChanged(object sender, EventArgs e)
        {
            if (Math.Round(Toolpathing_Manager.IMG_Height,3) != Math.Round(Convert.ToDouble(numericUpDown_TP_img_height_pp.Value),3))
            {
                double change_ratio = Convert.ToDouble(numericUpDown_TP_img_height_pp.Value) / Toolpathing_Manager.IMG_Height;

                Toolpathing_Manager.IMG_Width = Convert.ToDouble(numericUpDown_TP_img_width_pp.Value);

                Toolpathing_Manager.IMG_Width *= change_ratio;
                numericUpDown_TP_img_width_pp.Value = Convert.ToDecimal(Toolpathing_Manager.IMG_Width);
            }
            
        }


        private delegate void TP_UpdateProgress_callback(int progress, string status);

        private void TP_UpdateProgress(int progress, string status)
        {
            if (progressBar_TP_Progress.InvokeRequired)
            {
                TP_UpdateProgress_callback d = new TP_UpdateProgress_callback(TP_UpdateProgress);
                this.progressBar_TP_Progress.Invoke(d, new object[] { progress, status });
                Application.DoEvents();
                return;
            }

            progressBar_TP_Progress.Value = progress;
            label_TP_Progress.Text = progress.ToString() + "%";

            label_TP_Status.Text = status;
        }




        // Configurations

        private delegate void CNC_BusyStateChanged_callbck(bool state);
        private void CNC_BusyStateChanged(bool state)
        {
            if (button_Configurations_Apply.InvokeRequired)
            {
                CNC_BusyStateChanged_callbck d = new CNC_BusyStateChanged_callbck(CNC_BusyStateChanged);
                this.button_Configurations_Apply.Invoke(d, new object[] { state });
                Application.DoEvents();
                return;
            }

            if (state)
            {
                button_Configurations_Apply.Enabled = false;
                button_Configurations_Cancel.Enabled = false;

                button_Configurations_Apply.BackColor = _LightGray;
                button_Configurations_Cancel.BackColor = _LightGray;
            }
            else
            {
                button_Configurations_Apply.Enabled = true;
                button_Configurations_Cancel.Enabled = true;

                button_Configurations_Apply.BackColor = _LightBlue;
                button_Configurations_Cancel.BackColor = _LightBlue;
            }

        }

        private void checkBox_waitForCNCAck_CheckedChanged(object sender, EventArgs e)
        {
            Machine.setWaitForOk(checkBox_waitForCNCAck.Checked);

            pictureBox_COM_Config.Image = checkBox_waitForCNCAck.Checked ? CNC_Pathfinder.Properties.Resources.gif_double_fast : CNC_Pathfinder.Properties.Resources.gif_single_fast;


        }

        private void button_Configurations_Apply_Click(object sender, EventArgs e)
        {
            // Update CNC.Configurations from the GUI
            Machine.Configurations.Axes_Step_Dimension.X = Convert.ToInt32(numericUpDown_Config_Axes_Steps_Dim_X.Value); // !!
            Machine.Configurations.Axes_Step_Dimension.Y = Convert.ToInt32(numericUpDown_Config_Axes_Steps_Dim_Y.Value); // !!
            Machine.Configurations.Axes_Step_Dimension.Z = Convert.ToInt32(numericUpDown_Config_Axes_Steps_Dim_Z.Value);
            Machine.Configurations.Axes_Units_Dimension.X = Convert.ToDouble(numericUpDown_Config_Axes_Units_Dim_X.Value);
            Machine.Configurations.Axes_Units_Dimension.Y = Convert.ToDouble(numericUpDown_Config_Axes_Units_Dim_Y.Value);
            Machine.Configurations.Axes_Units_Dimension.Z = Convert.ToDouble(numericUpDown_Config_Axes_Units_Dim_Z.Value);
            Machine.Configurations.StepResolution.X = Convert.ToDouble(Machine.Configurations.Axes_Step_Dimension.X / Machine.Configurations.Axes_Units_Dimension.X);
            Machine.Configurations.StepResolution.Y = Convert.ToDouble(Machine.Configurations.Axes_Step_Dimension.Y / Machine.Configurations.Axes_Units_Dimension.Y);
            Machine.Configurations.StepResolution.Z = Convert.ToDouble(Machine.Configurations.Axes_Step_Dimension.Z / Machine.Configurations.Axes_Units_Dimension.Z);

            Machine.Configurations.MinFeedrate = Convert.ToDouble(numericUpDown_Config_Feedrate_Min.Value); // !!
            Machine.Configurations.MaxFeedrate = Convert.ToDouble(numericUpDown_Config_Feedrate_Max.Value); // !!

            Machine.Configurations.laser_maxpower = Convert.ToInt32(numericUpDown_Config_LaserPower_Max.Value); // !!

            Machine.Configurations._Units_Mode = (CNC_Properties.Units_Mode)comboBox_Config_Units.SelectedIndex;
            Machine.Configurations._Home_Position = (CNC_Properties.Home_Position)comboBox_Config_HomePosition.SelectedIndex; // !!
            
            Machine.Configurations.CNC_Multiblock_BufferSize = Convert.ToInt32(numericUpDown_Config_Multiblocks_Size.Value);

            Machine.Configurations._ToolType = CNC_Properties.ToolType.Laser;//(CNC_Properties.ToolType)comboBox_Config_ToolType.SelectedIndex;
            Machine.Configurations.laser_power_fill = Convert.ToInt32(numericUpDown_Config_Laser_Power_Cut.Value);
            Machine.Configurations.laser_power_idle = Convert.ToInt32(numericUpDown_Config_Laser_Power_Idle.Value);
            Machine.Configurations.drill_spindle = 0;// Convert.ToInt32(numericUpDown_Config_Drill_Speed.Value);
            Machine.Configurations.drill_down_height = 0;// Convert.ToDouble(numericUpDown_Config_Drill_Down_height.Value);
            Machine.Configurations.drill_up_height = 0;// Convert.ToDouble(numericUpDown_Config_Drill_Up_height.Value);
            Machine.Configurations.RapidFeedrate = Convert.ToInt32(numericUpDown_Config_Feedrate_Rapid.Value);
            Machine.Configurations.LinearFeedrate = Convert.ToInt32(numericUpDown_Config_Feedrate_Linear.Value);
            Machine.Configurations.tool_diameter_contour = Convert.ToDouble(numericUpDown_Config_Tool_Diam_Precision.Value);
            Machine.Configurations.tool_diameter_fill = Convert.ToDouble(numericUpDown_Config_Tool_Diam_Rough.Value);

            Machine.Configurations.ContourFeedrate = Convert.ToDouble(numericUpDown_Config_Feedrate_Contour.Value);
            Machine.Configurations.laser_power_contour = Convert.ToInt32(numericUpDown_Config_Laser_Power_Contour.Value);

            // deprecated
            Machine.Configurations.laser_height_precision = 0;// Convert.ToDouble(numericUpDown_Config_Laser_height_precision.Value);
            Machine.Configurations.laser_height_roughing = 0;// Convert.ToDouble(numericUpDown_Config_Laser_height_rough.Value);


            // Save the configurations
            CNC_Settings.Save(comboBox_Configuration_baudrate.SelectedIndex, comboBox_Configuration_LineTerminator.SelectedIndex, checkBox_waitForCNCAck.CheckState == CheckState.Checked? true : false );

            // update home/current possition (just the number no the location) + numericboxes limits of feedrate and laser power + CNC panel ratio (X/Y)
            // *if connected    (still update panel x/y and numericboxes while off)
            // + Costume_Home_Position
            Config_UpdateComponents();

        }

        private void button_Configurations_Cancel_Click(object sender, EventArgs e)
        {
            CNC_Settings.Cancel();
        }

        private void Config_GUI_Update()
        {
            // update de configurations in GUI from CNC.Configs
            numericUpDown_Config_Axes_Steps_Dim_X.Value = Machine.Configurations.Axes_Step_Dimension.X;
            numericUpDown_Config_Axes_Steps_Dim_Y.Value = Machine.Configurations.Axes_Step_Dimension.Y;
            numericUpDown_Config_Axes_Steps_Dim_Z.Value = Machine.Configurations.Axes_Step_Dimension.Z;
            numericUpDown_Config_Axes_Units_Dim_X.Value = Convert.ToDecimal(Machine.Configurations.Axes_Units_Dimension.X);
            numericUpDown_Config_Axes_Units_Dim_Y.Value = Convert.ToDecimal(Machine.Configurations.Axes_Units_Dimension.Y);
            numericUpDown_Config_Axes_Units_Dim_Z.Value = Convert.ToDecimal(Machine.Configurations.Axes_Units_Dimension.Z);
            //numericUpDown_Config_Axes_Steps_Res_X.Value = Convert.ToDecimal(numericUpDown_Config_Axes_Steps_Dim_X.Value / numericUpDown_Config_Axes_Units_Dim_X.Value);
            //numericUpDown_Config_Axes_Steps_Res_Y.Value = Convert.ToDecimal(numericUpDown_Config_Axes_Steps_Dim_Y.Value / numericUpDown_Config_Axes_Units_Dim_Y.Value);
            //numericUpDown_Config_Axes_Steps_Res_Z.Value = Convert.ToDecimal(numericUpDown_Config_Axes_Steps_Dim_Z.Value / numericUpDown_Config_Axes_Units_Dim_Z.Value);

            numericUpDown_Config_Feedrate_Min.Value = Convert.ToDecimal(Machine.Configurations.MinFeedrate);
            numericUpDown_Config_Feedrate_Max.Value = Convert.ToDecimal(Machine.Configurations.MaxFeedrate);

            numericUpDown_Config_LaserPower_Max.Value = Machine.Configurations.laser_maxpower;

            comboBox_Config_Units.SelectedIndex = (int)Machine.Configurations._Units_Mode;

            if(Machine.Configurations._Home_Position == CNC_Properties.Home_Position.Costume)
            {
                comboBox_Config_HomePosition.SelectedIndex = (int)CNC_Properties.Home_Position.Center;
            }
            else
            {
                comboBox_Config_HomePosition.SelectedIndex = (int)Machine.Configurations._Home_Position;
            }
            
            
            numericUpDown_Config_Multiblocks_Size.Value = Machine.Configurations.CNC_Multiblock_BufferSize;

            //comboBox_Config_ToolType.SelectedIndex = (int)Machine.Configurations._ToolType;
            numericUpDown_Config_Laser_Power_Cut.Value = Machine.Configurations.laser_power_fill;
            numericUpDown_Config_Laser_Power_Idle.Value = Machine.Configurations.laser_power_idle;
            //numericUpDown_Config_Drill_Speed.Value = Machine.Configurations.drill_spindle;
            //numericUpDown_Config_Drill_Down_height.Value = Convert.ToDecimal(Machine.Configurations.drill_down_height);
            //numericUpDown_Config_Drill_Up_height.Value = Convert.ToDecimal(Machine.Configurations.drill_up_height);
            numericUpDown_Config_Feedrate_Rapid.Value = Convert.ToDecimal(Machine.Configurations.RapidFeedrate);
            numericUpDown_Config_Feedrate_Linear.Value = Convert.ToDecimal(Machine.Configurations.LinearFeedrate);
            numericUpDown_Config_Tool_Diam_Precision.Value = Convert.ToDecimal(Machine.Configurations.tool_diameter_contour);
            numericUpDown_Config_Tool_Diam_Rough.Value = Convert.ToDecimal(Machine.Configurations.tool_diameter_fill);
            //numericUpDown_Config_Laser_height_precision.Value = Convert.ToDecimal(Machine.Configurations.laser_height_precision);
            //numericUpDown_Config_Laser_height_rough.Value = Convert.ToDecimal(Machine.Configurations.laser_height_roughing);

            numericUpDown_Config_Feedrate_Contour.Value = Convert.ToDecimal(Machine.Configurations.ContourFeedrate);
            numericUpDown_Config_Laser_Power_Contour.Value = Machine.Configurations.laser_power_contour;

            // update home/current possition (just the number no the location) + numericboxes limits of feedrate and laser power + CNC panel ratio (X/Y)
            // *if connected    (still update panel x/y and numericboxes while off)
            // + Costume_Home_Position
            Config_UpdateComponents();
        }

        private void Config_UpdateComponents()
        {
            // update home/current possition (just the number no the location) + numericboxes limits of feedrate and laser power + CNC panel ratio (X/Y)
            // *if connected    (still update panel x/y and numericboxes while off)
            // + Costume_Home_Position

            // Connected + Disconnected Updates
            numericUpDown_ControlPanel_feedrate.Maximum = numericUpDown_Config_Feedrate_Max.Value;
            trackBar_ControlPanel_feedrate.Maximum = Convert.ToInt32(numericUpDown_Config_Feedrate_Max.Value);
            numericUpDown_ControlPanel_feedrate.Minimum = numericUpDown_Config_Feedrate_Min.Value;
            trackBar_ControlPanel_feedrate.Minimum = Convert.ToInt32(numericUpDown_Config_Feedrate_Min.Value);

            numericUpDown_ControlPanel_laser.Maximum = Machine.Configurations.laser_maxpower;
            trackBar_ControlPanel_laser.Maximum = Machine.Configurations.laser_maxpower;

            // panel ratio
            Resize_XY_Panel();

            // Units update

            label_ConfigXDim.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Dimension [mm]:" : "Dimension [inch]:";
            label_ConfigYDim.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Dimension [mm]:" : "Dimension [inch]:";
            label_ConfigZDim.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Dimension [mm]:" : "Dimension [inch]:";

            label_ConfigXRes.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Resolution [step/mm]:" : "Resolution [step/inch]:";
            label_ConfigYRes.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Resolution [step/mm]:" : "Resolution [step/inch]:";
            label_ConfigZRes.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Resolution [step/mm]:" : "Resolution [step/inch]:";

            label_ConfigMaxFeed.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Max. Feedrate [mm/min]:" : "Max. Feedrate [inch/min]:";
            label_ConfigMinFeed.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Min. Feedrate [mm/min]:" : "Min. Feedrate [inch/min]:";

            label_ControlPanel_distance.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Distance [mm]:" : "Distance [inch]:";
            label_ControlPanel_feedrate.Text = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Feedrate [mm/min]:" : "Feedrate [inch/min]:";

            comboBox_Config_Units.SelectedItem = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? "Metric" : "Imperial";

            if (Machine.Connection_Status) // Connected only
            {
                string units = Machine.Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? GCODE.Units_Metric : GCODE.Units_Imperial;
                /*
                string home = "";
                switch (Machine.Configurations._Home_Position)
                {
                    case CNC_Properties.Home_Position.BottomLeft:
                        home = GCODE.Set_Home_BottomLeft;
                        break;
                    case CNC_Properties.Home_Position.BottomRight:
                        home = GCODE.Set_Home_BottomRight;
                        break;
                    case CNC_Properties.Home_Position.TopLeft:
                        home = GCODE.Set_Home_TopLeft;
                        break;
                    case CNC_Properties.Home_Position.TopRight:
                        home = GCODE.Set_Home_TopRight;
                        break;
                    default:
                        home = GCODE.Set_Home_Center;
                        break;
                }

                Machine.printFile(new string[] { units, home });
                */
                Machine.sendCMD(units);
            }
            

        }

        private void Config_COM_Update(int baudrateIndex, int lineterminatorIndex, bool waitforok)
        {
            comboBox_Configuration_baudrate.SelectedIndex = baudrateIndex;
            comboBox_Configuration_LineTerminator.SelectedIndex = lineterminatorIndex;
            checkBox_waitForCNCAck.CheckState = waitforok ? CheckState.Checked : CheckState.Unchecked;
        }

        private void numericUpDown_Config_Axes_Units_Dim_X_ValueChanged(object sender, EventArgs e)
        {
            label_Config_Axes_Steps_Res_X.Text = Math.Round(numericUpDown_Config_Axes_Steps_Dim_X.Value / numericUpDown_Config_Axes_Units_Dim_X.Value , 4).ToString();
        }

        private void numericUpDown_Config_Axes_Steps_Dim_X_ValueChanged(object sender, EventArgs e)
        {
            label_Config_Axes_Steps_Res_X.Text = Math.Round(numericUpDown_Config_Axes_Steps_Dim_X.Value / numericUpDown_Config_Axes_Units_Dim_X.Value, 4).ToString();
        }

        private void numericUpDown_Config_Axes_Units_Dim_Y_ValueChanged(object sender, EventArgs e)
        {
            label_Config_Axes_Steps_Res_Y.Text = Math.Round(numericUpDown_Config_Axes_Steps_Dim_Y.Value / numericUpDown_Config_Axes_Units_Dim_Y.Value, 4).ToString();
        }

        private void numericUpDown_Config_Axes_Steps_Dim_Y_ValueChanged(object sender, EventArgs e)
        {
            label_Config_Axes_Steps_Res_Y.Text = Math.Round(numericUpDown_Config_Axes_Steps_Dim_Y.Value / numericUpDown_Config_Axes_Units_Dim_Y.Value, 4).ToString();
        }

        private void numericUpDown_Config_Axes_Units_Dim_Z_ValueChanged(object sender, EventArgs e)
        {
            label_Config_Axes_Steps_Res_Z.Text = Math.Round(numericUpDown_Config_Axes_Steps_Dim_Z.Value / numericUpDown_Config_Axes_Units_Dim_Z.Value, 4).ToString();
        }

        private void numericUpDown_Config_Axes_Steps_Dim_Z_ValueChanged(object sender, EventArgs e)
        {
            label_Config_Axes_Steps_Res_Z.Text = Math.Round(numericUpDown_Config_Axes_Steps_Dim_Z.Value / numericUpDown_Config_Axes_Units_Dim_Z.Value, 4).ToString();
        }

        private void button_AdvancedOptions_Click(object sender, EventArgs e)
        {
            panel_AdvancedOptions.Visible = !panel_AdvancedOptions.Visible;
            panel_AdvancedOptions.Location = new Point(2, button_AdvancedOptions.Location.Y + 40);

            button_AdvancedOptions.Text = panel_AdvancedOptions.Visible ? "Hide Advanced Options" : "Show Advanced Options";

        }

        private void checkBoxGenReport_CheckedChanged(object sender, EventArgs e)
        {
            Toolpathing_Manager.Generate_Report = checkBoxGenReport.Checked;
        }

 


        //


    }
}
