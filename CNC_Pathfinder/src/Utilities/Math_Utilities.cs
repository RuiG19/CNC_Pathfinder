using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CNC_Pathfinder.src.GCode;

namespace CNC_Pathfinder.src.Utilities
{
    class Math_Utilities
    {

        public class Line
        {
            public Point p1 { get; set; }
            public Point p2 { get; set; }
            
            public Line(Point p1, Point p2)
            {
                this.p1 = new Point(p1.X,p1.Y);
                this.p2 = new Point(p2.X, p2.Y);
            }

            public Line(int x1, int y1, int x2, int y2)
            {
                this.p1 = new Point(x1, y1);
                this.p2 = new Point(x2, y2);
            }
        }

        public class LineF
        {
            public PointF p1 { get; set; }
            public PointF p2 { get; set; }

            public LineF(PointF p1, PointF p2)
            {
                this.p1 = new PointF(p1.X, p1.Y);
                this.p2 = new PointF(p2.X, p2.Y);
            }

            public LineF(float x1, float y1, float x2, float y2)
            {
                this.p1 = new PointF(x1, y1);
                this.p2 = new PointF(x2, y2);
            }
        }

        public class BezierF
        {
            public PointF Start { get; set; }
            public PointF End { get; set; }
            public PointF FirstControl { get; set; }
            public PointF SecondControl { get; set; }

            public BezierF(float start_x, float start_y, float c1_x, float c1_y, float c2_x, float c2_y, float end_x, float end_y)
            {
                Start = new PointF(start_x, start_y);
                End = new PointF(end_x, end_y);
                FirstControl = new PointF(c1_x, c1_y);
                SecondControl = new PointF(c2_x, c2_y);
            }

            public BezierF(PointF Start, PointF FirstControl, PointF SecondControl, PointF End)
            {
                this.Start = Start;
                this.FirstControl = FirstControl;
                this.SecondControl = SecondControl;
                this.End = End;
            }

            /// <summary>
            /// Creates a Cubic Bezier Curve from a Quadratic Bezier Curve.
            /// </summary>
            /// <param name="Start"></param>
            /// <param name="Control"></param>
            /// <param name="End"></param>
            public BezierF(PointF Start, PointF Control, PointF End)
            {
                this.Start = Start;
                this.FirstControl = new PointF(Start.X + (2f/3f) * (Control.X - Start.X), Start.Y + (2f / 3f) * (Control.Y - Start.Y));
                this.SecondControl = new PointF(End.X + (2f / 3f) * (Control.X - End.X), End.Y + (2f / 3f) * (Control.Y - End.Y));
                this.End = End;
            }
        }



        // Angles Calc

        public static float AngleBetween_v2(Point A, Point B)
        {
            double A_Angle = Math.Acos(A.X / (Math.Sqrt(Math.Pow(A.X, 2) + Math.Pow(A.Y, 2))));

            if (A.Y < 0)
            {
                A_Angle = 2 * Math.PI - A_Angle;
            }

            double B_Angle = Math.Acos(B.X / (Math.Sqrt(Math.Pow(B.X, 2) + Math.Pow(B.Y, 2))));

            if (B.Y < 0)
            {
                B_Angle = 2 * Math.PI - B_Angle;
            }

            double Angle = B_Angle - A_Angle;

            if (Angle < 0)
            {
                Angle += 2 * Math.PI;
            }

            return Convert.ToSingle(Angle * 180 / Math.PI);
        }

        public static float AngleBetween_v2(PointF A, PointF B)
        {
            double A_Angle = Math.Acos(A.X / (Math.Sqrt(Math.Pow(A.X, 2) + Math.Pow(A.Y, 2))));

            if (A.Y < 0)
            {
                A_Angle = 2 * Math.PI - A_Angle;
            }

            double B_Angle = Math.Acos(B.X / (Math.Sqrt(Math.Pow(B.X, 2) + Math.Pow(B.Y, 2))));

            if (B.Y < 0)
            {
                B_Angle = 2 * Math.PI - B_Angle;
            }

            double Angle = B_Angle - A_Angle;

            if (Angle < 0)
            {
                Angle += 2 * Math.PI;
            }

            return Convert.ToSingle(Angle * 180 / Math.PI);
        }

