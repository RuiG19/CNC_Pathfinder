using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Svg;

namespace CNC_Pathfinder.src.SVG
{
    class SVG_Types
    {
        private static SvgColourConverter ColorConverter = new SvgColourConverter();

        public static string getType(Svg.SvgElement e)
        {
            return e.GetType().ToString();
        }

        public static string getType(Svg.Pathing.SvgPathSegment s) 
        {
            return s.GetType().ToString();
        }

        public static string getType(Svg.Transforms.SvgTransform t)
        {
            return t.GetType().ToString();
        }

        public static Color ColorConvertFromString(string color)
        {
            if(color == "none")
            {
                return Color.White;
            }
            return (Color)ColorConverter.ConvertFromString(color);
        }

        //
        // Types of SVG Elements
        //

        // Elements with Children
        public const string Document = "Svg.SvgDocument";
        public const string Switch = "Svg.SvgSwitch";
        public const string Group = "Svg.SvgGroup";

        // Simple Elements
        public const string Line = "Svg.SvgLine";
        public const string Path = "Svg.SvgPath";
        public const string Rectangle = "Svg.SvgRectangle";
        public const string Circle = "Svg.SvgCircle";
        public const string Ellipse = "Svg.SvgEllipse";
        public const string Polygon = "Svg.SvgPolygon";
        public const string Polyline = "Svg.SvgPolyline";
        public const string Text = "Svg.SvgText";

        //
        // Types of SVG Path Segments
        //

        public const string MoveTo = "Svg.Pathing.SvgMoveToSegment";
        public const string LineTo = "Svg.Pathing.SvgLineSegment";
        public const string ArcTo = "Svg.Pathing.SvgArcSegment";
        public const string QuadraticCurveTo = "Svg.Pathing.SvgQuadraticCurveSegment";
        public const string CubicCurveTo = "Svg.Pathing.SvgCubicCurveSegment";
        public const string ClosePath = "Svg.Pathing.SvgClosePathSegment"; // "z"

        //
        // Types of Transforms
        //

        public const string Matrix = "Svg.Transforms.SvgMatrix";
        public const string Translate = "Svg.Transforms.SvgTranslate";
        public const string Rotate = "Svg.Transforms.SvgRotate";
        public const string Scale = "Svg.Transforms.SvgScale";
        public const string Shear = "Svg.Transforms.SvgShear";
        public const string Skew = "Svg.Transforms.SvgSkew";
        


    }
}
