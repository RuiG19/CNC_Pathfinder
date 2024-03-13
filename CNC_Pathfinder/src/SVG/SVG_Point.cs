using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNC_Pathfinder.src.SVG
{
    class SVG_Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public SVG_Point()
        {
            Set(0, 0);
        }

        public SVG_Point(double X, double Y)
        {
            Set(X, Y);
        }

        public SVG_Point(SVG_Point p)
        {
            Set(p.X, p.Y);
        }

        public SVG_Point(PointF p)
        {
            Set(p.X, p.Y);
        }

        public void Set (double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

    }
}
