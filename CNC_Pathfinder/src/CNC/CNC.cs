using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.Drawing;
// Costum namespaces
using CNC_Pathfinder.src.Communication;
using CNC_Pathfinder.src.GCode;
using CNC_Pathfinder.src.Utilities;
using System.IO;

namespace CNC_Pathfinder.src.CNC
{
    public class CNC : COM_Port
    {

        #region Parameters & Local Variables

        private NumberFormatInfo nfi = new NumberFormatInfo();
        
        /// <summary>
        /// Structure that stores all the properties of the CNC and its tool.
        /// </summary>
        public CNC_Properties Configurations { get; private set; }

        // COM vars

        /// <summary>
        /// Used in certain operations like checking the cnc status. Prevents blocking in case of disconnection or cnc fault.
        /// </summary>
        private int COM_timeout = 2000;

        /// <summary>
        /// Number of tries to send a valid messsage, if an GCODE.nack is received.
        /// </summary>
        private int resend_attempts = 5;

        private const int Image_Side_Size_Limit = 1000; // in pixeis

        private Pen redPen = new Pen(Color.Red, 2);

        private Pen blackPen = new Pen(Color.Black, 2);

        /// <summary>
        /// Backgroundworker for the simulation of G-Code files.
        /// </summary>
        private BackgroundWorker sim_backgroundwroker;

        /// <summary>
        /// Backgroundworker for the communication.
        /// </summary>
        private BackgroundWorker cnc_backgroundwroker;

        /// <summary>
        /// Backgroundworker pauser/resumer.
        /// </summary>
        private ManualResetEvent cnc_backgroundwroker_pauser;

        private bool wasPaused = false;

        private string error_log_location;

        private const int ERROR_LIST_CHUNK_SIZE = 256;
        #endregion

        #region Inner Classes

        /// <summary>
        /// Inner class that defines the CNC exceptions.
        /// </summary>
        public class CNCException : Exception
        {
            public enum CNC_Error
            {
                busy = 0,
                off = 1,
                hardware_fault = 2,
                invalid_message_received = 3,
                invalid_message_sent = 4
            }

            public string cmd { get; private set; }

            public CNC_Error error { get; private set; }

            public CNCException(CNC_Error error)
            {
                this.error = error;
            }

            public CNCException(string cmd)
            {
                this.cmd = cmd;
                
            }

            public CNCException(CNC_Error error, string cmd)
            {
                this.error = error;
                this.cmd = cmd;
                
            }
        }

        #endregion

        #region Constructores

        public CNC() : base()
        {
            initializeConfigurations();


            cnc_backgroundwroker_pauser = new ManualResetEvent(true);

            this.MessageReceived += new EventHandler(asynchronous_ReceiveControl);

            this.Connected += new EventHandler(CNC_onConnect);
            this.Disconnected += new EventHandler(CNC_onDisconnect);

            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = "";
        }

        private void init_bgw()
        {
            cnc_backgroundwroker = new BackgroundWorker();
            cnc_backgroundwroker.WorkerReportsProgress = true;
            cnc_backgroundwroker.WorkerSupportsCancellation = true;
            cnc_backgroundwroker.DoWork += new DoWorkEventHandler(cnc_backgroundworker_DoWork);
            cnc_backgroundwroker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(cnc_backgroundworker_RunWorkerCompleted);
            cnc_backgroundwroker.ProgressChanged += new ProgressChangedEventHandler(cnc_backgroundworker_ProgressChanged);

            sim_backgroundwroker = new BackgroundWorker();
            sim_backgroundwroker.WorkerReportsProgress = true;
            sim_backgroundwroker.WorkerSupportsCancellation = true;
            sim_backgroundwroker.DoWork += new DoWorkEventHandler(sim_backgroundworker_DoWork);
            sim_backgroundwroker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(sim_backgroundworker_RunWorkerCompleted);
            sim_backgroundwroker.ProgressChanged += new ProgressChangedEventHandler(sim_backgroundworker_ProgressChanged);

        }

        private void dispose_bgw()
        {
            try
            {
                cnc_backgroundwroker.Dispose();
            }
            catch (Exception e)
            {

            }

            try
            {
                sim_backgroundwroker.Dispose();
            }
            catch (Exception e)
            {

            }
        }

        private void CNC_onConnect(object sender, EventArgs e)
        {
            init_bgw();
        }

        protected void CNC_onDisconnect(object sender, EventArgs e)
        {
            dispose_bgw();
        }

        #endregion

        #region Configurations

        private void initializeConfigurations()
        {
            Configurations = new CNC_Properties();
            Configurations.Axes_Step_Dimension = new CNC_Properties.Coordinates(880000,660000,220000); // default values
            Configurations.Axes_Units_Dimension = new CNC_Properties.CoordinatesD(400.0,300.0,100.0); // default values
            Configurations.StepResolution = new CNC_Properties.CoordinatesD(2200, 2200, 2200); // default values
            Configurations.Costume_Home_Position = new CNC_Properties.Coordinates(0,0,0);

            //Configurations.waitForOk = true;
            Configurations.current_location = new CNC_Properties.CoordinatesD(0, 0, 0); // rt_config
            Configurations.toolActive = false; // rt_config
            Configurations.motorsActive = true; // rt_config

            Configurations._Coordinates_Mode = CNC_Properties.Coordinates_Mode.Relative;
            //Configurations._Units_Mode = CNC_Properties.Units_Mode.Metric;
            Configurations._Arc_Plane_Mode = CNC_Properties.Arc_Plane_Mode.Plane_XY;
            //Configurations._Feedrate_Mode = CNC_Properties.Feedrate_Mode.UnitsPerMinute;
            //Configurations._Home_Position = CNC_Properties.Home_Position.Center;

            //Configurations.Costume_Home_Position = new CNC_Properties.Coordinates(0,0, Configurations.Axes_Step_Dimension.Z / 2);

            //Configurations.MinFeedrate = 0.028; // mm/min
            //Configurations.MaxFeedrate = 1300.0; // mm/min

            Configurations.CNC_Multiblocks_Active = false; // rt_config
            //Configurations.CNC_Multiblock_BufferSize = 150; 
            
            //Configurations.laser_maxpower = 2500;

            Configurations.Current_Feedrate = 500;
            Configurations.Current_LaserPower = 1;

            // (For Toolpathing) Temporary
            //Configurations.LinearFeedrate = 1000; 
            //Configurations.RapidFeedrate = 1300; 
            //Configurations.tool_diameter_precision = 0.2;  
            //Configurations.laser_power_cut = 2000;
            //Configurations.laser_power_idle = 1;
            Configurations.TP_Coordinates_Mode = CNC_Properties.Coordinates_Mode.Absolute;
            Configurations.TP_Units_Mode = CNC_Properties.Units_Mode.Metric;

        }

        private string[] getConfigMsg()
        {
            string[] init_Configurations = new string[9];

            init_Configurations[0] = GCODE.TurnOff_Multiblocs;
            //init_Configurations[1] = GCODE.SetMultiblocks_Size + Configurations.CNC_Multiblock_BufferSize.ToString();
            init_Configurations[1] = GCODE.Units_Metric;
            init_Configurations[2] = GCODE.Distance_Relative;
            init_Configurations[3] = Configurations._Feedrate_Mode == CNC_Properties.Feedrate_Mode.UnitsPerMinute ? GCODE.Feedrate_UnitsPerMinute : GCODE.Feedrate_InverseTime;
            switch (Configurations._Home_Position)
            {
                case CNC_Properties.Home_Position.BottomLeft:
                    init_Configurations[4] = GCODE.Set_Home_BottomLeft;
                    break;
                case CNC_Properties.Home_Position.BottomRight:
                    init_Configurations[4] = GCODE.Set_Home_BottomRight;
                    break;
                case CNC_Properties.Home_Position.TopLeft:
                    init_Configurations[4] = GCODE.Set_Home_TopLeft;
                    break;
                case CNC_Properties.Home_Position.TopRight:
                    init_Configurations[4] = GCODE.Set_Home_TopRight;
                    break;
                case CNC_Properties.Home_Position.Center:
                    init_Configurations[4] = GCODE.Set_Home_Center;
                    break;
                default: // costum home set to bottom left again
                    init_Configurations[4] = GCODE.Set_Home_BottomLeft;
                    break;
            }
            init_Configurations[5] = GCODE.Tool_Off;
            // motor on to move to current position then set to off
            init_Configurations[6] = GCODE.Motor_On;
            init_Configurations[7] = GCODE.Set_Positioning_Movement + " X0 Y0 Z0 F" + Configurations.MaxFeedrate;
            init_Configurations[8] = GCODE.Set_Laser_Power + " E" + 1;

            return init_Configurations;
        }

        public bool sendConfigurations()
        {
            return printFile(getConfigMsg());
        }

        public void setWaitForOk(bool waitforOk)
        {
            Configurations.waitForOk = waitforOk;
        }



        #endregion


        #region G-Code Commands

        public void GetCurrentPositio()
        {
            sendCMD(GCODE.Get_Current_Position);
        }

        /// <summary>
        /// Move all axes to home position.
        /// </summary>
        public void Home()
        {
            Home(true, true, true);
        }

