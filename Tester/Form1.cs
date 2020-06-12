using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MathNet.Numerics.Statistics;

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
            
            foreach(var info in ExecTester.Info)
            {
                this.LoadNewResult(info);
            }

            var logdir = @".\logs\";

            if (!System.IO.Directory.Exists(logdir))
            {
                System.IO.Directory.CreateDirectory(logdir);
            }
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
            button2.Enabled = false;

            // only wait for enabling button, don't block GUI thread otherwise
            var waitThread = new Thread(() => {
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < numericUpDown1.Value; i++)
                {
                    Task t = Task.Run(() =>
                    {
                        var info = this.ExecTester.Run();
                        Thread load = new Thread( ()=> LoadNewResult(info));
                        load.Start();
                    });
                    tasks.Add(t);
                    //Don't start all at once, as it will give false results. Random seed is time based
                    Thread.Sleep(50);
                }

                Task.WaitAll(tasks.ToArray());
                EnableButtonSafe();
            });
            waitThread.Start();
        }

        private void EnableButtonSafe()
        {
            if (this.InvokeRequired)
            {
                SafeVoidDelagate d = new SafeVoidDelagate(EnableButtonSafe);
                listBox1.Invoke(d, new object[] { });
            }
            else
            {
                button2.Enabled = true;
            }
        }

        delegate void SafeVoidDelagate();

        private void LoadNewResult(RunInfo info)
        {
            if (!listBox1.InvokeRequired)
            {
                listBox1.Items.Add(info);
            }
            else
            {
                SafeCallDelegate d = new SafeCallDelegate(LoadNewResult);
                listBox1.Invoke(d, new object[] { info });
            }
        }

        delegate void SafeCallDelegate(RunInfo info);

        private void SaveFilePath()
        {
            System.IO.File.WriteAllText(this.saveFilePath, openFileDialog1.FileName);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var info = listBox1.SelectedItem as RunInfo;

            if(info?.Status == Runner.Status.Successful)
            {
                var (time, speed) = CalculateFrames(info);
                this.chart1.Series.Clear();
                this.chart1.Series.Add("Angular velocity");
                this.chart1.Series["Angular velocity"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                for (int i = 1; i < speed.Count; i++)
                {
                    this.chart1.Series["Angular velocity"].Points.AddXY(time[i], speed[i]);
                }
                var stats = $"median = {Statistics.Median(speed):0.000}, value = {Statistics.Mean(speed):0.000} +- {Statistics.StandardDeviation(speed):0.000}, variance {Statistics.Variance(speed):0.000} ";
                this.statsTextBox.Text = stats;
            }
        }

        private (List<float> time, List<float> speed) CalculateFrames(RunInfo info)
        {
            var frames = info.Frames;

            List<float> time = new List<float>();
            List<float> delta_ang = new List<float>();
            List<float> speed = new List<float>();
            time.Add(frames[0].TimeDelta);
            delta_ang.Add(0);
            speed.Add(0);
            for (int i = 1; i < info.Frames.Count; i++)
            {
                time.Add(frames[i].TimeDelta + time[i - 1]);
                var delta = frames[i].RotationAngle - frames[i - 1].RotationAngle;
                if (delta < 0)
                    delta += 360;
                delta_ang.Add(delta);
                speed.Add(delta_ang[i] / frames[i].TimeDelta);
            }
            return (time, speed);
        }
    }
}
