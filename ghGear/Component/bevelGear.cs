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
    public class bevelGear : GH_Component
    {
        public List<string> texts = new List<string>();
        public List<Point3d> locations = new List<Point3d>();
        public List<double> sizes = new List<double>();
        List<Curve> Gears = new List<Curve>();
        Brep[,] refCone;
        List<Mesh> GearMeshes = new List<Mesh>();
        List<double> Ratio = new List<double>();

        List<Circle> Circles = new List<Circle>();
        Polyline refAxe;
        Curve refC;
        double Teeth, Depth, Angle, shift, addendum, dedendum;
        bool showOpt;

        public bevelGear()
          : base("BevelGear", "BevelG",
              "Build Bevel Gear from Pitch Circles",
              "Gears", "Build")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCircleParameter("Circle", "C", "Pair Pitch Circles", GH_ParamAccess.list);
            pManager.AddCurveParameter("Polyline", "L", "Three points Polyline", GH_ParamAccess.item);
            pManager.AddNumberParameter("Teeth", "T", "Teeth Number", GH_ParamAccess.item);
            pManager.AddNumberParameter("Depth", "D", "Teeth Number", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angle", "A", "Pressure Angle (Degree)", GH_ParamAccess.item, 22.5);
            pManager.AddNumberParameter("shift", "S", "Profile shift coefficient, from 0 to 0.5, default is 0.1", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("addendum", "ad", "addendum, 1.0 module", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("dedendum", "de", "dedendum, 1.25 module", GH_ParamAccess.item, 1.25);
            pManager.AddBooleanParameter("option", "opt", "0= Curve, 1=surface, 2=mesh", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Gears", "G", "Gears", GH_ParamAccess.list);
            pManager.AddCurveParameter("Pitch", "P", "Pitch", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Ratio", "R", "Ratio", GH_ParamAccess.list);
            pManager.AddBrepParameter("RefCone", "rC", "reference cone", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Util.Gears gear = new Util.Gears();
            Circles.Clear();
            Gears.Clear();
            GearMeshes.Clear();
            Ratio.Clear();
            List<int> ratioInt = new List<int>();
            List<Circle> Pitch = new List<Circle>();

            DA.GetDataList<Circle>("Circle", Circles);
            DA.GetData<Curve>("Polyline", ref refC);
            DA.GetData<double>("Teeth", ref Teeth);
            DA.GetData<double>("Depth", ref Angle);
            DA.GetData<double>(4, ref Angle);
            DA.GetData<double>(5, ref shift);
            DA.GetData<double>(6, ref addendum);
            DA.GetData<double>(7, ref dedendum);
            DA.GetData<bool>("option", ref showOpt);
            refC.TryGetPolyline(out refAxe);
            
            //main code
            double m = 2 * gear.smallestCircle(Circles).Radius / Teeth;
            List<double> coneOffset = new List<double>();
            foreach(Circle thisC in Circles)
            {
                coneOffset.Add(2.5 * m);
            }
            ArcCurve[,] coneCircles = new ArcCurve[2, 2];
            refCone = gear.pitchCone(Circles, refAxe, coneOffset, Depth, Point3d.Unset, Point3d.Unset, out coneCircles);
            List<Curve[]> profiles = new List<Curve[]>();
            profiles = gear.buildBevelGear(Circles, refAxe, refCone, Teeth, Angle, shift, addendum, dedendum, out ratioInt);
            Ratio = new Util.GCD(ratioInt).getGCD();
            if (showOpt)
            {
                GearMeshes = new List<Mesh>();
                DA.SetDataList(0, GearMeshes);
            }
            else
            {
                foreach(Curve[] arrC in profiles)
                {
                    foreach(Curve thisC in arrC)
                    {
                        Gears.Add(thisC);
                    }
                }
                DA.SetDataList(0, Gears);
                
            }
            
            Pitch = Circles;
            List<Brep> cones = new List<Brep> { refCone[0, 0], refCone[0, 1], refCone[1, 1], refCone[1, 1] };
            DA.SetDataList(1, Pitch);
            DA.SetDataList(2, Ratio);
            DA.SetDataList(3, cones);

            texts = gear.texts;
            locations = gear.locations;
            sizes = gear.sizes;
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                List<Point3d> points = new List<Point3d>();
                foreach (Curve thisC in Gears)
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

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);
            /*
            args.Display.DrawBrepWires(refCone[0, 0], args.WireColour);
            args.Display.DrawBrepWires(refCone[0, 1], args.WireColour);
            args.Display.DrawBrepWires(refCone[1, 0], args.WireColour);
            args.Display.DrawBrepWires(refCone[1, 1], args.WireColour);
            */
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {

                return Properties.Resources.bevelGear;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("3362F94A-B2DB-475A-A319-7BE6943C0831"); }
        }
    }
}