        /// <summary>
        /// Move to home position in one or more axes. 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Home(bool x, bool y, bool z)
        {
            if(!Connection_Status || (!x && !y && !z))
            {
                return;
            }

            if(x && y && z)
            {
                sendCMD(GCODE.Home_Movement);
                return;
            }

            StringBuilder cmd = new StringBuilder();
            cmd.Append(GCODE.Home_Movement);
            if (x)
            {
                cmd.Append(" X");
            }
            if (y)
            {
                cmd.Append(" Y");
            }
            if (z)
            {
                cmd.Append(" Z");
            }

            sendCMD(cmd.ToString());
        }
        
        public void Move(double x, double y, double z, double feedrate)
        {
            if((x == 0 && y == 0 && z == 0) || feedrate == 0 || !Configurations.motorsActive)
            {
                return;
            }

            StringBuilder cmd = new StringBuilder();

            cmd.Append(GCODE.Linear_Movement);

            if(x != 0)
            {
                cmd.Append(" X" + x);
            }
            if (y != 0)
            {
                cmd.Append(" Y" + y);
            }
            if (z != 0)
            {
                cmd.Append(" Z" + z);
            }

            cmd.Append(" F" + feedrate);

            sendCMD( cmd.ToString().Replace(',','.') );
        }
        
        public void Move(double x, double y, double z, double feedrate, double toolPower)
        {
            if ((x == 0 && y == 0 && z == 0) || feedrate == 0 || !Configurations.motorsActive)
            {
                return;
            }

            StringBuilder cmd = new StringBuilder();

            cmd.Append(GCODE.Linear_Movement);

            if (x != 0)
            {
                cmd.Append(" X" + x);
            }
            if (y != 0)
            {
                cmd.Append(" Y" + y);
            }
            if (z != 0)
            {
                cmd.Append(" Z" + z);
            }

            cmd.Append(" F" + feedrate);

            if (Configurations.toolActive)
            {
                cmd.Append(" E" + toolPower);
            }

            sendCMD(cmd.ToString().Replace(',', '.'));
        }

        public void SetHome()
        {
            sendCMD(GCODE.Set_Home);
        }

        public void toggleTool()
        {
            StringBuilder cmd = new StringBuilder();
            if (Configurations.toolActive)
            {
                cmd.Append(GCODE.Tool_Off);
                //Configurations.toolActive = false;
            }
            else
            {
                cmd.Append(GCODE.Tool_On);
                //Configurations.toolActive = true;
            }

            sendCMD(cmd.ToString());
        }

        
        public void toggleMotors()
        {
            StringBuilder cmd = new StringBuilder();
            if (Configurations.motorsActive)
            {
                cmd.Append(GCODE.Motor_Off);
                //Configurations.motorsActive = false;
            }
            else
            {
                cmd.Append(GCODE.Motor_On);
                //Configurations.motorsActive = true;
            }

            sendCMD(cmd.ToString());
        }

        public void Reset()
        {
            sendCMD(GCODE.CNC_Reset);
        }

        public void Calibrate()
        {
            sendCMD(GCODE.CNC_Calibration);
        }

        public void setLaserPower(double power)
        {
            //if (Configurations.toolActive)
            //{
                sendCMD(GCODE.Set_Laser_Power + " E" + power.ToString().Replace(',', '.'));
            //}
        }
        
        public void setMultiblock(bool b)
        {
            if (b)
            {
                sendCMD(GCODE.TurnOn_Multiblocs);
            }
            else
            {
                sendCMD(GCODE.TurnOff_Multiblocs);
            }

        }

        public void setMultiblock_size(int s)
        {
            sendCMD(GCODE.SetMultiblocks_Size + s);
        }

        #endregion

        #region Backgroundworker controls


        public delegate void CNC_BusyStateChange(bool state);
        public CNC_BusyStateChange BusyStateChange;


        /// <summary>
        /// Checks if the backgroundworker is busy.
        /// </summary>
        /// <returns>True if the backgroundworker is busy. False otherwise.</returns>
        public bool isBusy()
        {
            return cnc_backgroundwroker.IsBusy; // or single cmd is waiting
        }

        /// <summary>
        /// Send a single command to the CNC.
        /// </summary>
        /// <param name="cmd"></param>
        public void sendCMD(string cmd)
        {
            if(cmd == null || cmd.Length < 2)
            {
                return;
            }
            if (!cnc_backgroundwroker.IsBusy && Connection_Status)
            {
                Resume();
                BusyStateChange?.Invoke(true);
                cnc_backgroundwroker.RunWorkerAsync(new string[] { cmd });
            }

        }

        /// <summary>
        /// Send multiple commands to the CNC.
        /// </summary>
        /// <param name="file"></param>
        public bool printFile(string[] file)
        {
            if(file.Length == 0) // empty msg 
            {
                return false;
            }

            if (!cnc_backgroundwroker.IsBusy && Connection_Status)
            {
                Resume();
                BusyStateChange?.Invoke(true);
                cnc_backgroundwroker.RunWorkerAsync(file);
                return true;
            }

            return false;
            
        }

        public void simulateFile(string[] file, string error_log_location) 
        {
            // criar um backgroundworker ou usar uma thread ? cancelavel? update progress bar states: loading, simulating?
            if (file.Length == 0) // empty msg 
            {
                return;
            }

            if (!sim_backgroundwroker.IsBusy)
            {
                this.error_log_location = error_log_location;
                sim_backgroundwroker.RunWorkerAsync(file);
            }
        }

        public void Cancel_Sim()
        {
            sim_backgroundwroker.CancelAsync();
        }

        /// <summary>
        /// Cancel the current backgroundworker opperation.
        /// </summary>
        public void Cancel()
        {
            if (!isBusy() && Connection_Status)
            {
                Send(GCODE.Emergency_Stop);
                calculatePositionAndConfigs(GCODE.Program_Stop);
                return;
            }

            try
            {
                cnc_backgroundwroker.CancelAsync();
                if (wasPaused)
                {
                    Resume();
                }
            }
            catch(Exception e)
            {
                
            }
        }

        /// <summary>
        /// Resumes the current backgroundworker opperation.
        /// </summary>
        public void Resume()
        {
            cnc_backgroundwroker_pauser.Set();
        }

        /// <summary>
        /// Pauses the current backgroundworker opperation.
        /// </summary>
        public void Pause()
        {
            cnc_backgroundwroker_pauser.Reset();
            Send(GCODE.Program_Pause);
            calculatePositionAndConfigs(GCODE.Program_Pause);
            wasPaused = true;
        }

        #endregion

        #region Backgroundworker events

        private void cnc_backgroundworker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            string[] file = (e.Argument as string[]).Clone() as string [];

            // background operation goes here
            int progress = 0;
            
            try
            {
                cleanInBuffer();
            }
            catch(Exception ex)
            {
                throw new CNCException(CNCException.CNC_Error.off);
            }

            string cnc_msg;
            bool reset_attempt = false;

            cnc_backgroundwroker_pauser = new ManualResetEvent(true);

            string[] init_configs = getConfigMsg();

            if(file.Length > 1 && file[0] != init_configs[0])
            {
                file = file.Concat(init_configs).ToArray(); // add init configs at the end
            }

