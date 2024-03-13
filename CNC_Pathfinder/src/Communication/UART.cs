using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CNC_Pathfinder.src.Communication
{
    class UART
    {
        /// <summary>
        /// Serial Port ID.
        /// </summary>
        private System.IO.Ports.SerialPort Port;

        /// <summary>
        /// Current connection status.
        /// </summary>
        public bool Connection_Status { get; private set; }

        /// <summary>
        /// Line terminator for each message sent.
        /// </summary>
        public string lineTerminator { get; private set; }

        /// <summary>
        /// Serial Communication Watchdog timer.
        /// </summary>
        private System.Timers.Timer Watchdog;
        
        /// <summary>
        /// Watchdog default time value, in seconds.
        /// </summary>
        private readonly int Watchdog_Time = 500;

        /// <summary>
        /// UART Inner Class for Message (Read) EventArgs
        /// </summary>
        public class MessageEventArgs : EventArgs
        {
            public string msg { get; private set; }

            public MessageEventArgs(string msg)
            {
                this.msg = msg;
            }
        }


        public UART()
        {
            Connection_Status = false;
            lineTerminator = "";
        }

        /// <summary>
        /// Returns a list of all the Serial Ports available.
        /// </summary>
        /// <returns>Returns a list of all the Serial Ports available.</returns>
        /// <seealso cref="System.IO.Ports.SerialPort.GetPortNames()"></seealso>
        public string[] checkSerialPortsAvailable()
        {
            return System.IO.Ports.SerialPort.GetPortNames();
        }

        public bool Connect(string portname, int baudrate, string lineTerminator)
        {
            try
            {
                Port = new System.IO.Ports.SerialPort(portname, baudrate, Parity.None, 8, StopBits.One);
                Port.DataReceived += new SerialDataReceivedEventHandler(Read);
                Port.Open();
            }
            catch (Exception e)
            {
                Disconnect();
                return false;
            }

            Connection_Status = true;
            this.lineTerminator = lineTerminator;
            Set_Watchdog(Watchdog_Time);
            return true;
        }

        public void Disconnect()
        {
            Connection_Status = false;
            onDisconnect();

            try
            {
                Watchdog.Dispose();
            }
            catch (Exception e)
            {

            }

            try
            {
                Port.Close();
                Port.Dispose();
            }
            catch (Exception e)
            {

            }
            Application.DoEvents();
        }

        /// <summary>
        /// Occurs when the connection is terminated.
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// Raise the disconnect event.
        /// </summary>
        protected virtual void onDisconnect()
        {
            if (Disconnected != null)
            {
                Disconnected(this, new EventArgs()); // call the event handler
            }
        }


        public bool Send(string msg)
        {
            if (Connection_Status == false)
            {
                return false;
            }

            if (Port == null || Port.IsOpen == false) // serialPort_CNC != null -> not initialized (needs to be check first or else serialPort_CNC.IsOpen trows an Exception)
            {
                Disconnect();
                return false;
            }

            if (msg[msg.Length - 1] != lineTerminator[lineTerminator.Length - 1])
            {
                msg += lineTerminator;
            }


            try
            {
                Port.Write(msg);
            }
            catch (Exception e)
            {
                Disconnect();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Occurs when a new message is received.
        /// </summary>
        public event EventHandler MessageReceived;

        /// <summary>
        /// Raise the MessageReceived event.
        /// </summary>
        protected virtual void onMessageReceived(string msg)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new MessageEventArgs(msg)); // call the event handler
            }
        }


        //

        private void Read(object sender, SerialDataReceivedEventArgs eventArgs)
        {
            try // restart watchdog
            {
                Watchdog.Stop();
                Watchdog.Start();
            }
            catch (Exception e)
            {
                Set_Watchdog(Watchdog_Time);
            }

            string msg = "";

            try
            {
                msg = Port.ReadLine();
            }
            catch (Exception e)
            {
                Disconnect();
                return;
            }

            onMessageReceived(msg); // raise the newMessage event
        }

        /// <summary>
        /// Sets the Serial Communication Watchdog timer.
        /// </summary>
        /// <param name="time"> Time interval in seconds.</param>
        private void Set_Watchdog(int time)
        {
            Watchdog = new System.Timers.Timer(time);
            Watchdog.Elapsed += Watchdog_event;
            Watchdog.Start();
        }

        /// <summary>
        /// Watchdog time ellapsed. Evaluates if connection is still valid.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void Watchdog_event(object source, System.Timers.ElapsedEventArgs e)
        {
            if (Port == null || Port.IsOpen == false)
            {
                Disconnect();
                return;
            }
            Watchdog.Start(); // restart watchdog
        }

    }
}
