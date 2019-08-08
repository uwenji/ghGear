using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ghGear
{
    public class ghGearInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Gear";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return Properties.Resources.gearBMP;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Build Gears in Grasshopper.\n\r acknowledgement refernce: KHK, calculation of gear dimensions, Available at:https://khkgears.net/new/gear_knowledge/gear_technical_reference/calculation_gear_dimensions.html";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("11905f73-385c-4d38-8909-70aaca072485");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "You-Wen Ji";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "ohisyouwen.ji@gmail.com, github: github.com/uwenji";
            }
        }
    }

    public class GearCategoryIcon : Grasshopper.Kernel.GH_AssemblyPriority
    {
        public override Grasshopper.Kernel.GH_LoadingInstruction PriorityLoad()
        {
            Grasshopper.Instances.ComponentServer.AddCategoryIcon("Gears", Properties.Resources.gear);
            Grasshopper.Instances.ComponentServer.AddCategorySymbolName("Gears", 'G');
            return Grasshopper.Kernel.GH_LoadingInstruction.Proceed;
        }
    }
}
