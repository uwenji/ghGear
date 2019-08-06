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

        public bevelGear()
          : base("BevelGear", "BevelG",
              "Build Bevel Gear from Pitch Circles",
              "Gears", "Build")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Util.Gears gear = new Util.Gears();
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
