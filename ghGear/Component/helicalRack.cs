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
    public class helicalRack : GH_Component
    {
        List<string> texts = new List<string>();
        List<Point3d> locations = new List<Point3d>();
        List<double> sizes = new List<double>();
        List<Brep> HelicalRacks = new List<Brep>();

        List<System.Object> LModules = new List<System.Object>();
        List<Line> Tangents = new List<Line>();
        double Teeth, Angle, betaAngle, addendum, dedendum;
        double Depth;
        public helicalRack()
          : base("HelicalRack", "HelicalR",
              "Build helical rack from Line with Circle or Module",
              "Gears", "Build")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Line", "L", "Base Line", GH_ParamAccess.list);
            pManager.AddGenericParameter("CircleModule", "C/M", "Tangent Circle or Module", GH_ParamAccess.list);
            pManager.AddNumberParameter("Teeth", "T", "Teeth number", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angle", "A", "pressure angle(Degree), default is 22.5 and range should 15 to 35", GH_ParamAccess.item, 22.5);
            pManager.AddNumberParameter("HelicalAngle", "β", "Helical Surface Angle(Degree)", GH_ParamAccess.item, 40.0);
            pManager.AddNumberParameter("Depth", "D", "Rack Depth", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("addendum", "ad", "addendum, 1.0 module", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("dedendum", "de", "dedendum, 1.25 module", GH_ParamAccess.item, 1.25);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Gear", "G", "Gear Surface or Solid Gear", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            Util.Gears gear = new Util.Gears();
            LModules.Clear();
            Tangents.Clear();
            HelicalRacks.Clear();

            DA.GetDataList<Line>("Line", Tangents);
            DA.GetDataList<System.Object>("CircleModule", LModules);
            DA.GetData<double>("Teeth", ref Teeth);
            DA.GetData<double>("Angle", ref Angle);
            DA.GetData<double>("HelicalAngle", ref betaAngle);
            DA.GetData<double>("Depth", ref Depth);
            DA.GetData<double>("addendum", ref addendum);
            DA.GetData<double>("dedendum", ref dedendum);
            
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
                    HelicalRacks.Add(gear.buildHelicalRack(Tangents[i], c, Teeth, Angle, betaAngle, Depth, addendum, dedendum));
                }
                if (GH_Convert.ToDouble(obj, out n, GH_Conversion.Primary))
                    HelicalRacks.Add(gear.buildHelicalRack(Tangents[i], n, Teeth, Angle, betaAngle, Depth, addendum, dedendum));
            }
            

            DA.SetDataList(0, HelicalRacks);

            texts = gear.texts;
            locations = gear.locations;
            sizes = gear.sizes;
            
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                List<Point3d> points = new List<Point3d>();
                foreach (Brep B in HelicalRacks)
                {
                    Point3d[] pbox = B.GetBoundingBox(false).GetCorners();
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
                return Properties.Resources.helicalRack;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("7E393642-DCBF-4316-A2BF-609B3DBBE8FC"); }
        }
    }
}
