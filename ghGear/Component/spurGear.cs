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
            pManager.AddNumberParameter("shift", "T", "Profile shift coefficient, from 0 to 0.5, default is 0.1", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("addendum", "ad", "addendum, 1.0 module", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("dedendum", "de", "dedendum, 1.25 module", GH_ParamAccess.item, 1.25);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Gears", "G", "Gears", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
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
