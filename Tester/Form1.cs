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
            LoadUniqeRuns();
        }

        private void LoadUniqeRuns()
        {
            foreach (var item in ExecTester.Info)
            {
                if (!listBox1.Items.Contains(item))
                {
                    listBox1.Items.Add(item);
                }
            }
        }

        private void SaveFilePath()
        {
            System.IO.File.WriteAllText(this.saveFilePath, openFileDialog1.FileName);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var info = listBox1.SelectedItem as RunInfo;

            if(info?.Status == Runner.Status.Successful)
            {
                var frames = info.Frames;
                List<float> time = new List<float>();
                List<float> delta_ang = new List<float>();
                List<float> speed = new List<float>();
                time.Add(frames[0].TimeDelta);
                delta_ang.Add(0);
                speed.Add(0);

                for (int i =1; i<info.Frames.Count; i++)
                {
                    time.Add(frames[i].TimeDelta + time[i-1]);
                    var delta = frames[i].RotationAngle - frames[i - 1].RotationAngle;
                    if (delta < 0)
                        delta += 360;
                    delta_ang.Add(delta);
                    speed.Add(delta_ang[i] / frames[i].TimeDelta);
                }

                this.chart1.Series.Clear();
                this.chart1.Series.Add("Angular velocity");
                this.chart1.Series["Angular velocity"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                for (int i = 1; i < speed.Count; i++)
                {
                    this.chart1.Series["Angular velocity"].Points.AddXY(time[i], speed[i]);
                }
                var stats = $"mean = {speed.Average()}";
                this.statsTextBox.Text = stats;
            }
        }
    }
}
