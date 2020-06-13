using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tester
{
    class ExecTester
    {
        public ExecTester()
        {
            if (File.Exists(runsFilePath))
            {
                Info.AddRange(this.ReadFromBinary());
            }
        }

        public string FilePath { get; set; }
        public List<RunInfo> Info { get; private set; } = new List<RunInfo>();
        private StringBuilder errors = new StringBuilder("Errors:\n");
        private readonly object infoLock = new object();
        private readonly object writeLock = new object();
        private readonly string runsFilePath = @".\runs.bin";
        public event EventHandler RunEnded;

        public RunInfo Run(int seconds)
        {
            ProcessStartInfo cmd = new ProcessStartInfo
            {
                FileName = FilePath,
                Arguments = $"-t {seconds}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var process = Process.Start(cmd);
            var reader = process.StandardOutput;
            var output = reader.ReadToEndAsync();
            process.WaitForExit();
            RunEnded?.Invoke(this, new EventArgs());
            if (process.ExitCode == 0)
            {
                output.Wait();
                var runInfo = new RunInfo(output.Result, process);
                lock (infoLock)
                {
                    Info.Add(runInfo);
                }
                WriteToBinary(runInfo);
                return runInfo;
            }
            else
            {
                var runInfo = new RunInfo(process);
                lock (infoLock)
                {
                    Info.Add(runInfo);
                    WriteToBinary(runInfo);
                    var settings = new JsonSerializerSettings { Error = HandleSerializationError };
                    string jsonString = JsonConvert.SerializeObject(process, settings);
                    jsonString += $"\nOutput: {output.Result}";
                    jsonString += errors.ToString();
                    File.WriteAllText(runInfo.CrashLogPath, jsonString);
                    errors.Clear();
                }
                return runInfo;
            }
        }

        public void HandleSerializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs)
        {
            errors.Append(errorArgs.ErrorContext.Error.Message);
            errorArgs.ErrorContext.Handled = true;
        }

        private void WriteToBinary(RunInfo info)
        {
            lock (writeLock)
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(runsFilePath, FileMode.Append)))
                {
                    var json = JsonConvert.SerializeObject(info);
                    writer.Write(json);
                }
            }
        }

        private List<RunInfo> ReadFromBinary()
        {
            lock (infoLock)
            {
                List<RunInfo> read = new List<RunInfo>();
                if (File.Exists(runsFilePath))
                {
                    using (BinaryReader reader = new BinaryReader(File.Open(runsFilePath, FileMode.Open)))
                    {
                        try
                        {
                            while (true)
                            {
                                var json = reader.ReadString();
                                var info = JsonConvert.DeserializeObject<RunInfo>(json);
                                read.Add(info);
                            }
                        }
                        catch(EndOfStreamException eos)
                        {
                            Console.WriteLine("End of read");
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine($"Exception e {e.Message}");
                        }
                    }
                }
                return read;
            }
        }
    }
}