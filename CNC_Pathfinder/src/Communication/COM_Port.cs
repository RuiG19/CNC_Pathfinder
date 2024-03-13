using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Timers;
using System.Collections.Concurrent;
using System.Windows.Forms;

namespace CNC_Pathfinder.src.Communication
{
    /// <summary>
    /// Abstract class to handle the Serial communication.
    /// GUI functionalities need to be implemented by child class.
    /// </summary>
    /// <seealso cref="COM_Manager"></seealso>
    public class COM_Port
    {

        #region Serial Parameters

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
        protected string lineTerminator = "";

        /// <summary>
        /// Input queue for recieved messages.
        /// </summary>
        private BlockingCollection<string> inQueue;

        private int default_inBufferSize = 100;

        /// <summary>
        /// Serial Communication Watchdog timer.
        /// </summary>
        private System.Timers.Timer Watchdog;

        /// <summary>
        /// Watchdog default time value, in seconds.
        /// </summary>
        private readonly int Watchdog_Time = 5;

        #endregion

        #region Constructors

        /// <summary>
        /// COM_Port constructor. Uses a default value(100 items) for the buffer size.
        /// </summary>
        /// <param name="COM_Lost_msg">Message representing a </param>
        public COM_Port()
        {
            inQueue = new BlockingCollection<string>(new ConcurrentQueue<string>(), default_inBufferSize);
            Connection_Status = false;
        }

        /// <summary>
        /// COM_Port constructor.
        /// </summary>
        public COM_Port(int inBufferSize)
        {
            inQueue = new BlockingCollection<string>(new ConcurrentQueue<string>(), inBufferSize);
            Connection_Status = false;
        }

        #endregion

        #region Connection Functions

        /// <summary>
        /// Returns a list of all the Serial Ports available.
        /// </summary>
        /// <returns>Returns a list of all the Serial Ports available.</returns>
        /// <seealso cref="System.IO.Ports.SerialPort.GetPortNames()"></seealso>
        public string[] checkSerialPortsAvailable()
        {
            return System.IO.Ports.SerialPort.GetPortNames();
        }

        /// <summary>
        /// Connects to a new Device.
        /// </summary>
        /// <seealso cref = "COM_Port.Update_Connection_Status()"></seealso>
        /// <seealso cref = "COM_Port.Connect_ExceptionHandler()"></seealso>
        public virtual bool Connect(string portname, int baudrate, string lineTerminator)
        {
            try
            {
                Port = new System.IO.Ports.SerialPort(portname, baudrate, Parity.None, 8, StopBits.One);
                Port.DataReceived += new SerialDataReceivedEventHandler(Read);
                Port.Open();
                Port.DiscardInBuffer();
                Port.DiscardOutBuffer();
                Application.DoEvents();
            }
            catch (Exception e)
            {
                Disconnect();
                Application.DoEvents();
                return false;
            }
                       

            Connection_Status = true;
            this.lineTerminator = lineTerminator;
            Set_Watchdog(Watchdog_Time);

            onConnect();

            return true;
        }

        public event EventHandler Connected;

        protected virtual void onConnect()
        {
            if (Connected != null)
            {
                Connected(this, new EventArgs()); // call the event handler
            }
        }

        /// <summary>
        /// Disconnects from current device. Ignores exceptions (forced disconnect).
        /// </summary>
        /// <seealso cref = "COM_Port.Update_Connection_Status()"></seealso>
        public void Disconnect()
        {
            Connection_Status = false;
            Application.DoEvents();
            onDisconnect();
            Application.DoEvents();

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
            if(Disconnected != null)
            {
                Disconnected(this, new EventArgs()); // call the event handler
            }
        }

        #endregion

        #region Send & Read Functions

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="msg">Message.</param>
        /// <seealso cref = "COM_Port.Send_ExceptionHandler()"></seealso>
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

            if(msg == null)
            {
                return false;
            }

            if(msg[msg.Length - 1] != lineTerminator[lineTerminator.Length - 1])
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

            onMessageSent(msg);
            
            return true;
        }

        /// <summary>
        /// Occurs when a new message is sent;
        /// </summary>
        public event EventHandler MessageSent;

        /// <summary>
        /// Raises the MessageSent event.
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void onMessageSent(string msg)
        {
            if (MessageSent != null)
            {
                MessageSent(this, new MessageEventArgs(msg)); // call the event handler
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Interrupt function for Serial Read event. Message received gets relayed to Msg_Received Interface.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        /// <seealso cref = "COM_Port.Msg_Received()"></seealso>
        /// <seealso cref = "COM_Port.Read_ExceptionHandler()"></seealso>
        private void Read(object sender, SerialDataReceivedEventArgs eventArgs)
        {
            try // restart watchdog
            {
                Watchdog.Stop();
                Watchdog.Start();
            }
            catch (Exception e)
            {
                
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
            
            if(inQueue.Count == inQueue.BoundedCapacity) // intput buffer is full => remove last
            {
                inQueue.Take();
            }

            inQueue.Add(msg); // add the msg to the input buffer

            onMessageReceived(msg); // raise the newMessage event
            
            //Port.DiscardInBuffer();
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
                Application.DoEvents();
            }
        }

        /// <summary>
        /// COM_Port Inner Class for Message (Read) EventArgs
        /// </summary>
        public class MessageEventArgs : EventArgs
        {
            public string msg { get; private set; }

            public MessageEventArgs(string msg)
            {
                this.msg = msg;
            }
        }

        /// <summary>
        /// Get a new message from the input buffer. Blocks waiting if the buffer is empty.
        /// </summary>
        /// <returns></returns>
        public string Get()
        {
            return inQueue.Take();
        }

        /// <summary>
        /// Tries to get a new message from the input buffer. Blocks for the timeout duration.
        /// </summary>
        /// <param name="data">Output data</param>
        /// <param name="timeout">Time in milliseconds.</param>
        /// <returns>True if data was available. False otherwise.</returns>
        public bool Get(out string data, int timeout)
        {
            return inQueue.TryTake(out data, timeout);
        }

        /// <summary>
        /// Number of messages stored in the input buffer.
        /// </summary>
        /// <returns></returns>
        public int inBufferCount()
        {
            return inQueue.Count;
        }

        /// <summary>
        /// Empties the input buffer
        /// </summary>
        public void cleanInBuffer()
        {
            if(inQueue == null)
            {
                return;
            }

            while (inQueue.Count > 0)
            {
                string item;
                inQueue.TryTake(out item);
            }
        }

        #endregion

        #region Watchdog

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
        /// <seealso cref="COM_Port.Communication_TimedOut()"></seealso>
        private void Watchdog_event(object source, System.Timers.ElapsedEventArgs e)
        {
            if (Port == null || Port.IsOpen == false)
            {
                Disconnect();
                return;
            }
            Watchdog.Start(); // restart watchdog
        }

        #endregion
    }
}
