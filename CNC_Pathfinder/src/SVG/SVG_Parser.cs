﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Svg;
using System.Drawing;

namespace CNC_Pathfinder.src.SVG
{
    /// <summary>
    /// Class containg code for manipulating SVG graphics.
    /// </summary>
    class SVG_Parser
    {
        /// <summary>
        /// The maximum image size supported.
        /// </summary>
        public static Size MaximumSize { get; set; }
        
        /// <summary>
        /// Converts an SVG file to a Bitmap image.
        /// </summary>
        /// <param name="filePath">The full path of the SVG image.</param>
        /// <returns>Returns the converted Bitmap image.</returns>
        public static Bitmap GetBitmapFromSVG(string filePath)
        {
            SvgDocument document = GetSvgDocument(filePath);

            Bitmap bmp = document.Draw();
            return bmp;
        }

        /// <summary>
        /// Gets a SvgDocument for manipulation using the path provided.
        /// </summary>
        /// <param name="filePath">The path of the Bitmap image.</param>
        /// <returns>Returns the SVG Document.</returns>
        public static SvgDocument GetSvgDocument(string filePath)
        {
            SvgDocument document;
            try
            {
                document = SvgDocument.Open(filePath);
            }
            catch(Exception e)
            {
                throw new InvalidOperationException();
            }

            return AdjustSize(document);
        }

        /// <summary>
        /// Makes sure that the image does not exceed the maximum size, while preserving aspect ratio.
        /// </summary>
        /// <param name="document">The SVG document to resize.</param>
        /// <returns>Returns a resized or the original document depending on the document.</returns>
        private static SvgDocument AdjustSize(SvgDocument document)
        {
            if (document.Height > MaximumSize.Height)
            {
                document.Width = (int)((document.Width / (double)document.Height) * MaximumSize.Height);
                document.Height = MaximumSize.Height;
            }
            else
            {
                document.Height = (int)((document.Height / (double)document.Width) * MaximumSize.Width);
                document.Width = MaximumSize.Width;
            }
                                   
             
            return document;
        }


    }

}

