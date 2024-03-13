using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Svg;
using System.Drawing;

namespace CNC_Pathfinder.src.SVG
{
    class SVG_Image
    {
        
        public List<SVG_Object> Objects { get; private set; }
        public int Count { get; private set; }

        public double Width { get; private set; }
        public double Height { get; private set; }

        public SVG_Image(SvgDocument img)
        {
            Objects = new List<SVG_Object>();
            this.Width = img.Width;
            this.Height = img.Height;

            foreach(SvgElement e in img.Children)
            {
                SvgParseElements(e);
            }

            // Mirror Y axis
            Parallel.ForEach(Objects, (obj) =>
            {
                List<SVG_Path> paths = obj.Paths;
                SVG_Object.Scale(ref paths, 1, -1);
                SVG_Object.Translation(ref paths, 0, img.ViewBox.Height + img.ViewBox.MinY);
                obj.Paths = paths;

            });
        }

        private void SvgParseElements(SvgElement e)
        {
            switch (SVG_Types.getType(e))
            {
                case SVG_Types.Document:
                case SVG_Types.Switch:
                case SVG_Types.Group:
                    foreach (SvgElement children in e.Children)
                    {
                        SvgParseElements(children);
                    }                    
                    break;
                case SVG_Types.Line:
                case SVG_Types.Path:
                case SVG_Types.Rectangle:
                case SVG_Types.Circle:
                case SVG_Types.Ellipse:
                case SVG_Types.Polygon:
                case SVG_Types.Polyline:
                    //case SVG_Types.Text:
                    this.Objects.Add(new SVG_Object(e));
                    this.Count++;
                    break;
                default: // ignore others (most are color related)
                    break;
            }            
        }


        public void Resize(double factor)
        {
            if(Count == 0)
            {
                return;
            }
            //foreach(SVG_Object obj in Objects)
            Parallel.ForEach(Objects, obj =>
            {
                if (obj.Paths.Count != 0)
                {
                    List<SVG_Path> paths = obj.Paths;
                    SVG_Object.Scale(ref paths, factor, factor);
                    //SVG_Object.Translation(ref paths, Width*(1-factor), -Height*(1-factor));
                    obj.Paths = paths;

                    /*
                    Parallel.ForEach(obj.Paths, (p) =>
                    {
                        

                        /*
                        p.Start.X *= factor;
                        p.Start.Y *= factor;
                        p.End.X *= factor;
                        p.End.Y *= factor;

                        
                        switch (p.PathType)
                        {
                            case SVG_Path.SVG_Path_Type.Curve_Center:
                                p.Center.X *= factor;
                                p.Center.Y *= factor;
                                break;
                            case SVG_Path.SVG_Path_Type.Curve_Radius:
                                p.Radius *= factor;
                                break;
                        }
                        
                    });
                    */
                }
            });
        }

        public Bitmap DrawDraft() // draws form Objects list
        {

            throw new NotImplementedException();
        }


    }
}
