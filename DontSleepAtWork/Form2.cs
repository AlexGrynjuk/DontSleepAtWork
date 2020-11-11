using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DontSleepAtWork
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            numericUpDown1.Controls.RemoveAt(0);
            numericUpDown2.Controls.RemoveAt(0);
            numericUpDown3.Controls.RemoveAt(0);
        }

        //private Form1 fromMain
        //{
        //    get { return (Form1)this.Parent; }
        //}

        public int SleepTime { get; set; }

        public int CheckTime { get; set; }

        public int Radius { get; set; }

        private void Form2_Load(object sender, EventArgs e)
        {
            numericUpDown1.Text = SleepTime.ToString();
            numericUpDown2.Text = CheckTime.ToString();
            numericUpDown3.Text = Radius.ToString();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            SleepTime = Convert.ToInt32(numericUpDown1.Text);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            CheckTime = Convert.ToInt32(numericUpDown2.Text);
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            Radius = Convert.ToInt32(numericUpDown3.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FileStream f = File.Open("Settings", FileMode.OpenOrCreate);
            f.Close();
            using (StreamWriter stream = new StreamWriter("Settings", false))
            {
                stream.WriteLine(SleepTime.ToString());
                stream.WriteLine(CheckTime.ToString());
                stream.WriteLine(Radius.ToString());
            }
        }
    }
}
