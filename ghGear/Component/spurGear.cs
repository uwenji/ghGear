using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace ghGear
{
    public class spurGear : GH_Component
    {
        public List<string> texts = new List<string>();
        public List<Point3d> locations = new List<Point3d>();
        public List<double> sizes = new List<double>();
        List<Curve> Spur = new List<Curve>();
        List<double> Ratio = new List<double>();

        List<Circle> Circles = new List<Circle>();
        double Teeth;
        double Angle;
        double shift;
        double addendum;
        double dedendum;


        public spurGear()
          : base("SpurGear", "spurG",
              "Spur Gear from Circles",
              "Gears", "Build")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCircleParameter("Circles", "C", "Circles for spur gears", GH_ParamAccess.list);
            pManager.AddNumberParameter("Teeth", "T", "Teeth number", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angle", "A", "pressure angle degree, default is 22.5 and range should 15 to 35", GH_ParamAccess.item, 22.5);
            pManager.AddNumberParameter("shift", "S", "Profile shift coefficient, from 0 to 0.5, default is 0.1", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("addendum", "ad", "addendum, 1.0 module", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("dedendum", "de", "dedendum, 1.25 module", GH_ParamAccess.item, 1.25);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Gears", "G", "Gears", GH_ParamAccess.list);
            pManager.AddCurveParameter("Pitch", "P", "Pitch", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Ratio", "R", "Ratio", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Util.Gears gear = new Util.Gears();
            Circles = new List<Circle>();
            Spur = new List<Curve>();
            Ratio = new List<double>();
            List<int> ratioInt = new List<int>();
            List<Circle> Pitch = new List<Circle>();

            DA.GetDataList<Circle>(0, Circles);
            DA.GetData<double>(1, ref Teeth);
            DA.GetData<double>(2, ref Angle);
            DA.GetData<double>(3, ref shift);
            DA.GetData<double>(4, ref addendum);
            DA.GetData<double>(5, ref dedendum);
            
            Spur = gear.buildGear(Circles, Teeth, Angle, shift, addendum, dedendum, out ratioInt);
            foreach(int i in ratioInt)
            {
                Ratio.Add((double)i);
            }
            DA.SetDataList(0, Spur);
            Pitch = Circles;
            DA.SetDataList(1, Pitch);
            DA.SetDataList(2, Ratio);

            texts = gear.texts;
            locations = gear.locations;
            sizes = gear.sizes;
        }
        public override BoundingBox ClippingBox
        {
            get
            {
                List<Point3d> points = new List<Point3d>();
                foreach (Curve thisC in Spur)
                {
                    Point3d[] pbox = thisC.GetBoundingBox(false).GetCorners();
                    foreach(Point3d thisP in pbox)
                    {
                        points.Add(thisP);
                    }
                }
                BoundingBox bbox = new BoundingBox(points);
                return bbox;
            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (texts.Count == 0)
                return;
            
            Plane plane;
            args.Viewport.GetFrustumFarPlane(out plane);

            for (int i = 0; i < texts.Count; i++)
            {
                string text = texts[i];
                double size = sizes[i];
                Point3d location = locations[i];
                plane.Origin = location;

                // Figure out the size. This means measuring the visible size in the viewport AT the current location.
                double pixPerUnit;
                Rhino.Display.RhinoViewport viewport = args.Viewport;
                viewport.GetWorldToScreenScale(location, out pixPerUnit);

                size = size / pixPerUnit;

                Rhino.Display.Text3d drawText = new Rhino.Display.Text3d(text, plane, size);
                
                args.Display.Draw3dText(drawText, args.WireColour);
                drawText.Dispose();
            }
        }
        
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.spurGear;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("a23c5eb6-43dd-4c86-bc7f-a9dc9ddeacab"); }
        }
    }
}
