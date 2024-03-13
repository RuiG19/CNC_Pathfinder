using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Globalization;
using System.IO;

// costum namespaces
using CNC_Pathfinder.src.CNC;
using CNC_Pathfinder.src.GCode;
using CNC_Pathfinder.src.Utilities;
using Svg;
using CNC_Pathfinder.src.SVG;

namespace CNC_Pathfinder.src.Toolpathing
{
    class IMG_ToolPathing
    {

        #region Parameters

        private NumberFormatInfo nfi = new NumberFormatInfo();

        private string Firmware_Version = "STM32F443RE_XXX";

        private BackgroundWorker toolpath_worker;


        private bool Generate_Report = false;

        private bool segmentDiagonals = false;

        // Report Vars

        private int Report_img_original_width = 0;
        private int Report_img_original_height = 0;
        private int Report_img_new_width = 0;
        private int Report_img_new_height = 0;

        // SVG Parameters

        private bool usingSVG = false;
        private SVG_Image svg_edges;

        private bool EdgesOnly = false;

        #endregion

        #region Inner Classes and Enums

        public enum ToolPathingType
        {
            ZigZag = 0,
            Spiral = 1,
            SpiralHighQuality = 2
        }

        private enum Direction
        {
            Top = 0,
            Left = 1,
            Bottom = 2,
            Right = 3
        }



        // Inner Class for a segment of pixels
        private class Segment
        {
            private List<Point> PointsList;
            public int Length { get; private set; }

            public int x_min { get; private set; }
            public int x_max { get; private set; }
            public int y_min { get; private set; }
            public int y_max { get; private set; }

            // 

            public Segment()
            {
                PointsList = new List<Point>();
                Length = 0;

                x_min = -1;
                x_max = -1;
                y_min = -1;
                y_max = -1;
            }

            public void Add(int x, int y)
            {
                Add(new Point(x, y));
            }

            public void Add(Point p)
            {
                PointsList.Add(p);
                Length++;

                if (x_min == -1 || x_min > p.X)
                {
                    x_min = p.X;
                }
                if (y_min == -1 || y_min > p.Y)
                {
                    y_min = p.Y;
                }
                if (x_max == -1 || x_max < p.X)
                {
                    x_max = p.X;
                }
                if (y_max == -1 || y_max < p.Y)
                {
                    y_max = p.Y;
                }
            }

            public Point Get(int index)
            {
                return PointsList[index];
            }
        }

        // Inner Class for a Tool Path Line (in Pixel/subPixel(ToolPoint) Coordinates)
        private class ToolLine
        {
            public Point start { get; private set; }
            public Point end { get; private set; }

            public Boolean ToolActive { get; private set; }

            public ToolLine(Point start, Point end, Boolean ToolActive)
            {
                this.start = start;
                this.end = end;
                this.ToolActive = ToolActive;
            }
        }

        #endregion

        #region Constructors

        public IMG_ToolPathing()
        {
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = "";

            toolpath_worker = new BackgroundWorker();
            toolpath_worker.WorkerReportsProgress = true;
            toolpath_worker.WorkerSupportsCancellation = true;
            toolpath_worker.DoWork += new DoWorkEventHandler(toolpath_backgroundwroker_DoWork);
            toolpath_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(toolpath_backgroundwroker_RunWrokerCompleted);
            toolpath_worker.ProgressChanged += new ProgressChangedEventHandler(toolpath_backgroundwroker_ProgressChanged);

        }

        #endregion

        #region Toolpath Public Funtions: Start & Cancel

        /// <summary>
        /// For Bitmap Images.
        /// </summary>
        /// <param name="cnc"></param>
        /// <param name="img"></param>
        /// <param name="mark_White"></param>
        /// <param name="PathType"></param>
        /// <param name="starting_point"></param>
        /// <param name="save_file_path"></param>
        /// <returns></returns>
        public bool Start(CNC_Properties cnc, Bitmap img, bool mark_White, ToolPathingType PathType, CNC_Properties.CoordinatesD starting_point, string save_file_path, bool Generate_Report)
        {
            if (toolpath_worker.IsBusy)
            {
                return false;
            }

            this.Generate_Report = Generate_Report;

            double pixel_size; 
                        
            if (cnc.TP_Units_Mode == CNC_Properties.Units_Mode.Metric)
            {
                pixel_size = 1.0 / (img.HorizontalResolution * 25.4); // 1 inch = 25.4 mm
            }
            else
            {
                pixel_size = 1.0 / (img.HorizontalResolution);
            }

            // adapt image to the size of the tool, making each pixel the size of the diameter 
            double ratio = pixel_size / cnc.tool_diameter_fill;
            int BW_ratio = 99;
            if(ratio < 0.4)
            {
                BW_ratio = Convert.ToInt32(Math.Round(-32.0 * ratio + 99.0));
            }

            BitArray2D newIMG;
            
            using (Bitmap scaledImg = IMG_Filters.ImageResize(img, ratio))
            using (Bitmap scaledBW = IMG_Filters.BlackWhite(scaledImg, BW_ratio))
            {
                newIMG = mark_White ? new BitArray2D(scaledBW, (byte)255) : new BitArray2D(scaledBW, (byte)0);
            }


            Report_img_original_width = img.Width;
            Report_img_original_height = img.Height;
            Report_img_new_width = newIMG.width;
            Report_img_new_height = newIMG.height;

            usingSVG = false;

            var background_parameters = Tuple.Create(newIMG, cnc, PathType, starting_point, save_file_path);

            toolpath_worker.RunWorkerAsync(background_parameters);

            return true;
        }


        public bool Start(CNC_Properties cnc, SvgDocument img, double img_width_mm, double img_height_mm, bool mark_White, int colorDepth, ToolPathingType PathType, CNC_Properties.CoordinatesD starting_point, string save_file_path, bool EdgesOnly, bool Generate_Report)
        {
            if (toolpath_worker.IsBusy)
            {
                return false;
            }

            this.Generate_Report = Generate_Report;


            double pixel_size = img_width_mm / img.Width;

            double px_to_mm_ratio = pixel_size / cnc.tool_diameter_fill;

            SvgUnit original_width = img.Width;
            SvgUnit original_height = img.Height;

            usingSVG = true;
            this.EdgesOnly = EdgesOnly;

            try
            {
                svg_edges = new SVG_Image(img);
            }
            catch (Exception e)
            {
                return false;
            }
            
            
            svg_edges.Resize(px_to_mm_ratio * img.Width / img.ViewBox.Width);
            
            img.Width = Convert.ToInt32(Math.Round(img.Width * px_to_mm_ratio));
            img.Height = Convert.ToInt32(Math.Round(img.Height * px_to_mm_ratio));
            
            BitArray2D newIMG;

            using (Bitmap bmp = IMG_Filters.BlackWhite(IMG_Filters.ConvertTo24bpp(img.Draw()), colorDepth))
            {
                newIMG = mark_White ? new BitArray2D(bmp, (byte)255) : new BitArray2D(bmp, (byte)0);
            }
            
            img.Width = original_width;
            img.Height = original_height;

            Report_img_original_width = (int)img.Width;
            Report_img_original_height = (int)img.Height;
            Report_img_new_width = newIMG.width;
            Report_img_new_height = newIMG.height;

            var background_parameters = Tuple.Create(newIMG, cnc, PathType, starting_point, save_file_path);

            toolpath_worker.RunWorkerAsync(background_parameters);

            return true;
        }



        public void Cancel()
        {
            try
            {
                toolpath_worker.CancelAsync();
            }
            catch (Exception e)
            {

            }
        }

        #endregion

        #region Toolpath Private Funtions