            // Send file/cmd
            for (int i = 0; i < file.Length; i++)
            {
                // If the operation was canceled by the user, 
                // set the DoWorkEventArgs.Cancel property to true.
                if (bw.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                cnc_backgroundwroker_pauser.WaitOne(); // check if program paused backgroundworker

                // Check progress change
                if ((i * 100) / file.Length != progress)
                {
                    // Update the progress
                    progress = (i * 100) / file.Length;
                    if(file.Length > 1)
                    {
                        bw.ReportProgress(progress);
                    }
                    
                }

                reset_attempt = (file[i] == GCODE.CNC_Reset);

                if (Configurations.waitForOk)
                {

                    for (int attempt = 0; attempt < resend_attempts; attempt++)
                    {
                        // send cmd
                        if (!Send(file[i]))
                        {
                            throw new CNCException(CNCException.CNC_Error.off);
                        }


                        // wait response. this process is cancelable or pausable.
                        while (!Get(out cnc_msg, COM_timeout))
                        {
                            if (bw.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }
                            cnc_backgroundwroker_pauser.WaitOne(); // check if program paused backgroundworker
                            if (wasPaused)
                            {
                                wasPaused = false;
                                while (!Get(out cnc_msg, COM_timeout)) // get the ok from the pause out of the queue
                                {
                                    if (bw.CancellationPending)
                                    {
                                        e.Cancel = true;
                                        return;
                                    }
                                }
                            }
                        }


                        //if (i == file.Length - 1) // last cmd => store result
                        //{
                            e.Result = cnc_msg;
                        //}

                        // check if message is valid
                        if (cnc_msg.Length < 2)
                        {
                            throw new CNCException(CNCException.CNC_Error.invalid_message_received, cnc_msg);
                        }

                        if (cnc_msg.Contains(GCODE.cnc_start)) // cnc just booted up
                        {
                            Disconnect(); // force a restart from the application to send the initial configurations
                            return;
                            //continue; // retry to get the ok
                        }

                        string msg = cnc_msg.Substring(0, 2);// (char)cnc_msg[0] + (char)cnc_msg[1] + string.Empty;


                        if (msg == GCODE.ack)
                        {
                            // calculate + update position => send that position on ReportProgress(progress, position) if position changed!
                            calculatePositionAndConfigs(file[i]);

                            break; // skip the resend cicle
                        }
                        else if (msg == GCODE.nack)
                        {
                            // Try again
                            if (attempt == resend_attempts - 1) // last attampt
                            {
                                throw new CNCException(CNCException.CNC_Error.invalid_message_sent, file[i]);
                            }
                            continue;
                        }
                        else if (msg == GCODE.info_start)
                        {
                            break;
                        }
                        else if (msg == GCODE.hardware_fault)
                        {
                            // warn app!
                            throw new CNCException(CNCException.CNC_Error.hardware_fault);
                        }
                        else if (msg == GCODE.cnc_start)
                        {
                            // Try again
                            if (attempt == resend_attempts - 1) // last attampt
                            {
                                throw new CNCException(CNCException.CNC_Error.invalid_message_sent, file[i]);
                            }
                            continue;
                        }
                        else
                        {
                            // abort and disconnect port
                            throw new CNCException(CNCException.CNC_Error.invalid_message_received, msg);
                        }

                    }
                }
                else
                {
                    // send cmd
                    if (!Send(file[i]))
                    {
                        throw new CNCException(CNCException.CNC_Error.off);
                    }

                    // calculate + update position => send that position on ReportProgress(progress, position) if position changed!
                    calculatePositionAndConfigs(file[i]);


                }

                if (reset_attempt)
                {
                    // wait for start
                    if (Configurations.waitForOk)
                    {
                        while (!Get(out cnc_msg, COM_timeout))
                        {
                            if (bw.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }
                            cnc_backgroundwroker_pauser.WaitOne(); // check if program paused backgroundworker
                            if (wasPaused)
                            {
                                wasPaused = false;
                                while (!Get(out cnc_msg, COM_timeout)) // get the ok from the pause out of the queue
                                {
                                    if (bw.CancellationPending)
                                    {
                                        e.Cancel = true;
                                        return;
                                    }
                                }
                            }
                        }

                        // if start send configurations
                        if (cnc_msg.Contains(GCODE.cnc_start))
                        {
                            List<string> new_file = getConfigMsg().ToList<string>();

                            List<string> file_continuation = file.ToList<string>();

                            file_continuation.RemoveRange(0, i + 1);

                            new_file.AddRange(file_continuation);

                            file = new_file.ToArray();
                            i = -1;
                        }
                        else
                        {
                            throw new CNCException(CNCException.CNC_Error.invalid_message_received, cnc_msg);
                        }
                    }
                    else
                    {

                        List<string> new_file = getConfigMsg().ToList<string>();

                        List<string> file_continuation = file.ToList<string>();

                        file_continuation.RemoveRange(0,i + 1);

                        new_file.AddRange(file_continuation);
                        
                        file = new_file.ToArray();
                        i = -1;
                    }


                    reset_attempt = false;
                }


            }
            if (file.Length > 1)
            {
                bw.ReportProgress(100);
            }
            // end of background operation ---
            
        }

        private void cnc_backgroundworker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BusyStateChange?.Invoke(false);
            if (e.Cancelled)
            {
                if (Connection_Status)
                {
                    // STOP cnc 
                    Send(GCODE.Program_Stop);
                    calculatePositionAndConfigs(GCODE.Program_Stop);

                    // verificar ok 
                }

                if(backgroundworker_finished != null)
                {
                    backgroundworker_finished();
                }

                return;
            }

            if (e.Error != null)
            {
                // warn the error
                CNCException err = e.Error as CNCException;
                
                switch (err.error)
                {
                    case CNCException.CNC_Error.busy: // not used
                        //...
                        break;
                    case CNCException.CNC_Error.off:
                        Disconnect();
                        return;
                    case CNCException.CNC_Error.hardware_fault:
                        Disconnect();
                        return;
                    case CNCException.CNC_Error.invalid_message_received:
                        if(printWarning != null && err.cmd != null)
                        {
                            printWarning("<<Invalid message received: '" + err.cmd + "'>>");
                        }
                        break;
                    case CNCException.CNC_Error.invalid_message_sent:
                        if (printWarning != null && err.cmd != null)
                        {
                            printWarning("<<Invalid message sent: '" + err.cmd + "'>>");
                        }
                        break;
                    default:
                        //...
                        Disconnect();
                        return;
                        
                }


                if (backgroundworker_finished != null)
                {
                    backgroundworker_finished();
                }

                return;
            }

            // end program (redundant in case of file). needed for incorrect file terminations, single commands
            //COM.Send(GCODE.Program_End);
            // verificar ok 

            string result;
            try
            {
                result = e.Result as string;
            }
            catch(Exception exc)
            {

                if (backgroundworker_finished != null)
                {
                    backgroundworker_finished();
                }
                return;
            }

            if(result != null && result != string.Empty)
            {
                if(result.IndexOf(GCODE.cnc_position_update) == 0)
                {
                    // parse + update position
                    double new_x;
                    double new_y;
                    double new_z;
                    try
                    {
                        new_x = double.Parse(result.Split(new char[] { ' ', 'X', 'Y', 'Z' })[3], nfi);
                        new_y = double.Parse(result.Split(new char[] { ' ', 'X', 'Y', 'Z' })[5], nfi);
                        new_z = double.Parse(result.Split(new char[] { ' ', 'X', 'Y', 'Z' })[7], nfi);
                    }
                    catch(Exception exc)
                    {
                        if(printWarning != null)
                        {
                            printWarning("<<Invalid message received: '" + result + "'>>");
                        }


                        if (backgroundworker_finished != null)
                        {
                            backgroundworker_finished();
                        }

                        return;
                    }

                    // update the config and call the gui update
                    Configurations.current_location.X = new_x;
                    Configurations.current_location.Y = new_y;
                    Configurations.current_location.Z = new_z;
                    if (updatePosition != null)
                    {
                        updatePosition(Configurations.current_location.X, Configurations.current_location.Y, Configurations.current_location.Z);
                    }
                    if(updateConfigurations != null)
                    {
                        updateConfigurations();
                    }
                }
                else if(result.IndexOf(GCODE.cnc_definitions_update) == 0)
                {
                    // parametros D, E, .......
                }
                else if (result.IndexOf(GCODE.info_start) == 0)
                {
                    // display/process info
                    switch (result)
                    {
                        case GCODE.cnc_error_syntax:
                            if (printWarning != null)
                            {
                                printWarning("<<Syntax error! " + result + ">>");
                            }
                            break;
                        case GCODE.cnc_error_motorOff:
                            if (printWarning != null)
                            {
                                printWarning("<<Motors are off! " + result + ">>");
                            }
                            // TURN OFF THE MOTORS AND CALL EVENT TO UPDATE FORM!!!!!!
                            break;
                        // OTHER CONFIGURATIONS/ERRORS
                        case GCODE.cnc_error_ok:
                        default:

                            break;
                    }
                }
            }

            if (backgroundworker_finished != null)
            {
                backgroundworker_finished();
            }
        }

        private void cnc_backgroundworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (printProgress == null)
            {
                return;
            }
            printProgress(e.ProgressPercentage);

