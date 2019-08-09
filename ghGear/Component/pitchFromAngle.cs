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
    public class pitchFromAngle : GH_Component
    {
        List<string> texts = new List<string>();
        List<Point3d> locations = new List<Point3d>();
        List<double> sizes = new List<double>();
        List<Circle> Pitches = new List<Circle>();
        Polyline Axe;

        Circle Base;
        double Radius, Angle, Location;

        public pitchFromAngle()
          : base("Angle2Pitch", "AnglePitch",
              "Make Pitch Circles from base Circle with angle and Radius",
              "Gears", "Utility")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCircleParameter("Circle", "C", "Base Circle", GH_ParamAccess.item);
            pManager.AddNumberParameter("Radius", "R", "Radius of Pitch Circle", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angle", "A", "Angle(Degree) between Base Circle Plane and Second Circle Plane", GH_ParamAccess.item, 90.0);
            pManager.AddNumberParameter("Location", "LA", "Pitch Circle Locate Base Circle in Angle(Degree)", GH_ParamAccess.item);
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

            DA.GetData<Circle>(0, ref Base);
            DA.GetData<double>(1, ref Radius);
            DA.GetData<double>(2, ref Angle);
            DA.GetData<double>(3, ref Location);
            
            Pitches.Add(Base);
            Pitches.Add(gear.buildPitchFromRadius(Base, Radius, Angle, Location, out Axe));

            DA.SetDataList(0, Pitches);
            DA.SetData(1, Axe);

            texts = gear.texts;
            locations = gear.locations;
            sizes = gear.sizes;
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                List<Point3d> points = new List<Point3d>();
                foreach (Circle C in Pitches)
                {
                    Point3d[] pbox = C.BoundingBox.GetCorners();
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
                return Properties.Resources.pitchFromAngle;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("966061C9-732F-4E51-AA8F-C6862684538E"); }
        }
    }
}
