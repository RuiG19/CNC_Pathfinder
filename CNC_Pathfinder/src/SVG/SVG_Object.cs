using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Svg;
using CNC_Pathfinder.src.Utilities;

namespace CNC_Pathfinder.src.SVG
{
    class SVG_Object
    {
        private static bool Bezier_UseLinearInterpolation = true;
        private static float Bezier_LinearInterpolation_MinimumError = 0.01f;

        public List<SVG_Path> Paths { get; set; }
        
        public bool Fill { get; private set; }

        public SVG_Object(SvgElement svg_object)
        {
            List<SVG_Path> Paths = new List<SVG_Path>();
            // parse the object - separate the diferente elements in lines and arcs

            if (svg_object.Fill != null && SVG_Types.ColorConvertFromString(svg_object.Fill.ToString()) != Color.White)
            {
                Fill = true;
            }

            switch (SVG_Types.getType(svg_object))
            {
                case SVG_Types.Line:
                    Paths.Add(SVG_ConvertLine(svg_object as SvgLine));
                    break;
                case SVG_Types.Path:
                    Paths.AddRange(SVG_ConvertPath(svg_object as SvgPath));
                    break;
                case SVG_Types.Rectangle:
                    Paths.AddRange(SVG_ConvertRectangle(svg_object as SvgRectangle));
                    break;
                case SVG_Types.Circle:
                    Paths.Add(SVG_ConvertCircle(svg_object as SvgCircle));
                    break;
                case SVG_Types.Ellipse:
                    Paths.AddRange(SVG_ConvertEllipse(svg_object as SvgEllipse));
                    break;
                case SVG_Types.Polygon:
                    Paths.AddRange(SVG_ConvertPolygon(svg_object as SvgPolygon));
                    break;
                case SVG_Types.Polyline:
                    Paths.AddRange(SVG_ConvertPolyline(svg_object as SvgPolyline));
                    break;
                default:
                    throw new InvalidOperationException();
            }

            // Verify if Transformations are necessery
            for(int t = 0; t < svg_object.Transforms.Count; t++)
            {
                //svg_object.Transforms[0].  // find the type of transfor
                switch (SVG_Types.getType(svg_object.Transforms[t]))
                {
                    case SVG_Types.Matrix:
                        Svg.Transforms.SvgMatrix svg_matrix = svg_object.Transforms[t] as Svg.Transforms.SvgMatrix;

                        double[] matrix = { svg_matrix.Points[0],
                                            svg_matrix.Points[1],
                                            svg_matrix.Points[2],
                                            svg_matrix.Points[3],
                                            svg_matrix.Points[4],
                                            svg_matrix.Points[5] };

                        Transform(ref Paths, matrix);
                        break;
                    case SVG_Types.Translate:
                        Translation(ref Paths, (svg_object.Transforms[t] as Svg.Transforms.SvgTranslate).X,
                                               (svg_object.Transforms[t] as Svg.Transforms.SvgTranslate).Y);
                        break;
                    case SVG_Types.Rotate:
                        Rotation(ref Paths, (svg_object.Transforms[t] as Svg.Transforms.SvgRotate).Angle,
                                            (svg_object.Transforms[t] as Svg.Transforms.SvgRotate).CenterX,
                                            (svg_object.Transforms[t] as Svg.Transforms.SvgRotate).CenterY);
                        break;
                    case SVG_Types.Scale:
                        Scale(ref Paths, (svg_object.Transforms[t] as Svg.Transforms.SvgScale).X,
                                         (svg_object.Transforms[t] as Svg.Transforms.SvgScale).Y);
                        break;
                    case SVG_Types.Shear:
                        Shear(ref Paths, (svg_object.Transforms[t] as Svg.Transforms.SvgShear).X,
                                         (svg_object.Transforms[t] as Svg.Transforms.SvgShear).Y);
                        break;
                    case SVG_Types.Skew:
                        Skew(ref Paths, (svg_object.Transforms[t] as Svg.Transforms.SvgSkew).AngleX,
                                        (svg_object.Transforms[t] as Svg.Transforms.SvgSkew).AngleY);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                
                
            }
            
            this.Paths = Paths;
        }
           

        public static SVG_Path SVG_ConvertLine(SvgLine line)
        {
            return new SVG_Path(new SVG_Point(line.StartX, line.StartY),
                                new SVG_Point(line.EndX,line.EndY),
                                line.StrokeWidth);
        }

        public static List<SVG_Path> SVG_ConvertPath(SvgPath path) // !!
        {
            List<SVG_Path> object_path = new List<SVG_Path>();

            SVG_Point current_Location = new SVG_Point(0, 0);
            SVG_Point new_Path_Start = new SVG_Point(0, 0);
                        

            foreach (Svg.Pathing.SvgPathSegment segment in path.PathData)
            {
                switch (SVG_Types.getType(segment))
                {
                    case SVG_Types.MoveTo:
                        current_Location.X = (segment as Svg.Pathing.SvgMoveToSegment).End.X;
                        current_Location.Y = (segment as Svg.Pathing.SvgMoveToSegment).End.Y;

                        new_Path_Start.X = (segment as Svg.Pathing.SvgMoveToSegment).End.X;
                        new_Path_Start.Y = (segment as Svg.Pathing.SvgMoveToSegment).End.Y;
                        break;
                    case SVG_Types.LineTo:
                        object_path.Add(new SVG_Path(new SVG_Point((segment as Svg.Pathing.SvgLineSegment).Start.X, (segment as Svg.Pathing.SvgLineSegment).Start.Y),
                                                     new SVG_Point((segment as Svg.Pathing.SvgLineSegment).End.X, (segment as Svg.Pathing.SvgLineSegment).End.Y),
                                                     path.StrokeWidth));

                        current_Location.X = (segment as Svg.Pathing.SvgLineSegment).End.X;
                        current_Location.Y = (segment as Svg.Pathing.SvgLineSegment).End.Y;
                        break;
                    case SVG_Types.ArcTo:
                        //(segment as Svg.Pathing.SvgArcSegment).Angle
                        
                        //(segment as Svg.Pathing.SvgArcSegment).Sweep

                        //(segment as Svg.Pathing.SvgArcSegment).Size

                        //(segment as Svg.Pathing.SvgArcSegment).RadiusX
                        //(segment as Svg.Pathing.SvgArcSegment).RadiusY

                        //(segment as Svg.Pathing.SvgArcSegment).Start
                        //(segment as Svg.Pathing.SvgArcSegment).End

                        // Circle
                        if((segment as Svg.Pathing.SvgArcSegment).RadiusX == (segment as Svg.Pathing.SvgArcSegment).RadiusY)
                        {
                            object_path.Add(new SVG_Path(new SVG_Point((segment as Svg.Pathing.SvgArcSegment).Start.X, (segment as Svg.Pathing.SvgArcSegment).Start.Y),
                                                         new SVG_Point((segment as Svg.Pathing.SvgArcSegment).End.X, (segment as Svg.Pathing.SvgArcSegment).End.Y),
                                                         path.StrokeWidth,
                                                         (segment as Svg.Pathing.SvgArcSegment).RadiusX,
                                                         (segment as Svg.Pathing.SvgArcSegment).Sweep == Svg.Pathing.SvgArcSweep.Negative,
                                                         (segment as Svg.Pathing.SvgArcSegment).Size == Svg.Pathing.SvgArcSize.Small));
                        }
                        else // Ellipse
                        {
                            // Temp !!!!  Aproximates to a single arc (incomplet)
                            object_path.Add(new SVG_Path(new SVG_Point((segment as Svg.Pathing.SvgArcSegment).Start.X, (segment as Svg.Pathing.SvgArcSegment).Start.Y),
                                                         new SVG_Point((segment as Svg.Pathing.SvgArcSegment).End.X, (segment as Svg.Pathing.SvgArcSegment).End.Y),
                                                         path.StrokeWidth,
                                                         ((segment as Svg.Pathing.SvgArcSegment).RadiusX > (segment as Svg.Pathing.SvgArcSegment).RadiusY)? (segment as Svg.Pathing.SvgArcSegment).RadiusX : (segment as Svg.Pathing.SvgArcSegment).RadiusY,
                                                         (segment as Svg.Pathing.SvgArcSegment).Sweep == Svg.Pathing.SvgArcSweep.Negative,
                                                         (segment as Svg.Pathing.SvgArcSegment).Size == Svg.Pathing.SvgArcSize.Small));
                        }

                        current_Location.X = (segment as Svg.Pathing.SvgArcSegment).End.X;
                        current_Location.Y = (segment as Svg.Pathing.SvgArcSegment).End.Y;
                        //throw new NotImplementedException();
                        break;
                    case SVG_Types.QuadraticCurveTo:
                        //(segment as Svg.Pathing.SvgQuadraticCurveSegment).ControlPoint

                        //(segment as Svg.Pathing.SvgQuadraticCurveSegment).Start
                        //(segment as Svg.Pathing.SvgQuadraticCurveSegment).End

                        object_path.AddRange(QuadraticBezierToArc((segment as Svg.Pathing.SvgQuadraticCurveSegment), Bezier_UseLinearInterpolation, path.StrokeWidth));

                        current_Location.X = (segment as Svg.Pathing.SvgQuadraticCurveSegment).End.X;
                        current_Location.Y = (segment as Svg.Pathing.SvgQuadraticCurveSegment).End.Y;
                        
                        break;
                    case SVG_Types.CubicCurveTo:
                        //(segment as Svg.Pathing.SvgCubicCurveSegment).FirstControlPoint
                        //(segment as Svg.Pathing.SvgCubicCurveSegment).SecondControlPoint

                        //(segment as Svg.Pathing.SvgCubicCurveSegment).Start
                        //(segment as Svg.Pathing.SvgCubicCurveSegment).End

                        object_path.AddRange(CubicBezierToArc((segment as Svg.Pathing.SvgCubicCurveSegment), Bezier_UseLinearInterpolation, path.StrokeWidth));

                        current_Location.X = (segment as Svg.Pathing.SvgCubicCurveSegment).End.X;
                        current_Location.Y = (segment as Svg.Pathing.SvgCubicCurveSegment).End.Y;
                        //throw new NotImplementedException();
                        break;
                    case SVG_Types.ClosePath:
                        object_path.Add(new SVG_Path(new SVG_Point(current_Location), new SVG_Point(new_Path_Start), path.StrokeWidth));
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            return object_path;
        }

        public static List<SVG_Path> SVG_ConvertRectangle(SvgRectangle path)
        {
            List<SVG_Path> object_path = new List<SVG_Path>();

            SVG_Point p1 = new SVG_Point(path.X, path.Y);
            SVG_Point p2 = new SVG_Point(path.X + path.Width, path.Y);
            SVG_Point p3 = new SVG_Point(path.X + path.Width, path.Y + path.Height);
            SVG_Point p4 = new SVG_Point(path.X, path.Y + path.Height);

            object_path.Add(new SVG_Path(p1, p2, path.StrokeWidth));
            object_path.Add(new SVG_Path(p2, p3, path.StrokeWidth));
            object_path.Add(new SVG_Path(p3, p4, path.StrokeWidth));
            object_path.Add(new SVG_Path(p4, p1, path.StrokeWidth));

            return object_path;
        }

        public static SVG_Path SVG_ConvertCircle(SvgCircle path)
        {
            SVG_Point center = new SVG_Point(path.CenterX, path.CenterY);
            double radius = path.Radius;

            SVG_Point start = new SVG_Point(path.CenterX - radius, path.CenterY);
            
            return new SVG_Path(start, start, center, path.StrokeWidth);
        }

        public static List<SVG_Path> SVG_ConvertEllipse(SvgEllipse path)
        {
            if(path.RadiusX == path.RadiusY) // Not an Ellipse but a Circle !
            {
                SVG_Point center = new SVG_Point(path.CenterX, path.CenterY);
                double radius = path.RadiusX;

                SVG_Point start = new SVG_Point(path.CenterX - radius, path.CenterY);

                List<SVG_Path> object_path = new List<SVG_Path>();
                object_path.Add(new SVG_Path(start, start, center, path.StrokeWidth));

                return object_path;
            }

            return EllipseToArc(path);
        }

        public static List<SVG_Path> SVG_ConvertPolygon(SvgPolygon path)
        {
            List<SVG_Path> object_path = new List<SVG_Path>();

            // Get all the Points of the Polygon
            int last_value_index = path.Points.Count - path.Points.Count % 2; // if the path.Points.Count is odd ignore the last value (first value is x1 and second is y1  third is x2 , ....  should never be a odd number of values)
            List<SVG_Point> polyPoints = new List<SVG_Point>();
            for (int i = 0; i < last_value_index; i += 2)
            {
                polyPoints.Add(new SVG_Point(path.Points[i], path.Points[i + 1]));
            }

            if(polyPoints.Count < 2) // return empty  can not creat a polygone with just 1 point (2 points simplify to a line)
            {
                return object_path;
            }

            // Creat the Paths
            SVG_Point currentPoint = polyPoints[0];
            SVG_Point nextPoint;
            for (int i = 1; i < polyPoints.Count; i++)
            {
                nextPoint = polyPoints[i];
                object_path.Add(new SVG_Path(currentPoint, nextPoint, path.StrokeWidth));
                currentPoint = nextPoint;
            }

            // Close the Polygon creating a last line uniting the first and the last points (not necessery in polylines)
            object_path.Add(new SVG_Path(polyPoints[polyPoints.Count - 1], polyPoints[0], path.StrokeWidth));

            return object_path;
        }

        public static List<SVG_Path> SVG_ConvertPolyline(SvgPolyline path)
        {
            List<SVG_Path> object_path = new List<SVG_Path>();

            // Get all the Points of the Polygon
            int last_value_index = path.Points.Count - path.Points.Count % 2; // if the path.Points.Count is odd ignore the last value (first value is x1 and second is y1  third is x2 , ....  should never be a odd number of values)
            List<SVG_Point> polyPoints = new List<SVG_Point>();
            for (int i = 0; i < last_value_index; i += 2)
            {
                polyPoints.Add(new SVG_Point(path.Points[i], path.Points[i + 1]));
            }

            if (polyPoints.Count < 2) // return empty  can not creat a polygone with just 1 point (2 points simplify to a line)
            {
                return object_path;
            }

            // Creat the Paths
            SVG_Point currentPoint = polyPoints[0];
            SVG_Point nextPoint;
            for (int i = 1; i < polyPoints.Count; i++)
            {
                nextPoint = polyPoints[i];
                object_path.Add(new SVG_Path(currentPoint, nextPoint, path.StrokeWidth));
                currentPoint = nextPoint;
            }

            // Close the Polygon creating a last line uniting the first and the last points (not necessery in polylines)
            //object_path.Add(new SVG_Path(polyPoints[polyPoints.Count - 1], polyPoints[0], path.StrokeWidth));

            return object_path;
        }

        //
        // Curve Aproximations
        //

        private static List<SVG_Path> EllipseToArc(SvgEllipse Ellipse) 
        {
            int NumberOfArcsPerQuadrant = Ellipse.RadiusX > Ellipse.RadiusY ? Convert.ToInt32(Math.Round(Ellipse.RadiusX / Ellipse.RadiusY)) * 8 : Convert.ToInt32(Math.Round(Ellipse.RadiusY / Ellipse.RadiusX)) * 8;
                    
            // Find the Ellipse Points (Arcs start and End points)
            PointF[] EllipsePoints = Math_Utilities.FindEllipsePoints(Ellipse.CenterX, Ellipse.CenterY, Ellipse.RadiusX, Ellipse.RadiusY, NumberOfArcsPerQuadrant);

            // Find theire centers
            PointF[] EllipseCenters = new PointF[(NumberOfArcsPerQuadrant - 1) * 4]; // -1(*4) because the horizontal + vertical arcs cover 3 points, the rest just covers between 2 points

            // (a,0) Center
            EllipseCenters[0] = Math_Utilities.FindArcCenter(EllipsePoints[EllipsePoints.Length - 1], 
                                                             EllipsePoints[0], 
                                                             EllipsePoints[1]);

            // (0,b) Center
            EllipseCenters[NumberOfArcsPerQuadrant - 1] = Math_Utilities.FindArcCenter(EllipsePoints[NumberOfArcsPerQuadrant - 1], 
                                                                                       EllipsePoints[NumberOfArcsPerQuadrant], 
                                                                                       EllipsePoints[NumberOfArcsPerQuadrant + 1]);

            // (-a,0) Center
            EllipseCenters[NumberOfArcsPerQuadrant * 2 - 2] = Math_Utilities.FindArcCenter(EllipsePoints[NumberOfArcsPerQuadrant * 2- 1],
                                                                                       EllipsePoints[NumberOfArcsPerQuadrant * 2],
                                                                                       EllipsePoints[NumberOfArcsPerQuadrant * 2 + 1]);

            // (0,-b) Center
            EllipseCenters[NumberOfArcsPerQuadrant * 3 - 3] = Math_Utilities.FindArcCenter(EllipsePoints[NumberOfArcsPerQuadrant * 3 - 1],
                                                                                       EllipsePoints[NumberOfArcsPerQuadrant * 3],
                                                                                       EllipsePoints[NumberOfArcsPerQuadrant * 3 + 1]);

            // the rest of the centers
            for(int i = 1; i < NumberOfArcsPerQuadrant - 1; i++) // Only do if there are more than 2 arcs per quadrant
            {
                // 1st Quadrant
                EllipseCenters[i] = Math_Utilities.FindArcCenter(EllipsePoints[i - 1],
                                                                 EllipsePoints[i],
                                                                 EllipsePoints[i + 1]);
                // 2nd Quadrant
                EllipseCenters[NumberOfArcsPerQuadrant * 2 - 2 - i] = Math_Utilities.FindArcCenter(EllipsePoints[NumberOfArcsPerQuadrant * 2 - i - 1],
                                                                                                   EllipsePoints[NumberOfArcsPerQuadrant * 2 - i],
                                                                                                   EllipsePoints[NumberOfArcsPerQuadrant * 2 - i + 1]);
                // 3rd Quadrand
                EllipseCenters[NumberOfArcsPerQuadrant * 2 - 2 + i] = Math_Utilities.FindArcCenter(EllipsePoints[NumberOfArcsPerQuadrant * 2 + i - 1],
                                                                 EllipsePoints[NumberOfArcsPerQuadrant * 2 + i],
                                                                 EllipsePoints[NumberOfArcsPerQuadrant * 2 + i + 1]);
                // 4th quadrant
                if(i == 1)
                {
                    EllipseCenters[EllipseCenters.Length - i] = Math_Utilities.FindArcCenter(EllipsePoints[EllipsePoints.Length - 2],
                                                                                             EllipsePoints[EllipsePoints.Length - 1],
                                                                                             EllipsePoints[0]);
                }
                else
                {
                    EllipseCenters[EllipseCenters.Length - i] = Math_Utilities.FindArcCenter(EllipsePoints[EllipsePoints.Length - i - 1],
                                                                                             EllipsePoints[EllipsePoints.Length - i],
                                                                                             EllipsePoints[EllipsePoints.Length - i + 1]);
                }

                
            }

            // Create the object paths
            SVG_Path[] object_path = new SVG_Path[EllipseCenters.Length];

            // Horizontal and Vertical Paths
            //PointF Ref = new PointF(1,0);

            // (a,0)
            object_path[0] = new SVG_Path(EllipsePoints[EllipsePoints.Length - 1], 
                                          EllipsePoints[1], 
                                          EllipseCenters[0], 
                                          Ellipse.StrokeWidth,
                                          true);

            // (0,b)
            object_path[NumberOfArcsPerQuadrant - 1] = new SVG_Path(EllipsePoints[NumberOfArcsPerQuadrant - 1],
                                                                    EllipsePoints[NumberOfArcsPerQuadrant + 1],
                                                                    EllipseCenters[NumberOfArcsPerQuadrant - 1],
                                                                    Ellipse.StrokeWidth,
                                                                    true);
            // (-a,0)
            object_path[NumberOfArcsPerQuadrant * 2 - 2] = new SVG_Path(EllipsePoints[NumberOfArcsPerQuadrant * 2 - 1],
                                                                    EllipsePoints[NumberOfArcsPerQuadrant * 2 + 1],
                                                                    EllipseCenters[NumberOfArcsPerQuadrant * 2 - 2],
                                                                    Ellipse.StrokeWidth,
                                                                    true);
            // (0,-b)
            object_path[NumberOfArcsPerQuadrant * 3 - 3] = new SVG_Path(EllipsePoints[NumberOfArcsPerQuadrant * 3 - 1],
                                                                        EllipsePoints[NumberOfArcsPerQuadrant * 3 + 1],
                                                                        EllipseCenters[NumberOfArcsPerQuadrant * 3 - 3],
                                                                        Ellipse.StrokeWidth,
                                                                        true);

            // The rest of the paths
            for (int i = 1; i < NumberOfArcsPerQuadrant - 1; i++)
            {
                // 1st Quadrant
                object_path[i] = new SVG_Path(EllipsePoints[i],
                                              EllipsePoints[i + 1],
                                              EllipseCenters[i],
                                              Ellipse.StrokeWidth,
                                              true);
                // 2nd Quadrant
                object_path[NumberOfArcsPerQuadrant * 2 - 2 - i] = new SVG_Path(EllipsePoints[NumberOfArcsPerQuadrant * 2 - i - 1],
                                                                                EllipsePoints[NumberOfArcsPerQuadrant * 2 - i],
                                                                                EllipseCenters[NumberOfArcsPerQuadrant * 2 - 2 - i],
                                                                                Ellipse.StrokeWidth,
                                                                                true);
                // 3rd Quadrant
                object_path[NumberOfArcsPerQuadrant * 2 - 2 + i] = new SVG_Path(EllipsePoints[NumberOfArcsPerQuadrant * 2 + i],
                                                                                EllipsePoints[NumberOfArcsPerQuadrant * 2 + i + 1],
                                                                                EllipseCenters[NumberOfArcsPerQuadrant * 2 - 2 + i],
                                                                                Ellipse.StrokeWidth,
                                                                                true);
                // 4th Quadrant
                object_path[EllipseCenters.Length - i] = new SVG_Path(EllipsePoints[EllipsePoints.Length - i - 1],
                                                                      EllipsePoints[EllipsePoints.Length - i],
                                                                      EllipseCenters[EllipseCenters.Length - i],
                                                                      Ellipse.StrokeWidth,
                                                                      true);
            }

            
            return object_path.ToList();
        }

        private static List<SVG_Path> QuadraticBezierToArc(Svg.Pathing.SvgQuadraticCurveSegment BezierPath, bool LinearInterpolation, double PathWidth)
        {
            return CubicBezierToArc(new Svg.Pathing.SvgCubicCurveSegment(BezierPath.Start,
                                                                         new PointF(BezierPath.Start.X + 2f / 3f * (BezierPath.ControlPoint.X - BezierPath.Start.X),
                                                                                    BezierPath.Start.Y + 2f / 3f * (BezierPath.ControlPoint.Y - BezierPath.Start.Y)),
                                                                         new PointF(BezierPath.End.X + 2f / 3f * (BezierPath.ControlPoint.X - BezierPath.End.X),
                                                                                    BezierPath.End.Y + 2f / 3f * (BezierPath.ControlPoint.Y - BezierPath.End.Y)), 
                                                                         BezierPath.End),
                                    LinearInterpolation,
                                    PathWidth);
        }

        private static List<SVG_Path> CubicBezierToArc(Svg.Pathing.SvgCubicCurveSegment BezierPath, bool LinearInterpolation, double PathWidth)
        {
            List<SVG_Path> Path = new List<SVG_Path>();

            Math_Utilities.BezierF BezierCurve = new Math_Utilities.BezierF(BezierPath.Start, BezierPath.FirstControlPoint, BezierPath.SecondControlPoint, BezierPath.End);
            if (LinearInterpolation)
            {
                List<Math_Utilities.LineF> LinePath = Math_Utilities.BezierLinearInterpolation(BezierCurve, Bezier_LinearInterpolation_MinimumError);
                
                foreach(Math_Utilities.LineF l in LinePath)
                {
                    Path.Add(new SVG_Path(l.p1, l.p2, PathWidth));
                }
                                
            }
            else
            {
                throw new NotImplementedException(); // !!
            }

            return Path;
        }

        //
        // Transformations
        //

        public static void TransformPreservRadius(ref List<SVG_Path> path, double[] matrix)
        {
            foreach (SVG_Path p in path)
            {
                double transformedX = 0;
                double transformedY = 0;

                Math_Utilities.MatrixTransfortion(matrix, p.Start.X, p.Start.Y, out transformedX, out transformedY);
                p.Start = new SVG_Point(transformedX, transformedY);

                Math_Utilities.MatrixTransfortion(matrix, p.End.X, p.End.Y, out transformedX, out transformedY);
                p.End = new SVG_Point(transformedX, transformedY);

                if (p.PathType == SVG_Path.SVG_Path_Type.Curve_Center)
                {
                    Math_Utilities.MatrixTransfortion(matrix, p.Center.X, p.Center.Y, out transformedX, out transformedY);
                    p.Center = new SVG_Point(transformedX, transformedY);
                }
                
            }
        }

        public static void Transform(ref List<SVG_Path> path, double[] matrix)
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

            foreach (SVG_Path p in path)
            {
                double transformedX = 0;
                double transformedY = 0;

                Math_Utilities.MatrixTransfortion(matrix,p.Start.X, p.Start.Y, out transformedX, out transformedY);
                p.Start = new SVG_Point(transformedX, transformedY);

                Math_Utilities.MatrixTransfortion(matrix, p.End.X, p.End.Y, out transformedX, out transformedY);
                p.End = new SVG_Point(transformedX, transformedY);

                if(p.PathType == SVG_Path.SVG_Path_Type.Curve_Center)
                {
                    Math_Utilities.MatrixTransfortion(matrix, p.Center.X, p.Center.Y, out transformedX, out transformedY);
                    p.Center = new SVG_Point(transformedX, transformedY);
                }
                if(p.PathType == SVG_Path.SVG_Path_Type.Curve_Radius)
                {
                    Math_Utilities.MatrixTransfortion(matrix, p.Radius, p.Radius, out transformedX, out transformedY);
                    p.Radius = (transformedX > transformedY)? transformedX : transformedY;
                }
            }
                        
        }

        public static void Translation(ref List<SVG_Path> path, double Tx, double Ty)
        {
            TransformPreservRadius(ref path, new double[] { 1, 0, 0, 1, Tx, Ty });
        }

        public static void Scale(ref List<SVG_Path> path, double Sx, double Sy)
        {
            Transform(ref path, new double[] { Sx, 0, 0, Sy, 0, 0 });
        }

        public static void Skew(ref List<SVG_Path> path, double AngleX, double AngleY) 
        {
            Transform(ref path, new double[] { 1, Math.Tan(AngleY), Math.Tan(AngleX), 1, 0, 0 });
        }

        public static void Rotation(ref List<SVG_Path> path, double Angle)
        {
            Transform(ref path, new double[] { Math.Cos(Angle), Math.Sin(Angle), -Math.Sin(Angle), Math.Cos(Angle), 0, 0 });
        }

        public static void Rotation(ref List<SVG_Path> path, double Angle, double Cx, double Cy)
        {
            Translation(ref path, Cx, Cy);
            Rotation(ref path, Angle);
            Translation(ref path, -Cx, -Cy);
        }

        public static void Shear(ref List<SVG_Path> path, double Sx, double Sy) 
        {
            Transform(ref path, new double[] { 1, Sy, Sx, 1, 0, 0 });
        }

    }
}
