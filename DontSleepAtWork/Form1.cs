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
    /// <summary>
    /// основна форма прграми зі всіма обрахунками
    /// є два таймера: таймер сну та таймер перевірки
    /// таймер сну - визначає час за який не відбулось жодних дій мишкою, тобто не змінилась позиція
    /// таймер перевірки - визначає час за який робиться перевірка чи відбулась зміна позиції миші
    /// коли під час перевірки нова позиція миші відрізняється від попередньої відбувається рестарт таймеру сну
    /// коли таймер сну завершується відбуваєтсья зміна позиції миші в певному радіусі(задаєтсья в налаштуваннях в пікселях) та зміна стану сисетми, тобто онулення системного таймеру блокування комп'ютера.
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>
        /// конструктор основної форми
        /// ініціалізовує стандартні елементи і делегати та зчитує налаштування параметрів чи створює новий файл з параметрами по замовчуванню(для першого запуску)
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            //додаємо делегат на зміну стану сесії для відслідковування стану обліковго запису - заблоковано/активно
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            Radius = 10;
            Lock = false;
            //перевіряємо чи існує файл з налашуваннями
            if (File.Exists("Settings"))
            {
                using (StreamReader stream = new StreamReader("Settings"))
                {
                    //для існуючого файлу читаємо параметри
                    SleepTime = Convert.ToInt32(stream.ReadLine());
                    CheckTime = Convert.ToInt32(stream.ReadLine());
                    Radius = Convert.ToInt32(stream.ReadLine());
                }
            }
            else
            {
                //якщо файлу нема створюємо новий і вносимо значення по замовчуванню
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
        /// <summary>
        /// підключаємо бібліотеку ядра Windows для обнулення системного таймеру блокування користувача
        /// </summary>
        /// <param name="esFlags">enum з станами системи</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        /// <summary>
        /// enum з станами системи
        /// </summary>
        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            /// <summary>
            /// очікування
            /// </summary>
            ES_AWAYMODE_REQUIRED = 0x00000040,
            /// <summary>
            /// робота
            /// </summary>
            ES_CONTINUOUS = 0x80000000,
            /// <summary>
            /// вимагається дисплей
            /// </summary>
            ES_DISPLAY_REQUIRED = 0x00000002,
            /// <summary>
            /// вимагається тільки робота системи
            /// </summary>
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }
        /// метод при двойному кліку на іконку в треї
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            WindowState = FormWindowState.Normal;
        }
        /// <summary>
        /// метод запускаєтсья при загрузці форми
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            notifyIcon1.Text = "Don't Sleep At Work!";
            Sleep = false;
            Check = true;
        }
        /// <summary>
        /// метод який при згортанні форми ховає її в трей
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Resize(object sender, EventArgs e)
        {
            if(WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }
        /// <summary>
        /// кнопка "вихід" на контекстому меню в треї
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopCheck();
            ToAwake();
            this.Close();
        }
        #region параметри
        /// <summary>
        /// ознака "сну", тобто роботи основного функціоналу програми
        /// вмикаєтсья та вимикається користувачем
        /// </summary>
        private bool Sleep { get; set; }
        /// <summary>
        /// <summary>
        /// збережена позиція миші по осі Х
        /// </summary>
        private int mouseX { get; set; }
        /// <summary>
        /// збережена позиція миші по осі Y
        /// </summary>
        private int mouseY { get; set; }
        /// <summary>
        /// параметр радіусу на який робити зміну курсору
        /// </summary>
        private int Radius { get; set; }
        /// <summary>
        /// ознака для перевірки чи збереження позиції миші
        /// </summary>
        private bool Check { get; set; }
        /// <summary>
        /// ознака блокування профілю
        /// </summary>
        private bool Lock { get; set; }
        /// <summary>
        /// повертає поточну позицію миші по осі Х
        /// </summary>
        private int PositionX
        {
            get { return Cursor.Position.X; }
        }
        /// <summary>
        /// повертає поточну позицію мишу по осі Y
        /// </summary>
        private int PositionY
        {
            get { return Cursor.Position.Y; }
        }
        #endregion
        /// <summary>
        /// зберігає поточну позицію миші
        /// </summary>
        private void MouseCapture()
        {
            mouseX = PositionX;
            mouseY = PositionY;
        }
        /// <summary>
        /// звіряє поточну позицію миші з збереженою позицією
        /// якщо не співпадає - перезапускає таймер сну
        /// </summary>
        private void MouseWatching()
        {
            if (PositionX != mouseX || PositionY != mouseY)
                StartTimer();
        }
        /// <summary>
        /// перезапуск таймеру сну
        /// </summary>
        private void StartTimer()
        {
            timerSleep.Enabled = true;
            ToAwake();
            ToSleep();
        }
        /// <summary>
        /// подія коли таймер сну відпрацював повністю без перезапуску
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerSleep_Tick(object sender, EventArgs e)
        {
            //перевірка чи не заблоковано обліковий запис
            if (!Lock)
            {
                Random rand = new Random();
                //визначає нову позицію курсору в межах радіусу(+- величина радіусу) по обох осях
                Cursor.Position = new Point(PositionX + (rand.Next(Radius * 2) - Radius), PositionY + (rand.Next(Radius * 2) - Radius));
                //встановлює стан системи для обнулення системного таймеру блокування користувача
                SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED);
            }
            //перезапускає таймер
            StartTimer();
            //ToAwake();
            ////timerSleep.Enabled = false;  
            //ToSleep();
        }

        #region методи таймерів
        /// <summary>
        /// виключення таймеру сну
        /// </summary>
        private void ToAwake()
        {
            timerSleep.Stop();
        }
        /// <summary>
        /// включення таймеру сну
        /// </summary>
        private void ToSleep()
        {
            timerSleep.Start();
        }
        /// <summary>
        /// включення таймеру перевірки
        /// </summary>
        private void StartCheck()
        {
            timerCheck.Start();
        }
        /// <summary>
        /// виключення таймеру перевірки
        /// </summary>
        private void StopCheck()
        {
            timerCheck.Stop();
        }
        #endregion

        #region параметри таймерів
        /// <summary>
        /// час для таймеру сну
        /// </summary>
        public int SleepTime
        {
            get { return timerSleep.Interval; }
            set { timerSleep.Interval = value; }
        }
        /// <summary>
        /// час для таймеру перевірки
        /// </summary>
        public int CheckTime
        {
            get { return timerCheck.Interval; }
            set { timerCheck.Interval = value; }
        }
        #endregion
        /// <summary>
        /// подія відбувається коли завершуєтсья час таймеру перевірки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerCheck_Tick(object sender, EventArgs e)
        {
            //перевірка чи встановлена ознака сну
            if (Sleep)
            {
                //true коли відбулась перевірка, false коли збережено нову позицію яку потрібно перевірити
                if (Check)
                {
                    //зберігає положення курсору
                    MouseCapture();
                    //Check = false;
                }
                else
                {
                    //перевіряє положення курсору
                    MouseWatching();
                    //Check = true;
                }
                //змінює стан ознаки
                ChangeCheck();
                //заново запускає таймер перевірки
                StartCheck();
            }
        }
        /// <summary>
        /// метод для змінення ознаки перевірки на протилежне значення
        /// </summary>
        private void ChangeCheck()
        {
            Check = !Check;
        }
        /// <summary>
        /// подія включає роботу програми, тобто переводить параметр Sleep в значення true
        /// також відбуваються дрібні косметичні зміні для відображення, що програма працює
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            Sleep = true;
            Check = true;
            label1.Text = "Sleeping";
            conditionToolStripMenuItem.Text = "Sleeping";
            conditionToolStripMenuItem.BackColor = Color.FromArgb(225, 255, 210);
            StartCheck();
        }
        /// <summary>
        /// подія вимикає функціонал програми, тобто переводить параметр Sleep в значення false
        /// також відбуваються дрібні косметичні зміні для відображення, що програма не працює
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <summary>
        /// закриває програму по кнопці в верхньому меню
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopCheck();
            ToAwake();
            this.Close();
        }
        /// <summary>
        /// відкриває форму з налаштуваннями параметрів для таймерів та радіусу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void customizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Form2 customizeForm = new Form2())
            {
                //передаємо параметри на форму для відображення
                customizeForm.SleepTime = SleepTime;
                customizeForm.CheckTime = CheckTime;
                customizeForm.Radius = Radius;
                //відкриваємо у режимі діалогового вікна
                if (customizeForm.ShowDialog() == DialogResult.OK)
                {
                    //зчитуємо нвоі параметри
                    SleepTime = customizeForm.SleepTime;
                    CheckTime = customizeForm.CheckTime;
                    Radius = customizeForm.Radius;

                    //зберігаємо нові параметри до файле з параметрами
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
        /// <summary>
        /// метод для відслідковування зміни стану облікового запису користувача
        /// змінює ознаку блокування в залежності від зміни стану
        /// передається в делегат в систему Windows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
