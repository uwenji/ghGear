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
    public class helicalGear : GH_Component
    {
        List<string> texts = new List<string>();
        List<Point3d> locations = new List<Point3d>();
        List<double> sizes = new List<double>();
        List<Brep> Helical = new List<Brep>();

        List<Curve> Profiles = new List<Curve>();
        List<Circle> Pitches = new List<Circle>();
        double Angle;
        double Heigh;
        Boolean ifSolid;

        public helicalGear()
          : base("HelicalGear", "HelicalG",
              "Build helical gear from profile",
              "Gears", "Build")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Gear", "G", "Profile Gear", GH_ParamAccess.list);
            pManager.AddCircleParameter("Pitch", "C", "Pitch Circle", GH_ParamAccess.list);
            pManager.AddNumberParameter("Angle", "A", "Helical Angle(Degree)", GH_ParamAccess.item, 30.0);
            pManager.AddNumberParameter("Heigh", "H", "Gear Heigh", GH_ParamAccess.item, 20.0);
            pManager.AddBooleanParameter("Solid?", "s", "Solid? solid process could take slow", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Gear", "G", "Gear Surface or Solid Gear", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Util.Gears gear = new Util.Gears();
            Helical = new List<Brep>();
            Profiles = new List<Curve>();
            Pitches = new List<Circle>();

            DA.GetDataList<Curve>(0, Profiles);
            DA.GetDataList<Circle>(1, Pitches);
            DA.GetData<double>(2, ref Angle);
            DA.GetData<double>(3, ref Heigh);
            DA.GetData<Boolean>(4, ref ifSolid);
            int flip = 0;
            for (int i = 0; i < Profiles.Count; i++)
            {
                if(flip == 0)
                {
                    Helical.Add(gear.buildHelical(Profiles[i], Pitches[i], 90.0 - Angle, Heigh, ifSolid));
                    flip -= 1;
                }
                else
                {
                    Helical.Add(gear.buildHelical(Profiles[i], Pitches[i], 90.0 + Angle, Heigh, ifSolid));
                    flip += 1;
                }
            }

            DA.SetDataList(0, Helical);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.helicalGear;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("803FFE0A-580E-441A-9D1D-D1EF28780641"); }
        }
    }
}
