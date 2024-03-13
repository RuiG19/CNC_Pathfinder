using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Drawing;

namespace CNC_Pathfinder.src.Toolpathing
{
    /// <summary>
    /// BitArray2D. 
    /// Based on the Java2s implementation.
    /// <a href="http://www.java2s.com/Code/CSharp/Collections-Data-Structure/BitArray2D.htm">BitArray2D</a>
    /// </summary>
    class BitArray2D
    {

        #region Parameters

        /// <summary>
        /// Gets the number of columns in the BitArray2D.
        /// </summary>
        public int width { get; private set; }

        /// <summary>
        /// Gets the number of rows in the BitArray2D.
        /// </summary>
        public int height { get; private set; }

        /// <summary>
        /// Gets the number of elements contained in the BitArray.
        /// </summary>
        public int count { get; private set;  }

        /// <summary>
        /// Gets or sets the value of the bit at a specific position in the BitArray2D.
        /// </summary>
        /// <param name="col">Column.</param>
        /// <param name="row">Row.</param>
        /// <returns></returns>
        public bool this[int col, int row]
        {
            get { return this.Get(col, row); }
            set { this.Set(col, row, value); }
        }

        /// <summary>
        /// Gets or sets the value of the bit at a specific position in the BitArray2D, using the default BitArray indexation.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool this[int pos]
        {
            get { return this._bitArray[pos]; }
            set { this._bitArray[pos] = value; }
        }

        /// <summary>
        /// BitArray that stores the BitArray2D values.
        /// </summary>
        private BitArray _bitArray;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the BitArray2D class with the specified size.
        /// </summary>
        /// <param name="width">Number of columns.</param>
        /// <param name="height">Number of rows.</param>
        public BitArray2D(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.count = width * height;

            _bitArray = new BitArray(this.width * this.height);
        }

        /// <summary>
        /// Copies a Bitmap image to the BitArray2D, setting to true the Pixels with the ColorToMark.
        /// Uses unsafe code for improved performance!
        /// </summary>
        /// <param name="img">Bitmap image with the 24bit format.</param>
        /// <param name="ColorToMark"></param>
        public BitArray2D(Bitmap img, byte ColorToMark)
        {
            this.width = img.Width;
            this.height = img.Height;
            this.count = width * height;

            _bitArray = new BitArray(this.width * this.height);

            unsafe
            {
                //lock the original bitmap in memory
                System.Drawing.Imaging.BitmapData originalData = img.LockBits(
                   new Rectangle(0, 0, width, height),
                   System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //set the number of bytes per pixel
                int pixelSize = 3;

                Parallel.For(0, height, y =>
                {
                    //get the data from the original image
                    byte* oRow = (byte*)originalData.Scan0 + (y * originalData.Stride);

                    for (int x = 0; x < width; x++)
                    {
                        //set true the pixels with the Color to Mark
                        if (oRow[x * pixelSize] == ColorToMark)
                        {
                            Set(x, (height - 1) - y, true);
                        }
                        else
                        {
                            Set(x, (height - 1) - y, false);
                        }
                    }
                });

                //unlock the bitmap
                img.UnlockBits(originalData);
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Sets the value of the bit at a specific position in the BitArray2D.
        /// </summary>
        /// <param name="col">Column.</param>
        /// <param name="row">Row.</param>
        /// <param name="value">Boolean value.</param>
        public void Set(int col, int row, bool value)
        {
            if (col < 0 || col >= width)
            {
                throw new ArgumentOutOfRangeException("col");
            }

            if (row < 0 || row >= height)
            {
                throw new ArgumentOutOfRangeException("row");
            }
            
            int pos = (row * width) + col;
           _bitArray[pos] = value;
        }

        /// <summary>
        /// Gets the value of the bit at a specific position in the BitArray2D.
        /// </summary>
        /// <param name="col">Column.</param>
        /// <param name="row">Row.</param>
        /// <returns></returns>
        public bool Get(int col, int row)
        {
            if (col < 0 || col >= width)
            {
                throw new ArgumentOutOfRangeException("col");
            }

            if (row < 0 || row >= height)
            {
                throw new ArgumentOutOfRangeException("row");
            }

            int pos = (row * width) + col;

            return _bitArray[pos];
        }

        /// <summary>
        /// Sets all the bits equal to the value.
        /// </summary>
        /// <param name="value"></param>
        public void SetAll(bool value)
        {
            this._bitArray.SetAll(value);
        }

        #endregion

    }
}
