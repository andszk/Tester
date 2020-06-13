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
using static Tester.RunInfo;

namespace Tester
{
    public partial class Form1 : Form
    {
        private string saveFilePath = @".\defaultBinary.txt";
        private ExecTester ExecTester { get; }
        private readonly object infoLock = new object();
        private readonly object globalStatLock = new object();

        public Form1()
        {
            InitializeComponent();
            this.ExecTester = new ExecTester();            
            foreach(var info in ExecTester.Info)
            {
                this.LoadNewResult(info);
            }
            if (listBox1.Items.Count > 0)
            {
                UpdateGlobalStatistics();
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
            numericUpDown1.Enabled = false;
            numericUpDown2.Enabled = false;
            progressBar1.Maximum = (int)numericUpDown1.Value;
            progressBar1.Value = 0;
            progressBar1.Visible = true;

            // only wait for enabling controls, don't block GUI thread otherwise
            var waitThread = new Thread(() => {
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < numericUpDown1.Value; i++)
                {
                    Task t = Task.Run(() =>
                    {
                        var info = this.ExecTester.Run((int)numericUpDown2.Value);
                        lock (infoLock)
                        {
                            LoadNewResult(info);
                        }
                        UpdateGlobalStatistics();
                    });
                    UpdateProgressBarOnCompletion(t);
                    tasks.Add(t);
                    //Don't start all at once, as it will give false results. Random seed is time based
                    Thread.Sleep(50);
                }

                Task.WaitAll(tasks.ToArray());
                EnableControlsSafe();
            });
            waitThread.Start();
        }

        async Task UpdateProgressBarOnCompletion(Task task)
        {
            await task;
            UpdateProgressBar();
        }

        private void UpdateProgressBar()
        {
            if (this.InvokeRequired)
            {
                SafeVoidDelagate d = new SafeVoidDelagate(UpdateProgressBar);
                progressBar1.Invoke(d, new object[] { });
            }
            else
            {
                this.progressBar1.Value++;
            }
        }

        private void EnableControlsSafe()
        {
            if (this.InvokeRequired)
            {
                SafeVoidDelagate d = new SafeVoidDelagate(EnableControlsSafe);
                listBox1.Invoke(d, new object[] { });
            }
            else
            {
                button2.Enabled = true;
                numericUpDown1.Enabled = true;
                numericUpDown2.Enabled = true;
                progressBar1.Visible = false;
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

        private void UpdateGlobalStatistics()
        {
            if (!InvokeRequired)
            {
                lock (globalStatLock)
                {
                    var runs = listBox1.Items.Cast<RunInfo>();
                    var validRuns = runs.Where(item => item.Status == Runner.Status.Successful);
                    var speeds = validRuns.Select(run => CalculateFrames(run).speed);
                    int totalRot = validRuns.Sum(run => run.Rotations);
                    var rotations = validRuns.Select(run => run.Rotations);
                    List<int> numberOfRotations = new List<int>();
                    List<int> count = new List<int>();
                    for (int i = rotations.Min(); i <= rotations.Max(); i++)
                    {
                        numberOfRotations.Add(i);
                        count.Add(rotations.Count(r => r == i));
                    }

                    this.chart2.Series.Clear();
                    this.chart2.Series.Add("Rotations");
                    this.chart2.ChartAreas.First().AxisX.Title = "Number of rotations in single run";
                    this.chart2.ChartAreas.First().AxisY.Title = "Count";
                    this.chart2.Series["Rotations"].Label = null;
                    this.chart2.Series["Rotations"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
                    for (int i = 0; i < count.Count; i++)
                    {
                        this.chart2.Series["Rotations"].Points.AddXY(numberOfRotations[i], count[i]);
                    }
                    var crashed = runs.Where(item => item.Status == Runner.Status.Crashed).Count();
                    double crashedd = (double)crashed / runs.Count() * 100;
                    var allRuns = new List<float>();
                    foreach(var run in validRuns)
                    {
                        var times = run.Frames.Select(frame => frame.TimeDelta).ToList();
                        allRuns.AddRange(times);
                    }
                    textBox2.Text = $"Total rotations {totalRot} in successful {rotations.Count()} runs. {crashedd:0.00}% crashed.\r\n" +
                        $"Median frame length from all runs {Statistics.Median(allRuns):0.00} ms, mean {Statistics.Mean(allRuns):0.00} ms";
                }
            }
            else
            {
                SafeVoidDelagate d = new SafeVoidDelagate(UpdateGlobalStatistics);
                chart2.Invoke(d, new object[] {});
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
                var (time, speed) = CalculateFrames(info);
                this.chart1.Series.Clear();
                this.chart1.ChartAreas[0].AxisX.Title = "Time";
                this.chart1.ChartAreas[0].AxisY.Title = "Angular velocity";
                this.chart1.ChartAreas[0].AxisX.LabelStyle.Format = "{0:000}ms";
                this.chart1.Series.Add("Angular velocity");
                this.chart1.Series["Angular velocity"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                for (int i = 1; i < speed.Count; i++)
                {
                    this.chart1.Series["Angular velocity"].Points.AddXY(time[i], speed[i]);
                }
                var stats = $"median = {Statistics.Median(speed):0.000}, value = {Statistics.Mean(speed):0.000} +- {Statistics.StandardDeviation(speed):0.000}, variance {Statistics.Variance(speed):0.000} [deg/s]";
                this.statsTextBox.Text = stats;
            }
            if(info?.Status == Runner.Status.Crashed)
            {
                if(File.Exists(info.CrashLogPath))
                {
                    System.Diagnostics.Process.Start(info.CrashLogPath);
                }
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