        public static float AngleBetween_v2(GCODE.Location A, GCODE.Location B)
        {
            double A_Angle = Math.Acos( A.x / (Math.Sqrt(Math.Pow(A.x,2) + Math.Pow(A.y,2))) );

            if(A.y < 0)
            {
                A_Angle = 2 * Math.PI - A_Angle;
            }

            double B_Angle = Math.Acos( B.x / (Math.Sqrt(Math.Pow(B.x, 2) + Math.Pow(B.y, 2))) );

            if (B.y < 0)
            {
                B_Angle = 2 * Math.PI - B_Angle;
            }

            double Angle = B_Angle - A_Angle;

            if(Angle < 0)
            {
                Angle += 2 * Math.PI;
            }

            return Convert.ToSingle(Angle * 180 / Math.PI);
        }

        // following functions are deprecated

        private static float AngleBetween(Point A, Point B)
        {
            double angle = (A.X * B.X + A.Y * B.Y);

            angle /= (Math.Sqrt(Math.Pow(A.X, 2) + Math.Pow(A.Y, 2)) * Math.Sqrt(Math.Pow(B.X, 2) + Math.Pow(B.Y, 2)));

            if (angle > 1)
            {
                angle = Math.Floor(angle);
            }
            else if (angle < -1)
            {
                angle = Math.Ceiling(angle);
            }

            angle = Math.Acos(angle);

            angle *= 180.0 / Math.PI;

            if (A.Y < 0) // angulos negativos
            {
                angle *= -1;
            }
                        

            return Convert.ToSingle(angle);
        }

        private static float AngleBetween(PointF A, PointF B)
        {
            double angle = (A.X * B.X + A.Y * B.Y);

            angle /= (Math.Sqrt(Math.Pow(A.X, 2) + Math.Pow(A.Y, 2)) * Math.Sqrt(Math.Pow(B.X, 2) + Math.Pow(B.Y, 2)));

            if (angle > 1)
            {
                angle = Math.Floor(angle);
            }
            else if (angle < -1)
            {
                angle = Math.Ceiling(angle);
            }

            angle = Math.Acos(angle);

            if (A.Y < 0) // angulos negativos
            {
                angle *= -1;
            }

            angle *= 180 / Math.PI;


            return Convert.ToSingle(angle);
        }

        private static float AngleBetween(GCODE.Location A, GCODE.Location B)
        {
            double angle = (A.x * B.x + A.y * B.y);

            angle /= (Math.Sqrt(Math.Pow(A.x, 2) + Math.Pow(A.y, 2)) * Math.Sqrt(Math.Pow(B.x, 2) + Math.Pow(B.y, 2)));

            if (angle > 1)
            {
                angle = Math.Floor(angle);
            }
            else if (angle < -1)
            {
                angle = Math.Ceiling(angle);
            }

            angle = Math.Acos(angle);

            angle *= 180 / Math.PI;


            if (A.y < 0) // angulos negativos
            {
                angle *= -1;
            }

            return Convert.ToSingle(angle);
        }

        // ---------

        public static double map(double value, double from_min, double from_max, double to_min, double to_max)
        {
            return (value - from_min) * (to_max - to_min) / (from_max - from_min) + to_min;
        }

        public static int Abs(int i)
        {
            return (i + (i >> 31)) ^ (i >> 31);
        }

        public static float Abs(float f)
        {
            return Convert.ToSingle(Math.Abs(f));
        }

        public static float Sqrt(float f)
        {
            return Convert.ToSingle(Math.Sqrt(f));
        }

        public static float Pow(float f, float e)
        {
            return Convert.ToSingle(Math.Pow(f,e));
        }

