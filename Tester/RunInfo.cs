﻿using System;
using System.Text.RegularExpressions;

namespace Tester
{
    public class RunInfo
    {
        public int Rotations { get; private set; }

        public RunInfo(string output)
        {
            this.Parse(output);
        }

        private void Parse(string output)
        {
            var pattern = @"Rotations: (\d)";
            Regex reg = new Regex(pattern);
            var match = reg.Match(output);
            if (match.Success)
            {
                this.Rotations = int.Parse(match.Groups[1].Value);
            }
            else
            {
                this.Rotations = 0;
            }
        }
    }
}