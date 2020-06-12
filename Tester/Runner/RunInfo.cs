using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Tester.Runner;

namespace Tester
{
    [Serializable]
    public class RunInfo
    {
        [JsonConstructor]
        public RunInfo() { }
        public RunInfo(string output, Process process)
        {
            this.Time = process.ExitTime;
            this.Parse(output);
        }

        public RunInfo(Process process)
        {
            this.Status = Status.Crashed;
            this.Time = process.ExitTime;
            this.ExitCode = process.ExitCode;
            this.CrashLogPath = $@".\logs\CrashLog{this.Time.ToString().Replace('.', ' ').Replace(':', ' ')}{process.Id}.txt";
        }

        [JsonProperty]
        public int Rotations { get; private set; }
        public List<FrameInfo> Frames { get; private set; } = new List<FrameInfo>();
        public Status Status { get; set; }
        public DateTime Time { get; set; }
        public int ExitCode { get; set; }
        public string CrashLogPath { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{Time} {Enum.GetName(typeof(Status) ,Status)}");

            switch (Status)
            {
                case Status.Successful:
                    sb.Append($", Rotations = {Rotations}");
                    break;
                case Status.Crashed:
                    sb.Append($", Exit code={ExitCode}");
                    break;
                case Status.ParseException:
                    sb.Append(", Exception parsing output");
                    break;
            }

            return sb.ToString();
        }

        private void Parse(string output)
        {
            var rotPattern = @"Rotations: (?<rot>\d)";
            var framePattern = @"dt: (?<delta>\d*.\d*) r: (?<angle>\d*.\d*)";

            try
            {
                Regex reg = new Regex(rotPattern);
                var match = reg.Match(output);
                if (match.Success)
                {
                    this.Rotations = int.Parse(match.Groups["rot"].Value);
                }
                else
                {
                    this.Rotations = 0;
                }

                var frameReg = new Regex(framePattern);
                foreach (Match frameMatch in frameReg.Matches(output))
                {
                    var delta = float.Parse(frameMatch.Groups["delta"].Value, CultureInfo.InvariantCulture);
                    var angle = float.Parse(frameMatch.Groups["angle"].Value, CultureInfo.InvariantCulture);
                    Frames.Add(new FrameInfo(delta, angle));
                }

                Status = Status.Successful;
            }
            catch (FormatException fe)
            {
                Console.Out.WriteLine($"FormatException: {fe.Message}, when parsing output.");
                Status = Status.ParseException;
            }
        }

        public class FrameInfo
        {
            public float TimeDelta;
            public float RotationAngle;

            public FrameInfo(float timeDelta, float rotationAngle)
            {
                TimeDelta = timeDelta;
                RotationAngle = rotationAngle;
            }
        }
    }
}