        public static void MatrixTransfortion(double[] matrix, double oldX, double oldY, out double newX, out double newY)
        {
            //
            // matrix = { a , b , c , d , e , f }:
            //
            // + - + - + - +
            // | a | c | e |
            // + - + - + - +
            // | b | d | f |
            // + - + - + - +
            // | 0 | 0 | 1 |
            // + - + - + - +

            // newX = OldX * a + oldY * c + e 
            newX = oldX * matrix[0] + oldY * matrix[2] + matrix[4];

            // newY = OldX * b + oldY * d + f 
            newY = oldX * matrix[1] + oldY * matrix[3] + matrix[5];
        }


        // Ellipse To arc Functions

        public static PointF[] FindEllipsePoints(float Cx, float Cy, float Rx, float Ry, int NumberOfArcsPerQuadrant)
        {
            float Rx2 = Rx * Rx; // Rx ^2
            float Ry2 = Ry * Ry; // Ry ^2
            float Rxy = Rx * Ry;
            float invRy2mRx2 = 1 / (Ry2 - Rx2); // 1 / (Rx^2 - Rx^2) -> Rx != Ry

            int NumberOfPoints = NumberOfArcsPerQuadrant * 4;
            PointF[] EllipsePoints = new PointF[NumberOfPoints];

            // Store the vertical and horizontal points (a,0), (0,b), ...
            EllipsePoints[0] = new PointF(Cx + Rx, Cy);                             // (a,0)
            EllipsePoints[NumberOfArcsPerQuadrant] = new PointF(Cx, Cy + Ry);       // (0,b)
            EllipsePoints[NumberOfArcsPerQuadrant * 2] = new PointF(Cx - Rx, Cy);   // (-a,0)
            EllipsePoints[NumberOfArcsPerQuadrant * 3] = new PointF(Cx , Cy - Ry);   // (0,-b)

            float curv0 = Rx / Ry2;
            float curv1 = Ry / Rx2;

            float invNumberOfArcsPerQuadrant = 1 / Convert.ToSingle(NumberOfArcsPerQuadrant);

            for (int i = 1; i < NumberOfArcsPerQuadrant; i++)
            {
                float weight1 = Convert.ToSingle(i) * invNumberOfArcsPerQuadrant;
                float weight0 = 1f - weight1;

                float curv = weight0 * curv0 + weight1 * curv1;

                float tmp = Pow((Rxy / curv), (2f / 3f));

                EllipsePoints[i] = new PointF(Cx + Rx * Sqrt(Abs((tmp - Rx2) * invRy2mRx2)),
                                              Cy + Ry * Sqrt(Abs((tmp - Ry2) * invRy2mRx2)));
                EllipsePoints[NumberOfPoints / 2 - i] = new PointF(Cx - Rx * Sqrt(Abs((tmp - Rx2) * invRy2mRx2)),
                                                                   Cy + Ry * Sqrt(Abs((tmp - Ry2) * invRy2mRx2)));
                EllipsePoints[NumberOfPoints / 2 + i] = new PointF(Cx - Rx * Sqrt(Abs((tmp - Rx2) * invRy2mRx2)),
                                                                   Cy - Ry * Sqrt(Abs((tmp - Ry2) * invRy2mRx2)));
                EllipsePoints[NumberOfPoints - i] = new PointF(Cx + Rx * Sqrt(Abs((tmp - Rx2) * invRy2mRx2)),
                                                               Cy - Ry * Sqrt(Abs((tmp - Ry2) * invRy2mRx2)));

            }

            return EllipsePoints;
        }


        public static PointF FindArcCenter(PointF P1, PointF P2, PointF P3)
        {
            return FindArcCenter(P1.X, P1.Y, P2.X, P2.Y, P3.X , P3.Y);
        }

