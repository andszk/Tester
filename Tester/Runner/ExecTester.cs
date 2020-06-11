using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tester
{
    class ExecTester
    {
        public string FilePath { get; set; }
        public static List<RunInfo> Info { get; set; } = new List<RunInfo>();

        public StringBuilder errors = new StringBuilder("Errors:\n");

        public RunInfo Run()
        {
            ProcessStartInfo cmd = new ProcessStartInfo
            {
                FileName = FilePath,
                Arguments = "-t 5",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var process = Process.Start(cmd);
            var reader = process.StandardOutput;
            var output = reader.ReadToEndAsync();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                output.Wait();
                var runInfo = new RunInfo(output.Result);
                Info.Add(runInfo);
                return runInfo;
            }
            else
            {
                var runInfo = new RunInfo(process);
                Info.Add(runInfo);
                var settings = new JsonSerializerSettings{Error = HandleSerializationError };
                string jsonString = JsonConvert.SerializeObject(process, settings);
                jsonString += $"\nOutput: {output.Result}";
                jsonString += errors.ToString();
                File.WriteAllText(runInfo.CrashLogPath, jsonString);
                return runInfo;
            }
        }

        public void HandleSerializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs)
        {
            errors.Append(errorArgs.ErrorContext.Error.Message);
            errorArgs.ErrorContext.Handled = true;
        }
    }
}