        /// <summary>
        /// Creates a new segment of pixels from the BitArray2D img.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="start"></param>
        /// <param name="pixels_processed"></param>
        /// <returns></returns>
        private Segment Search_Segment(BitArray2D img, Point start, ref BitArray2D pixels_processed)
        {
            Segment newSegment = new Segment();

            newSegment.Add(start);

            for (int search_pointer = 0; search_pointer < newSegment.Length; search_pointer++)
            {
                Point currentPoint = newSegment.Get(search_pointer);

                //
                // Search Order 
                //
                // +---+---+---+
                // | 6 | 2 | 5 |
                // +---+---+---+
                // | 3 | X | 1 |
                // +---+---+---+
                // | 7 | 4 | 8 |
                // +---+---+---+
                //
                //
                // OR (if diagonals are disabled)
                //
                // +---+---+---+
                // | - | 2 | - |
                // +---+---+---+
                // | 3 | X | 1 |
                // +---+---+---+
                // | - | 4 | - |
                // +---+---+---+
                //

                // 1
                if (currentPoint.X < (img.width - 1))
                {
                    if (!pixels_processed[currentPoint.X + 1, currentPoint.Y] && img[currentPoint.X + 1, currentPoint.Y])
                    {
                        newSegment.Add(currentPoint.X + 1, currentPoint.Y);
                    }

                    pixels_processed[currentPoint.X + 1, currentPoint.Y] = true;
                }
                // 2
                if (currentPoint.Y < (img.height - 1))
                {
                    if (!pixels_processed[currentPoint.X, currentPoint.Y + 1] && img[currentPoint.X, currentPoint.Y + 1])
                    {
                        newSegment.Add(currentPoint.X, currentPoint.Y + 1);
                    }

                    pixels_processed[currentPoint.X, currentPoint.Y + 1] = true;
                }
                // 3
                if (currentPoint.X > 0)
                {
                    if (!pixels_processed[currentPoint.X - 1, currentPoint.Y] && img[currentPoint.X - 1, currentPoint.Y])
                    {
                        newSegment.Add(currentPoint.X - 1, currentPoint.Y);
                    }

                    pixels_processed[currentPoint.X - 1, currentPoint.Y] = true;
                }
                // 4
                if (currentPoint.Y > 0)
                {
                    if (!pixels_processed[currentPoint.X, currentPoint.Y - 1] && img[currentPoint.X, currentPoint.Y - 1])
                    {
                        newSegment.Add(currentPoint.X, currentPoint.Y - 1);
                    }

                    pixels_processed[currentPoint.X, currentPoint.Y - 1] = true;
                }

                if (segmentDiagonals)
                {
                    // 5
                    if (currentPoint.X < (img.width - 1) && currentPoint.Y < (img.height - 1))
                    {
                        if (!pixels_processed[currentPoint.X + 1, currentPoint.Y + 1] && img[currentPoint.X + 1, currentPoint.Y + 1])
                        {
                            newSegment.Add(currentPoint.X + 1, currentPoint.Y + 1);
                        }

                        pixels_processed[currentPoint.X + 1, currentPoint.Y + 1] = true;
                    }
                    // 6
                    if (currentPoint.X > 0 && currentPoint.Y < (img.height - 1))
                    {
                        if (!pixels_processed[currentPoint.X - 1, currentPoint.Y + 1] && img[currentPoint.X - 1, currentPoint.Y + 1])
                        {
                            newSegment.Add(currentPoint.X - 1, currentPoint.Y + 1);
                        }

                        pixels_processed[currentPoint.X - 1, currentPoint.Y + 1] = true;
                    }
                    // 7
                    if (currentPoint.X > 0 && currentPoint.Y > 0)
                    {
                        if (!pixels_processed[currentPoint.X - 1, currentPoint.Y - 1] && img[currentPoint.X - 1, currentPoint.Y - 1])
                        {
                            newSegment.Add(currentPoint.X - 1, currentPoint.Y - 1);
                        }

                        pixels_processed[currentPoint.X - 1, currentPoint.Y - 1] = true;
                    }
                    // 8
                    if (currentPoint.X < (img.width - 1) && currentPoint.Y > 0)
                    {
                        if (!pixels_processed[currentPoint.X + 1, currentPoint.Y - 1] && img[currentPoint.X + 1, currentPoint.Y - 1])
                        {
                            newSegment.Add(currentPoint.X + 1, currentPoint.Y - 1);
                        }

                        pixels_processed[currentPoint.X + 1, currentPoint.Y - 1] = true;
                    }

                }
                


            }

            return newSegment;
        }

        /// <summary>
        /// Segment tool path creation. Each path is a pair of points representing a subpixel(with the size of the tool diameter).
        /// Uses the Contour spiral inwards algorithm.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="startingCorner"></param>
        /// <param name="seg_x_min">Segment X coordinate start offset.</param>
        /// <param name="seg_y_min">Segment Y coordinate start offset.</param>
        /// <param name="nToolPathsPerPixel">Subpixels per Pixel.</param>
        /// <returns></returns>
        private List<ToolLine> Contour_SpiralIn_Path(ref BitArray2D segment, Point startingCorner, int seg_x_min, int seg_y_min)
        {
            List<ToolLine> LinesList = new List<ToolLine>();

            Direction search = Direction.Top;
            Point p = startingCorner;

            Point lineStart = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1)); // Pixel
            Point lineEnd;

