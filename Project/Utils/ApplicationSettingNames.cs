using System;
using System.Diagnostics;
using System.IO.IsolatedStorage;

namespace Kazyx.WPPMM.Utils
{
    public class FramingGridTypes
    {
        public const string Off = "grid_off";
        public const string RuleOfThirds = "grid_rule_third";
        public const string Diagonal = "diagonal";
        public const string GoldenRatio = "grid_golden_ratio";
        public const string Crosshairs = "grid_crosshairs";
        public const string Square = "grid_square";
        public const string Fibonacci = "grid_fibonacci";
    }

    public class FramingGridColor
    {
        public const string White = "white";
        public const string Black = "black";
        public const string Red = "red";
        public const string Blue = "blue";
        public const string Green = "green";
    }

    public class FibonacciLineOrigin
    {
        public const string UpperLeft = "upper_left";
        public const string UpperRight = "upper_right";
        public const string BottomLeft = "bottom_left";
        public const string BottomRight = "bottom_right";
    }
}