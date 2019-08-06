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
                return Properties.Resources.gear;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Build Gears in Grasshopper";
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
}
