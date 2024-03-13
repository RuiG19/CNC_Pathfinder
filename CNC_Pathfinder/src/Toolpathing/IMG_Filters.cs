using CNC_Pathfinder.src.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNC_Pathfinder.src.Toolpathing
{
    class IMG_Filters
    {
        public enum EdgeDetection_Operators
        {
            Prewitt = 1,
            Sobel = 2
        }

        private static int[] kernel_sharpen_0 =   {-1, -1, -1,
                                                   -1, 17, -1,
                                                   -1, -1, -1};

        private static double sharpen_factor_0 = (double)1 / 9;

        private static int[] kernel_sharpen_1 =   { 0, -1,  0,
                                                   -1,  5, -1,
                                                    0, -1,  0};

        private static double sharpen_factor_1 = (double)1;

        private static int[] kernel_sharpen_2 =   {-1, -1, -1,
                                                   -1,  9, -1,
                                                   -1, -1, -1};

        private static double sharpen_factor_2 = (double)1;


        private static int[] kernel_blur_0 =   { 1,  1,  1,
                                                 1,  1,  1,
                                                 1,  1,  1};

        private static double blur_factor_0 = (double)1 / 9;

        private static int[] kernel_blur_1 =   { 1,  2,  1,
                                                 2,  4,  2,
                                                 1,  2,  1};

        private static double blur_factor_1 = (double)1 / 16;

        
        #region Image Filters

        /// <summary>
        /// Returns a Gray scale representation of a RGB Bitmap image.
        /// </summary>
        /// <param name="img"></param>
        /// <returns>Gray scaled Bitmap.</returns>
        public static Bitmap GrayScale(Bitmap img)
        {
            

            unsafe
            {
                //create an empty bitmap the same size as original
                Bitmap newBitmap = new Bitmap(img.Width, img.Height);

                //lock the original bitmap in memory
                System.Drawing.Imaging.BitmapData originalData = img.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //lock the new bitmap in memory
                System.Drawing.Imaging.BitmapData newData = newBitmap.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //set the number of bytes per pixel
                int pixelSize = 3;

                int img_Width = img.Width;
                int img_Height = img.Height;
                //for (int y = 0; y < img.Height; y++)
                Parallel.For(0, img_Height, y =>
                {
                    //get the data from the original image
                    byte* oRow = (byte*)originalData.Scan0 + (y * originalData.Stride);

                    //get the data from the new image
                    byte* nRow = (byte*)newData.Scan0 + (y * newData.Stride);

                    byte Gray_Color;

                    for (int x = 0; x < img_Width; x++)
                    {
                        //create the grayscale version
                        Gray_Color =
                           (byte)((oRow[x * pixelSize] * .11) + //B
                           (oRow[x * pixelSize + 1] * .59) +  //G
                           (oRow[x * pixelSize + 2] * .3)); //R


                        //set the new image's pixel to the grayscale version
                        nRow[x * pixelSize] = Gray_Color; //B
                        nRow[x * pixelSize + 1] = Gray_Color; //G
                        nRow[x * pixelSize + 2] = Gray_Color; //R
                    }
                });

                //unlock the bitmaps
                newBitmap.UnlockBits(newData);
                img.UnlockBits(originalData);

                return newBitmap;
            }
        }

        /// <summary>
        /// Returns a Black and White representation of a RGB Bitmap image. The depth value is adjustable.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="depth">Value of % of white/black threshold color. Higher values results in a darker image.</param>
        /// <returns></returns>
        public static Bitmap BlackWhite(Bitmap img, int depth)
        {
            int d = depth;
            if (d > 100)
            {
                d = 100;
            }
            else if (d < 0)
            {
                d = 0;
            }

            int threshold = Convert.ToInt32(Math.Round(((double)255 * d) / 100));

            
            unsafe
            {
                //create an empty bitmap the same size as original
                Bitmap newBitmap = new Bitmap(img.Width, img.Height);

                //lock the original bitmap in memory
                System.Drawing.Imaging.BitmapData originalData = img.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //lock the new bitmap in memory
                System.Drawing.Imaging.BitmapData newData = newBitmap.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //set the number of bytes per pixel
                int pixelSize = 3;

                int img_Width = img.Width;
                int img_Height = img.Height;
                //for (int y = 0; y < img.Height; y++)
                Parallel.For(0 , img_Height, y =>
                {
                    //get the data from the original image
                    byte* oRow = (byte*)originalData.Scan0 + (y * originalData.Stride);

                    //get the data from the new image
                    byte* nRow = (byte*)newData.Scan0 + (y * newData.Stride);

                    byte BlackWhite_Color;

                    for (int x = 0; x < img_Width; x++)
                    {
                        //create the grayscale version
                        BlackWhite_Color =
                           (byte)((oRow[x * pixelSize] * .11) + //B
                           (oRow[x * pixelSize + 1] * .59) +  //G
                           (oRow[x * pixelSize + 2] * .3)); //R

                        if (BlackWhite_Color > threshold)
                        {
                            BlackWhite_Color = 255;
                        }
                        else
                        {
                            BlackWhite_Color = 0;
                        }

                        //set the new image's pixel to the grayscale version
                        nRow[x * pixelSize] = BlackWhite_Color; //B
                        nRow[x * pixelSize + 1] = BlackWhite_Color; //G
                        nRow[x * pixelSize + 2] = BlackWhite_Color; //R
                    }
                });

                //unlock the bitmaps
                newBitmap.UnlockBits(newData);
                img.UnlockBits(originalData);

                return newBitmap;
            }

        }

        //

        /// <summary>
        /// Inverts the Colors of a RGB 24bpp format Bitmap image.
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static Bitmap Invert(Bitmap img)
        {
            unsafe
            {
                //create an empty bitmap the same size as original
                Bitmap newBitmap = new Bitmap(img.Width, img.Height);

                //lock the original bitmap in memory
                System.Drawing.Imaging.BitmapData originalData = img.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //lock the new bitmap in memory
                System.Drawing.Imaging.BitmapData newData = newBitmap.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //set the number of bytes per pixel
                int pixelSize = 3;

                int img_Width = img.Width;
                int img_Height = img.Height;
                //for (int y = 0; y < img.Height; y++)
                Parallel.For(0, img_Height, y =>
                {
                    //get the data from the original image
                    byte* oRow = (byte*)originalData.Scan0 + (y * originalData.Stride);

                    //get the data from the new image
                    byte* nRow = (byte*)newData.Scan0 + (y * newData.Stride);

                    for (int x = 0; x < img_Width; x++)
                    {
                        //set the new image's pixel to the invert color version
                        nRow[x * pixelSize] = (byte)(255 - oRow[x * pixelSize]); //B
                        nRow[x * pixelSize + 1] = (byte)(255 - oRow[x * pixelSize + 1]); //G
                        nRow[x * pixelSize + 2] = (byte)(255 - oRow[x * pixelSize + 2]); //R
                    }
                });

                //unlock the bitmaps
                newBitmap.UnlockBits(newData);
                img.UnlockBits(originalData);

                return newBitmap;
            }
        }

        /// <summary>
        /// Sharpens the colors of a RGB 24bpp format Bitmap image.
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static Bitmap Sharp(Bitmap img)
        {
            return Image_Convolution(img, kernel_sharpen_1, sharpen_factor_1);
        }

        /// <summary>
        /// Blurs the colors of a RGB 24bpp format Bitmap image.
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static Bitmap Blur(Bitmap img)
        {
            return Image_Convolution(img, kernel_blur_0, blur_factor_0);
        }

        //

        /// <summary>
        /// Applies a Edgedetection 3x3 operator to a Grayscale image.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="Operator">Sobel or Perwitt</param>
        /// <param name="depth">Value of % of edge threshold sensitivity. Higher values result in more edges represented.</param>
        /// <returns></returns>
        public static Bitmap EdgeDetection(Bitmap img, EdgeDetection_Operators Operator, int depth)
        {
            int d = depth;
            if (d > 100)
            {
                d = 100;
            }
            else if (d < 0)
            {
                d = 0;
            }

            d = 100 - d;

            int threshold = 0;

            if (Operator == EdgeDetection_Operators.Sobel)
            {
                threshold = Convert.ToInt32(Math.Round(((double)(6 * 255) * d) / 100));
            }
            else // Perwitt
            {
                threshold = Convert.ToInt32(Math.Round(((double)(4 * 255) * d) / 100));
            }
                     
            
            unsafe
            {
                //create an empty bitmap the same size as original
                Bitmap newBitmap = new Bitmap(img.Width, img.Height);

                //lock the original bitmap in memory
                System.Drawing.Imaging.BitmapData originalData = img.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //lock the new bitmap in memory
                System.Drawing.Imaging.BitmapData newData = newBitmap.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //set the number of bytes per pixel
                int pixelSize = 3;

                int img_Width = img.Width;
                int img_Height = img.Height;
                //for (int y = 0; y < img.Height; y++)
                Parallel.For(0, img_Height, y =>
                {
                    //get the data from the original image
                    byte* oRow = (byte*)originalData.Scan0 + (y * originalData.Stride);
                    byte* oRowPrev = oRow;
                    byte* oRowNext = oRow;
                    if (y != 0)
                    {
                        oRowPrev = (byte*)originalData.Scan0 + ((y - 1) * originalData.Stride);
                    }
                    if (y != img_Height - 1)
                    {
                        oRowNext = (byte*)originalData.Scan0 + ((y + 1) * originalData.Stride);
                    }

                    //get the data from the new image
                    byte* nRow = (byte*)newData.Scan0 + (y * newData.Stride);

                    int Pixel_Gradient = 0;
                    int[] Pixel_Matrix = new int[9];


                    // first line ---
                    Pixel_Matrix[0] = 255 ;
                    Pixel_Matrix[1] = oRowNext[(0) * pixelSize];
                    Pixel_Matrix[2] = oRowNext[(1) * pixelSize];
                    Pixel_Matrix[3] = 255;
                    //Pixel_Matrix[4] = oRow[(x) * pixelSize];
                    Pixel_Matrix[5] = oRow[(1) * pixelSize];
                    Pixel_Matrix[6] = 255 ;
                    Pixel_Matrix[7] = oRowPrev[(0) * pixelSize];
                    Pixel_Matrix[8] = oRowPrev[(1) * pixelSize];

                    Pixel_Gradient = Math.Abs((Pixel_Matrix[2] + (int)Operator * Pixel_Matrix[5] + Pixel_Matrix[8]) -
                                                        (Pixel_Matrix[0] + (int)Operator * Pixel_Matrix[3] + Pixel_Matrix[6])) +
                                     Math.Abs((Pixel_Matrix[0] + (int)Operator * Pixel_Matrix[1] + Pixel_Matrix[2]) -
                                                        (Pixel_Matrix[6] + (int)Operator * Pixel_Matrix[7] + Pixel_Matrix[8]));

                    if (Pixel_Gradient > threshold)
                    {
                        Pixel_Gradient = 0;
                    }
                    else
                    {
                        Pixel_Gradient = 255;
                    }

                    //set the new image's pixel to the grayscale version
                    nRow[0] = (byte)Pixel_Gradient; //B
                    nRow[1] = (byte)Pixel_Gradient; //G
                    nRow[2] = (byte)Pixel_Gradient; //R

                    // middle lines ---
                    for (int x = 1; x < img_Width - 1; x++)
                    {
                        Pixel_Matrix[0] = oRowNext[(x - 1) * pixelSize];
                        Pixel_Matrix[1] = oRowNext[(x) * pixelSize];
                        Pixel_Matrix[2] = oRowNext[(x + 1) * pixelSize];
                        Pixel_Matrix[3] = oRow[(x - 1) * pixelSize];
                        //Pixel_Matrix[4] = oRow[(x) * pixelSize];
                        Pixel_Matrix[5] = oRow[(x + 1) * pixelSize];
                        Pixel_Matrix[6] = oRowPrev[(x - 1) * pixelSize];
                        Pixel_Matrix[7] = oRowPrev[(x) * pixelSize];
                        Pixel_Matrix[8] = oRowPrev[(x + 1) * pixelSize];

                        Pixel_Gradient = Math.Abs((Pixel_Matrix[2] + (int)Operator * Pixel_Matrix[5] + Pixel_Matrix[8]) -
                                                            (Pixel_Matrix[0] + (int)Operator * Pixel_Matrix[3] + Pixel_Matrix[6])) +
                                         Math.Abs((Pixel_Matrix[0] + (int)Operator * Pixel_Matrix[1] + Pixel_Matrix[2]) -
                                                            (Pixel_Matrix[6] + (int)Operator * Pixel_Matrix[7] + Pixel_Matrix[8]));

                        if (Pixel_Gradient > threshold)
                        {
                            Pixel_Gradient = 0;
                        }
                        else
                        {
                            Pixel_Gradient = 255;
                        }

                        //set the new image's pixel to the grayscale version
                        nRow[x * pixelSize] = (byte)Pixel_Gradient; //B
                        nRow[x * pixelSize + 1] = (byte)Pixel_Gradient; //G
                        nRow[x * pixelSize + 2] = (byte)Pixel_Gradient; //R
                    }

                    // last line ---

                    Pixel_Matrix[0] = oRowNext[(img_Width - 2) * pixelSize];
                    Pixel_Matrix[1] = oRowNext[(img_Width - 1) * pixelSize];
                    Pixel_Matrix[2] = 255;
                    Pixel_Matrix[3] = oRow[(img_Width - 2) * pixelSize];
                    //Pixel_Matrix[4] = oRow[(x) * pixelSize];
                    Pixel_Matrix[5] = 255;
                    Pixel_Matrix[6] = oRowPrev[(img_Width - 2) * pixelSize];
                    Pixel_Matrix[7] = oRowPrev[(img_Width - 1) * pixelSize];
                    Pixel_Matrix[8] = 255;

                    Pixel_Gradient = Math.Abs((Pixel_Matrix[2] + (int)Operator * Pixel_Matrix[5] + Pixel_Matrix[8]) -
                                                        (Pixel_Matrix[0] + (int)Operator * Pixel_Matrix[3] + Pixel_Matrix[6])) +
                                     Math.Abs((Pixel_Matrix[0] + (int)Operator * Pixel_Matrix[1] + Pixel_Matrix[2]) -
                                                        (Pixel_Matrix[6] + (int)Operator * Pixel_Matrix[7] + Pixel_Matrix[8]));

                    if (Pixel_Gradient > threshold)
                    {
                        Pixel_Gradient = 0;
                    }
                    else
                    {
                        Pixel_Gradient = 255;
                    }

                    //set the new image's pixel to the grayscale version
                    nRow[(img_Width - 1) * pixelSize] = (byte)Pixel_Gradient; //B
                    nRow[(img_Width - 1) * pixelSize + 1] = (byte)Pixel_Gradient; //G
                    nRow[(img_Width - 1) * pixelSize + 2] = (byte)Pixel_Gradient; //R


                });

                //unlock the bitmaps
                newBitmap.UnlockBits(newData);
                img.UnlockBits(originalData);

                return newBitmap;

            }

        }

        /// <summary>
        /// Edgedetection algorithm with Robert's Cross operator for Grayscale or Black and White images.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static Bitmap EdgeDetection_RC(Bitmap img, int depth)
        {
            int d = depth;
            if (d > 100)
            {
                d = 100;
            }
            else if (d < 0)
            {
                d = 0;
            }

            d = 100 - d;
                        
            unsafe
            {
                //create an empty bitmap the same size as original
                Bitmap newBitmap = new Bitmap(img.Width, img.Height);

                //lock the original bitmap in memory
                System.Drawing.Imaging.BitmapData originalData = img.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //lock the new bitmap in memory
                System.Drawing.Imaging.BitmapData newData = newBitmap.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //set the number of bytes per pixel
                int pixelSize = 3;

                int img_Width = img.Width;
                int img_Height = img.Height;
                //for (int y = 0; y < img.Height; y++)
                Parallel.For(0, img_Height, y =>
                {
                    //get the data from the original image
                    byte* oRow = (byte*)originalData.Scan0 + (y * originalData.Stride);
                    byte* oRowNext = oRow;
                    if (y != img_Height - 1)
                    {
                        oRowNext = (byte*)originalData.Scan0 + ((y + 1) * originalData.Stride);
                    }

                    //get the data from the new image
                    byte* nRow = (byte*)newData.Scan0 + (y * newData.Stride);

                    int threshold = Convert.ToInt32(Math.Round(((double)(2 * 255) * d) / 100));

                    int Pixel_Gradient = 0;
                    int[] Pixel_Matrix = new int[4];

                    for (int x = 0; x < img_Width - 1; x++)
                    {
                        Pixel_Matrix[0] = oRow[(x) * pixelSize];
                        Pixel_Matrix[2] = oRowNext[(x) * pixelSize];
                        Pixel_Matrix[1] = oRow[(x + 1) * pixelSize];
                        Pixel_Matrix[3] = oRowNext[(x + 1) * pixelSize];

                        Pixel_Gradient = Math.Abs(Pixel_Matrix[0] - Pixel_Matrix[3]) + Math.Abs(Pixel_Matrix[1] - Pixel_Matrix[2]);

                        if (Pixel_Gradient > threshold)
                        {
                            Pixel_Gradient = 0;
                        }
                        else
                        {
                            Pixel_Gradient = 255;
                        }

                        //set the new image's pixel to white
                        nRow[x * pixelSize] = (byte)Pixel_Gradient; //B
                        nRow[x * pixelSize + 1] = (byte)Pixel_Gradient; //G
                        nRow[x * pixelSize + 2] = (byte)Pixel_Gradient; //R

                    }

                    // last line
                    Pixel_Matrix[0] = oRow[(img_Width - 1) * pixelSize];
                    Pixel_Matrix[2] = oRowNext[(img_Width - 1) * pixelSize];
                    Pixel_Matrix[1] = oRow[(img_Width - 1) * pixelSize];
                    Pixel_Matrix[3] = oRowNext[(img_Width - 1) * pixelSize];

                    Pixel_Gradient = Math.Abs(Pixel_Matrix[0] - Pixel_Matrix[3]) + Math.Abs(Pixel_Matrix[1] - Pixel_Matrix[2]);

                    if (Pixel_Gradient > threshold)
                    {
                        Pixel_Gradient = 0;
                    }
                    else
                    {
                        Pixel_Gradient = 255;
                    }

                    //set the new image's pixel to white
                    nRow[(img_Width - 1) * pixelSize] = (byte)Pixel_Gradient; //B
                    nRow[(img_Width - 1) * pixelSize + 1] = (byte)Pixel_Gradient; //G
                    nRow[(img_Width - 1) * pixelSize + 2] = (byte)Pixel_Gradient; //R

                });

                //unlock the bitmaps
                newBitmap.UnlockBits(newData);
                img.UnlockBits(originalData);

                // Remove last pixel row and colum
                Rectangle img_frame = new Rectangle(0, 0, img.Width - 1, img.Height - 1);
                Bitmap newBitmap_Crop = newBitmap.Clone(img_frame, newBitmap.PixelFormat);

                return newBitmap_Crop;
            }

        }

        /// <summary>
        /// Simple XOR edgedetection algorithm for Black and White only! images.
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static Bitmap EdgeDetection_SXOR(Bitmap img)
        {
            
            unsafe
            {
                //create an empty bitmap the same size as original
                Bitmap newBitmap = new Bitmap(img.Width, img.Height);

                //lock the original bitmap in memory
                System.Drawing.Imaging.BitmapData originalData = img.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //lock the new bitmap in memory
                System.Drawing.Imaging.BitmapData newData = newBitmap.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //set the number of bytes per pixel
                int pixelSize = 3;

                int img_Width = img.Width;
                int img_Height = img.Height;
                //for (int y = 0; y < img.Height; y++)
                Parallel.For(0, img_Height, y =>
                {
                    //get the data from the original image
                    byte* oRow = (byte*)originalData.Scan0 + (y * originalData.Stride);
                    byte* oRowNext = oRow;
                    if (y != img_Height - 1)
                    {
                        oRowNext = (byte*)originalData.Scan0 + ((y + 1) * originalData.Stride);
                    }

                    //get the data from the new image
                    byte* nRow = (byte*)newData.Scan0 + (y * newData.Stride);

                    bool Horizontal_Gradient = false;
                    bool Vertical_Gradient = false;

                    for (int x = 0; x < img_Width - 1; x++)
                    {

                        Horizontal_Gradient = (oRow[x * pixelSize] == oRow[(x + 1) * pixelSize]) ? false : true;

                        Vertical_Gradient = (oRow[x * pixelSize] == oRowNext[(x) * pixelSize]) ? false : true;

                        if (Horizontal_Gradient || Vertical_Gradient)
                        {
                            //set the new image's pixel to black
                            nRow[x * pixelSize] = 0; //B
                            nRow[x * pixelSize + 1] = 0; //G
                            nRow[x * pixelSize + 2] = 0; //R
                        }
                        else
                        {
                            //set the new image's pixel to white
                            nRow[x * pixelSize] = 255; //B
                            nRow[x * pixelSize + 1] = 255; //G
                            nRow[x * pixelSize + 2] = 255; //R
                        }

                    }

                    // last line
                    Vertical_Gradient = (oRow[(img_Width - 1) * pixelSize] == oRowNext[(img_Width - 1) * pixelSize]) ? false : true;

                    if (Vertical_Gradient)
                    {
                        //set the new image's pixel to black
                        nRow[(img_Width - 1) * pixelSize] = 0; //B
                        nRow[(img_Width - 1) * pixelSize + 1] = 0; //G
                        nRow[(img_Width - 1) * pixelSize + 2] = 0; //R
                    }
                    else
                    {
                        //set the new image's pixel to white
                        nRow[(img_Width - 1) * pixelSize] = 255; //B
                        nRow[(img_Width - 1) * pixelSize + 1] = 255; //G
                        nRow[(img_Width - 1) * pixelSize + 2] = 255; //R
                    }


                });

                //unlock the bitmaps
                newBitmap.UnlockBits(newData);
                img.UnlockBits(originalData);

                // Remove last pixel row and colum
                Rectangle img_frame = new Rectangle(0, 0, img.Width - 1, img.Height - 1);
                Bitmap newBitmap_Crop = newBitmap.Clone(img_frame, newBitmap.PixelFormat);


                return newBitmap_Crop;
            }


        }

        //

        /// <summary>
        /// Convolves an image in a specified 3x3 kernel. Fast version, using Bitmapdata. Only works for 24bpp images.
        /// </summary>
        /// <param name="img">Must be on a 24bpp fixel format.</param>
        /// <param name="kernel">Must have a size equaled to 9.</param>
        /// <param name="factor">Kernel multiplicative factor.</param>
        /// <returns></returns>
        public static Bitmap Image_Convolution(Bitmap img, int[] kernel, double factor)
        {
            if (kernel.Length != 9)
            {
                throw new InvalidOperationException();
            }

            

            unsafe
            {
                //create an empty bitmap the same size as original
                Bitmap newBitmap = new Bitmap(img.Width, img.Height);

                //lock the original bitmap in memory
                System.Drawing.Imaging.BitmapData originalData = img.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //lock the new bitmap in memory
                System.Drawing.Imaging.BitmapData newData = newBitmap.LockBits(
                   new Rectangle(0, 0, img.Width, img.Height),
                   System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //set the number of bytes per pixel
                int pixelSize = 3;

                int img_Width = img.Width;
                int img_Height = img.Height;
                //for (int y = 0; y < img.Height; y++)
                Parallel.For(0, img_Height, y =>
                {
                    //get the data from the original image
                    byte* oRow = (byte*)originalData.Scan0 + (y * originalData.Stride);
                    byte* oRowPrev = oRow;
                    byte* oRowNext = oRow;
                    if (y != 0)
                    {
                        oRowPrev = (byte*)originalData.Scan0 + ((y - 1) * originalData.Stride);
                    }
                    if (y != img_Height - 1)
                    {
                        oRowPrev = (byte*)originalData.Scan0 + ((y + 1) * originalData.Stride);
                    }

                    //get the data from the new image
                    byte* nRow = (byte*)newData.Scan0 + (y * newData.Stride);

                    int[] Pixel_Color = new int[3];

                    for (int x = 0; x < img_Width; x++)
                    {
                        if (x == 0 || x == (img_Width - 1))
                        {
                            
                            Pixel_Color[0] = oRow[(x) * pixelSize]; // B
                            Pixel_Color[1] = oRow[(x) * pixelSize + 1]; // G
                            Pixel_Color[2] = oRow[(x) * pixelSize + 2]; // R

                            //set the new image's pixel to the new version
                            nRow[x * pixelSize] = (byte)Pixel_Color[0]; //B
                            nRow[x * pixelSize + 1] = (byte)Pixel_Color[1]; //G
                            nRow[x * pixelSize + 2] = (byte)Pixel_Color[2]; //R

                            continue;
                        }

                        // kernel convolution
                        else
                        {
                            Pixel_Color[0] = oRowPrev[(x - 1) * pixelSize] * kernel[0] +
                                             oRowPrev[(x) * pixelSize] * kernel[1] +
                                             oRowPrev[(x + 1) * pixelSize] * kernel[2] +
                                             oRow[(x - 1) * pixelSize] * kernel[3] +
                                             oRow[(x) * pixelSize] * kernel[4] +
                                             oRow[(x + 1) * pixelSize] * kernel[5] +
                                             oRowNext[(x - 1) * pixelSize] * kernel[6] +
                                             oRowNext[(x) * pixelSize] * kernel[7] +
                                             oRowNext[(x + 1) * pixelSize] * kernel[8]; // B

                            Pixel_Color[1] = oRowPrev[(x - 1) * pixelSize + 1] * kernel[0] +
                                             oRowPrev[(x) * pixelSize + 1] * kernel[1] +
                                             oRowPrev[(x + 1) * pixelSize + 1] * kernel[2] +
                                             oRow[(x - 1) * pixelSize + 1] * kernel[3] +
                                             oRow[(x) * pixelSize + 1] * kernel[4] +
                                             oRow[(x + 1) * pixelSize + 1] * kernel[5] +
                                             oRowNext[(x - 1) * pixelSize + 1] * kernel[6] +
                                             oRowNext[(x) * pixelSize + 1] * kernel[7] +
                                             oRowNext[(x + 1) * pixelSize + 1] * kernel[8]; // G

                            Pixel_Color[2] = oRowPrev[(x - 1) * pixelSize + 2] * kernel[0] +
                                             oRowPrev[(x) * pixelSize + 2] * kernel[1] +
                                             oRowPrev[(x + 1) * pixelSize + 2] * kernel[2] +
                                             oRow[(x - 1) * pixelSize + 2] * kernel[3] +
                                             oRow[(x) * pixelSize + 2] * kernel[4] +
                                             oRow[(x + 1) * pixelSize + 2] * kernel[5] +
                                             oRowNext[(x - 1) * pixelSize + 2] * kernel[6] +
                                             oRowNext[(x) * pixelSize + 2] * kernel[7] +
                                             oRowNext[(x + 1) * pixelSize + 2] * kernel[8]; // R
                        }

                        // factor multiplication
                        Pixel_Color[0] = Convert.ToInt32(Math.Round(Convert.ToDouble(Pixel_Color[0]) * factor));
                        Pixel_Color[1] = Convert.ToInt32(Math.Round(Convert.ToDouble(Pixel_Color[1]) * factor));
                        Pixel_Color[2] = Convert.ToInt32(Math.Round(Convert.ToDouble(Pixel_Color[2]) * factor));

                        // color saturation
                        if (Pixel_Color[0] < 0)
                        {
                            Pixel_Color[0] = 0;
                        }
                        else if (Pixel_Color[0] > 255)
                        {
                            Pixel_Color[0] = 255;
                        }

                        if (Pixel_Color[1] < 0)
                        {
                            Pixel_Color[1] = 0;
                        }
                        else if (Pixel_Color[1] > 255)
                        {
                            Pixel_Color[1] = 255;
                        }

                        if (Pixel_Color[2] < 0)
                        {
                            Pixel_Color[2] = 0;
                        }
                        else if (Pixel_Color[2] > 255)
                        {
                            Pixel_Color[2] = 255;
                        }

                        //set the new image's pixel to the new version
                        nRow[x * pixelSize] = (byte)Pixel_Color[0]; //B
                        nRow[x * pixelSize + 1] = (byte)Pixel_Color[1]; //G
                        nRow[x * pixelSize + 2] = (byte)Pixel_Color[2]; //R
                    }
                });

                //unlock the bitmaps
                newBitmap.UnlockBits(newData);
                img.UnlockBits(originalData);

                return newBitmap;
            }

        }

        /// <summary>
        /// Converts an image to a 24bpp bitmap image.
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static Bitmap ConvertTo24bpp(Image img)
        {
            Bitmap bmp = new Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (var b = new SolidBrush(Color.White))
            using (var gr = Graphics.FromImage(bmp))
            {
                gr.FillRectangle(b, 0, 0, img.Width, img.Height);
                gr.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height));
            }
            

            return bmp;
        }

        public static Bitmap ImageResize(Bitmap img, double ratio)
        {
            if(ratio == 1)
            {
                return (Bitmap)img.Clone();
            }

            int newWidth = Convert.ToInt32(Math.Round((double)img.Width * ratio));
            int newHeight = Convert.ToInt32(Math.Round((double)img.Height * ratio));

            newWidth = newWidth == 0 ? 1 : newWidth;
            newHeight = newHeight == 0 ? 1 : newHeight;


            Bitmap Bmp = new Bitmap(newWidth, newHeight);
            using (var b = new SolidBrush(Color.White))
            using (Graphics gfx = Graphics.FromImage(Bmp))
            {
                gfx.FillRectangle(b, 0, 0, newWidth, newWidth);

                if (ratio > 1)
                {
                    if(img.Width * img.Height < (160 * 110))
                    {
                        gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    }
                    else
                    {
                        gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    }
                                        

                    using (Bitmap aux = new Bitmap(img.Width + 1, img.Height + 1))
                    using (Graphics gfx_aux = Graphics.FromImage(aux))
                    {
                        gfx_aux.FillRectangle(b, 0, 0, img.Width + 1, img.Height + 1);
                        gfx_aux.DrawImage(img, 1, 1, img.Width, img.Height);

                        gfx.DrawImage(aux, 0, 0, newWidth, newHeight);
                    }
                }
                else if(ratio > 0.40)
                {                    
                    gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic; // best for image reduction (ensures high quality till x4 reduction)
                    gfx.DrawImage(img, 0, 0, newWidth, newHeight);
                }
                else
                {
                    gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; // best for image reduction 
                    gfx.DrawImage(img, 0, 0, newWidth, newHeight);
                }

                
                

            }

            return Bmp;
        }



        #endregion

    }
}
