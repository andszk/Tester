﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Tester.Runner;

namespace Tester
{
    public class RunInfo
    {
        public RunInfo(string output)
        {
            this.Parse(output);
        }

        public int Rotations { get; private set; }
        public List<FrameInfo> Frames { get; private set; } = new List<FrameInfo>();
        public Status Status { get; set; }

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