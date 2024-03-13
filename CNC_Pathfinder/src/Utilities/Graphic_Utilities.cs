using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNC_Pathfinder.src.Utilities
{
    class Graphic_Utilities
    {
        private static Pen ErrorPen = new Pen(Color.Red);
        
        public static void DrawArc(Graphics g, Pen color, float StartX, float StartY, float EndX, float EndY, float CenterX, float CenterY, bool Clockwise)
        {
            float radius = Convert.ToSingle((Math.Sqrt(Math.Pow(CenterX - StartX, 2) + Math.Pow(CenterY - StartY, 2))));
            if (radius == 0)
            {
                radius = 1;
            }

            RectangleF rect = new RectangleF(CenterX - radius, CenterY - radius, radius * 2, radius * 2);

            PointF Ref = new PointF(radius, 0);
            PointF start_ref = new PointF(StartX - CenterX, StartY - CenterY);
            PointF finish_ref = new PointF(EndX - CenterX, EndY - CenterY);

            float startAngle = Math_Utilities.AngleBetween_v2(start_ref, Ref);

            float finishAngle = Math_Utilities.AngleBetween_v2(finish_ref, Ref);


            float sweepAngle = Math_Utilities.AngleBetween_v2(start_ref, finish_ref);

            if (Clockwise)
            {
                sweepAngle = Math.Abs(360 - sweepAngle);

            }
            else
            {
                sweepAngle *= -1;
            }

            startAngle = -finishAngle;

            if (Math.Abs(sweepAngle) < 0.5) // hotfix to OutMemoryException when drawing very small angles
            {
                g.DrawLine(color, StartX, StartY, EndX, EndY);
            }
            else
            {
                g.DrawArc(color, rect, startAngle, sweepAngle);

            }



        }

        public static void DrawArc(Graphics g, Pen color, int StartX, int StartY, int EndX, int EndY, int CenterX, int CenterY, bool Clockwise)
        {
            int radius = Convert.ToInt32(Math.Round(Math.Sqrt(Math.Pow(CenterX - StartX, 2) + Math.Pow(CenterY - StartY, 2))));
            if(radius == 0)
            {
                radius = 1;
            }

            Rectangle rect = new Rectangle(CenterX - radius, CenterY - radius, radius * 2, radius * 2);

            Point Ref = new Point(radius, 0);
            Point start_ref = new Point(StartX - CenterX, StartY - CenterY);
            Point finish_ref = new Point(EndX - CenterX, EndY - CenterY);

            float startAngle = Math_Utilities.AngleBetween_v2(start_ref, Ref);
            //if(startAngle < 0)
            //{
            //    startAngle += 360;
            //}
            float finishAngle = Math_Utilities.AngleBetween_v2(finish_ref, Ref);
            //if (finishAngle < 0)
            //{
            //    finishAngle += 360;
            //}

            float sweepAngle = Math_Utilities.AngleBetween_v2(start_ref, finish_ref);

            if (Clockwise)
            {
                sweepAngle = Math.Abs(360 - sweepAngle);
                
            }
            else
            {
                sweepAngle *= -1;
            }

            startAngle = -finishAngle;

            if (Math.Abs(sweepAngle) < 0.5) // hotfix to OutMemoryException when drawing very small angles
            {
                g.DrawLine(color, StartX, StartY, EndX, EndY);
            }
            else
            {
                g.DrawArc(color, rect, startAngle, sweepAngle);

            }

            /*
            float Small_sweepAngle = Math.Abs(finishAngle - startAngle);
            if(Small_sweepAngle > 180)
            {
                Small_sweepAngle = 360 - Small_sweepAngle;
            }
            float Big_sweepAngle = 360 - Small_sweepAngle;

            if (Clockwise)
            {
                float sweepAngle = startAngle - finishAngle;
                //if(startAngle <= 90 && finishAngle >= 270 || finishAngle <= 90 && startAngle >= 270)
                if (startAngle < 180 && finishAngle > 180 /*|| finishAngle < 180 && startAngle > 180)
                {
                    sweepAngle += 360;
                }

                if(Math.Abs(sweepAngle) < 0.5) // hotfix to OutMemoryException when drawing very small angles
                {
                    g.DrawLine(color, StartX, StartY, EndX, EndY);
                }
                else
                {
                    g.DrawArc(color, rect, startAngle, -sweepAngle);
                }

                
            }
            else
            {
                float sweepAngle = startAngle - finishAngle;
                if (startAngle <= 90 && finishAngle >= 270 || finishAngle <= 90 && startAngle >= 270)
                {
                    sweepAngle = -1 * (360 - sweepAngle);
                }

                if (Math.Abs(sweepAngle) < 0.5) // hotfix to OutMemoryException when drawing very small angles
                {
                    g.DrawLine(color, StartX, StartY, EndX, EndY);
                }
                else
                {
                    g.DrawArc(color, rect, finishAngle, sweepAngle);
                }

                
            }
                    
            */


        }

        public static bool DrawArc(Graphics g, Pen color, float StartX, float StartY, float EndX, float EndY, float radius, bool SmallArc, bool Clockwise)
        {
            Point c1 = new Point();
            Point c2 = new Point();

            try
            {
                double x3 = ((double)StartX + EndX) / 2.0;
                double y3 = ((double)StartY + EndY) / 2.0;
                double Q = Math.Sqrt(Math.Pow(StartX - EndX, 2) + Math.Pow(StartY - EndY, 2));

                c1.X = Convert.ToInt32(Math.Round(x3 + Math.Sqrt(Math.Pow(radius, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(StartY - EndY) / Q));
                c1.Y = Convert.ToInt32(Math.Round(y3 + Math.Sqrt(Math.Pow(radius, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(EndX - StartX) / Q));
                c2.X = Convert.ToInt32(Math.Round(x3 - Math.Sqrt(Math.Pow(radius, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(StartY - EndY) / Q));
                c2.Y = Convert.ToInt32(Math.Round(y3 - Math.Sqrt(Math.Pow(radius, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(EndX - StartX) / Q));

                //c1.X = Convert.ToInt32((double)StartX / 2 + EndX / 2 - (StartY * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2 + (EndY * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2);
                //c2.X = Convert.ToInt32((double)StartX / 2 + EndX / 2 + (StartY * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2 - (EndY * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2);

                //c1.Y = Convert.ToInt32((double)StartY / 2 + EndY / 2 + (StartX * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2 - (EndX * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2);
                //c2.Y = Convert.ToInt32((double)StartY / 2 + EndY / 2 - (StartX * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2 + (EndX * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2);

            }
            catch (Exception e)
            {
                // error 
                g.DrawLine(ErrorPen, new PointF(StartX, StartY),
                                     new PointF(EndX, EndY));
                return false;
            }

            PointF Ref = new PointF(radius, 0);
            PointF start_1 = new PointF(StartX - c1.X, StartY - c1.Y);
            PointF finish_1 = new PointF(EndX - c1.X, EndY - c1.Y);
            PointF start_2 = new PointF(StartX - c2.X, StartY - c2.Y);
            PointF finish_2 = new PointF(EndX - c2.X, EndY - c2.Y);

            //float startAngle_1 = Math_Utilities.AngleBetween(start_1, Ref);
            //float finishAngle_1 = Math_Utilities.AngleBetween(finish_1, Ref);

            //float startAngle_2 = Math_Utilities.AngleBetween(start_2, Ref);
            //float finishAngle_2 = Math_Utilities.AngleBetween(finish_2, Ref);

            float sweepAngle_1 = Math_Utilities.AngleBetween_v2(start_1, finish_1);//finishAngle_1 - startAngle_1;
            float sweepAngle_2 = Math_Utilities.AngleBetween_v2(start_2, finish_2);//finishAngle_2 - startAngle_2;

            if (Clockwise)
            {
                sweepAngle_1 = Math.Abs(360 - sweepAngle_1);
                sweepAngle_2 = Math.Abs(360 - sweepAngle_2);
            }


            if (SmallArc)
            {
                if (sweepAngle_1 > sweepAngle_2)
                {
                    DrawArc(g, color, StartX, StartY, EndX, EndY, c2.X, c2.Y, Clockwise);
                }
                else
                {
                    DrawArc(g, color, StartX, StartY, EndX, EndY, c1.X, c1.Y, Clockwise);
                }
            }
            else
            {
                if (sweepAngle_1 > sweepAngle_2)
                {
                    DrawArc(g, color, StartX, StartY, EndX, EndY, c1.X, c1.Y, Clockwise);
                }
                else
                {
                    DrawArc(g, color, StartX, StartY, EndX, EndY, c2.X, c2.Y, Clockwise);
                }
            }

            return true;

        }

        public static bool DrawArc(Graphics g, Pen color, int StartX, int StartY, int EndX, int EndY, int radius, bool SmallArc, bool Clockwise)
        {
            Point c1 = new Point();
            Point c2 = new Point();

            try
            {
                double x3 = ((double)StartX + EndX) / 2.0;
                double y3 = ((double)StartY + EndY) / 2.0;
                double Q = Math.Sqrt(Math.Pow(StartX - EndX, 2) + Math.Pow(StartY - EndY, 2));

                c1.X = Convert.ToInt32(Math.Round(x3 + Math.Sqrt(Math.Pow(radius, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(StartY - EndY) / Q));
                c1.Y = Convert.ToInt32(Math.Round(y3 + Math.Sqrt(Math.Pow(radius, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(EndX - StartX) / Q));
                c2.X = Convert.ToInt32(Math.Round(x3 - Math.Sqrt(Math.Pow(radius, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(StartY - EndY) / Q));
                c2.Y = Convert.ToInt32(Math.Round(y3 - Math.Sqrt(Math.Pow(radius, 2) - Math.Pow(Q / 2, 2)) * /*Math.Abs*/(EndX - StartX) / Q));
                
                //c1.X = Convert.ToInt32((double)StartX / 2 + EndX / 2 - (StartY * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2 + (EndY * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2);
                //c2.X = Convert.ToInt32((double)StartX / 2 + EndX / 2 + (StartY * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2 - (EndY * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2);

                //c1.Y = Convert.ToInt32((double)StartY / 2 + EndY / 2 + (StartX * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2 - (EndX * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2);
                //c2.Y = Convert.ToInt32((double)StartY / 2 + EndY / 2 - (StartX * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2 + (EndX * Math.Sqrt(-(-4 * Math.Pow(radius, 2) + Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)) / (Math.Pow(StartX, 2) - 2 * StartX * EndX + Math.Pow(EndX, 2) + Math.Pow(StartY, 2) - 2 * StartY * EndY + Math.Pow(EndY, 2)))) / 2);

            }
            catch (Exception e)
            {
                // error 
                g.DrawLine(ErrorPen, new Point(StartX, StartY),
                                     new Point(EndX, EndY));
                return false;
            }
            
            Point Ref = new Point(radius, 0);
            Point start_1 = new Point(StartX - c1.X, StartY - c1.Y);
            Point finish_1 = new Point(EndX - c1.X, EndY - c1.Y);
            Point start_2 = new Point(StartX - c2.X, StartY - c2.Y);
            Point finish_2 = new Point(EndX - c2.X, EndY - c2.Y);

            //float startAngle_1 = Math_Utilities.AngleBetween(start_1, Ref);
            //float finishAngle_1 = Math_Utilities.AngleBetween(finish_1, Ref);

            //float startAngle_2 = Math_Utilities.AngleBetween(start_2, Ref);
            //float finishAngle_2 = Math_Utilities.AngleBetween(finish_2, Ref);

            float sweepAngle_1 = Math_Utilities.AngleBetween_v2(start_1, finish_1);//finishAngle_1 - startAngle_1;
            float sweepAngle_2 = Math_Utilities.AngleBetween_v2(start_2, finish_2);//finishAngle_2 - startAngle_2;

            if (Clockwise)
            {
                sweepAngle_1 = Math.Abs(360 - sweepAngle_1);
                sweepAngle_2 = Math.Abs(360 - sweepAngle_2);
            }

            
            if (SmallArc)
            {
                if (sweepAngle_1 > sweepAngle_2)
                {
                    DrawArc(g, color, StartX, StartY, EndX, EndY, c2.X, c2.Y, Clockwise);
                }
                else
                {
                    DrawArc(g, color, StartX, StartY, EndX, EndY, c1.X, c1.Y, Clockwise);
                }
            }
            else
            {
                if (sweepAngle_1 > sweepAngle_2)
                {
                    DrawArc(g, color, StartX, StartY, EndX, EndY, c1.X, c1.Y, Clockwise);
                }
                else
                {
                    DrawArc(g, color, StartX, StartY, EndX, EndY, c2.X, c2.Y, Clockwise);
                }
            }

            return true;
                                    
        }

    }
}
