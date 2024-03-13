using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNC_Pathfinder.src.SVG
{
    class SVG_Path
    {
        public SVG_Point Start { get; set; }
        public SVG_Point End { get; set; }

        public SVG_Point Center { get; set; }
        public bool Clockwise { get; set; }

        public double Radius { get; set; }
        public bool SmallArc { get; set; }

        public double PathWidth { get; set; }

        public enum SVG_Path_Type
        {
            Line = 0,
            Curve_Center = 1,
            Curve_Radius = 2
        }

        public SVG_Path_Type PathType { get; private set; }

        public SVG_Path(SVG_Point Start , SVG_Point End, double PathWidth)
        {
            PathType = SVG_Path_Type.Line;
            this.Start = Start;
            this.End = End;
            this.PathWidth = PathWidth;
        }

        public SVG_Path(PointF Start, PointF End, double PathWidth)
        {
            PathType = SVG_Path_Type.Line;
            this.Start = new SVG_Point(Start);
            this.End = new SVG.SVG_Point(End);
            this.PathWidth = PathWidth;
        }

        public SVG_Path(SVG_Point Start, SVG_Point End, SVG_Point Center, double PathWidth)
        {
            PathType = SVG_Path_Type.Curve_Center;
            this.Start = Start;
            this.End = End;
            this.Center = Center;
            this.PathWidth = PathWidth;
            this.Clockwise = true;
        }

        public SVG_Path(SVG_Point Start, SVG_Point End, SVG_Point Center, double PathWidth, bool Clockwise)
        {
            PathType = SVG_Path_Type.Curve_Center;
            this.Start = Start;
            this.End = End;
            this.Center = Center;
            this.PathWidth = PathWidth;
            this.Clockwise = Clockwise;
        }

        public SVG_Path(PointF Start, PointF End, PointF Center, double PathWidth, bool Clockwise)
        {
            PathType = SVG_Path_Type.Curve_Center;
            this.Start = new SVG_Point(Start);
            this.End = new SVG_Point(End);
            this.Center = new SVG_Point(Center);
            this.PathWidth = PathWidth;
            this.Clockwise = Clockwise;
        }

        public SVG_Path(SVG_Point Start, SVG_Point End, double PathWidth, double Radius, bool Clockwise, bool SmallArc)
        {
            PathType = SVG_Path_Type.Curve_Radius;
            this.Start = new SVG_Point(Start);
            this.End = new SVG_Point(End);
            this.Radius = Radius;
            this.PathWidth = PathWidth;
            this.Clockwise = Clockwise;
            this.SmallArc = SmallArc;
        }
    }
}
