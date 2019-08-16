using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace ghGear.Component
{
    public class RatechPawl : GH_Component
    {
        
        public RatechPawl()
          : base("Ratchet&Pawl", "Ratchet",
              "Create ratchet and inside pawl",
              "Gears", "Build")
        {
        }

        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCircleParameter("Circle", "C", "Circle normal direction determine teeth direction is clockwise or counterclockwise.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Teeth", "T", "Teeth number", GH_ParamAccess.item, 12);
            pManager.AddNumberParameter("TeethDeth", "D", "Teeth depth", GH_ParamAccess.item, 2.0);
            pManager.AddIntegerParameter("PawlNumber", "pNum", "Pawl number", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("PawlRadius", "pRad", "Pawl plate circle offset from tooth tip", GH_ParamAccess.item, 0.2);
            pManager.AddNumberParameter("PawlThickness", "pThick", "Thichness of pawl", GH_ParamAccess.item, 1.0);
        }

        
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Ratchet", "RG", "Ratchet", GH_ParamAccess.item);
            pManager.AddCurveParameter("Pawl", "P", "[Optional] Pawl made for elastic material", GH_ParamAccess.item);
            pManager.AddCurveParameter("PawlCover", "C", "[Optional] Cover for pawl", GH_ParamAccess.item);
        }

        Circle C = new Circle();
        int ratchCount, pawlNum;
        double depth, pawlRadius, pawlThickness;
        Curve Pawl;
        bool needPawl;
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData<Circle>(0, ref C);
            DA.GetData<int>(1, ref ratchCount);
            DA.GetData<double>(2, ref depth);
            DA.GetData<int>(3, ref pawlNum);
            DA.GetData<double>(4, ref pawlRadius);
            DA.GetData<double>(5, ref pawlThickness);
            
            DA.SetData(0, Util.Gears.RatchetPawl(C, ratchCount, depth, pawlNum, pawlRadius, pawlThickness, out Pawl));
            DA.SetData(1, Pawl);
            DA.SetData(2, Util.Gears.RatchetCover(C, depth, pawlRadius));

        }

        
        protected override System.Drawing.Bitmap Icon
        {
            get
            {

                return Properties.Resources.ratchet;
            }
        }

        
        public override Guid ComponentGuid
        {
            get { return new Guid("c9f79de0-487a-4cb3-8d6b-47b90d4e8502"); }
        }
    }
}