            if(e.UserState != null)
            {
                // userstate guarda a nova posição
            }
            
        }


        private void sim_backgroundworker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            string[] file = (e.Argument as string[]).Clone() as string[];

            // copy the current configurations

            // wait for the other bgw to finish(sethome)
            while(isBusy()){
                Thread.Sleep(100);

                if (bw.CancellationPending)
                {
                    return;
                }
            }

            CNC_Properties simulation_Properties = Configurations.Clone();

            if (simulation_Properties == null)
            {
                throw new NullReferenceException();
            }

            simulation_Properties.toolActive = false;

            // creat image
            double img_ratio = Configurations.Axes_Step_Dimension.X / (double)Configurations.Axes_Step_Dimension.Y;
            int img_frame_x = Image_Side_Size_Limit, img_frame_y = Image_Side_Size_Limit;

            if (Configurations.Axes_Step_Dimension.X > Configurations.Axes_Step_Dimension.Y)
            {
                img_frame_y = Convert.ToInt32(Math.Round(img_frame_x / img_ratio));
            }
            else
            {
                img_frame_x = Convert.ToInt32(Math.Round(img_frame_y * img_ratio));
            }

            GCODE cmd;
            double start_x_mm = 0, start_y_mm = 0, finish_x_mm = 0, finish_y_mm = 0, center_x_mm = 0, center_y_mm = 0, radius_mm = 0; // in mm
            int start_x_px = 0, start_y_px = 0, finish_x_px = 0, finish_y_px = 0, center_x_px = 0, center_y_px = 0, radius_px = 0; // in pixels
            bool radius_defined = false, draw_arc = false;


            CNC_Properties.CoordinatesD next_Location = new CNC_Properties.CoordinatesD();

            List<string> error_list = new List<string>();
                       
            Bitmap gcode_img = new Bitmap(img_frame_x, img_frame_y);

            Graphics grph = Graphics.FromImage(gcode_img);
            string gcode;

            int sim_progress = 0;
            bw.ReportProgress(0);

            for (int i = 0; i < file.Length; i++)
            {
                if(i * 100 / file.Length != sim_progress)
                {
                    sim_progress = i * 100 / file.Length;
                    bw.ReportProgress(sim_progress);
                }

                gcode = file[i];

                if (bw.CancellationPending)
                {
                    try
                    {
                        grph.Dispose();
                        gcode_img.Dispose();
                    }
                    catch(Exception err)
                    {
                        
                    }
                    return;
                }


                // Parse cmd
                
                try
                {
                    cmd = new GCODE(file[i]);
                }
                catch (Exception err)
                {
                    error_list.Add("<<SYNTAX ERROR in line " + i + ": '" + file[i].Replace("\n","") + "'>>");
                    continue;
                }

                if(cmd.gcode.func_type == 'G')
                {
                    switch (cmd.gcode.func_index)
                    {
                        // Movement
                        case 0: // line
                        case 1: // line
                        case 2: // arc
                        case 3:  // arc
                        case 28: // home
                        case 92: // line 
                            // starting point in mm
                            start_x_mm = simulation_Properties._Units_Mode == CNC_Properties.Units_Mode.Metric ? simulation_Properties.current_location.X : simulation_Properties.current_location.X * CNC_Properties.inch_to_mm;
                            start_y_mm = simulation_Properties._Units_Mode == CNC_Properties.Units_Mode.Metric ? simulation_Properties.current_location.Y : simulation_Properties.current_location.Y * CNC_Properties.inch_to_mm;

                            // case a param is not defined use the starting point
                            finish_x_mm = start_x_mm;
                            finish_y_mm = start_y_mm;

                            // arc vars init
                            center_x_mm = start_x_mm;
                            center_y_mm = start_y_mm;

                            radius_defined = false;
                            draw_arc = false;

                            // next location in the simulation_properties
                            next_Location.X = simulation_Properties.current_location.X;
                            next_Location.Y = simulation_Properties.current_location.Y;

                            // finish point in mm
                            if(cmd.gcode.func_index != 28)
                            {
                                foreach (GCODE.Parameter p in cmd.gcode.ParametersList)
                                {
                                    switch (p.type)
                                    {
                                        case 'X':
                                            if (simulation_Properties._Coordinates_Mode == CNC_Properties.Coordinates_Mode.Absolute || cmd.gcode.func_index == 92)
                                            {
                                                finish_x_mm = simulation_Properties._Units_Mode == CNC_Properties.Units_Mode.Metric ? p.value : p.value * CNC_Properties.inch_to_mm;
                                                next_Location.X = p.value;
                                            }
                                            else
                                            {
                                                finish_x_mm = simulation_Properties._Units_Mode == CNC_Properties.Units_Mode.Metric ? start_x_mm + p.value : start_x_mm + p.value * CNC_Properties.inch_to_mm;
                                                next_Location.X += p.value;
                                            }

                                            break;
                                        case 'Y':
                                            if (simulation_Properties._Coordinates_Mode == CNC_Properties.Coordinates_Mode.Absolute || cmd.gcode.func_index == 92)
                                            {
                                                finish_y_mm = simulation_Properties._Units_Mode == CNC_Properties.Units_Mode.Metric ? p.value : p.value * CNC_Properties.inch_to_mm;
                                                next_Location.Y = p.value;
                                            }
                                            else
                                            {
                                                finish_y_mm = simulation_Properties._Units_Mode == CNC_Properties.Units_Mode.Metric ? start_y_mm + p.value : start_y_mm + p.value * CNC_Properties.inch_to_mm;
                                                next_Location.Y += p.value;
                                            }
                                            break;
                                        case 'I':
                                            // add to start position(always relative)
                                            center_x_mm += simulation_Properties._Units_Mode == CNC_Properties.Units_Mode.Metric ? p.value : p.value * CNC_Properties.inch_to_mm;
                                            draw_arc = true;
                                            break;
                                        case 'J':
                                            // add to start position(always relative)
                                            center_y_mm += simulation_Properties._Units_Mode == CNC_Properties.Units_Mode.Metric ? p.value : p.value * CNC_Properties.inch_to_mm;
                                            draw_arc = true;
                                            break;
                                        case 'R':
                                            radius_defined = true;
                                            radius_mm = simulation_Properties._Units_Mode == CNC_Properties.Units_Mode.Metric ? p.value : p.value * CNC_Properties.inch_to_mm;
                                            draw_arc = true;
                                            break;
                                        case 'K': // draw line *ignore*
                                        case 'Z':
                                        case 'E':
                                        case 'F':
                                            break;
                                        default:
                                            error_list.Add("<<SYNTAX ERROR in line " + i + ": '" + file[i].Replace("\n", "") + "'>>");
                                            continue;
                                    }
                                }
                            }
                            else // home movement
                            {
                                if (cmd.gcode.ParameterCount == 0)
                                {
                                    finish_x_mm = 0;
                                    finish_y_mm = 0;

                                    next_Location.X = 0;
                                    next_Location.Y = 0;
                                }
                                else
                                {
                                    foreach (GCODE.Parameter p in cmd.gcode.ParametersList)
                                    {
                                        switch (p.type)
                                        {
                                            case 'X':
                                                finish_x_mm = 0;
                                                next_Location.X = 0;
                                                break;
                                            case 'Y':
                                                finish_y_mm = 0;
                                                next_Location.Y = 0;
                                                break;
                                            case 'Z':
                                                break;
                                            default:
                                                error_list.Add("<<SYNTAX ERROR in line " + i + ": '" + file[i].Replace("\n", "") + "'>>");
                                                continue;
                                        }
                                    }
                                }
                            }
                            

                            // take the home position into account
                            switch (simulation_Properties._Home_Position)
                            {
                                case CNC_Properties.Home_Position.BottomLeft:
                                    // reference
                                    break;
                                case CNC_Properties.Home_Position.BottomRight:
                                    start_x_mm += simulation_Properties.Axes_Units_Dimension.X;
                                    finish_x_mm += simulation_Properties.Axes_Units_Dimension.X;
                                    center_x_mm += simulation_Properties.Axes_Units_Dimension.X;
                                    break;
                                case CNC_Properties.Home_Position.TopLeft:
                                    start_y_mm += simulation_Properties.Axes_Units_Dimension.Y;
                                    finish_y_mm += simulation_Properties.Axes_Units_Dimension.Y;
                                    center_y_mm += simulation_Properties.Axes_Units_Dimension.Y;
                                    break;
                                case CNC_Properties.Home_Position.TopRight:
                                    start_x_mm += simulation_Properties.Axes_Units_Dimension.X;
                                    finish_x_mm += simulation_Properties.Axes_Units_Dimension.X;
                                    center_x_mm += simulation_Properties.Axes_Units_Dimension.X;
                                    start_y_mm += simulation_Properties.Axes_Units_Dimension.Y;
                                    finish_y_mm += simulation_Properties.Axes_Units_Dimension.Y;
                                    center_y_mm += simulation_Properties.Axes_Units_Dimension.Y;
                                    break;
                                case CNC_Properties.Home_Position.Center:
                                    start_x_mm += simulation_Properties.Axes_Units_Dimension.X / 2;
                                    finish_x_mm += simulation_Properties.Axes_Units_Dimension.X / 2;
                                    center_x_mm += simulation_Properties.Axes_Units_Dimension.X / 2;
                                    start_y_mm += simulation_Properties.Axes_Units_Dimension.Y / 2; 
                                    finish_y_mm += simulation_Properties.Axes_Units_Dimension.Y / 2;
                                    center_y_mm += simulation_Properties.Axes_Units_Dimension.Y / 2;
                                    break;
                                case CNC_Properties.Home_Position.Costume:
                                    start_x_mm += simulation_Properties.Costume_Home_Position.X / simulation_Properties.StepResolution.X;
                                    finish_x_mm += simulation_Properties.Costume_Home_Position.X / simulation_Properties.StepResolution.X;
                                    center_x_mm += simulation_Properties.Costume_Home_Position.X / simulation_Properties.StepResolution.X;
                                    start_y_mm += simulation_Properties.Costume_Home_Position.Y / simulation_Properties.StepResolution.Y;
                                    finish_y_mm += simulation_Properties.Costume_Home_Position.Y / simulation_Properties.StepResolution.Y;
                                    center_y_mm += simulation_Properties.Costume_Home_Position.Y / simulation_Properties.StepResolution.Y;
                                    
                                    break;
                            }

                            if (radius_defined)
                            {
                                GCODE.Location _center = new GCODE.Location(start_x_mm, start_y_mm);
                                // convert radius to center points
                                _center = GCODE.Convert_R_to_IJ(new GCODE.Location(start_x_mm, start_y_mm), new GCODE.Location(finish_x_mm, finish_y_mm), radius_mm, (cmd.gcode.func_index == 2)); // == 2 => Clockwise

                                center_x_mm = _center.x;
                                center_y_mm = _center.y;
                            }

                            // convert start and finish to % of the total axes dimensions
                            start_x_mm /= simulation_Properties.Axes_Units_Dimension.X;
                            start_y_mm /= simulation_Properties.Axes_Units_Dimension.Y;
                            finish_x_mm /= simulation_Properties.Axes_Units_Dimension.X;
                            finish_y_mm /= simulation_Properties.Axes_Units_Dimension.Y;
                            center_x_mm /= simulation_Properties.Axes_Units_Dimension.X;
                            center_y_mm /= simulation_Properties.Axes_Units_Dimension.Y;

                            // out of bounds check
                            if (finish_x_mm > 1)
                            {
                                error_list.Add("<<OUT OF BOUNDS in Axis X (Right) at line " + i + ": '" + file[i].Replace("\n", "") + "'>>");
                                // update current_location
                                simulation_Properties.current_location.X = next_Location.X;
                                simulation_Properties.current_location.Y = next_Location.Y;
                                continue;

                            }
                            else if(finish_x_mm < 0)
                            {
                                error_list.Add("<<OUT OF BOUNDS in Axis X (Left) at line " + i + ": '" + file[i].Replace("\n", "") + "'>>");
                                // update current_location
                                simulation_Properties.current_location.X = next_Location.X;
                                simulation_Properties.current_location.Y = next_Location.Y;
                                continue;
                            }
                            if (finish_y_mm > 1)
                            {
                                error_list.Add("<<OUT OF BOUNDS in Axis Y (Top) at line " + i + ": '" + file[i].Replace("\n", "") + "'>>");
                                // update current_location
                                simulation_Properties.current_location.X = next_Location.X;
                                simulation_Properties.current_location.Y = next_Location.Y;
                                continue;
                            }
                            else if (finish_y_mm < 0)
                            {
                                error_list.Add("<<OUT OF BOUNDS in Axis Y (Bottom) at line " + i + ": '" + file[i].Replace("\n", "") + "'>>");
                                // update current_location
                                simulation_Properties.current_location.X = next_Location.X;
                                simulation_Properties.current_location.Y = next_Location.Y;
                                continue;
                            }


                            // calculate pixels for the img
                            start_x_px = Convert.ToInt32(Math.Round(gcode_img.Width * start_x_mm));
                            start_y_px = Convert.ToInt32(Math.Round(gcode_img.Height * start_y_mm));
                            finish_x_px = Convert.ToInt32(Math.Round(gcode_img.Width * finish_x_mm));
                            finish_y_px = Convert.ToInt32(Math.Round(gcode_img.Height * finish_y_mm));
                            center_x_px = Convert.ToInt32(Math.Round(gcode_img.Width * center_x_mm));
                            center_y_px = Convert.ToInt32(Math.Round(gcode_img.Height * center_y_mm));

                            // Draw

                            if (draw_arc)
                            {
                                
                                if (simulation_Properties.toolActive)
                                {
                                    Graphic_Utilities.DrawArc(grph, blackPen, start_x_px, start_y_px, finish_x_px, finish_y_px, center_x_px, center_y_px, (cmd.gcode.func_index == 2));
                                }
                                else
                                {
                                    Graphic_Utilities.DrawArc(grph, redPen, start_x_px, start_y_px, finish_x_px, finish_y_px, center_x_px, center_y_px, (cmd.gcode.func_index == 2));
                                }
                                
                                    /*
                                    // calculate arc
                                    int radius = Convert.ToInt32(Math.Round(Math.Sqrt(Math.Pow(center_x_px - start_x_px, 2) + Math.Pow(center_y_px - start_y_px, 2))));
                                    if (radius == 0)
                                    {
                                        radius = 1;
                                    }

                                    Rectangle rect = new Rectangle(center_x_px - radius, center_y_px - radius, radius * 2, radius * 2);

                                    Point Ref = new Point(radius, 0);
                                    Point start_ref = new Point(start_x_px - center_x_px, start_y_px - center_y_px);
                                    Point finish_ref = new Point(finish_x_px - center_x_px, finish_y_px - center_y_px);

                                    float startAngle = Math_Utilities.AngleBetween(start_ref, Ref);
                                    float finishAngle = Math_Utilities.AngleBetween(finish_ref, Ref);
                                    float sweepAngle;


                                    if (cmd.gcode.func_index == 2) // clockwise
                                    {
                                        sweepAngle = finishAngle - startAngle;
                                    }
                                    else
                                    {
                                        sweepAngle = startAngle - finishAngle;
                                    }

                                    if (sweepAngle < 0)
                                    {
                                        sweepAngle *= -1;
                                    }
                                    else
                                    {
                                        sweepAngle = 360 - sweepAngle;
                                    }

                                    if (cmd.gcode.func_index == 2)
                                    {
                                        if (simulation_Properties.toolActive)
                                        {
                                            grph.DrawArc(blackPen, rect, startAngle, -1 * sweepAngle);
                                        }
                                        else
                                        {
                                            grph.DrawArc(redPen, rect, startAngle, -1 * sweepAngle);
                                        }
                                    }
                                    else
                                    {
                                        if (simulation_Properties.toolActive)
                                        {
                                            grph.DrawArc(blackPen, rect, startAngle, sweepAngle);
                                        }
                                        else
                                        {
                                            grph.DrawArc(redPen, rect, startAngle, sweepAngle);
                                        }
                                    }
                                    
                                    */
                                    
                                }
                            else
                            {
                                if (cmd.gcode.func_index == 1 && simulation_Properties.toolActive)
                                {
                                    grph.DrawLine(blackPen, start_x_px, start_y_px, finish_x_px, finish_y_px);
                                }
                                else
                                {
                                    grph.DrawLine(redPen, start_x_px, start_y_px, finish_x_px, finish_y_px);
                                }
                            }

                            // update current_location
                            simulation_Properties.current_location.X = next_Location.X;
                            simulation_Properties.current_location.Y = next_Location.Y;

                            break;

                        // configurations
                        case 20:
                            if(simulation_Properties._Units_Mode != CNC_Properties.Units_Mode.Imperial)
                            {
                                simulation_Properties._Units_Mode = CNC_Properties.Units_Mode.Imperial;
                                // update current position
                                simulation_Properties.current_location.X /= CNC_Properties.inch_to_mm;
                                simulation_Properties.current_location.Y /= CNC_Properties.inch_to_mm;
                            }

                            break;
                        case 21:
                            if(simulation_Properties._Units_Mode != CNC_Properties.Units_Mode.Metric)
                            {
                                simulation_Properties._Units_Mode = CNC_Properties.Units_Mode.Metric;
                                // update current position
                                simulation_Properties.current_location.X *= CNC_Properties.inch_to_mm;
                                simulation_Properties.current_location.Y *= CNC_Properties.inch_to_mm;
                            }

                            break;
                        case 90:
                            simulation_Properties._Coordinates_Mode = CNC_Properties.Coordinates_Mode.Absolute;
                            break;
                        case 91:
                            simulation_Properties._Coordinates_Mode = CNC_Properties.Coordinates_Mode.Relative;
                            break;
                        case 93:
                            simulation_Properties._Feedrate_Mode = CNC_Properties.Feedrate_Mode.Frequency;
                            break;
                        case 94:
                            simulation_Properties._Feedrate_Mode = CNC_Properties.Feedrate_Mode.UnitsPerMinute;
                            break;
                    }
                }
                else if (cmd.gcode.func_type == 'M')
                {
                    switch (cmd.gcode.func_index)
                    {
                        case 3:
                        case 4: // tool on
                            simulation_Properties.toolActive = true;
                            break;
                        case 5: // tool off
                            simulation_Properties.toolActive = false;
                            break;
                        // Configurations
                        case 261: // set home
                            change_home(simulation_Properties, cmd);
                            break;
                    }
                }

                
                

            }

            bw.ReportProgress(100);
                        
            grph.Dispose();

            var Result_ImgAndErrors = Tuple.Create(gcode_img, error_list);

            e.Result = Result_ImgAndErrors;
        }

        private void sim_backgroundworker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                
                return;
            }

            if(e.Error != null)
            {
                printWarning?.Invoke("<<ERROR DURING SIMULATION!: " + e.ToString() +">>");
                return;
            }

            Bitmap gcode_img;
            List<string> error_list = new List<string>();

            Tuple<Bitmap, List<string>> sim_result;

            try
            {
                sim_result = e.Result as Tuple<Bitmap, List<string>>;
            }
            catch(Exception err)
            {
                printWarning?.Invoke("<<ERROR DURING SIMULATION!: Unable to load image and list of erros!>>");
                return;
            }

            gcode_img = sim_result.Item1;
            error_list = sim_result.Item2;

            try
            {
                // Clean background image of the panel
                backgroundworker_draw?.Invoke(gcode_img);
            }
            catch(Exception err)
            {
                printWarning?.Invoke("<<ERROR DURING SIMULATION!: Drawing operation failed!>>");
                return;
            }

            if(error_list.Count > ERROR_LIST_CHUNK_SIZE) // print in file
            {
                printWarning?.Invoke("<<WARNING!: To many errors detected!>>");
                if (error_log_location == null)
                {
                    printWarning?.Invoke("<<ERROR!: Unable to save error log file!>>");
                }

                
                try
                {
                    File.WriteAllText(error_log_location, string.Join("\n", error_list));
                    printWarning?.Invoke("<<WARNING!: Error Log file saved at: '" + error_log_location + "'>>");
                }
                catch(Exception err)
                {
                    printWarning?.Invoke("<<ERROR!: Unable to save error log file at: '" + error_log_location + "'>>");
                }
                
            }
            else
            {
                printWarning?.Invoke(string.Join("\r\n", error_list));
            }
            

            //error_log_location

            printWarning?.Invoke("<<Simulation Completed>>");
            backgroundworker_sim_finished?.Invoke();
        }

        private void sim_backgroundworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            printProgress?.Invoke(e.ProgressPercentage);
            if(e.ProgressPercentage == 0)
            {
                printWarning("<<Simulating...>>");
            }
        }

        #endregion

        // interface with gui

        public delegate void cnc_printProgress(int p);
        public cnc_printProgress printProgress;

        public delegate void cnc_printWarning(string msg);
        public cnc_printWarning printWarning;

        public delegate void cnc_updatePosition(double x, double y, double z);
        public cnc_updatePosition updatePosition;

        public delegate void cnc_updateConfigurations();
        public cnc_updateConfigurations updateConfigurations;

        public delegate void cnc_backgroundworker_finished();
        public cnc_backgroundworker_finished backgroundworker_finished;

        public delegate void sim_backgroundworker_draw(Bitmap img);
        public sim_backgroundworker_draw backgroundworker_draw;

        public delegate void sim_backgroundworker_finished();
        public sim_backgroundworker_finished backgroundworker_sim_finished;
        //

        public void calculatePositionAndConfigs(string msg)
        {
            // check if its a movement function
            GCODE cmd;
            try
            {
                cmd = new GCODE(msg);
            }
            catch (Exception e)
            {
                return; // invalid
            }

            if (cmd.gcode.func_type == 'M') // M function doesn't change the position
            {

                switch (cmd.gcode.func_index)
                {
                    case 0:
                    case 112:
                        Configurations.toolActive = false;
                        Configurations.motorsActive = false;
                        break;
                    case 1: // pause
                        Configurations.toolActive = false; //?
                        
                        break;
                    case 3:
                    case 4:
                        Configurations.toolActive = true;
                        break;
                    case 5:
                        Configurations.toolActive = false;
                        break;
                    case 17:
                        Configurations.motorsActive = true;
                        break;
                    case 18:
                        Configurations.motorsActive = false;
                        break;
                    case 261: // set home

                        change_home(Configurations, cmd);

                        break;
                    case 264: // set laser power
                        if(cmd.gcode.ParameterCount > 0 && cmd.gcode.ParametersList[0].type == 'E')
                        {
                            Configurations.Current_LaserPower = cmd.gcode.ParametersList[0].value;
                        }
                        
                        break;
                    case 270: // set configurations

                        foreach (GCODE.Parameter p in cmd.gcode.ParametersList)
                        {
                            switch (p.type)
                            {
                                case 'A':

                                    break;
                                case 'B':

                                    break;
                                case 'C':

                                    break;
                                case 'D':
                                    if(p.value == 0)
                                    {
                                        Configurations.CNC_Multiblocks_Active = false;
                                    }
                                    else
                                    {
                                        Configurations.CNC_Multiblocks_Active = true;
                                    }
                                    break;
                                case 'E':
                                    Configurations.CNC_Multiblock_BufferSize = Convert.ToInt32(p.value);
                                    break;
                                case 'F':

                                    break;
                                case 'H':

                                    break;
                                case 'I':

                                    break;
                                case 'J':

                                    break;
                                case 'K':

                                    break;

                            }
                        }

                        break;
                }

            }
            else if (cmd.gcode.func_type == 'G')
            {
                // check which G funtion
                switch (cmd.gcode.func_index)
                {
                    // move funtions
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 92: // 
                        bool XcoordDefined = false, YcoordDefined = false, ZcoordDefined = false;
                        CNC_Properties.CoordinatesD destination = new CNC_Properties.CoordinatesD();
                        foreach (GCODE.Parameter param in cmd.gcode.ParametersList)
                        {
                            switch (param.type)
                            {
                                case GCODE.CoordX:
                                    destination.X = param.value;
                                    XcoordDefined = true;
                                    break;
                                case GCODE.CoordY:
                                    destination.Y = param.value;
                                    YcoordDefined = true;
                                    break;
                                case GCODE.CoordZ:
                                    destination.Z = param.value;
                                    ZcoordDefined = true;
                                    break;
                                case GCODE.Feedrate:
                                    Configurations.Current_Feedrate = param.value;
                                    break;
                                case GCODE.param_E:
                                    Configurations.Current_LaserPower = param.value;
                                    break;
                            }
                        }

                        if (Configurations._Coordinates_Mode == CNC_Properties.Coordinates_Mode.Absolute || cmd.gcode.func_index == 92)
                        {
                            Configurations.current_location.X = XcoordDefined ? destination.X : Configurations.current_location.X;
                            Configurations.current_location.Y = YcoordDefined ? destination.Y : Configurations.current_location.Y;
                            Configurations.current_location.Z = ZcoordDefined ? destination.Z : Configurations.current_location.Z;

                        }
                        else // relative
                        {
                            Configurations.current_location.X += destination.X;
                            Configurations.current_location.Y += destination.Y;
                            Configurations.current_location.Z += destination.Z;
                        }

                        // verify if out of bounds
                        int location_x = Convert.ToInt32(Math.Round(Configurations.current_location.X * Configurations.StepResolution.X));
                        int location_y = Convert.ToInt32(Math.Round(Configurations.current_location.Y * Configurations.StepResolution.Y));
                        int location_z = Convert.ToInt32(Math.Round(Configurations.current_location.Z * Configurations.StepResolution.Z));

                        if(Configurations._Units_Mode == CNC_Properties.Units_Mode.Imperial)
                        {
                            location_x = Convert.ToInt32(Math.Round(Configurations.current_location.X * Configurations.StepResolution.X * CNC_Properties.inch_to_mm));
                            location_y = Convert.ToInt32(Math.Round(Configurations.current_location.Y * Configurations.StepResolution.Y * CNC_Properties.inch_to_mm));
                            location_z = Convert.ToInt32(Math.Round(Configurations.current_location.Z * Configurations.StepResolution.Z * CNC_Properties.inch_to_mm));
                        }

                        switch (Configurations._Home_Position)
                        {
                            case CNC_Properties.Home_Position.BottomLeft:
                                location_z += Configurations.Axes_Step_Dimension.Z / 2;
                                break;
                            case CNC_Properties.Home_Position.BottomRight:
                                location_x += Configurations.Axes_Step_Dimension.X;
                                location_z += Configurations.Axes_Step_Dimension.Z / 2;
                                break;
                            case CNC_Properties.Home_Position.Center:
                                location_x += Configurations.Axes_Step_Dimension.X / 2;
                                location_y += Configurations.Axes_Step_Dimension.Y / 2;
                                location_z += Configurations.Axes_Step_Dimension.Z / 2;
                                break;
                            case CNC_Properties.Home_Position.TopLeft:
                                location_y += Configurations.Axes_Step_Dimension.Y;
                                location_z += Configurations.Axes_Step_Dimension.Z / 2;
                                break;
                            case CNC_Properties.Home_Position.TopRight:
                                location_x += Configurations.Axes_Step_Dimension.X;
                                location_y += Configurations.Axes_Step_Dimension.Y;
                                location_z += Configurations.Axes_Step_Dimension.Z / 2;
                                break;
                            case CNC_Properties.Home_Position.Costume:
                                location_x += Configurations.Costume_Home_Position.X;
                                location_y += Configurations.Costume_Home_Position.Y;
                                location_z += Configurations.Costume_Home_Position.Z;
                                break;
                        }

                        double relative_x = (double)location_x / Configurations.Axes_Step_Dimension.X;
                        double relative_y = (double)location_y / Configurations.Axes_Step_Dimension.Y;
                        double relative_z = (double)location_z / Configurations.Axes_Step_Dimension.Z;


                        switch (Configurations._Home_Position)
                        {
                            case CNC_Properties.Home_Position.BottomLeft:

                                if (relative_x < 0)
                                {
                                    Configurations.current_location.X = 0;
                                }
                                else if (relative_x > 1)
                                {
                                    Configurations.current_location.X = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric? Configurations.Axes_Units_Dimension.X : Configurations.Axes_Units_Dimension.X / CNC_Properties.inch_to_mm;
                                }
                                if (relative_y < 0)
                                {
                                    Configurations.current_location.Y = 0;
                                }
                                else if (relative_y > 1)
                                {
                                    Configurations.current_location.Y = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Configurations.Axes_Units_Dimension.Y : Configurations.Axes_Units_Dimension.Y / CNC_Properties.inch_to_mm;
                                }
                                if (relative_z < 0)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? - Configurations.Axes_Units_Dimension.Z / 2 : Configurations.Axes_Units_Dimension.Z / ( 2 * CNC_Properties.inch_to_mm);
                                }
                                else if (relative_z > 1)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Configurations.Axes_Units_Dimension.Z / 2 : Configurations.Axes_Units_Dimension.Z / ( 2 * CNC_Properties.inch_to_mm);
                                }

                                break;
                            case CNC_Properties.Home_Position.BottomRight:

                                if (relative_x < 0)
                                {
                                    Configurations.current_location.X = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Axes_Units_Dimension.X : -Configurations.Axes_Units_Dimension.X / CNC_Properties.inch_to_mm;
                                }
                                else if (relative_x > 1)
                                {
                                    Configurations.current_location.X = 0;
                                }
                                if (relative_y < 0)
                                {
                                    Configurations.current_location.Y = 0;
                                }
                                else if (relative_y > 1)
                                {
                                    Configurations.current_location.Y = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Configurations.Axes_Units_Dimension.Y : Configurations.Axes_Units_Dimension.Y / CNC_Properties.inch_to_mm;
                                }
                                if (relative_z < 0)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Axes_Units_Dimension.Z / 2 : -Configurations.Axes_Units_Dimension.Z / (2 * CNC_Properties.inch_to_mm);
                                }
                                else if (relative_z > 1)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Configurations.Axes_Units_Dimension.Z / 2 : -Configurations.Axes_Units_Dimension.Z / (2 * CNC_Properties.inch_to_mm);
                                }

                                break;
                            case CNC_Properties.Home_Position.Center:

                                if (relative_x < 0)
                                {
                                    Configurations.current_location.X = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Axes_Units_Dimension.X / 2 : -Configurations.Axes_Units_Dimension.X / (2 * CNC_Properties.inch_to_mm);
                                }
                                else if (relative_x > 1)
                                {
                                    Configurations.current_location.X = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Configurations.Axes_Units_Dimension.X / 2 : Configurations.Axes_Units_Dimension.X / (2 * CNC_Properties.inch_to_mm);
                                }
                                if (relative_y < 0)
                                {
                                    Configurations.current_location.Y = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Axes_Units_Dimension.Y / 2 : -Configurations.Axes_Units_Dimension.X / (2 * CNC_Properties.inch_to_mm);
                                }
                                else if (relative_y > 1)
                                {
                                    Configurations.current_location.Y = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Configurations.Axes_Units_Dimension.Y / 2 : Configurations.Axes_Units_Dimension.X / (2 * CNC_Properties.inch_to_mm);
                                }
                                if (relative_z < 0)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Axes_Units_Dimension.Z / 2 : -Configurations.Axes_Units_Dimension.X / (2 * CNC_Properties.inch_to_mm);
                                }
                                else if (relative_z > 1)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Configurations.Axes_Units_Dimension.Z / 2 : Configurations.Axes_Units_Dimension.X / (2 * CNC_Properties.inch_to_mm);
                                }

                                break;
                            case CNC_Properties.Home_Position.TopLeft:

                                if (relative_x < 0)
                                {
                                    Configurations.current_location.X = 0;
                                }
                                else if (relative_x > 1)
                                {
                                    Configurations.current_location.X = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Configurations.Axes_Units_Dimension.X : Configurations.Axes_Units_Dimension.X / CNC_Properties.inch_to_mm;
                                }
                                if (relative_y < 0)
                                {
                                    Configurations.current_location.Y = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Axes_Units_Dimension.Y : -Configurations.Axes_Units_Dimension.Y / CNC_Properties.inch_to_mm;
                                }
                                else if (relative_y > 1)
                                {
                                    Configurations.current_location.Y = 0;
                                }
                                if (relative_z < 0)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Axes_Units_Dimension.Z / 2 : -Configurations.Axes_Units_Dimension.Z / ( 2 * CNC_Properties.inch_to_mm );
                                }
                                else if (relative_z > 1)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Configurations.Axes_Units_Dimension.Z / 2 : Configurations.Axes_Units_Dimension.Z / (2 * CNC_Properties.inch_to_mm);
                                }

                                break;
                            case CNC_Properties.Home_Position.TopRight:

                                if (relative_x < 0)
                                {
                                    Configurations.current_location.X = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Axes_Units_Dimension.X : -Configurations.Axes_Units_Dimension.X / CNC_Properties.inch_to_mm;
                                }
                                else if (relative_x > 1)
                                {
                                    Configurations.current_location.X = 0;
                                }
                                if (relative_y < 0)
                                {
                                    Configurations.current_location.Y = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Axes_Units_Dimension.Y : -Configurations.Axes_Units_Dimension.Y / CNC_Properties.inch_to_mm;
                                }
                                else if (relative_y > 1)
                                {
                                    Configurations.current_location.Y = 0;
                                }
                                if (relative_z < 0)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Axes_Units_Dimension.Z / 2 : -Configurations.Axes_Units_Dimension.Z / (2 * CNC_Properties.inch_to_mm);
                                }
                                else if (relative_z > 1)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? Configurations.Axes_Units_Dimension.Z / 2 : Configurations.Axes_Units_Dimension.Z / (2 * CNC_Properties.inch_to_mm);
                                }

                                break;
                            case CNC_Properties.Home_Position.Costume:

                                if (relative_x < 0)
                                {
                                    Configurations.current_location.X = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Costume_Home_Position.X / Configurations.StepResolution.X : -Configurations.Costume_Home_Position.X / (Configurations.StepResolution.X * CNC_Properties.inch_to_mm);
                                }
                                else if (relative_x > 1)
                                {
                                    Configurations.current_location.X = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? (Configurations.Axes_Step_Dimension.X - Configurations.Costume_Home_Position.X) / Configurations.StepResolution.X : (Configurations.Axes_Step_Dimension.X - Configurations.Costume_Home_Position.X) / (Configurations.StepResolution.X * CNC_Properties.inch_to_mm);
                                }
                                if (relative_y < 0)
                                {
                                    Configurations.current_location.Y = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Costume_Home_Position.Y / Configurations.StepResolution.Y : -Configurations.Costume_Home_Position.Y / (Configurations.StepResolution.Y * CNC_Properties.inch_to_mm);
                                }
                                else if (relative_y > 1)
                                {
                                    Configurations.current_location.Y = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? (Configurations.Axes_Step_Dimension.Y - Configurations.Costume_Home_Position.Y) / Configurations.StepResolution.Y : (Configurations.Axes_Step_Dimension.Y - Configurations.Costume_Home_Position.Y) / (Configurations.StepResolution.Y * CNC_Properties.inch_to_mm);
                                }
                                if (relative_z < 0)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? -Configurations.Costume_Home_Position.Z / Configurations.StepResolution.Z : -Configurations.Costume_Home_Position.Z / (Configurations.StepResolution.Z * CNC_Properties.inch_to_mm);
                                }
                                else if (relative_z > 1)
                                {
                                    Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? (Configurations.Axes_Step_Dimension.Z - Configurations.Costume_Home_Position.Z) / Configurations.StepResolution.Z : (Configurations.Axes_Step_Dimension.Z - Configurations.Costume_Home_Position.Z) / (Configurations.StepResolution.Z * CNC_Properties.inch_to_mm);
                                }


                                break;
                        }



                        break;
                    case 28: // home
                        if(cmd.gcode.ParameterCount == 0)
                        {
                            Configurations.current_location.X = 0;
                            Configurations.current_location.Y = 0;
                            Configurations.current_location.Z = 0;
                        }
                        else
                        {
                            foreach (GCODE.Parameter p in cmd.gcode.ParametersList)
                            {
                                switch (p.type)
                                {
                                    case 'X':
                                        Configurations.current_location.X = 0;
                                        break;
                                    case 'Y':
                                        Configurations.current_location.Y = 0;
                                        break;
                                    case 'Z':
                                        Configurations.current_location.Z = 0;
                                        break;
                                }
                            }
                        }

                        


                        break;

                    case 20:
                        if(Configurations._Units_Mode != CNC_Properties.Units_Mode.Imperial)
                        {
                            Configurations._Units_Mode = CNC_Properties.Units_Mode.Imperial;
                            // update current location for new units?
                            Configurations.current_location.X = Configurations.current_location.X / CNC_Properties.inch_to_mm;
                            Configurations.current_location.Y = Configurations.current_location.Y / CNC_Properties.inch_to_mm;
                            Configurations.current_location.Z = Configurations.current_location.Z / CNC_Properties.inch_to_mm;
                        }

                        break;
                    case 21:
                        if(Configurations._Units_Mode != CNC_Properties.Units_Mode.Metric)
                        {
                            Configurations._Units_Mode = CNC_Properties.Units_Mode.Metric;
                            // update current location for new units?
                            Configurations.current_location.X = Configurations.current_location.X * CNC_Properties.inch_to_mm;
                            Configurations.current_location.Y = Configurations.current_location.Y * CNC_Properties.inch_to_mm;
                            Configurations.current_location.Z = Configurations.current_location.Z * CNC_Properties.inch_to_mm;
                        }

                        break;
                    case 90:
                        Configurations._Coordinates_Mode = CNC_Properties.Coordinates_Mode.Absolute;
                        break;
                    case 91:
                        Configurations._Coordinates_Mode = CNC_Properties.Coordinates_Mode.Relative;
                        break;
                    case 93:
                        Configurations._Feedrate_Mode = CNC_Properties.Feedrate_Mode.Frequency;
                        break;
                    case 94:
                        Configurations._Feedrate_Mode = CNC_Properties.Feedrate_Mode.UnitsPerMinute;
                        break;

                }
            }


            // update Configurations.Current_position + (GUI)
            if (updatePosition != null)
            {
                updatePosition(Configurations.current_location.X, Configurations.current_location.Y, Configurations.current_location.Z);
            }
            if(updateConfigurations != null)
            {
                updateConfigurations();
            }
        }

        private void change_home(CNC_Properties Configurations, GCODE cmd)
        {
            if(cmd.gcode.func_index != 261 || cmd.gcode.func_type != 'M')
            {
                return;
            }
            
            // get current position in steps

            // change home in config and update the position with the new values in relation to the home

            // current position in steps
            int location_x = Convert.ToInt32(Math.Round(Configurations.current_location.X * Configurations.StepResolution.X));
            int location_y = Convert.ToInt32(Math.Round(Configurations.current_location.Y * Configurations.StepResolution.Y));
            int location_z = Convert.ToInt32(Math.Round(Configurations.current_location.Z * Configurations.StepResolution.Z));

            if (Configurations._Units_Mode == CNC_Properties.Units_Mode.Imperial)
            {
                location_x = Convert.ToInt32(Math.Round(Configurations.current_location.X * Configurations.StepResolution.X * CNC_Properties.inch_to_mm));
                location_y = Convert.ToInt32(Math.Round(Configurations.current_location.Y * Configurations.StepResolution.Y * CNC_Properties.inch_to_mm));
                location_z = Convert.ToInt32(Math.Round(Configurations.current_location.Z * Configurations.StepResolution.Z * CNC_Properties.inch_to_mm));
            }
            // .. in relation to the current home position
            switch (Configurations._Home_Position)
            {
                case CNC_Properties.Home_Position.BottomLeft:
                    location_z += Configurations.Axes_Step_Dimension.Z / 2;

                    Configurations.Costume_Home_Position.X = 0;
                    Configurations.Costume_Home_Position.Y = 0;
                    Configurations.Costume_Home_Position.Z = Configurations.Axes_Step_Dimension.Z / 2;

                    break;
                case CNC_Properties.Home_Position.BottomRight:
                    location_x += Configurations.Axes_Step_Dimension.X;
                    location_z += Configurations.Axes_Step_Dimension.Z / 2;

                    Configurations.Costume_Home_Position.X = Configurations.Axes_Step_Dimension.X;
                    Configurations.Costume_Home_Position.Y = 0;
                    Configurations.Costume_Home_Position.Z = Configurations.Axes_Step_Dimension.Z / 2;
                    break;
                case CNC_Properties.Home_Position.Center:
                    location_x += Configurations.Axes_Step_Dimension.X / 2;
                    location_y += Configurations.Axes_Step_Dimension.Y / 2;
                    location_z += Configurations.Axes_Step_Dimension.Z / 2;

                    Configurations.Costume_Home_Position.X = Configurations.Axes_Step_Dimension.X / 2;
                    Configurations.Costume_Home_Position.Y = Configurations.Axes_Step_Dimension.Y / 2;
                    Configurations.Costume_Home_Position.Z = Configurations.Axes_Step_Dimension.Z / 2;

                    break;
                case CNC_Properties.Home_Position.TopLeft:
                    location_y += Configurations.Axes_Step_Dimension.Y;
                    location_z += Configurations.Axes_Step_Dimension.Z / 2;

                    Configurations.Costume_Home_Position.X = 0;
                    Configurations.Costume_Home_Position.Y = Configurations.Axes_Step_Dimension.Y;
                    Configurations.Costume_Home_Position.Z = Configurations.Axes_Step_Dimension.Z / 2;
                    break;
                case CNC_Properties.Home_Position.TopRight:
                    location_x += Configurations.Axes_Step_Dimension.X;
                    location_y += Configurations.Axes_Step_Dimension.Y;
                    location_z += Configurations.Axes_Step_Dimension.Z / 2;

                    Configurations.Costume_Home_Position.X = Configurations.Axes_Step_Dimension.X;
                    Configurations.Costume_Home_Position.Y = Configurations.Axes_Step_Dimension.Y;
                    Configurations.Costume_Home_Position.Z = Configurations.Axes_Step_Dimension.Z / 2;
                    break;
                case CNC_Properties.Home_Position.Costume:
                    location_x += Configurations.Costume_Home_Position.X;
                    location_y += Configurations.Costume_Home_Position.Y;
                    location_z += Configurations.Costume_Home_Position.Z;
                    break;
            }
            // current position in relation to the new home position
            if (cmd.gcode.ParameterCount == 0)
            {
                Configurations._Home_Position = CNC_Properties.Home_Position.Costume;
                // change the costum home position
                Configurations.Costume_Home_Position.X = location_x;
                Configurations.Costume_Home_Position.Y = location_y;
                Configurations.Costume_Home_Position.Z = location_z;
            }
            else
            {
                foreach (GCODE.Parameter p in cmd.gcode.ParametersList)
                {
                    switch (p.type)
                    {
                        case 'S': // Bottom Left
                            Configurations._Home_Position = CNC_Properties.Home_Position.BottomLeft;

                            Configurations.Costume_Home_Position.X = 0;
                            Configurations.Costume_Home_Position.Y = 0;
                            Configurations.Costume_Home_Position.Z = Configurations.Axes_Step_Dimension.Z / 2;
                            break;
                        case 'F': // Bottom Right
                            Configurations._Home_Position = CNC_Properties.Home_Position.BottomRight;

                            Configurations.Costume_Home_Position.X = Configurations.Axes_Step_Dimension.X;
                            Configurations.Costume_Home_Position.Y = 0;
                            Configurations.Costume_Home_Position.Z = Configurations.Axes_Step_Dimension.Z / 2;
                            break;
                        case 'E': // Top Left
                            Configurations._Home_Position = CNC_Properties.Home_Position.TopLeft;

                            Configurations.Costume_Home_Position.X = 0;
                            Configurations.Costume_Home_Position.Y = Configurations.Axes_Step_Dimension.Y;
                            Configurations.Costume_Home_Position.Z = Configurations.Axes_Step_Dimension.Z / 2;
                            break;
                        case 'P': // Top Right
                            Configurations._Home_Position = CNC_Properties.Home_Position.TopRight;

                            Configurations.Costume_Home_Position.X = Configurations.Axes_Step_Dimension.X;
                            Configurations.Costume_Home_Position.Y = Configurations.Axes_Step_Dimension.Y;
                            Configurations.Costume_Home_Position.Z = Configurations.Axes_Step_Dimension.Z / 2;
                            break;
                        case 'K': // Center
                            Configurations._Home_Position = CNC_Properties.Home_Position.Center;

                            Configurations.Costume_Home_Position.X = Configurations.Axes_Step_Dimension.X / 2;
                            Configurations.Costume_Home_Position.Y = Configurations.Axes_Step_Dimension.Y / 2;
                            Configurations.Costume_Home_Position.Z = Configurations.Axes_Step_Dimension.Z / 2;
                            break;
                        case 'X': // Current Positon
                            Configurations._Home_Position = CNC_Properties.Home_Position.Costume;
                            Configurations.Costume_Home_Position.X = location_x;
                            break;
                        case 'Y':
                            Configurations._Home_Position = CNC_Properties.Home_Position.Costume;
                            Configurations.Costume_Home_Position.Y = location_y;
                            break;
                        case 'Z':
                            Configurations._Home_Position = CNC_Properties.Home_Position.Costume;
                            Configurations.Costume_Home_Position.Z = location_z;
                            break;
                    }
                }
                
            }

            // update the current location
            Configurations.current_location.X = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? (location_x - Configurations.Costume_Home_Position.X) / Configurations.StepResolution.X : (location_x - Configurations.Costume_Home_Position.X) / (Configurations.StepResolution.X * CNC_Properties.inch_to_mm);
            Configurations.current_location.Y = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? (location_y - Configurations.Costume_Home_Position.Y) / Configurations.StepResolution.Y : (location_y - Configurations.Costume_Home_Position.Y) / (Configurations.StepResolution.Y * CNC_Properties.inch_to_mm);
            Configurations.current_location.Z = Configurations._Units_Mode == CNC_Properties.Units_Mode.Metric ? (location_z - Configurations.Costume_Home_Position.Z) / Configurations.StepResolution.Z : (location_z - Configurations.Costume_Home_Position.Z) / (Configurations.StepResolution.Z * CNC_Properties.inch_to_mm);

        }

       
        private void asynchronous_ReceiveControl(object sender, EventArgs e)
        {
            string msg = (e as COM_Port.MessageEventArgs).msg;
            if (msg != null)
            {
                switch (msg)
                {
                    case GCODE.cnc_start:
                        if (!isBusy()) // if isBusy the background worker will take care of it
                        {
                            sendConfigurations();
                        }
                        break;
                    case GCODE.cnc_stop:
                        if (isBusy())
                        {
                            Cancel();
                            for (int attempts = 0; attempts < 100 && isBusy(); attempts++)
                            {
                                Thread.Sleep(50);
                            }
                        }
                        Configurations.motorsActive = false;
                        Configurations.toolActive = false;
                        // send cmd to update position
                        //sendCMD(GCODE.Get_Current_Position); --- dont
                        if(updateConfigurations != null)
                        {
                            updateConfigurations();
                        }
                        
                        break;
                    case GCODE.hardware_fault:
                        Disconnect();
                        if (isBusy())
                        {
                            Cancel();
                        }
                        printWarning("<<WARNING: Hardware fault!>>");
                        break;
                    default:
                        return;
                }
            }
        }
    }
}
