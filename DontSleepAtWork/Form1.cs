using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace DontSleepAtWork
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            Radius = 10;
            Lock = false;
            if (File.Exists("Settings"))
            {
                using (StreamReader stream = new StreamReader("Settings"))
                {
                    SleepTime = Convert.ToInt32(stream.ReadLine());
                    CheckTime = Convert.ToInt32(stream.ReadLine());
                    Radius = Convert.ToInt32(stream.ReadLine());
                }
            }
            else
            {
                FileStream f = File.Open("Settings", FileMode.Create);
                f.Close();
                using (StreamWriter stream = new StreamWriter("Settings"))
                {
                    stream.WriteLine(SleepTime.ToString());
                    stream.WriteLine(CheckTime.ToString());
                    stream.WriteLine(Radius.ToString());
                }
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }

        private bool Sleep { get; set; }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            WindowState = FormWindowState.Normal;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            notifyIcon1.Text = "Don't Sleep At Work!";
            Sleep = false;
            Check = true;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if(WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopCheck();
            ToAwake();
            this.Close();
        }
        #region параметри
        private int mouseX { get; set; }
        private int mouseY { get; set; }

        private int Radius { get; set; }
        private bool Check { get; set; }
        private bool Lock { get; set; }

        private int PositionX
        {
            get { return Cursor.Position.X; }
        }
        private int PositionY
        {
            get { return Cursor.Position.Y; }
        }
        #endregion

        private void MouseCapture()
        {
            mouseX = PositionX;
            mouseY = PositionY;
        }

        private void MouseWatching()
        {
            if (PositionX != mouseX || PositionY != mouseY)
                StartTimer();
        }

        private void StartTimer()
        {
            timerSleep.Enabled = true;
            ToAwake();
            ToSleep();
        }

        private void timerSleep_Tick(object sender, EventArgs e)
        {
            if (!Lock)
            {
                Random rand = new Random();
                Cursor.Position = new Point(PositionX + (rand.Next(Radius * 2) - Radius), PositionY + (rand.Next(Radius * 2) - Radius));
                SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED);
            }
            ToAwake();
            //timerSleep.Enabled = false;  
            ToSleep();
        }

        #region методи таймерів
        private void ToAwake()
        {
            timerSleep.Stop();
        }

        private void ToSleep()
        {
            timerSleep.Start();
        }

        private void StartCheck()
        {
            timerCheck.Start();
        }

        private void StopCheck()
        {
            timerCheck.Stop();
        }
        #endregion

        #region параметри таймерів
        public int SleepTime
        {
            get { return timerSleep.Interval; }
            set { timerSleep.Interval = value; }
        }

        public int CheckTime
        {
            get { return timerCheck.Interval; }
            set { timerCheck.Interval = value; }
        }
        #endregion

        private void timerCheck_Tick(object sender, EventArgs e)
        {
            if (Sleep)
            {
                if (Check)
                {
                    MouseCapture();
                    Check = false;
                }
                else
                {
                    MouseWatching();
                    Check = true;
                }
                StartCheck();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Sleep = true;
            Check = true;
            label1.Text = "Sleeping";
            conditionToolStripMenuItem.Text = "Sleeping";
            conditionToolStripMenuItem.BackColor = Color.FromArgb(225, 255, 210);
            StartCheck();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Sleep = false;
            Check = true;
            label1.Text = "Awaked";
            conditionToolStripMenuItem.Text = "Awaked";
            conditionToolStripMenuItem.BackColor = Color.FromArgb(255, 230, 230);
            StopCheck();
            ToAwake();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopCheck();
            ToAwake();
            this.Close();
        }
        
        private void customizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Form2 customizeForm = new Form2())
            {
                customizeForm.SleepTime = SleepTime;
                customizeForm.CheckTime = CheckTime;
                customizeForm.Radius = Radius;
                if (customizeForm.ShowDialog() == DialogResult.OK)
                {
                    SleepTime = customizeForm.SleepTime;
                    CheckTime = customizeForm.CheckTime;
                    Radius = customizeForm.Radius;

                    FileStream f = File.Open("Settings", FileMode.Truncate);
                    f.Close();
                    using (StreamWriter stream = new StreamWriter("Settings"))
                    {
                        stream.WriteLine(SleepTime.ToString());
                        stream.WriteLine(CheckTime.ToString());
                        stream.WriteLine(Radius.ToString());
                    }
                }
            }                
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLogon:
                case SessionSwitchReason.SessionUnlock:
                    Lock = false;
                    break;

                case SessionSwitchReason.SessionLock:
                case SessionSwitchReason.SessionLogoff:
                    Lock = true;
                    break;
            }
        }
    }
}
