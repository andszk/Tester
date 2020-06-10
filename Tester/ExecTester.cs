using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
    class ExecTester
    {
        public string FilePath { get; set; }
        public List<RunInfo> Info { get; set; } = new List<RunInfo>();

        public void Run()
        {
            ProcessStartInfo cmd = new ProcessStartInfo {
                FileName = FilePath,
                Arguments = "-t 5",
                UseShellExecute = false,
                RedirectStandardOutput = true};

            var process = Process.Start(cmd);
            var reader = process.StandardOutput;
            var output = reader.ReadToEndAsync();
            process.WaitForExit();
            output.Wait();
            Info.Add(new RunInfo(output.Result));
        }
    }
}
