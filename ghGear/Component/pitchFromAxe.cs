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
    public class pitchFromAxe : GH_Component
    {
        List<string> texts = new List<string>();
        List<Point3d> locations = new List<Point3d>();
        List<double> sizes = new List<double>();
        List<Circle> Pitches = new List<Circle>();
        Polyline refAxe;
        Curve outAxe;

        Curve refC;
        double Radius;

        public pitchFromAxe()
          : base("Axe2Pitch", "AxePitch",
              "Make Pitch Circles from 3 Points base Polyline",
              "Gears", "Utility")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Axe", "P", "Three Points Polyline", GH_ParamAccess.item);
            pManager.AddNumberParameter("Radius", "R", "Radius of Base Circle (start point of polyline)", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCircleParameter("PitchCircle", "C", "Pitch Circles", GH_ParamAccess.list);
            pManager.AddCurveParameter("AxePolyline", "L", "Polyline of Axe", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Util.Gears gear = new Util.Gears();
            Pitches.Clear();
            
            DA.GetData<Curve>(0, ref refC);
            DA.GetData<double>(1, ref Radius);
            refC.TryGetPolyline(out refAxe);

            Pitches = gear.buildPitch(refAxe, Radius);
            outAxe = Curve.CreateInterpolatedCurve(new List<Point3d> { refAxe[0], refAxe[1], Pitches[1].Center }, 1);

            DA.SetDataList(0, Pitches);
            DA.SetData(1, outAxe);

            texts = gear.texts;
            locations = gear.locations;
            sizes = gear.sizes;
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                List<Point3d> points = new List<Point3d>();
                foreach(Circle C in Pitches)
                {
                    Point3d[] pbox = C.BoundingBox.GetCorners();
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
                return Properties.Resources.pitchFromAxe;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("5B78007B-2363-48F6-B566-6711F3641172"); }
        }
    }
}
