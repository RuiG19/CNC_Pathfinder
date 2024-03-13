using CNC_Pathfinder.src.CNC;
using CNC_Pathfinder.src.Utilities;
using CNC_Pathfinder.src.SVG;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CNC_Pathfinder.src.Toolpathing
{
    class IMG_Manager
    {
        private const int IMAGE_MAX_SIZE = 10*1920*1080;

        public bool isBusy { get; private set; }

        public bool ImageAvailable { get; private set; }

        public bool EdgeAvailable { get; private set; }

        public bool ToolPathAvailable { get; private set; }

        private OpenFileDialog FileBrowser;

        public string filename { get; private set; }

        private SaveFileDialog TPFileBrowser;

        // Manager Control

        public enum ImageDisplay
        {
            NoImage = 0,
            Color = 1,
            BW = 2,
            ED = 3,
            Processing = 4, // for toolpathing 
            Toolpath = 5 // Display TP result
        }

        // Image Pool

        public enum EdgeDetectionType
        {
            SXOR = 0,
            RobertsCross = 1,
            Perwitt = 2,
            Sobel = 3
        }

        private enum BitmapType
        {
            bw_10 = 0,
            bw_20 = 1,
            bw_30 = 2,
            bw_40 = 3,
            bw_50 = 4,
            bw_60 = 5,
            bw_70 = 6,
            bw_80 = 7,
            bw_90 = 8,
            color = 9,
            ed_SXOR = 10,
            ed_RC1 = 11,
            ed_RC2 = 12,
            ed_P1 = 13,
            ed_P2 = 14,
            ed_P3 = 15,
            ed_P4 = 16,
            ed_S1 = 17,
            ed_S2 = 18,
            ed_S3 = 19,
        }

        private BitmapType currentBMP_displayed = BitmapType.color;

        private BitmapType TP_currentBMP = BitmapType.color;

        public bool svg_image { get; private set; }


        private SvgDocument original_svg;
        //private SVG_Image simple_svg; // SvgDocument converted to only lines and simple arcs (no ellipse nor bezier curves)

        private Bitmap original_bmp;
        private Bitmap color_bmp; // for sharp/blur
        private Bitmap[] blackwhite_bmp = new Bitmap[9];
        private Bitmap toolpath_bmp;
        private Bitmap toolpath_bmp_toolOnly;
                
        private Bitmap edge_SXOR_bmp;
        private Bitmap[] edge_RC_bmp = new Bitmap[2];
        private Bitmap[] edge_Perwitt_bmp = new Bitmap[4];
        private Bitmap[] edge_Sobel_bmp = new Bitmap[3];

        public double IMG_Width { get; set; } // in mm
        public double IMG_Height { get; set; } // in mm

        public Size Img_Size { get; private set; }

        private const double inch_to_mm = 25.4;

        // Toolpathing

        public bool Generate_Report { get; set; }

        private CNC_Properties Machine_Properties;

        private IMG_ToolPathing toolpather;

        private ImageDisplay prevDisplayMode;
        //

        public IMG_Manager(CNC_Properties Machine_Properties)
        {
            FileBrowser = new OpenFileDialog();
            FileBrowser.Filter = "Image files|*.bmp;*.BMP;*.png;*.PNG;*.svg;*.SVG|All files (*.*)|*.*";
            FileBrowser.FilterIndex = 2;
            FileBrowser.Title = "Open an Image File";

            TPFileBrowser = new SaveFileDialog();
            TPFileBrowser.Filter = "gcode files|*.gcode;*.GCODE;*.tap;*.TAP;*.nc;*.NC|All files (*.*)|*.*";
            TPFileBrowser.FilterIndex = 2;
            TPFileBrowser.Title = "Save Toolpathing File";

            svg_image = false;
            isBusy = false;
            EdgeAvailable = false;
            ToolPathAvailable = false;

            SVG_Parser.MaximumSize = new Size(1000,1000);

            toolpather = new IMG_ToolPathing();
            toolpather.DisplayImage += new IMG_ToolPathing.TP_ImageDisplayer(getToolpathIMGResult);
            toolpather.DisplayProgress += new IMG_ToolPathing.TP_ProgressDisplayer(getToolpathProgress);

            this.Machine_Properties = Machine_Properties;

            Generate_Report = true;
        }

        public delegate void setDisplayMode(ImageDisplay mode);
        public setDisplayMode DisplayMode;

        public bool LoadImage() // load image with btn press
        {
            if(FileBrowser.ShowDialog() == DialogResult.OK)
            {
                return LoadImage(FileBrowser.FileName);
            }
            return false;
        }

        public bool LoadImage(string filepath) // load image 
        {
            string[] fileExtension = filepath.Split('.');
            string filetype = fileExtension[fileExtension.Length - 1];

            if (filetype == "svg" || filetype == "SVG")
            {
                try
                {
                    original_svg = SVG_Parser.GetSvgDocument(filepath);
                }
                catch(Exception e)
                {
                    CloseImage();
                    return false;
                }
                
                //simple_svg = new SVG_Image(original_svg);
                
                using (Image img = original_svg.Draw())
                {
                    try
                    {
                        original_bmp = IMG_Filters.ConvertTo24bpp(img);
                    }
                    catch (Exception e)
                    {
                        CloseImage();
                        return false;
                    }
                }
                    
                ImageAvailable = true;
                svg_image = true;
                EdgeAvailable = false;
                ToolPathAvailable = false;
                fileExtension = filepath.Split('\\');
                filename = fileExtension[fileExtension.Length - 1]; ;
            }
            else if (filetype == "bmp" || filetype == "BMP" || filetype == "png" || filetype == "PNG")
            {
                using (Image img = Image.FromFile(filepath, true))
                {
                    try
                    {
                        
                        if (img.Width * img.Height > IMAGE_MAX_SIZE)
                        {
                            original_bmp = IMG_Filters.ImageResize(IMG_Filters.ConvertTo24bpp(img), Convert.ToDouble(Math.Sqrt((double)IMAGE_MAX_SIZE /((double)img.Width / (double)img.Height)) / (double)img.Height));//( 2 * (double)IMAGE_MAX_SIZE / (img.Width * img.Height)));
                        }
                        else
                        {
                            original_bmp = IMG_Filters.ConvertTo24bpp(img);
                        }
                        

                        //original_bmp = IMG_Filters.BlackWhite(IMG_Filters.ImageResize(IMG_Filters.ConvertTo24bpp(img), 0.25),90);
                        //original_bmp = IMG_Filters.ImageResize(IMG_Filters.ConvertTo24bpp(img), 0.25);
                    }
                    catch (Exception e)
                    {
                        CloseImage();
                        return false;
                    }
                }
                
                
                ImageAvailable = true;
                svg_image = false;
                EdgeAvailable = false;
                ToolPathAvailable = false;
                fileExtension = filepath.Split('\\');
                filename = fileExtension[fileExtension.Length - 1];
            }

            
            if (ImageAvailable)
            {
                isBusy = false;
                Img_Size = new Size(original_bmp.Width, original_bmp.Height);

                resetToOriginalImage();
                               
                return true;
            }

            return false;
        }

        public bool CloseImage()
        {
            if (isBusy)
            {
                return false;
            }

            ImageAvailable = false;

            try
            {
                original_bmp.Dispose();
            }catch(Exception e)
            {

            }
            try
            {
                color_bmp.Dispose();
            }
            catch(Exception e)
            {

            }
            foreach(Bitmap bmp in blackwhite_bmp)
            {
                try
                {
                    bmp.Dispose();
                }
                catch(Exception e)
                {

                }
            }

            deleteEdgeImages();

            try
            {
                toolpath_bmp.Dispose();
                toolpath_bmp_toolOnly.Dispose();
            }
            catch(Exception e)
            {

            }


            original_svg = null;

            DisplayImage?.Invoke(null);
            DisplayMode?.Invoke(ImageDisplay.NoImage);

            Memory_Utilities.FreeMemory();

            return true;
        }

        public delegate void ImageDisplayer(Bitmap img);
        public ImageDisplayer DisplayImage;

        public void ShowColorImage()
        {
            if (ImageAvailable && DisplayImage != null)
            {
                currentBMP_displayed = BitmapType.color;
                DisplayImage(color_bmp);
                DisplayMode?.Invoke(ImageDisplay.Color);
                prevDisplayMode = ImageDisplay.Color;

                TP_currentBMP = BitmapType.color;
            }

            if (EdgeAvailable)
            {
                deleteEdgeImages();
            }

        }

        public void ShowBlackwhite(int index)
        {
            if (index < 0 || index >= blackwhite_bmp.Length)
            {
                return;
            }

            if (EdgeAvailable && index != (int)currentBMP_displayed)
            {
                deleteEdgeImages();
            }

            if (ImageAvailable && DisplayImage != null)
            {
                DisplayImage(blackwhite_bmp[index]);
                DisplayMode?.Invoke(ImageDisplay.BW);
                prevDisplayMode = ImageDisplay.BW;
                switch (index)
                {
                    case 0:
                        currentBMP_displayed = BitmapType.bw_10;
                        TP_currentBMP = BitmapType.bw_10;
                        break;
                    case 1:
                        currentBMP_displayed = BitmapType.bw_20;
                        TP_currentBMP = BitmapType.bw_20;
                        break;
                    case 2:
                        currentBMP_displayed = BitmapType.bw_30;
                        TP_currentBMP = BitmapType.bw_30;
                        break;
                    case 3:
                        currentBMP_displayed = BitmapType.bw_40;
                        TP_currentBMP = BitmapType.bw_40;
                        break;
                    case 4:
                        currentBMP_displayed = BitmapType.bw_50;
                        TP_currentBMP = BitmapType.bw_50;
                        break;
                    case 5:
                        currentBMP_displayed = BitmapType.bw_60;
                        TP_currentBMP = BitmapType.bw_60;
                        break;
                    case 6:
                        currentBMP_displayed = BitmapType.bw_70;
                        TP_currentBMP = BitmapType.bw_70;
                        break;
                    case 7:
                        currentBMP_displayed = BitmapType.bw_90;
                        TP_currentBMP = BitmapType.bw_90;
                        break;
                    case 8:
                        currentBMP_displayed = BitmapType.bw_90;
                        TP_currentBMP = BitmapType.bw_90;
                        break;
                }
            }
                                 
                    
        }

        private void generateBlackWhite()
        {
           
            using (Semaphore original_bmp_sem = new Semaphore(1, 1))
            {
                Parallel.For(0, blackwhite_bmp.Length,
                    index =>
                    {
                        try
                        {
                            blackwhite_bmp[index].Dispose();
                        }
                        catch (Exception e)
                        {

                        }
                                              

                        // copy color image for a temp bitmap  with semaphores
                        original_bmp_sem.WaitOne();
                        Bitmap tmp_img = (Bitmap)color_bmp.Clone();
                        original_bmp_sem.Release();

                        blackwhite_bmp[index] = IMG_Filters.BlackWhite(tmp_img, (index + 1) * 10);

                        // dispose tmp image
                        tmp_img.Dispose();

                    });
            }
        }

        private void deleteEdgeImages()
        {
            try
            {
                edge_SXOR_bmp.Dispose();
            }
            catch (Exception e)
            {

            }

            foreach (Bitmap bmp in edge_RC_bmp)
            {
                try
                {
                    bmp.Dispose();
                }
                catch (Exception e)
                {

                }
            }
            foreach (Bitmap bmp in edge_Perwitt_bmp)
            {
                try
                {
                    bmp.Dispose();
                }
                catch (Exception e)
                {

                }
            }
            foreach (Bitmap bmp in edge_Sobel_bmp)
            {
                try
                {
                    bmp.Dispose();
                }
                catch (Exception e)
                {

                }
            }
            EdgeAvailable = false;


            Memory_Utilities.FreeMemory();
        }
        
        public void ShowEdgeImage(EdgeDetectionType edge_type, int index)
        {
            if (EdgeAvailable)
            {

                if (ImageAvailable && DisplayImage != null)
                {
                    if (index < 0)
                    {
                        index = 0;
                    }
                    switch (edge_type)
                    {
                        case EdgeDetectionType.SXOR:
                            DisplayImage(edge_SXOR_bmp);
                            TP_currentBMP = BitmapType.ed_SXOR;
                            break;
                        case EdgeDetectionType.RobertsCross:
                            if(index > 1)
                            {
                                index = 1;
                            }
                            DisplayImage(edge_RC_bmp[index]);
                            TP_currentBMP = index == 0? BitmapType.ed_RC1 : BitmapType.ed_RC2;
                            break;
                        case EdgeDetectionType.Perwitt:
                            if (index > 3)
                            {
                                index = 3;
                            }
                            DisplayImage(edge_Perwitt_bmp[index]);
                            TP_currentBMP = index == 0 ? BitmapType.ed_P1 : TP_currentBMP = index == 1? BitmapType.ed_P2 : TP_currentBMP = index == 2 ? BitmapType.ed_P3 : BitmapType.ed_P4;
                            break;
                        case EdgeDetectionType.Sobel:
                            if (index > 2)
                            {
                                index = 2;
                            }
                            DisplayImage(edge_Sobel_bmp[index]);
                            TP_currentBMP = index == 0 ? BitmapType.ed_S1 : TP_currentBMP = index == 1 ? BitmapType.ed_S2 : BitmapType.ed_S3;
                            break;
                    }
                    DisplayMode?.Invoke(ImageDisplay.ED);
                    prevDisplayMode = ImageDisplay.ED;
                }
            }
        }

        public void ShowToolPathImage(bool ShowAllPaths)
        {
            if (ToolPathAvailable)
            {
                if (ImageAvailable && DisplayImage != null)
                {
                    currentBMP_displayed = BitmapType.color;
                    if (ShowAllPaths)
                    {
                        DisplayImage(toolpath_bmp);
                    }
                    else
                    {
                        DisplayImage(toolpath_bmp_toolOnly);
                    }                    
                    DisplayMode?.Invoke(ImageDisplay.Toolpath);
                }

                if (EdgeAvailable)
                {
                    deleteEdgeImages();
                }
            }
        }

        // Apply filters

        public void resetToOriginalImage()
        {
            if (isBusy)
            {
                return;
            }
            isBusy = true;

            if (ImageAvailable)
            {
                try
                {
                    color_bmp.Dispose();
                }
                catch(Exception e)
                {

                }

                color_bmp = (Bitmap)original_bmp.Clone();

                generateBlackWhite();

                deleteEdgeImages();

                if (currentBMP_displayed == BitmapType.color)
                {
                    ShowColorImage();
                }
                else
                {
                    ShowBlackwhite((int)currentBMP_displayed);
                }
                
            }

            IMG_Width = (original_bmp.Width / original_bmp.HorizontalResolution) * inch_to_mm;
            IMG_Height = (original_bmp.Height / original_bmp.VerticalResolution) * inch_to_mm;

            isBusy = false;

        }

        public void ApplySharp()
        {
            if (isBusy)
            {
                return;
            }
            isBusy = true;

            if (ImageAvailable)
            {
                Bitmap sharp_img = IMG_Filters.Sharp(color_bmp);

                try
                {
                    color_bmp.Dispose();
                }
                catch (Exception e) { }

                color_bmp = (Bitmap)sharp_img.Clone(); //new Bitmap(sharp_img);

                sharp_img.Dispose();

                generateBlackWhite();

                deleteEdgeImages();

                if (currentBMP_displayed == BitmapType.color)
                {
                    ShowColorImage();
                }
                else
                {
                    ShowBlackwhite((int)currentBMP_displayed);
                }
            }

            isBusy = false;
        }

        public void ApplyBlur()
        {
            if (isBusy)
            {
                return;
            }
            isBusy = true;

            if (ImageAvailable)
            {
                Bitmap blur_img = IMG_Filters.Blur(color_bmp);

                try
                {
                    color_bmp.Dispose();
                }
                catch (Exception e) { }

                color_bmp = (Bitmap)blur_img.Clone(); // new Bitmap(sharp_img);

                blur_img.Dispose();

                generateBlackWhite();

                deleteEdgeImages();

                if (currentBMP_displayed == BitmapType.color)
                {
                    ShowColorImage();
                }
                else
                {
                    ShowBlackwhite((int)currentBMP_displayed);
                }
            }

            isBusy = false;
        }

        public void ApplyNegative()
        {
            if (isBusy)
            {
                return;
            }
            isBusy = true;

            if (ImageAvailable)
            {
                Bitmap negative_img = IMG_Filters.Invert(color_bmp);

                try
                {
                    color_bmp.Dispose();
                }
                catch (Exception e) { }

                color_bmp = (Bitmap)negative_img.Clone(); // new Bitmap(sharp_img);

                negative_img.Dispose();

                generateBlackWhite();

                deleteEdgeImages();

                if (currentBMP_displayed == BitmapType.color)
                {
                    ShowColorImage();
                }
                else
                {
                    ShowBlackwhite((int)currentBMP_displayed);
                }
            }

            isBusy = false;
        }

        public void ApplyEdge(EdgeDetectionType edge_type, int ED_index, int BW_index)
        {
            if (!ImageAvailable || isBusy) 
            {
                return;
            }
            isBusy = true;

            deleteEdgeImages();
            
            using (Semaphore blackwhite_bmp_sem = new Semaphore(1, 1))
            {
                Parallel.For(0, 10,
                            _index =>
                            {
                                blackwhite_bmp_sem.WaitOne();
                                Bitmap tmp_img = (Bitmap)blackwhite_bmp[BW_index].Clone();
                                blackwhite_bmp_sem.Release();

                                switch (_index)
                                {
                                    case 0: // SXOR
                                        edge_SXOR_bmp = IMG_Filters.EdgeDetection_SXOR(tmp_img);
                                        break;
                                    case 1: // RC 0
                                        edge_RC_bmp[0] = IMG_Filters.EdgeDetection_RC(tmp_img, 10); // [0 - 50]
                                        break;
                                    case 2: // RC 0
                                        edge_RC_bmp[1] = IMG_Filters.EdgeDetection_RC(tmp_img, 70); // [60 - 100]
                                        break;
                                    case 3: // Perwitt 0
                                        edge_Perwitt_bmp[0] = IMG_Filters.EdgeDetection(tmp_img, IMG_Filters.EdgeDetection_Operators.Prewitt, 10);  // [0 - 20]
                                        break;
                                    case 4: // Perwitt 1
                                        edge_Perwitt_bmp[1] = IMG_Filters.EdgeDetection(tmp_img, IMG_Filters.EdgeDetection_Operators.Prewitt, 30);  // [30 - 50]
                                        break;
                                    case 5: // Perwitt 2
                                        edge_Perwitt_bmp[2] = IMG_Filters.EdgeDetection(tmp_img, IMG_Filters.EdgeDetection_Operators.Prewitt, 60);  // [60 - 70]
                                        break;
                                    case 6: // Perwitt 3
                                        edge_Perwitt_bmp[3] = IMG_Filters.EdgeDetection(tmp_img, IMG_Filters.EdgeDetection_Operators.Prewitt, 90); // [80 - 100]
                                        break;
                                    case 7: // Sobel 0
                                        edge_Sobel_bmp[0] = IMG_Filters.EdgeDetection(tmp_img, IMG_Filters.EdgeDetection_Operators.Sobel, 10); // [0 - 30]
                                        break;
                                    case 8: // Sobel 1
                                        edge_Sobel_bmp[1] = IMG_Filters.EdgeDetection(tmp_img, IMG_Filters.EdgeDetection_Operators.Sobel, 50); // [40 - 60]
                                        break;
                                    case 9: // Sobel 2
                                        edge_Sobel_bmp[2] = IMG_Filters.EdgeDetection(tmp_img, IMG_Filters.EdgeDetection_Operators.Sobel, 90); // [70 - 100]
                                        break;
                                }
                                
                                // dispose tmp image
                                tmp_img.Dispose();

                            });
            }

            EdgeAvailable = true;
            ShowEdgeImage(edge_type, ED_index);

            isBusy = false;

           

        }
        

        // Toolpathing

        public void Start(bool RemoveWhite, IMG_ToolPathing.ToolPathingType PathType)
        {
            if(TP_currentBMP == BitmapType.color)
            {
                return;
            }
            else if((int)TP_currentBMP < 9) // BW
            {
                Start(blackwhite_bmp[(int)TP_currentBMP], RemoveWhite, PathType);
            }
            else
            {
                switch (TP_currentBMP)
                {
                    case BitmapType.ed_SXOR:
                        Start(edge_SXOR_bmp, RemoveWhite, PathType);
                        break;
                    case BitmapType.ed_RC1:
                    case BitmapType.ed_RC2:
                        Start(edge_RC_bmp[((int)TP_currentBMP) - 11], RemoveWhite, PathType);
                        break;
                    case BitmapType.ed_P1:
                    case BitmapType.ed_P2:
                    case BitmapType.ed_P3:
                    case BitmapType.ed_P4:
                        Start(edge_Perwitt_bmp[((int)TP_currentBMP) - 13], RemoveWhite, PathType);
                        break;
                    case BitmapType.ed_S1:
                    case BitmapType.ed_S2:
                    case BitmapType.ed_S3:
                        Start(edge_Sobel_bmp[((int)TP_currentBMP) - 17], RemoveWhite, PathType);
                        break;

                }

                
            }
        }

        public void StartSVG(bool RemoveWhite, int colorDepth, IMG_ToolPathing.ToolPathingType PathType, bool EdgesOnly )
        {
                       
            // file save
            string filepath;
            TPFileBrowser.FileName = filename.Split('.')[0] + ".gcode";
            if (TPFileBrowser.ShowDialog() == DialogResult.OK)
            {
                filepath = TPFileBrowser.FileName;
            }
            else
            {
                return;
            }
            // tp start

            if (toolpather.Start(Machine_Properties, original_svg, IMG_Width, IMG_Height, RemoveWhite, colorDepth, PathType, new CNC_Properties.CoordinatesD(0, 0, 0), filepath, EdgesOnly, Generate_Report))
            {
                DisplayMode?.Invoke(ImageDisplay.Processing);
            }
        }

        public void Start(Bitmap img, bool RemoveWhite, IMG_ToolPathing.ToolPathingType PathType)
        {
            // image size
            img.SetResolution(Convert.ToSingle((Img_Size.Width / IMG_Width) / inch_to_mm) , Convert.ToSingle((Img_Size.Height / IMG_Height) / inch_to_mm));
            
            // file save
            string filepath;
            TPFileBrowser.FileName = filename.Split('.')[0] + ".gcode";
            if (TPFileBrowser.ShowDialog() == DialogResult.OK)
            {
                filepath = TPFileBrowser.FileName;
            }
            else
            {
                return;
            }
            // tp start
            
            if (toolpather.Start(Machine_Properties, img, RemoveWhite, PathType, new CNC_Properties.CoordinatesD(0, 0, 0), filepath, Generate_Report))
            {
                DisplayMode?.Invoke(ImageDisplay.Processing);
            }
            
        }

        public void Cancel()
        {
            toolpather.Cancel();
            DisplayMode?.Invoke(prevDisplayMode);
            
        }

        private void getToolpathIMGResult(Image gcode_img, Image gcode_img_ToolOnly)
        {
            toolpath_bmp = (Bitmap)gcode_img;
            toolpath_bmp_toolOnly = (Bitmap)gcode_img_ToolOnly;
            ToolPathAvailable = true;
            ShowToolPathImage(true);
        }

        public delegate void TPProgressUpdate(int progress, string status);
        public TPProgressUpdate ProgressUpdate;

        private void getToolpathProgress(int progress, string status)
        {
            ProgressUpdate?.Invoke(progress, status);
        }


    }
}
