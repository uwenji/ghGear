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
    public class BevelGearComponent : GH_Component
    {
        public List<string> texts = new List<string>();
        public List<Point3d> locations = new List<Point3d>();
        public List<double> sizes = new List<double>();
        List<GeometryBase> Gears = new List<GeometryBase>();
        List<double> Ratio = new List<double>();

        List<Circle> Circles = new List<Circle>();
        List<double> Holes = new List<double>();
        Polyline refAxe;
        Curve refC;
        double Teeth, Depth, Angle, shift, addendum, dedendum;
        bool showOpt;

        public BevelGearComponent()
          : base("BevelGear", "BevelG",
              "Build Bevel Gear from Pitch Circles",
              "Gears", "Build")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCircleParameter("Circle", "C", "Pair of Pitch Circles", GH_ParamAccess.list);
            pManager.AddCurveParameter("Polyline", "L", "Three points Polyline", GH_ParamAccess.item);
            pManager.AddNumberParameter("Teeth", "T", "Teeth Number", GH_ParamAccess.item);
            pManager.AddNumberParameter("Depth", "D", "Depth", GH_ParamAccess.item);
            pManager.AddNumberParameter("HoleSize", "H", "Hole size in daimeter", GH_ParamAccess.list);
            pManager.AddNumberParameter("Angle", "A", "Pressure Angle (Degree)", GH_ParamAccess.item, 22.5);
            pManager.AddNumberParameter("shift", "S", "Profile shift coefficient, from 0 to 0.5, default is 0.1", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("addendum", "ad", "addendum, default 0.95", GH_ParamAccess.item, 0.95);
            pManager.AddNumberParameter("dedendum", "de", "dedendum, default 1.25", GH_ParamAccess.item, 1.25);
            pManager.AddBooleanParameter("option", "opt", "0= Curve, 1=surface, 2=mesh", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Gears", "G", "Gears", GH_ParamAccess.list);
            pManager.AddCircleParameter("Pitch", "P", "Pitch", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Ratio", "R", "Ratio", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Holes.Clear();
            Circles.Clear();
            Util.BevelGear gear = new Util.BevelGear();
            Gears.Clear();
            Circles.Clear();
            Ratio.Clear();
            List<int> ratioInt = new List<int>();

            DA.GetDataList<Circle>(0, Circles);
            DA.GetData<Curve>(1, ref refC);
            DA.GetData<double>(2, ref Teeth);
            DA.GetData<double>(3, ref Depth);
            DA.GetDataList<double>(4, Holes);
            DA.GetData<double>(5, ref Angle);
            DA.GetData<double>(6, ref shift);
            DA.GetData<double>(7, ref addendum);
            DA.GetData<double>(8, ref dedendum);
            DA.GetData<bool>(9, ref showOpt);
            refC.TryGetPolyline(out refAxe);

            //=============main code=============
            double m = 2 * gear.smallestCircle(Circles).Radius / Teeth;  //module

            //pitchCone
            List<double> coneOffset = new List<double>();
            for(int i = 0; i < Circles.Count; i++)
            {
                coneOffset.Add(5 * m);
            }
            
            gear.PitchCones(Circles, refAxe, coneOffset, Depth, Holes);

            //build bevel gear profile
            gear.buildBevelGear(Circles, refAxe, Teeth, Angle, shift, addendum, dedendum, out ratioInt);

            //ratio
            Ratio = new Util.GCD(ratioInt).getGCD();

            //from section to mesh
            gear.LoftGearFromCurve();


            //pass to output, G is profiles or 
            if (showOpt)
            {
                DA.SetDataList(0, gear.bevelMeshes);
            }
            else
            {
                DA.SetDataList(0, gear.outBevelProfiles);
            }

            
            DA.SetDataList(1, gear.profileCircles);
            DA.SetDataList(2, Ratio);

            //display
            texts = gear.texts;
            locations = gear.locations;
            sizes = gear.sizes;
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

                return Properties.Resources.bevelGear;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("F9463E56-75E2-4CB3-B00F-79DE51D0D311"); }
        }
    }
}