            while (true)
            {
                // already passed here so path ended
                if (!segment[p.X, p.Y])
                {
                    break;
                }

                segment[p.X, p.Y] = false;
                switch (search)
                {
                    // Moving to the Top (follow left border)
                    // +---+---+---+
                    // | - | T | - |
                    // +---+---+---+
                    // | F | X | - |
                    // +---+---+---+
                    case Direction.Top:
                        // Save Line and Turn Left
                        if (segment[p.X - 1, p.Y])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X,lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X - 1, p.Y);
                            search = Direction.Left;

                            continue;
                        }
                        // Save Line and Turn Right
                        if (!segment[p.X, p.Y + 1])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X + 1, p.Y);
                            search = Direction.Right;

                            continue;
                        }
                        // Continue the same path
                        p = new Point(p.X, p.Y + 1);

                        break;
                    // Moving to the Right (follow Top border)
                    // +---+---+---+
                    // | - | F | - |
                    // +---+---+---+
                    // | - | X | T |
                    // +---+---+---+
                    case Direction.Right:
                        // Save Line and Turn Top
                        if (segment[p.X, p.Y + 1])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X, p.Y + 1);
                            search = Direction.Top;

                            continue;
                        }
                        // Save Line and Turn Bottom
                        if (!segment[p.X + 1, p.Y])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X, p.Y - 1);
                            search = Direction.Bottom;

                            continue;
                        }
                        // Continue the same path
                        p = new Point(p.X + 1, p.Y);

                        break;
                    // Moving to the Bottom (follow Right border)
                    // +---+---+---+
                    // | - | X | F |
                    // +---+---+---+
                    // | - | T | - |
                    // +---+---+---+
                    case Direction.Bottom:
                        // Save Line and Turn Right
                        if (segment[p.X + 1, p.Y])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X + 1, p.Y);
                            search = Direction.Right;

                            continue;
                        }
                        // Save Line and Turn Left
                        if (!segment[p.X, p.Y - 1])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X - 1, p.Y);
                            search = Direction.Left;

                            continue;
                        }
                        // Continue the same path
                        p = new Point(p.X, p.Y - 1);

                        break;
                    // Moving to the Left (follow Bottom border)
                    // +---+---+---+
                    // | T | X | - |
                    // +---+---+---+
                    // | - | F | - |
                    // +---+---+---+
                    case Direction.Left:
                        // Save Line and Turn Bottom
                        if (segment[p.X, p.Y - 1])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X, p.Y - 1);
                            search = Direction.Bottom;

                            continue;
                        }
                        // Save Line and Turn Top
                        if (!segment[p.X - 1, p.Y])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X, p.Y + 1);
                            search = Direction.Top;

                            continue;
                        }
                        // Continue the same path
                        p = new Point(p.X - 1, p.Y);

                        break;
                }
            }

            return LinesList;
        }

        /// <summary>
        /// Similiar to "Contour_SpiralIn_Path" but takes the adjacent diagonal lines into consideration.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="startingCorner"></param>
        /// <param name="seg_x_min"></param>
        /// <param name="seg_y_min"></param>
        /// <returns></returns>
        private List<ToolLine> Contour_SpiralIn_HighQuality_Path(ref BitArray2D segment, Point startingCorner, int seg_x_min, int seg_y_min)
        {
            throw new NotImplementedException();
            
            // Pathing Policy
            //
            // Moving Top example
            // +---+---+---+
            // | 1 | 2 | 3 |
            // +---+---+---+
            // | X | 0 | 4 |
            // +---+---+---+
            // | X | X | 5 |
            // +---+---+---+
            //
            // Moving Top+Right
            // +---+---+---+
            // | 1 | 2 | 3 |
            // +---+---+---+
            // | X | 0 | 4 |
            // +---+---+---+
            // | X | 6 | 5 |
            // +---+---+---+


            List<ToolLine> LinesList = new List<ToolLine>();

            Direction search = Direction.Top;
            Point p = startingCorner;

            Point lineStart = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1)); // Pixel
            Point lineEnd;

            while (true)
            {
                // already passed here so path ended
                if (!segment[p.X, p.Y])
                {
                    break;
                }

                segment[p.X, p.Y] = false;
                switch (search)
                {
                    // Moving to the Top (follow left border)
                    // +---+---+---+
                    // | - | T | - |
                    // +---+---+---+
                    // | F | X | - |
                    // +---+---+---+
                    case Direction.Top:
                        // Save Line and Turn Left
                        if (segment[p.X - 1, p.Y])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X - 1, p.Y);
                            search = Direction.Left;

                            continue;
                        }
                        // Save Line and Turn Right
                        if (!segment[p.X, p.Y + 1])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X + 1, p.Y);
                            search = Direction.Right;

                            continue;
                        }
                        // Continue the same path
                        p = new Point(p.X, p.Y + 1);

                        break;
                    // Moving to the Right (follow Top border)
                    // +---+---+---+
                    // | - | F | - |
                    // +---+---+---+
                    // | - | X | T |
                    // +---+---+---+
                    case Direction.Right:
                        // Save Line and Turn Top
                        if (segment[p.X, p.Y + 1])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X, p.Y + 1);
                            search = Direction.Top;

                            continue;
                        }
                        // Save Line and Turn Bottom
                        if (!segment[p.X + 1, p.Y])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X, p.Y - 1);
                            search = Direction.Bottom;

                            continue;
                        }
                        // Continue the same path
                        p = new Point(p.X + 1, p.Y);

                        break;
                    // Moving to the Bottom (follow Right border)
                    // +---+---+---+
                    // | - | X | F |
                    // +---+---+---+
                    // | - | T | - |
                    // +---+---+---+
                    case Direction.Bottom:
                        // Save Line and Turn Right
                        if (segment[p.X + 1, p.Y])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X + 1, p.Y);
                            search = Direction.Right;

                            continue;
                        }
                        // Save Line and Turn Left
                        if (!segment[p.X, p.Y - 1])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X - 1, p.Y);
                            search = Direction.Left;

                            continue;
                        }
                        // Continue the same path
                        p = new Point(p.X, p.Y - 1);

                        break;
                    // Moving to the Left (follow Bottom border)
                    // +---+---+---+
                    // | T | X | - |
                    // +---+---+---+
                    // | - | F | - |
                    // +---+---+---+
                    case Direction.Left:
                        // Save Line and Turn Bottom
                        if (segment[p.X, p.Y - 1])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X, p.Y - 1);
                            search = Direction.Bottom;

                            continue;
                        }
                        // Save Line and Turn Top
                        if (!segment[p.X - 1, p.Y])
                        {
                            lineEnd = new Point(seg_x_min + (p.X - 1), seg_y_min + (p.Y - 1));  // Pixel

                            LinesList.Add(new ToolLine(new Point(lineStart.X, lineStart.Y), new Point(lineEnd.X, lineEnd.Y), true));

                            lineStart = lineEnd;

                            p = new Point(p.X, p.Y + 1);
                            search = Direction.Top;

                            continue;
                        }
                        // Continue the same path
                        p = new Point(p.X - 1, p.Y);

                        break;
                }
            }

            return LinesList;
        }

        /// <summary>
        /// Finds which segment is closest to the point start.
        /// </summary>
        /// <param name="List_Of_Segments"></param>
        /// <param name="start"></param>
        /// <returns>Returns de index of the closest segment.</returns>
        private int Find_Nearest_Segment(List<List<ToolLine>> List_Of_Segments, Point start) 
        {
            
            double segment_distance = 0;

            double sortest_distance = Math_Utilities.SquareDistanceBetweenPoints(start, List_Of_Segments[0][0].start);

            int nearest_segment_index = 0;

            
            for (int i = 1; i < List_Of_Segments.Count; i++)
            {
                segment_distance = Math_Utilities.SquareDistanceBetweenPoints(start, List_Of_Segments[i][0].start);

                if (segment_distance < sortest_distance)
                {
                    sortest_distance = segment_distance;
                    nearest_segment_index = i;
                }

            }
                       
            

            return nearest_segment_index;
        }
                
        

        /// <summary>
        /// Appends to the stringbuilder "str" the initial file header.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="cnc"></param>
        /// <param name="File_Path"></param>
        private void Gcode_CreateHeader(StringBuilder str, CNC_Properties cnc, string File_Path)
        {
            str.Clear();

            // Time Stamp
            DateTime time = DateTime.Now;
            int hour_int = time.Hour;
            int minute_int = time.Minute;
            int second_int = time.Second;
            int day_int = time.Day;
            int month_int = time.Month;
            int year_int = time.Year;

            String hour;
            String minute;
            String second;
            String day;
            String month;
            String year;

            if (hour_int < 10)
            {
                hour = "0" + hour_int.ToString();
            }
            else
            {
                hour = hour_int.ToString();
            }
            if (minute_int < 10)
            {
                minute = "0" + minute_int.ToString();
            }
            else
            {
                minute = minute_int.ToString();
            }
            if (second_int < 10)
            {
                second = "0" + second_int.ToString();
            }
            else
            {
                second = second_int.ToString();
            }
            if (day_int < 10)
            {
                day = "0" + day_int.ToString();
            }
            else
            {
                day = day_int.ToString();
            }
            if (month_int < 10)
            {
                month = "0" + month_int.ToString();
            }
            else
            {
                month = month_int.ToString();
            }
            year = year_int.ToString();

            // ---Header---
            str.AppendLine("(" + File_Path + ")");
            str.AppendLine("(Generated by " + Application.ProductName + "_" + Application.ProductVersion + " Application (Author: Rui Graça)" + " on " + day + "-" + month + "-" + year + " at " + hour + ":" + minute + ":" + second + ")");
            //str.AppendLine("(Generated for Colinbus CNC Machine with the firmware version: " + Firmware_Version + " (Author: Daniel Lopes)" + ")");
            str.AppendLine("(CNC Properties)");
            if (cnc._ToolType == CNC_Properties.ToolType.Laser)
            {
                str.AppendLine("(Selected Tool: Laser)");
            }
            else
            {
                str.AppendLine("(Selected Tool: Drill)");
            }
            str.AppendLine("(Filling Tool Size: " + cnc.tool_diameter_fill + ")");
            str.AppendLine("(Contour Tool Size: " + cnc.tool_diameter_contour + ")");

            // ---Configurations---
            // Units Mode
            if (cnc.TP_Units_Mode == CNC_Properties.Units_Mode.Metric)
            {
                str.AppendLine("(Metric Units)");
                str.AppendLine(GCODE.Units_Metric);
            }
            else
            {
                str.AppendLine("(Imperial Units)");
                str.AppendLine(GCODE.Units_Imperial);
            }
            // Distance Mode
            if (cnc.TP_Coordinates_Mode == CNC_Properties.Coordinates_Mode.Absolute)
            {
                str.AppendLine("(Absolute Coordinates)");
                str.AppendLine(GCODE.Distance_Absolute);
            }
            else
            {
                str.AppendLine("(Relative Coordinates)");
                str.AppendLine(GCODE.Distance_Relative);
            }
            // Feedrate Mode
            if (cnc._Feedrate_Mode == CNC_Properties.Feedrate_Mode.UnitsPerMinute)
            {
                str.AppendLine("(Feedrate in *Units*/min)");
                str.AppendLine(GCODE.Feedrate_UnitsPerMinute);
            }
            else
            {
                str.AppendLine("(Feedrate in RPM)");
                str.AppendLine(GCODE.Feedrate_InverseTime);
            }
            // Multiblocks on

            str.AppendLine("(Multiblocks)");
            str.AppendLine(GCODE.TurnOn_Multiblocs /*+ " E150"*/ + " L2");

            // Set initialposition
            str.AppendLine("(Home X and Y axes)");
            str.AppendLine(GCODE.Home_Movement + " X" + " Y");

            // header and initializations are finished

        }

        /// <summary>
        /// Appends to the stringbuilder "str" the engage tool routine.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="cnc"></param>
        private void Gcode_Routine_EngageTool(StringBuilder str, CNC_Properties cnc, bool fill)
        {
            if (cnc._ToolType == CNC_Properties.ToolType.Laser)
            {
                // Add to the current line (previsou append doesnt have New Line)

                if (fill)
                {
                    str.AppendLine(" E" + cnc.laser_power_fill.ToString("###########0.######", nfi));
                }
                else
                {
                    str.AppendLine(" E" + cnc.laser_power_contour.ToString("###########0.######", nfi));
                }
            }
            else
            {
                str.AppendLine("G00 Z" + cnc.drill_down_height.ToString("###########0.######", nfi));
            }
        }

        /// <summary>
        /// Appends to the stringbuilder "str" the disengage tool routine.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="cnc"></param>
        private void Gcode_Routine_DisengageTool(StringBuilder str, CNC_Properties cnc)
        {
            if (cnc._ToolType == CNC_Properties.ToolType.Laser)
            {
                // Add to the current line (previsou append doesnt have New Line)

                str.AppendLine(" E" + cnc.laser_power_idle.ToString("###########0.######", nfi));


            }
            else
            {
                str.AppendLine("G00 Z" + cnc.drill_up_height.ToString("###########0.######", nfi));
            }
        }

        #endregion

        #region Backgroundworker

        private void toolpath_backgroundwroker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            Tuple<BitArray2D, CNC_Properties, ToolPathingType, CNC_Properties.CoordinatesD, string> bw_parameters;

            try
            {
                bw_parameters = e.Argument as Tuple<BitArray2D, CNC_Properties, ToolPathingType, CNC_Properties.CoordinatesD, string>;
            }
            catch(Exception err)
            {
                return;
            }

            BitArray2D img = bw_parameters.Item1;
            CNC_Properties cnc = bw_parameters.Item2;
            ToolPathingType PathType = bw_parameters.Item3;
            CNC_Properties.CoordinatesD starting_point = bw_parameters.Item4;
            string save_file_path = bw_parameters.Item5;

            // report vars
            int Report_fill_n_lines_on = 0;
            int Report_fill_n_lines_off = 0;
            int Report_contour_n_lines_on = 0;
            int Report_contour_n_arc_on = 0;
            int Report_contour_n_lines_off = 0;

            int Report_fill_pre_Segments = 0;
            int Report_fill_post_Segments = 0;

            int Report_Px_Remove = 0;
            int Report_Px_Keep = 0;

            Decimal Report_fill_distance_mm_tool_on = 0;
            Decimal Report_fill_distance_mm_tool_off = 0;

            Decimal Report_contour_line_distance_mm_tool_on = 0;
            Decimal Report_contour_arc_distance_mm_tool_on = 0;
            Decimal Report_contour_distance_mm_tool_off = 0;


            Decimal Report_fill_ETA_sec_tool_on = 0;
            Decimal Report_fill_ETA_sec_tool_off = 0;
            Decimal Report_contour_ETA_sec_tool_on = 0;
            Decimal Report_contour_ETA_sec_tool_off = 0;


            using (Semaphore ToRemove = new Semaphore(1, 1))
            using (Semaphore ToKeep = new Semaphore(1, 1))
            {
                Parallel.For(0, img.count, i =>
                {
                    if (img[i])
                    {
                        ToRemove.WaitOne();
                        Report_Px_Remove++;
                        ToRemove.Release();
                    }
                    else
                    {
                        ToKeep.WaitOne();
                        Report_Px_Keep++;
                        ToKeep.Release();
                    }
                });
            }

            System.Diagnostics.Stopwatch Report_TotalTP_Gen_sec = System.Diagnostics.Stopwatch.StartNew();
            System.Diagnostics.Stopwatch Report_ImgSegment_sec;
            System.Diagnostics.Stopwatch Report_SegmentPath_sec;
            System.Diagnostics.Stopwatch Report_SegmentConnection_sec;
            System.Diagnostics.Stopwatch Report_GCode_Fill_Gen_sec;
            System.Diagnostics.Stopwatch Report_GCode_Contour_Gen_sec;
            //


            int progress = 0;
            int progress_aux = 0;
            string progress_stage = "Separating image segments...";
            // background process ----

            //
            // Create the image segments (spiral) or lines(zigzag)
            //
            bw.ReportProgress(progress, progress_stage);

            Report_ImgSegment_sec = System.Diagnostics.Stopwatch.StartNew();

            BitArray2D pixels_processed = new BitArray2D(img.width, img.height);
            List<Segment> segmentsList = new List<Segment>();

            
            if (PathType == ToolPathingType.Spiral || PathType == ToolPathingType.SpiralHighQuality)
            {
                for (int y = 0; y < img.height; y++)
                {
                    for (int x = 0; x < img.width; x++)
                    {
                        // check cancelation
                        if (bw.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }

                        if (pixels_processed[x, y])
                        {
                            continue;
                        }

                        pixels_processed[x, y] = true;

                        if (!img[x, y])
                        {
                            continue;
                        }

                        // get the segment
                        segmentsList.Add(Search_Segment(img, new Point(x, y), ref pixels_processed));

                        // progress update
                        progress_aux = Convert.ToInt32(Math.Round(25 * ((double)y * img.width + x) / ((double)img.height * img.width))); // maximum of 25% per stage
                        if (progress_aux != progress)
                        {
                            progress = progress_aux;
                            bw.ReportProgress(progress, progress_stage);
                        }
                    }

                }
            }
            else
            {
                using (Semaphore List_semaphore = new Semaphore(1, 1))
                {
                    if (img.height > img.width) // path verticaly
                    {
                        int columns_done = 0;
                        Parallel.For(0, img.width, column =>
                        {
                            Segment s = new Segment();
                            if (column % 2 == 0) // Move Top
                            {
                                for (int l = 0; l < img.height; l++)
                                {
                                    if(img[column, l] == true)
                                    {
                                        s.Add(column, l);
                                    }                                    
                                }
                            }
                            else // Move Bottom
                            {
                                for (int l = img.height - 1; l > -1; l--)
                                {
                                    if (img[column, l] == true)
                                    {
                                        s.Add(column, l);
                                    }
                                }
                            }

                            List_semaphore.WaitOne();
                            segmentsList.Add(s);

                            // update progress
                            columns_done++;
                            progress_aux = Convert.ToInt32(Math.Round(25 * ((double)columns_done / ((double)img.width)))); // maximum of 25% per stage
                            if (progress_aux != progress)
                            {
                                progress = progress_aux;
                                bw.ReportProgress(progress, progress_stage);
                            }

                            List_semaphore.Release();
                        });
                    }
                    else // path horizontaly
                    {
                        int lines_done = 0;
                        Parallel.For(0, img.height, line =>
                        {
                            Segment s = new Segment();
                            if (line % 2 == 0) // Move Right
                            {
                                for (int c = 0; c < img.width; c++)
                                {
                                    if (img[c, line] == true)
                                    {
                                        s.Add(c, line);
                                    }                                    
                                }
                            }
                            else // Move Left
                            {
                                for (int c = img.width - 1; c > -1; c--)
                                {
                                    if (img[c, line] == true)
                                    {
                                        s.Add(c, line);
                                    } 
                                }
                            }

                            List_semaphore.WaitOne();
                            segmentsList.Add(s);

                            // update progress
                            lines_done++;
                            progress_aux = Convert.ToInt32(Math.Round(25 * ((double)lines_done / ((double)img.height)))); // maximum of 25% per stage
                            if (progress_aux != progress)
                            {
                                progress = progress_aux;
                                bw.ReportProgress(progress, progress_stage);
                            }

                            List_semaphore.Release();
                        });
                    }
                }
                    
            }

            Report_ImgSegment_sec.Stop();

            if (segmentsList.Count == 0) // no segments to remove
            {
                return; 
            }

            
            Report_fill_pre_Segments = segmentsList.Count;
            
            //
            // Path segments(for spiral) or lines for Zig-zag (can't be cancelled)
            //
            progress_stage = "Pathing segments...";
            bw.ReportProgress(progress, progress_stage);

            Report_SegmentPath_sec = System.Diagnostics.Stopwatch.StartNew();

            int total_segments = segmentsList.Count;
            int segments_done = 0;

            List<List<ToolLine>> ToolLine_Segments = new List<List<ToolLine>>();
            

            using (Semaphore Segment_Pathing = new Semaphore(1, 1))
            {
                if (PathType == ToolPathingType.Spiral || PathType == ToolPathingType.SpiralHighQuality)
                {
                    Parallel.ForEach(segmentsList, (segment) =>
                    {
                        // New list of ToolLines for current segment
                        List<List<ToolLine>> newSegmentPaths = new List<List<ToolLine>>();

                        // Rebuild segment image and one extra line of subPixels on each side
                        int seg_x_min = segment.x_min;
                        int seg_y_min = segment.y_min;
                        int seg_width = segment.x_max + 1 - seg_x_min;
                        int seg_height = segment.y_max + 1 - seg_y_min;

                        BitArray2D img_segment = new BitArray2D(seg_width + 2, seg_height + 2);

                        // Pixel Expansion to subpixels
                        for (int i = 0; i < segment.Length; i++)
                        {
                            Point p = segment.Get(i);

                            img_segment[(p.X - seg_x_min) + 1, (p.Y - seg_y_min) + 1] = true;

                        }

                        // Paths creation
                        
                        for (int y = 1; y < seg_height + 1; y++)
                        {
                            for (int x = 1; x < seg_width + 1; x++)
                            {
                                if (!img_segment[x, y])
                                {
                                    continue;
                                }

                                // Connect previous sub segment path to the new sub segment
                                /*
                                if (newSegmentPaths.Count != 0)
                                {
                                    newSegmentPaths.Add(new ToolLine(newSegmentPaths.Last().end,
                                                                     new Point(seg_x_min + (x - 1), seg_y_min + (y - 1)), // Pixel
                                                                     false));
                                }*/

                                // Generate spiral path for the new sub segment found
                                if (PathType == ToolPathingType.Spiral)
                                {
                                    newSegmentPaths.Add(Contour_SpiralIn_Path(ref img_segment, new Point(x, y), seg_x_min, seg_y_min));
                                                                        
                                }
                                else
                                {
                                    newSegmentPaths.Add(Contour_SpiralIn_HighQuality_Path(ref img_segment, new Point(x, y), seg_x_min, seg_y_min));
                                                                        
                                }


                            }
                        }

                        Segment_Pathing.WaitOne();
                        ToolLine_Segments.AddRange(newSegmentPaths);

                        // progress update
                        segments_done++;
                        progress_aux = Convert.ToInt32(Math.Round(25 + 25 * ((double)segments_done / (double)total_segments))); // maximum of 25% per stage 
                        if (progress_aux != progress)
                        {
                            progress = progress_aux;
                            bw.ReportProgress(progress, progress_stage);
                        }
                        Segment_Pathing.Release();


                    });
                }
                else // zig zag
                {
                    Parallel.ForEach(segmentsList, (segment) =>
                    {
                        if(segment.Length != 0)
                        {
                            List<List<ToolLine>> newSegmentPaths = new List<List<ToolLine>>();
                            Point start;

                            start = segment.Get(0);
                            Point prev_Point = start;
                            Point current_Point = start;

                            if (img.height > img.width) // path verticaly
                            {
                                for (int p = 1; p < segment.Length; p++)
                                {
                                    current_Point = segment.Get(p);
                                    if (current_Point.Y + 1 == prev_Point.Y || current_Point.Y - 1 == prev_Point.Y) // consecutive points
                                    {
                                        prev_Point = current_Point;
                                    }
                                    else // non consecutive points  save a new path
                                    {
                                        List<ToolLine> lineSegment = new List<ToolLine>();
                                        lineSegment.Add(new ToolLine(new Point(start.X, start.Y),
                                                                         new Point(prev_Point.X, prev_Point.Y),
                                                                         true));

                                        newSegmentPaths.Add(lineSegment);

                                        //newSegmentPaths.Add(new ToolLine(new Point(start.X, start.Y),
                                        //                                 new Point(prev_Point.X, prev_Point.Y),
                                        //                                 true));
                                        //newSegmentPaths.Add(new ToolLine(new Point(prev_Point.X, prev_Point.Y),
                                        //                                 new Point(current_Point.X, current_Point.Y),
                                        //                                 false));
                                        start = new Point(current_Point.X, current_Point.Y);
                                        prev_Point = start;
                                    }
                                }
                            }
                            else // path horizontaly
                            {
                                for (int p = 1; p < segment.Length; p++)
                                {
                                    current_Point = segment.Get(p);
                                    if (current_Point.X + 1 == prev_Point.X || current_Point.X - 1 == prev_Point.X) // consecutive points
                                    {
                                        prev_Point = current_Point;
                                    }
                                    else // non consecutive points  save a new path
                                    {
                                        List<ToolLine> lineSegment = new List<ToolLine>();
                                        lineSegment.Add(new ToolLine(new Point(start.X, start.Y),
                                                                         new Point(prev_Point.X, prev_Point.Y),
                                                                         true));

                                        newSegmentPaths.Add(lineSegment);

                                        //newSegmentPaths.Add(new ToolLine(new Point(start.X, start.Y),
                                        //                                 new Point(prev_Point.X, prev_Point.Y),
                                        //                                 true));
                                        //newSegmentPaths.Add(new ToolLine(new Point(prev_Point.X, prev_Point.Y),
                                        //                                 new Point(current_Point.X, current_Point.Y),
                                        //                                 false));
                                        start = new Point(current_Point.X, current_Point.Y);
                                        prev_Point = start;
                                    }
                                }
                            }

                            // last path of  the line
                            List<ToolLine> lastSegment = new List<ToolLine>();
                            lastSegment.Add(new ToolLine(new Point(start.X, start.Y),
                                                             new Point(current_Point.X, current_Point.Y),
                                                             true));

                            newSegmentPaths.Add(lastSegment);

                            //newSegmentPaths.Add(new ToolLine(new Point(start.X, start.Y),
                            //                                 new Point(current_Point.X, current_Point.Y),
                            //                                 true));

                            // save to the list queue
                            Segment_Pathing.WaitOne();
                            ToolLine_Segments.AddRange(newSegmentPaths);

                            // progress update
                            segments_done++;
                            progress_aux = Convert.ToInt32(Math.Round(25 + 25 * ((double)segments_done / (double)total_segments))); // maximum of 25% per stage 
                            if (progress_aux != progress)
                            {
                                progress = progress_aux;
                                bw.ReportProgress(progress, progress_stage);
                            }
                            Segment_Pathing.Release();
                        }
                    });
                }
            }
            
            segmentsList.Clear();


            Report_SegmentPath_sec.Stop();
            // check cancelation
            if (bw.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

                       

            //
            // Connect Segments
            //

            Report_SegmentConnection_sec = System.Diagnostics.Stopwatch.StartNew();

            progress_stage = "Connecting segments...";
            bw.ReportProgress(progress, progress_stage);

            total_segments = ToolLine_Segments.Count;
            Report_fill_post_Segments = total_segments;

            segments_done = 0;

            List<ToolLine> Pixel_ToolPath = new List<ToolLine>();
            int next_Segment;

            // Sort, to make the segments which are closest first
            // find nearest to 0
            if(ToolLine_Segments.Count > 0)
            {
                next_Segment = Find_Nearest_Segment(ToolLine_Segments, new Point(0, 0));


                // Add the paths of the nearest segment
                Pixel_ToolPath.Add(new ToolLine(new Point(0, 0),
                                                ToolLine_Segments[next_Segment][0].start,
                                                false));

                // Add the paths of the nearest segment
                Pixel_ToolPath.AddRange(ToolLine_Segments[next_Segment]);
                // remove the segment from the Segments list
                //ToolLine_Segments.RemoveRange(next_Segment, 1);
                ToolLine_Segments.RemoveAt(next_Segment);
            }            

            while (ToolLine_Segments.Count > 0)
            {
                // check cancelation
                if (bw.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                // find nearest to previous end point
                Point end = Pixel_ToolPath[Pixel_ToolPath.Count - 1].end;
                next_Segment = Find_Nearest_Segment(ToolLine_Segments, end);

                // create the connection between segments
                Pixel_ToolPath.Add(new ToolLine(Pixel_ToolPath[Pixel_ToolPath.Count - 1].end,
                                                 ToolLine_Segments[next_Segment][0].start,
                                                 false));

                // Add the paths of the nearest segment
                Pixel_ToolPath.AddRange(ToolLine_Segments[next_Segment]);
                // remove the segment from the Segments list
                //ToolLine_Segments.RemoveRange(next_Segment, 1);
                ToolLine_Segments.RemoveAt(next_Segment);


                // progress update
                segments_done++;
                progress_aux = Convert.ToInt32(Math.Round(50 + 25 * ((double)segments_done / (double)total_segments))); // maximum of 25% per stage 
                if (progress_aux != progress)
                {
                    progress = progress_aux;
                    bw.ReportProgress(progress, progress_stage);
                }
            }

            Report_SegmentConnection_sec.Stop();

            //
            // G-Code generation
            //

            progress_stage = "Generating G-Code...";
            bw.ReportProgress(progress, progress_stage);

            total_segments = Pixel_ToolPath.Count;
            segments_done = 0;

            Boolean tool_is_engaged = false;

            double pixel_size = cnc.tool_diameter_fill; 

            StringBuilder Gcode_ToolPath = new StringBuilder();

            // image for preview
            Bitmap gcode_img = new Bitmap(img.width * 2 + 1, img.height * 2 + 1); // image has double size of the subpixel amount to be able to se each path line (instead of every thing black)
            Bitmap gcode_img_ToolOnly = new Bitmap(img.width * 2 + 1, img.height * 2 + 1);
            Graphics img_graphics = Graphics.FromImage(gcode_img);
            Graphics img_graphics_ToolOnly = Graphics.FromImage(gcode_img_ToolOnly);
            Pen pen_G01 = new Pen(Color.Black);
            Pen pen_G00 = new Pen(Color.Cyan);
            
            // Gcode Header
            Gcode_CreateHeader(Gcode_ToolPath, cnc, save_file_path);
            Gcode_ToolPath.AppendLine(GCODE.Tool_On);

            if (cnc._ToolType == CNC_Properties.ToolType.Laser)
            {
                Gcode_ToolPath.AppendLine(GCODE.Set_Laser_Power);
            }

            Report_GCode_Fill_Gen_sec = System.Diagnostics.Stopwatch.StartNew();
            if (!EdgesOnly)
            {
                

                Gcode_ToolPath.AppendLine("(Filling Path)");

                Gcode_ToolPath.AppendLine(GCODE.Rapid_Movement + " X" + starting_point.X + " Y" + starting_point.Y + " Z" + (cnc._ToolType == CNC_Properties.ToolType.Laser? cnc.laser_height_roughing : cnc.drill_up_height));

                Report_fill_n_lines_off++;
                Report_fill_distance_mm_tool_off += Convert.ToDecimal(Math.Sqrt(Math.Pow(starting_point.X, 2) + Math.Pow(starting_point.Y, 2)));
                // if tool = dril -> Z -> tool_up_height   else  laser_height
                /*
                if (cnc._ToolType == CNC_Properties.ToolType.Laser)
                {
                    Gcode_ToolPath.AppendLine(GCODE.Rapid_Movement + " Z" + cnc.laser_height_roughing);
                }
                else
                {
                    Gcode_ToolPath.AppendLine(GCODE.Rapid_Movement + " Z" + cnc.drill_up_height);
                }*/

                // turn on tool and wait a few seconds

                //Gcode_ToolPath.AppendLine(GCODE.Dwell + " S2"); // wait 2 seconds

                foreach (ToolLine tl in Pixel_ToolPath)
                {
                    // check cancelation
                    if (bw.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    // calc x and y
                    int pixel_x = tl.end.X;
                    int pixel_y = tl.end.Y;

                    if (cnc.TP_Coordinates_Mode == CNC_Properties.Coordinates_Mode.Relative)
                    {
                        pixel_x -= tl.start.X;
                        pixel_y -= tl.start.Y;
                    }

                    double x = pixel_x * pixel_size + pixel_size / 2;
                    double y = pixel_y * pixel_size + pixel_size / 2;

                    if (cnc.TP_Coordinates_Mode == CNC_Properties.Coordinates_Mode.Absolute)
                    {
                        x += starting_point.X;
                        y += starting_point.Y;
                    }

                    //


                    if (cnc._ToolType == CNC_Properties.ToolType.Laser)
                    {
                        if (tl.ToolActive)
                        {
                            Gcode_ToolPath.Append(GCODE.Linear_Movement + " X" + x.ToString("###########0.######", nfi)
                                                                        + " Y" + y.ToString("###########0.######", nfi)
                                                                        + " F" + cnc.LinearFeedrate.ToString("###########0.######", nfi));

                            Gcode_Routine_EngageTool(Gcode_ToolPath, cnc,true); // add E parameter

                            // draw on image with pen_G01 (tool engaged)
                            img_graphics.DrawLine(pen_G01, new Point(2 * (tl.start.X),
                                                                     2 * (tl.start.Y)),
                                                           new Point(2 * (tl.end.X),
                                                                     2 * (tl.end.Y)));

                            img_graphics_ToolOnly.DrawLine(pen_G01, new Point(2 * (tl.start.X),
                                                                     2 * (tl.start.Y)),
                                                           new Point(2 * (tl.end.X),
                                                                     2 * (tl.end.Y)));

                            Report_fill_n_lines_on++;
                            Report_fill_distance_mm_tool_on += Convert.ToDecimal(Math.Sqrt(Math.Pow((tl.end.X - tl.start.X)*pixel_size, 2) + Math.Pow((tl.end.Y - tl.start.Y) * pixel_size, 2)));
                        }
                        else
                        {


                            Gcode_ToolPath.Append(GCODE.Rapid_Movement + " X" + x.ToString("###########0.######", nfi)
                                                                       + " Y" + y.ToString("###########0.######", nfi)
                                                                       + " F" + cnc.RapidFeedrate.ToString("###########0.######", nfi));

                            Gcode_Routine_DisengageTool(Gcode_ToolPath, cnc); // add E parameter

                            // draw on image with pen_G00 (tool disengaged)
                            img_graphics.DrawLine(pen_G00, new Point(2 * (tl.start.X),
                                                                     2 * (tl.start.Y)),
                                                           new Point(2 * (tl.end.X),
                                                                     2 * (tl.end.Y)));

                            Report_fill_n_lines_off++;
                            Report_fill_distance_mm_tool_off += Convert.ToDecimal(Math.Sqrt(Math.Pow((tl.end.X - tl.start.X) * pixel_size, 2) + Math.Pow((tl.end.Y - tl.start.Y) * pixel_size, 2)));
                        }
                    }
                    else
                    {
                        if (tl.ToolActive)
                        {
                            if (!tool_is_engaged) // engage
                            {
                                tool_is_engaged = true;
                                Gcode_Routine_EngageTool(Gcode_ToolPath, cnc,true);
                            }

                            Gcode_ToolPath.Append(GCODE.Linear_Movement + " X" + x.ToString("###########0.######", nfi)
                                                                        + " Y" + y.ToString("###########0.######", nfi)
                                                                        + " F" + cnc.LinearFeedrate.ToString("###########0.######", nfi));

                            // draw on image with pen_G01 (tool engaged)
                            img_graphics.DrawLine(pen_G01, new Point(2 * (tl.start.X),
                                                                     2 * (tl.start.Y)),
                                                           new Point(2 * (tl.end.X),
                                                                     2 * (tl.end.Y)));

                            img_graphics_ToolOnly.DrawLine(pen_G01, new Point(2 * (tl.start.X),
                                                                     2 * (tl.start.Y)),
                                                           new Point(2 * (tl.end.X),
                                                                     2 * (tl.end.Y)));
                            Report_fill_n_lines_on++;
                            Report_fill_distance_mm_tool_on += Convert.ToDecimal(Math.Sqrt(Math.Pow((tl.end.X - tl.start.X) * pixel_size, 2) + Math.Pow((tl.end.Y - tl.start.Y) * pixel_size, 2)));
                        }
                        else
                        {
                            if (tool_is_engaged) // disengage
                            {
                                tool_is_engaged = false;
                                Gcode_Routine_DisengageTool(Gcode_ToolPath, cnc);
                            }
                            Gcode_ToolPath.Append(GCODE.Rapid_Movement + " X" + x.ToString("###########0.######", nfi)
                                                                       + " Y" + y.ToString("###########0.######", nfi)
                                                                       + " F" + cnc.RapidFeedrate.ToString("###########0.######", nfi));

                            // draw on image with pen_G00 (tool disengaged)
                            img_graphics.DrawLine(pen_G00, new Point(2 * (tl.start.X),
                                                                     2 * (tl.start.Y)),
                                                           new Point(2 * (tl.end.X),
                                                                     2 * (tl.end.Y)));

                            Report_fill_n_lines_off++;
                            Report_fill_distance_mm_tool_off += Convert.ToDecimal(Math.Sqrt(Math.Pow((tl.end.X - tl.start.X) * pixel_size, 2) + Math.Pow((tl.end.Y - tl.start.Y) * pixel_size, 2)));
                        }
                    }

                    // progress update
                    segments_done++;
                    progress_aux = Convert.ToInt32(Math.Round(75 + 25 * ((double)segments_done / (double)total_segments))); // maximum of 25% per stage 
                    if (progress_aux != progress)
                    {
                        progress = progress_aux;
                        bw.ReportProgress(progress, progress_stage);
                    }

                }

                
            }
            Report_GCode_Fill_Gen_sec.Stop();

            Report_GCode_Contour_Gen_sec = System.Diagnostics.Stopwatch.StartNew();
            if (usingSVG) // path edges finishes here
            {
                Gcode_ToolPath.AppendLine("(Contour Path)");

                

                // change z for precision  
                tool_is_engaged = false;

                double x;// = svg_edges.Objects[0].Paths[0].Start.X * pixel_size + pixel_size / 2;
                double y;// = svg_edges.Objects[0].Paths[0].Start.Y * pixel_size + pixel_size / 2;

                int img_x_prev;
                int img_y_prev;

                try
                {
                    img_x_prev = Pixel_ToolPath[Pixel_ToolPath.Count - 1].end.X;
                    img_y_prev = Pixel_ToolPath[Pixel_ToolPath.Count - 1].end.Y;
                }
                catch(Exception err)
                {
                    img_x_prev = 0;
                    img_y_prev = 0;
                }
                

                if (EdgesOnly)
                {
                    img_x_prev = 0;
                    img_y_prev = 0;
                }

                if (cnc._ToolType == CNC_Properties.ToolType.Laser)
                {
                    Gcode_ToolPath.Append(GCODE.Rapid_Movement + " Z" + cnc.laser_height_precision);
                    Gcode_Routine_DisengageTool(Gcode_ToolPath, cnc);
                }
                else
                {
                    Gcode_Routine_DisengageTool(Gcode_ToolPath, cnc);
                }

                
                foreach (SVG_Object obj in svg_edges.Objects)
                {
                    // connect last object
                    x = obj.Paths[0].Start.X * pixel_size;// + pixel_size / 2.0;
                    y = obj.Paths[0].Start.Y * pixel_size;// + pixel_size / 2.0;

                    if (cnc._ToolType == CNC_Properties.ToolType.Laser)
                    {
                        Gcode_ToolPath.Append(GCODE.Rapid_Movement + " X" + x.ToString("###########0.######", nfi)
                                                                       + " Y" + y.ToString("###########0.######", nfi)
                                                                       + " F" + cnc.RapidFeedrate.ToString("###########0.######", nfi));
                        Gcode_Routine_DisengageTool(Gcode_ToolPath, cnc);
                    }
                    else
                    {
                        Gcode_Routine_DisengageTool(Gcode_ToolPath, cnc);
                        Gcode_ToolPath.AppendLine(GCODE.Rapid_Movement + " X" + x.ToString("###########0.######", nfi)
                                                                       + " Y" + y.ToString("###########0.######", nfi)
                                                                       + " F" + cnc.RapidFeedrate.ToString("###########0.######", nfi));
                    }

                    img_graphics.DrawLine(pen_G00, new PointF(2 * img_x_prev,
                                                             2 * img_y_prev),
                                                   new PointF(Convert.ToSingle(2 * obj.Paths[0].Start.X + 0.5 - ((obj.Paths[0].Start.X == 0) ? 0 : 1) ),
                                                             Convert.ToSingle(2 * obj.Paths[0].Start.Y - 0.5 - ((obj.Paths[0].Start.Y == 0) ? 0 : 1))));
                    Report_contour_n_lines_off++;
                    Report_contour_distance_mm_tool_off += Convert.ToDecimal(Math.Sqrt(Math.Pow((obj.Paths[0].Start.X - img_x_prev) * pixel_size, 2) + Math.Pow((obj.Paths[0].Start.X - img_x_prev) * pixel_size, 2)));

                    for (int i = 0; i < obj.Paths.Count; i++)
                    {
                        SVG_Path path = obj.Paths[i];

                        if (i != 0)
                        {
                            if (obj.Paths[i].Start.X != obj.Paths[i - 1].End.X || obj.Paths[i].Start.Y != obj.Paths[i - 1].End.Y) // discontinuous paths
                            {
                                if (cnc._ToolType == CNC_Properties.ToolType.Laser)
                                {
                                    Gcode_ToolPath.Append(GCODE.Rapid_Movement + " X" + (path.Start.X * pixel_size).ToString("###########0.######", nfi)
                                                                                    + " Y" + (path.Start.Y * pixel_size).ToString("###########0.######", nfi)
                                                                                    + " F" + cnc.RapidFeedrate.ToString("###########0.######", nfi));
                                    Gcode_Routine_DisengageTool(Gcode_ToolPath, cnc);
                                }
                                else
                                {
                                    Gcode_Routine_DisengageTool(Gcode_ToolPath, cnc);
                                    Gcode_ToolPath.AppendLine(GCODE.Rapid_Movement + " X" + (path.Start.X * pixel_size).ToString("###########0.######", nfi)
                                                                                    + " Y" + (path.Start.Y * pixel_size).ToString("###########0.######", nfi)
                                                                                    + " F" + cnc.RapidFeedrate.ToString("###########0.######", nfi));
                                }
                                
                                img_graphics.DrawLine(pen_G00, new PointF(Convert.ToSingle(2 * obj.Paths[i - 1].Start.X + 0.5 - ((obj.Paths[i - 1].Start.X == 0)? 0 : 1 )),
                                                                         Convert.ToSingle(2 * obj.Paths[i - 1].Start.Y - 0.5 - ((obj.Paths[i - 1].Start.Y == 0) ? 0 : 1))),
                                                               new PointF(Convert.ToSingle(2 * obj.Paths[i].Start.X + 0.5 - ((obj.Paths[i].Start.X == 0) ? 0 : 1)),
                                                                         Convert.ToSingle(2 * obj.Paths[i].Start.Y - 0.5 - ((obj.Paths[i].Start.Y == 0) ? 0 : 1))));
                                Report_contour_n_lines_off++;
                                Report_contour_distance_mm_tool_off += Convert.ToDecimal(Math.Sqrt(Math.Pow((obj.Paths[i].Start.X - obj.Paths[i - 1].End.X) * pixel_size, 2) + Math.Pow((obj.Paths[i].Start.Y - obj.Paths[i - 1].End.Y) * pixel_size, 2)));
                            }
                        }

                        
                                                
                        x = path.End.X * pixel_size;// + pixel_size / 2;
                        y = path.End.Y * pixel_size;// + pixel_size / 2;

                        switch (path.PathType)
                        {
                            case SVG_Path.SVG_Path_Type.Line:
                                
                                
                                if (cnc._ToolType == CNC_Properties.ToolType.Laser)
                                {
                                    Gcode_ToolPath.Append(GCODE.Linear_Movement + " X" + x.ToString("###########0.######", nfi)
                                                                                    + " Y" + y.ToString("###########0.######", nfi)
                                                                                    + " F" + cnc.ContourFeedrate.ToString("###########0.######", nfi));
                                    Gcode_Routine_EngageTool(Gcode_ToolPath, cnc,false);
                                }
                                else
                                {
                                    Gcode_Routine_EngageTool(Gcode_ToolPath, cnc, false);
                                    Gcode_ToolPath.AppendLine(GCODE.Linear_Movement + " X" + x.ToString("###########0.######", nfi)
                                                                                    + " Y" + y.ToString("###########0.######", nfi)
                                                                                    + " F" + cnc.ContourFeedrate.ToString("###########0.######", nfi));
                                }

                                img_graphics.DrawLine(pen_G01, new PointF(Convert.ToSingle(2 * path.Start.X + 0.5 - ((path.Start.X == 0) ? 0 : 1)),
                                                                         Convert.ToSingle(2 * path.Start.Y - 0.5 - ((path.Start.Y == 0) ? 0 : 1))),
                                                               new PointF(Convert.ToSingle(2 * path.End.X + 0.5 - ((path.End.X == 0) ? 0 : 1)),
                                                                         Convert.ToSingle(2 * path.End.Y - 0.5 - ((path.End.Y == 0) ? 0 : 1))));

                                img_graphics_ToolOnly.DrawLine(pen_G01, new PointF(Convert.ToSingle(2 * path.Start.X + 0.5 - ((path.Start.X == 0) ? 0 : 1)),
                                                                         Convert.ToSingle(2 * path.Start.Y - 0.5 - ((path.Start.Y == 0) ? 0 : 1))),
                                                               new PointF(Convert.ToSingle(2 * path.End.X + 0.5 - ((path.End.X == 0) ? 0 : 1)),
                                                                         Convert.ToSingle(2 * path.End.Y - 0.5 - ((path.End.Y == 0) ? 0 : 1))));

                                Report_contour_n_lines_on++;
                                Report_contour_line_distance_mm_tool_on += Convert.ToDecimal(Math.Sqrt(Math.Pow((path.Start.X - path.End.X) * pixel_size, 2) + Math.Pow((path.Start.Y - path.End.Y) * pixel_size, 2)));

                                break;
                            case SVG_Path.SVG_Path_Type.Curve_Center:

                                double start_x = path.Start.X * pixel_size;// + pixel_size / 2;
                                double start_y = path.Start.Y * pixel_size;// + pixel_size / 2;

                                if (cnc._ToolType == CNC_Properties.ToolType.Laser)
                                {
                                    Gcode_ToolPath.Append((path.Clockwise ? GCODE.Arc_CW_Movement : GCODE.Arc_CCW_Movement)
                                                                                    + " X" + x.ToString("###########0.######", nfi)
                                                                                    + " Y" + y.ToString("###########0.######", nfi)
                                                                                    + " I" + (path.Center.X * pixel_size - start_x).ToString("###########0.######", nfi)
                                                                                    + " J" + (path.Center.Y * pixel_size - start_y).ToString("###########0.######", nfi)
                                                                                    + " F" + cnc.ContourFeedrate.ToString("###########0.######", nfi));
                                    Gcode_Routine_EngageTool(Gcode_ToolPath, cnc, false);
                                }
                                else
                                {
                                    Gcode_Routine_EngageTool(Gcode_ToolPath, cnc, false);
                                    Gcode_ToolPath.AppendLine((path.Clockwise ? GCODE.Arc_CW_Movement : GCODE.Arc_CCW_Movement)
                                                                                    + " X" + x.ToString("###########0.######", nfi)
                                                                                    + " Y" + y.ToString("###########0.######", nfi)
                                                                                    + " I" + (path.Center.X * pixel_size - start_x).ToString("###########0.######", nfi)
                                                                                    + " J" + (path.Center.Y * pixel_size - start_y).ToString("###########0.######", nfi)
                                                                                    + " F" + cnc.ContourFeedrate.ToString("###########0.######", nfi));

                                }

                                // draw arc
                                Graphic_Utilities.DrawArc(img_graphics, pen_G01, Convert.ToSingle(2 * path.Start.X - 1), Convert.ToSingle(2 * path.Start.Y - 1),
                                                                                 Convert.ToSingle(2 * path.End.X - 1), Convert.ToSingle(2 * path.End.Y - 1),
                                                                                 Convert.ToSingle(2 * path.Center.X - 1), Convert.ToSingle(2 * path.Center.Y - 1),
                                                                                 path.Clockwise);

                                Graphic_Utilities.DrawArc(img_graphics_ToolOnly, pen_G01, Convert.ToSingle(2 * path.Start.X - 1), Convert.ToSingle(2 * path.Start.Y - 1),
                                                                                 Convert.ToSingle(2 * path.End.X - 1), Convert.ToSingle(2 * path.End.Y - 1),
                                                                                 Convert.ToSingle(2 * path.Center.X - 1), Convert.ToSingle(2 * path.Center.Y - 1),
                                                                                 path.Clockwise);

                                Report_contour_n_arc_on++;
                                Report_contour_arc_distance_mm_tool_on += 0; // !!
                                break;

                            case SVG_Path.SVG_Path_Type.Curve_Radius:

                                // draw arc
                                Graphic_Utilities.DrawArc(img_graphics, pen_G01, Convert.ToSingle(2 * path.Start.X - 1), Convert.ToSingle(2 * path.Start.Y - 1),
                                                                                 Convert.ToSingle(2 * path.End.X - 1), Convert.ToSingle(2 * path.End.Y - 1),
                                                                                 Convert.ToSingle(2 * path.Radius - 1),
                                                                                 path.SmallArc, path.Clockwise);

                                // if line was draw is because the arc give an overflow in the Center calculations. Aproximation to line needs to be used on gcode to prevent errors in the CNC aswell
                                if(!Graphic_Utilities.DrawArc(img_graphics_ToolOnly, pen_G01, Convert.ToSingle(2 * path.Start.X - 1), Convert.ToSingle(2 * path.Start.Y - 1),
                                                                                 Convert.ToSingle(2 * path.End.X - 1), Convert.ToSingle(2 * path.End.Y - 1),
                                                                                 Convert.ToSingle(2 * path.Radius - 1),
                                                                                 path.SmallArc, path.Clockwise))
                                {
                                    if (cnc._ToolType == CNC_Properties.ToolType.Laser)
                                    {
                                        Gcode_ToolPath.Append(GCODE.Linear_Movement
                                                                                        + " X" + x.ToString("###########0.######", nfi)
                                                                                        + " Y" + y.ToString("###########0.######", nfi)
                                                                                        + " F" + cnc.ContourFeedrate.ToString("###########0.######", nfi));
                                        Gcode_Routine_EngageTool(Gcode_ToolPath, cnc, false);
                                    }
                                    else
                                    {
                                        Gcode_Routine_EngageTool(Gcode_ToolPath, cnc, false);
                                        Gcode_ToolPath.AppendLine(GCODE.Linear_Movement
                                                                                        + " X" + x.ToString("###########0.######", nfi)
                                                                                        + " Y" + y.ToString("###########0.######", nfi)
                                                                                        + " F" + cnc.ContourFeedrate.ToString("###########0.######", nfi));

                                    }
                                }
                                else
                                {
                                    if (cnc._ToolType == CNC_Properties.ToolType.Laser)
                                    {
                                        Gcode_ToolPath.Append((path.Clockwise ? GCODE.Arc_CW_Movement : GCODE.Arc_CCW_Movement)
                                                                                        + " X" + x.ToString("###########0.######", nfi)
                                                                                        + " Y" + y.ToString("###########0.######", nfi)
                                                                                        + " R" + (path.Radius * (path.SmallArc ? 1 : -1)).ToString("###########0.######", nfi)
                                                                                        + " F" + cnc.ContourFeedrate.ToString("###########0.######", nfi));
                                        Gcode_Routine_EngageTool(Gcode_ToolPath, cnc, false);
                                    }
                                    else
                                    {
                                        Gcode_Routine_EngageTool(Gcode_ToolPath, cnc, false);
                                        Gcode_ToolPath.AppendLine((path.Clockwise ? GCODE.Arc_CW_Movement : GCODE.Arc_CCW_Movement)
                                                                                        + " X" + x.ToString("###########0.######", nfi)
                                                                                        + " Y" + y.ToString("###########0.######", nfi)
                                                                                        + " R" + (path.Radius * (path.SmallArc ? 1 : -1)).ToString("###########0.######", nfi)
                                                                                        + " F" + cnc.ContourFeedrate.ToString("###########0.######", nfi));

                                    }
                                }

                                Report_contour_n_arc_on++;
                                Report_contour_arc_distance_mm_tool_on += 0; // !!
                                break;
                        }

                        
                    }

                    img_x_prev = Convert.ToInt32(obj.Paths[obj.Paths.Count - 1].End.X);
                    img_y_prev = Convert.ToInt32(obj.Paths[obj.Paths.Count - 1].End.Y);
                }
                
            }

            Report_GCode_Contour_Gen_sec.Stop();

            Gcode_ToolPath.AppendLine(GCODE.Tool_Off);
            Gcode_ToolPath.AppendLine(GCODE.Program_End);

            Pixel_ToolPath.Clear();
            img_graphics.Dispose();
            img_graphics_ToolOnly.Dispose();
            pen_G00.Dispose();
            pen_G01.Dispose();

            gcode_img.RotateFlip(RotateFlipType.RotateNoneFlipY);
            gcode_img_ToolOnly.RotateFlip(RotateFlipType.RotateNoneFlipY);
            gcode_img = IMG_Filters.ConvertTo24bpp(gcode_img);
            gcode_img_ToolOnly = IMG_Filters.ConvertTo24bpp(gcode_img_ToolOnly);


            var result = Tuple.Create(gcode_img, gcode_img_ToolOnly);

            e.Result = result;

            //
            // Save File
            //

            // check cancelation
            if (bw.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            progress_stage = "Saving file...";
            bw.ReportProgress(100, progress_stage);

            if (File.Exists(save_file_path))
            {
                File.Delete(save_file_path);
                Application.DoEvents();
            }

            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(save_file_path))
            {
                sw.WriteLine(Gcode_ToolPath.ToString());
            }


            Report_TotalTP_Gen_sec.Stop();
            
            if (Generate_Report)
            {
                string[] file_path_split = save_file_path.Split('\\');
                string filename = file_path_split[file_path_split.Length - 1];

                string dir_path;
                StringBuilder path_only_Builder = new StringBuilder();
                for(int i = 0; i < file_path_split.Length - 1; i++)
                {
                    path_only_Builder.Append(file_path_split[i] + '\\');
                }
                dir_path = path_only_Builder.ToString() + "TP_Report" + '\\';

                try
                {
                    if (Directory.Exists(dir_path))
                    {
                        Directory.Delete(dir_path, true);
                        Application.DoEvents();
                    }

                    Directory.CreateDirectory(dir_path);

                    gcode_img.Save(dir_path + filename + ".bmp");
                    gcode_img_ToolOnly.Save(dir_path + filename + "_ToolOnly" + ".bmp");

                    // Report file


                    


                    using (StreamWriter sw = File.CreateText(dir_path + "Report.txt"))
                    {
                        sw.WriteLine("+--------------------+");
                        sw.WriteLine("| Toolpathing Report |");
                        sw.WriteLine("+--------------------+");
                        sw.WriteLine(" ");
                        sw.WriteLine("----------------- File Properties -----------------");
                        sw.WriteLine(" ");
                        sw.WriteLine("File generated: " + filename);
                        sw.WriteLine("Fill path: " + (EdgesOnly? "No" : (PathType == ToolPathingType.ZigZag ? "Zig-Zag" : "Spiral")));
                        sw.WriteLine("Contour path: " + (usingSVG ? "Yes" : "No"));
                        sw.WriteLine(" ");
                        sw.WriteLine("-------------- BitArray2D Properties --------------");
                        sw.WriteLine(" ");
                        sw.WriteLine("Original Image:");
                        sw.WriteLine("  - Width: " + Report_img_original_width);
                        sw.WriteLine("  - Height: " + Report_img_original_height);
                        sw.WriteLine("Post Resample:");
                        sw.WriteLine("  - Width: " + Report_img_new_width);
                        sw.WriteLine("  - Height: " + Report_img_new_height);

                        sw.WriteLine(" ");

                        double Report_AreaToRemove = 100.0 * ((double)Report_Px_Remove /((double)Report_Px_Remove + (double)Report_Px_Keep));
                        double Report_AreaToKeep = 100.0 * ((double)Report_Px_Keep / ((double)Report_Px_Remove + (double)Report_Px_Keep));

                        sw.WriteLine(" % of Px To Remove: " + Report_AreaToRemove + " %");
                        sw.WriteLine(" % of Px To Keep: " + Report_AreaToKeep + " %");

                        
                        sw.WriteLine(" ");
                        sw.WriteLine("-------------- File Generation Time ---------------");
                        sw.WriteLine(" ");
                        if (!EdgesOnly) // no fill
                        {
                            sw.WriteLine("Fill processing times: ");
                            sw.WriteLine(" - Image segmentation: " + Report_ImgSegment_sec.Elapsed);
                            sw.WriteLine(" - Segment pathing: " + Report_SegmentPath_sec.Elapsed);
                            sw.WriteLine(" - Segment connection:" + Report_SegmentConnection_sec.Elapsed);
                            sw.WriteLine(" - G-Code commands generation: " + Report_GCode_Fill_Gen_sec.Elapsed);
                        }
                        if (usingSVG)
                        {
                            sw.WriteLine("Contour path G-Code commands generation: " + Report_GCode_Contour_Gen_sec.Elapsed);
                        }

                        sw.WriteLine("Total path G-Code generation time: " + Report_TotalTP_Gen_sec.Elapsed);
                        sw.WriteLine(" ");
                        sw.WriteLine("---------- Number of Lines and Segments -----------");
                        sw.WriteLine(" ");
                        sw.WriteLine("Fill number of lines with tool on: " + Report_fill_n_lines_on);
                        sw.WriteLine("Fill number of lines with tool off: " + Report_fill_n_lines_off);
                        
                        sw.WriteLine("Contour number of lines with tool on: " + Report_contour_n_lines_on);
                        sw.WriteLine("Contour number of arcs with tool on: " + Report_contour_n_arc_on);
                        sw.WriteLine("Contour number of lines with tool off: " + Report_contour_n_lines_off);
                                                
                        sw.WriteLine(" ");
                        sw.WriteLine("Fill number of segments (pre pathing): " + Report_fill_pre_Segments);
                        sw.WriteLine("Fill number of segments (post pathing): " + Report_fill_post_Segments);
                        sw.WriteLine(" ");
                        sw.WriteLine("----------------- Distances [mm] ------------------");
                        sw.WriteLine(" ");
                        sw.WriteLine("Fill Distance tool on: " + Report_fill_distance_mm_tool_on);
                        sw.WriteLine("Fill Distance tool off: " + Report_fill_distance_mm_tool_off);
                        sw.WriteLine("Contour Line Distance tool on: " + Report_contour_line_distance_mm_tool_on);
                        sw.WriteLine("Contour Arc Distance tool on: " + Report_contour_arc_distance_mm_tool_on);
                        sw.WriteLine("Contour Distance tool off: " + Report_contour_distance_mm_tool_off);
                        sw.WriteLine(" ");
                        sw.WriteLine("----------------------- ETA -----------------------");
                        sw.WriteLine(" ");

                        Report_fill_ETA_sec_tool_on = Report_fill_distance_mm_tool_on / Convert.ToDecimal(cnc.LinearFeedrate);
                        Report_fill_ETA_sec_tool_off = Report_fill_distance_mm_tool_off / Convert.ToDecimal(cnc.RapidFeedrate);
                        Report_contour_ETA_sec_tool_on = (Report_contour_line_distance_mm_tool_on + Report_contour_arc_distance_mm_tool_on) / Convert.ToDecimal(cnc.ContourFeedrate);
                        Report_contour_ETA_sec_tool_off = Report_contour_distance_mm_tool_off / Convert.ToDecimal(cnc.RapidFeedrate);

                        
                        sw.WriteLine("Feedrate Fill tool on: " + cnc.LinearFeedrate.ToString());
                        sw.WriteLine("Feedrate Contour tool on: " + cnc.ContourFeedrate.ToString());
                        sw.WriteLine("Feedrate tool off: " + cnc.RapidFeedrate.ToString());
                        sw.WriteLine(" ");
                        sw.WriteLine("Fill ETA tool on [min]: " + Report_fill_ETA_sec_tool_on);
                        sw.WriteLine("Fill ETA tool off [min]: " + Report_fill_ETA_sec_tool_off);
                        sw.WriteLine("Contour ETA tool on [min]: " + Report_contour_ETA_sec_tool_on);
                        sw.WriteLine("contour ETA tool off [min]: " + Report_contour_ETA_sec_tool_off);
                        sw.WriteLine("ETA [min]: " + (Report_fill_ETA_sec_tool_on + Report_fill_ETA_sec_tool_off + Report_contour_ETA_sec_tool_on + Report_contour_ETA_sec_tool_off));
                        sw.WriteLine(" ");



                    }

                    


                }
                catch(Exception err)
                {

                }

                

                /*

                if (File.Exists(save_file_path + ".bmp"))
                {
                    File.Delete(save_file_path + ".bmp");
                    Application.DoEvents();
                }
                if (File.Exists(save_file_path + "_ToolOnly" + ".bmp"))
                {
                    File.Delete(save_file_path + "_ToolOnly" + ".bmp");
                    Application.DoEvents();
                }
                gcode_img.Save(save_file_path + ".bmp");
                gcode_img_ToolOnly.Save(save_file_path + "_ToolOnly" + ".bmp");
                */
            }
                        
            Gcode_ToolPath.Clear();

            progress_stage = "Done!";
            bw.ReportProgress(100, progress_stage);

        }

        private void toolpath_backgroundwroker_RunWrokerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //BackgroundWorker bw = sender as BackgroundWorker;
            //bw.DoWork -= new DoWorkEventHandler(toolpath_backgroundwroker_DoWork);
            //bw.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(toolpath_backgroundwroker_RunWrokerCompleted);
            //bw.ProgressChanged -= new ProgressChangedEventHandler(toolpath_backgroundwroker_ProgressChanged);
            //bw.Dispose();
            if (e.Error != null) // error has occured
            {
                Memory_Utilities.FreeMemory();
                return;
            }
            if (e.Cancelled) // cancelled operation
            {
                Memory_Utilities.FreeMemory();
                return;
            }

            var result = e.Result as Tuple<Bitmap, Bitmap>;

            Bitmap gcode_img = result.Item1;
            Bitmap gcode_img_ToolOnly = result.Item2;
            
            // display image representing the tool path (e.Result => img)
            DisplayImage?.Invoke((gcode_img as Image),(gcode_img_ToolOnly as Image));
            Memory_Utilities.FreeMemory();

        }

        private void toolpath_backgroundwroker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DisplayProgress?.Invoke(e.ProgressPercentage, e.UserState as string);
        }

        #region backgroundworker delegate functions

        public delegate void TP_ImageDisplayer(Image gcode_img, Image gcode_img_ToolOnly);

        public TP_ImageDisplayer DisplayImage;

        public delegate void TP_ProgressDisplayer(int progress, string status);

        public TP_ProgressDisplayer DisplayProgress;

        #endregion

        #endregion

    }
}
