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

namespace Tester
{
    public partial class Form1 : Form
    {
        private string saveFilePath = @".\defaultBinary.txt";
        private ExecTester ExecTester { get; }
        public Form1()
        {
            InitializeComponent();
            this.ExecTester = new ExecTester();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(saveFilePath))
            {
                string fileName = System.IO.File.ReadAllText(saveFilePath);
                this.textBox1.Text = fileName;
                this.button2.Enabled = true;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            ExecTester.FilePath = textBox1.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if(openFileDialog1.CheckFileExists)
                {
                    this.textBox1.Text = openFileDialog1.FileName;
                    this.button2.Enabled = true;
                    SaveFilePath();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.ExecTester.Run();
        }

        private void SaveFilePath()
        {
            System.IO.File.WriteAllText(this.saveFilePath, openFileDialog1.FileName);
        }
    }
}