        public static PointF FindArcCenter(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            // P1P2 Line
            float P1P2_Slop = (y2 - y1) / (x2 - x1);
            float P1P2_Offset = y1 - P1P2_Slop * x1;

            float P1P2_Mid_X = (x2 + x1) / 2;
            float P1P2_Mid_Y = (y2 + y1) / 2;
            
            // P1P2 Line
            float P2P3_Slop = (y3 - y2) / (x3 - x2);
            float P2P3_Offset = y2 - P2P3_Slop * x2;

            float P2P3_Mid_X = (x3 + x2) / 2;
            float P2P3_Mid_Y = (y3 + y2) / 2;

            // P1P2 Ortogonal Line
            float P1P2_Slop_Ortogonal = -1 / P1P2_Slop;
            float P1P2_Offset_Ortogonal = P1P2_Mid_Y - P1P2_Slop_Ortogonal * P1P2_Mid_X;

            float P2P3_Slop_Ortogonal = -1 / P2P3_Slop;
            float P2P3_Offset_Ortogonal = P2P3_Mid_Y - P2P3_Slop_Ortogonal * P2P3_Mid_X;

            // Intersection of the 2 ortogonal lines = center
            float Cx = (P2P3_Offset_Ortogonal - P1P2_Offset_Ortogonal) / (P1P2_Slop_Ortogonal - P2P3_Slop_Ortogonal);
            float Cy = Cx * P1P2_Slop_Ortogonal + P1P2_Offset_Ortogonal;

            return new PointF(Cx, Cy);
        }

        // Bezier Functions

        /// <summary>
        /// De Casteljau Algorithm
        /// </summary>
        /// <param name="Original"></param>
        /// <param name="FirstHalf"></param>
        /// <param name="SecondHalf"></param>
        public static void SplitCubicBezier(BezierF Original, out BezierF FirstHalf, out BezierF SecondHalf)
        {
            PointF R2 = new PointF( (Original.Start.X + Original.FirstControl.X) / 2f, 
                                    (Original.Start.Y + Original.FirstControl.Y) / 2f);

            PointF R3 = new PointF( R2.X / 2f + (Original.FirstControl.X + Original.SecondControl.X) / 4f, 
                                    R2.Y / 2f + (Original.FirstControl.Y + Original.SecondControl.Y) / 4f);

            PointF S3 = new PointF( (Original.End.X + Original.SecondControl.X) / 2f, 
                                    (Original.End.Y + Original.SecondControl.Y) / 2f);

            PointF S2 = new PointF( S3.X / 2f + (Original.FirstControl.X + Original.SecondControl.X) / 4f,
                                    S3.Y / 2f + (Original.FirstControl.Y + Original.SecondControl.Y) / 4f);

            PointF R4 = new PointF( (R3.X + S2.X) / 2f, (R3.Y + S2.Y) / 2f);

            FirstHalf = new BezierF(Original.Start, R2, R3, R4);
            SecondHalf = new BezierF(R4, S2, S3, Original.End);
        }

        public static List<LineF> BezierLinearInterpolation(BezierF Curve, float MinimumError)
        {
            List<LineF> BezierLines = new List<LineF>();
            float CurveToLineError = BezierLineError(Curve);

            if (CurveToLineError < MinimumError)
            {
                BezierLines.Add(new LineF(Curve.Start, Curve.End));
            }
            else
            {
                BezierF FirstHalf, SecondHalf;
                SplitCubicBezier(Curve, out FirstHalf, out SecondHalf);

                if(BezierLineError(FirstHalf) < CurveToLineError)
                {
                    BezierLines.AddRange(BezierLinearInterpolation(FirstHalf, MinimumError));
                }
                else
                {
                    BezierLines.Add(new LineF(FirstHalf.Start, FirstHalf.End));
                }
                if (BezierLineError(SecondHalf) < CurveToLineError)
                {
                    BezierLines.AddRange(BezierLinearInterpolation(SecondHalf, MinimumError));
                }
                else
                {
                    BezierLines.Add(new LineF(SecondHalf.Start, SecondHalf.End));
                }
                    
            }

            return BezierLines;
        }

        public static float BezierLineError(BezierF Curve)
        {
            float StartEnd_Lenght = Convert.ToSingle(Math.Sqrt( Math.Pow(Curve.End.X - Curve.Start.X,2) + Math.Pow(Curve.End.Y - Curve.Start.Y, 2) ));

            float C1_LineDistance = perpendicularDistance(Curve.FirstControl , new LineF(Curve.Start, Curve.End));
            float C2_LineDistance = perpendicularDistance(Curve.SecondControl, new LineF(Curve.Start, Curve.End));

            float error1 = Abs(C1_LineDistance) / StartEnd_Lenght;
            float error2 = Abs(C2_LineDistance) / StartEnd_Lenght;

            return (error1 > error2) ? error1 : error2;
        }

