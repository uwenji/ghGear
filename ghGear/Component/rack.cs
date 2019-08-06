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
    public class rack : GH_Component
    {
        List<string> texts = new List<string>();
        List<Point3d> locations = new List<Point3d>();
        List<double> sizes = new List<double>();
        List<Curve> Rack = new List<Curve>();

        List<System.Object> LModules = new List<System.Object>();
        List<Line> Tangents = new List<Line>();
        double Teeth;
        double Angle;
        double addendum;
        double dedendum;

        public rack()
          : base("Rack", "Rack",
              "Build 2D rack from tangent line with circle or module",
              "Gears", "Build")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Line", "L", "Base Line", GH_ParamAccess.list);
            pManager.AddGenericParameter("CircleModule", "C/M", "Tangent Circle or Module", GH_ParamAccess.list);
            pManager.AddNumberParameter("Teeth", "T", "Teeth number", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angle", "A", "pressure angle(Degree), default is 22.5 and range should 15 to 35", GH_ParamAccess.item, 22.5);
            pManager.AddNumberParameter("addendum", "ad", "addendum, 1.0 module", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("dedendum", "de", "dedendum, 1.25 module", GH_ParamAccess.item, 1.25);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Gears", "G", "Gears", GH_ParamAccess.list);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Util.Gears gear = new Util.Gears();
            LModules = new List<System.Object>();
            Tangents = new List<Line>();
            Rack = new List<Curve>();

            DA.GetDataList<Line>(0, Tangents);
            DA.GetDataList<System.Object>(1, LModules);
            DA.GetData<double>(2, ref Teeth);
            DA.GetData<double>(3, ref Angle);
            DA.GetData<double>(4, ref addendum);
            DA.GetData<double>(5, ref dedendum);

            for (int i = 0; i < Tangents.Count; i++)
            {
                System.Object obj = LModules[0];
                if (LModules.Count - 1 < i)
                    obj = LModules[LModules.Count - 1];
                else
                    obj = LModules[i];
                double n;
                Circle c = new Circle();
                if (GH_Convert.ToCircle(obj, ref c, GH_Conversion.Both))
                {
                    Rack.Add(gear.buildRack(Tangents[i], c, Teeth, Angle, addendum, dedendum));
                }
                if (GH_Convert.ToDouble(obj, out n, GH_Conversion.Primary))
                    Rack.Add(gear.buildRack(Tangents[i], n, Teeth, Angle, addendum, dedendum));
            }


            DA.SetDataList(0, Rack);

            texts = gear.texts;
            locations = gear.locations;
            sizes = gear.sizes;
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                List<Point3d> points = new List<Point3d>();
                foreach (Curve thisC in Rack)
                {
                    Point3d[] pbox = thisC.GetBoundingBox(false).GetCorners();
                    foreach (Point3d thisP in pbox)
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

                return Properties.Resources.rack;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("9BC1CE52-F18A-41C5-90E7-ADD2F0E46AEC"); }
        }
    }
}