        // Distances

        /// <summary>
        /// Measures the distance between two Points. 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static double DistanceBetweenPoints(Point start, Point end)
        {
            return Math.Sqrt(Math.Pow(((double)end.X - start.X), 2) + Math.Pow((double)end.Y - start.Y, 2));
        }

        /// <summary>
        /// Measures the square (x^2 + y^2) distance between two Points. Square root isn't applied for performance improvements.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static double SquareDistanceBetweenPoints(Point start, Point end)
        {
            return /*Math.Abs(Math.Sqrt*/(Math.Pow(((double)end.X - start.X), 2) + Math.Pow((double)end.Y - start.Y, 2));//);
        }


        public static float perpendicularDistance(PointF p, LineF l)
        {
            if((l.p2.Y == l.p1.Y) && (l.p2.X == l.p1.X)) // not a line but a point
            {
                //return 0;
                return Sqrt(Pow(l.p2.Y - p.Y, 2) + Pow(l.p2.X - p.X, 2));
            }

            float numerator = Abs((l.p2.Y - l.p1.Y) * p.X - (l.p2.X - l.p1.X) * p.Y + l.p2.X * l.p1.Y - l.p2.Y * l.p1.X);
            
            float denominator = Sqrt(Pow(l.p2.Y - l.p1.Y, 2) + Pow(l.p2.X - l.p1.X, 2));

            return numerator / denominator;
        }

        // --

        /// <summary>
        /// Perpendicular distance between a Point p and a Line l. 
        /// https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
        /// </summary>
        /// <param name="p"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        public static double perpendicularDistance(Point p, Line l)
        {
            if ((l.p2.Y == l.p1.Y) && (l.p2.X == l.p1.X))
            {
                //return 0;
                return Math.Sqrt(Math.Pow(l.p2.Y - p.Y, 2) + Math.Pow(l.p2.X - p.X, 2));
            }

            double numerator = Abs( (l.p2.Y - l.p1.Y) * p.X - (l.p2.X - l.p1.X) * p.Y + l.p2.X * l.p1.Y - l.p2.Y * l.p1.X);

            double denominator = Math.Sqrt( Math.Pow(l.p2.Y - l.p1.Y,2) + Math.Pow(l.p2.X - l.p1.X, 2));

            return numerator / denominator;
        }

        /// <summary>
        /// 
        /// https://en.wikipedia.org/wiki/Ramer%E2%80%93Douglas%E2%80%93Peucker_algorithm
        /// </summary>
        /// <param name="CornerPoints"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public List<Point> DoublasPeucker(List<Point> CornerPoints, double epsilon)
        {
            // Find the point with the maximum distance
            double dmax = 0;
            int index = 0;
            Line referenceLine = new Line(CornerPoints[0], CornerPoints[CornerPoints.Count - 1]); // line form the starting point to the last
            for(int i = 1; i < CornerPoints.Count - 2; i++)
            {
                double d = perpendicularDistance(CornerPoints[i], referenceLine);
                if(d > dmax)
                {
                    index = i;
                    dmax = d;
                }
            }
            // If max distance is greater than epsilon, recursively simplify
            List<Point> PathPoints = new List<Point>();
            if (dmax > epsilon)
            {
                PathPoints.AddRange(DoublasPeucker(CornerPoints.GetRange(0, index), epsilon));
                PathPoints.AddRange(DoublasPeucker(CornerPoints.GetRange(index, CornerPoints.Count - 1), epsilon));
            }
            else
            {
                PathPoints.Add(CornerPoints[0]);
                PathPoints.Add(CornerPoints[CornerPoints.Count - 1]);
            }
                            
            return PathPoints;
        }

    }
}
