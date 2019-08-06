using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace ghGear.Util
{
    class Gears
    {
        public List<string> texts = new List<string>();
        public List<Point3d> locations = new List<Point3d>();
        public List<double> sizes = new List<double>();
        public List<double> radius = new List<double>();
        //end declare

        public Curve involute(Circle baseCircle, double Angle, Circle max, Circle min)
        {
            List<Point3d> points = new List<Point3d>();
            double r = baseCircle.Radius;
            for (double i = 0; i < 500; i += 2)
            {
                double t = toRadian(i);
                double x = r * (Math.Cos(t) + t * Math.Sin(t));
                double y = r * (Math.Sin(t) - t * Math.Cos(t));
                Point3d p = new Point3d(x, y, 0);
                points.Add(p);
                if (p.DistanceTo(new Point3d(0, 0, 0)) > max.Radius) break;
            }
            Curve profile = Curve.CreateInterpolatedCurve(points, 3);
            profile = profile.Extend(CurveEnd.Start, (baseCircle.Radius - min.Radius) * 2, CurveExtensionStyle.Smooth);
            ArcCurve maxCircle = new ArcCurve(max);
            ArcCurve minCircle = new ArcCurve(min);
            Transform xform = Transform.PlaneToPlane(Plane.WorldXY, baseCircle.Plane);
            profile.Transform(xform);

            var ccxA = Rhino.Geometry.Intersect.Intersection.CurveCurve(profile, minCircle, 0.1, 0.1);
            var ccxB = Rhino.Geometry.Intersect.Intersection.CurveCurve(profile, maxCircle, 0.1, 0.1);
            List<double> ts = new List<double> { ccxA[0].ParameterA, ccxB[0].ParameterA };
            return profile.Split(ts)[1];

        }

        public List<Circle> buildPitch(Polyline poly, double bR)
        {
            List<Circle> pitchs = new List<Circle>();

            Line LA = poly.SegmentAt(0);
            Line LB = new Line(poly[2], poly[1]);
            Vector3d VN = Vector3d.CrossProduct(LA.Direction, LB.Direction); VN.Unitize();//Xaxis
            Vector3d VA = Vector3d.CrossProduct(LA.Direction, VN); VA.Unitize();// CA Y-Axis
            Vector3d VB = Vector3d.CrossProduct(LB.Direction, VN); VB.Unitize();// CB Y-Axis

            //circles
            Circle cA = new Circle(new Plane(LA.From, VN, VA), bR); // circle A
            Point3d pPt = LA.From + VA * bR;
            Point3d cBo = LB.ClosestPoint(pPt, false);
            double secR = cBo.DistanceTo(pPt);
            Circle cB = new Circle(new Plane(cBo, VN, VB), secR); // circle B
            pitchs.Add(cA); pitchs.Add(cB);
            return pitchs;
        }

        public Circle buildPitchFromRadius(Circle C, double bevelR, double pA, double rA, out Polyline Axe)
        {
            Point3d pP = C.PointAt(toRadian(rA)); //pitch point
            Vector3d nV = pP - C.Center; nV.Unitize(); //pitch circle normal
            Vector3d rAxe = Vector3d.CrossProduct(C.Normal, nV);
            nV.Rotate(toRadian(pA), rAxe);
            Vector3d pitch2Cen = Vector3d.CrossProduct(rAxe, -nV); pitch2Cen.Unitize();
            Plane pln = new Plane(pP + pitch2Cen * bevelR, -pitch2Cen, -rAxe);

            //show axes
            double tA, tB;
            Line lA = new Line(C.Center, C.Normal);
            Line lB = new Line(pln.Origin, pln.ZAxis);
            var ccx = Rhino.Geometry.Intersect.Intersection.LineLine(lA, lB, out tA, out tB, 1, false);
            Point3d LLX = lA.PointAt(tA); 
            Axe = new Polyline(new List<Point3d> { C.Center, LLX, pln.Origin });
            return new Circle(pln, bevelR);
        }

        public List<Curve> buildGear(List<Circle> C, double Teeth, double Angle, double profileShift, double addendum, double dedendum, out List<int> teethNumber)
        {
            List<Curve> curves = new List<Curve>();
            ArcCurve sC = new ArcCurve(C[0]);
            if (C.Count > 1)
                sC = smallestCircle(C);

            double m = (2 * sC.Radius) / Teeth;
            addendum = addendum * m;
            dedendum = dedendum * m;
            
            teethNumber = new List<int>();
            for (int i = 0; i < C.Count; i++)
            {
                List<Curve> profiles = new List<Curve>();
                Circle baseCir = new Circle(C[i].Plane, C[i].Radius * Math.Cos(toRadian(Angle)));
                Circle addCir = new Circle(C[i].Plane, C[i].Radius + addendum);
                Circle dedCir = new Circle(C[i].Plane, C[i].Radius - dedendum);
                Curve profile = involute(baseCir, Angle, addCir, dedCir);

                int teethCount = (int)(Teeth * (C[i].Radius / sC.Radius)); // teeth number
                teethNumber.Add(teethCount);
                //mirror profile
                Vector3d newX = C[i].Plane.XAxis;
                //the thickness of gear, should be halt of pitch angle
                if (profileShift != 0)
                    newX.Rotate((Math.PI + profileShift * Math.PI * Math.Tan(Angle)) / (double)teethCount, C[i].Normal); //circular pitch
                else
                    newX.Rotate(Math.PI / teethCount, C[i].Normal);
                Curve mirProfile = profile.DuplicateCurve();
                Transform mirror = Transform.Mirror(new Plane(C[i].Center, newX + C[i].Plane.XAxis, C[i].Normal));
                mirProfile.Transform(mirror);

                //align the profile
                var ccx = Rhino.Geometry.Intersect.Intersection.CurveCurve(profile, new ArcCurve(C[i]), 1, 1);
                Transform aligned = Transform.Rotation(ccx[0].PointA - C[i].Center, C[i].Plane.XAxis, C[i].Center);
                profile.Transform(aligned);
                mirProfile.Transform(aligned);

                //tip and buttom arc
                double addAngle = Vector3d.VectorAngle(profile.PointAtEnd - C[i].Center, mirProfile.PointAtEnd - C[i].Center);
                double dedAngle = (1.0 / (double)teethCount) * Math.PI * 2 - Vector3d.VectorAngle(profile.PointAtStart - C[i].Center, mirProfile.PointAtStart - C[i].Center);
                Plane addPln = new Plane(C[i].Center, profile.PointAtEnd - C[i].Center, C[i].Plane.YAxis);
                Plane dedPln = new Plane(C[i].Center, mirProfile.PointAtStart - C[i].Center, C[i].Plane.YAxis);
                ArcCurve tip = new ArcCurve(new Arc(addPln, C[i].Radius + addendum, addAngle));
                ArcCurve buttom = new ArcCurve(new Arc(dedPln, C[i].Radius - dedendum, dedAngle));

                //display
                locations.Add(tip.PointAtNormalizedLength(0.5)); //tip
                texts.Add(Math.Round(tip.GetLength(), 2).ToString());
                sizes.Add((addendum + dedendum) * 5);
                locations.Add(profile.PointAtStart); //profile
                texts.Add(Math.Round(profile.GetLength(), 2).ToString());
                sizes.Add((addendum + dedendum) * 5);

                //teeth
                mirProfile.Reverse();
                List<Curve> CS = new List<Curve> { profile, tip, mirProfile, buttom };
                Curve teethC = Curve.JoinCurves(CS, 0.1, true)[0];
                for (int j = 0; j < teethCount; j++)
                {
                    Curve presentProfile = teethC.DuplicateCurve();
                    double step = ((double)j / (double)teethCount) * Math.PI * 2;
                    Transform rotate = Transform.Rotation(step, C[i].Normal, C[i].Center);
                    presentProfile.Transform(rotate);

                    profiles.Add(presentProfile);
                }

                curves.Add(Curve.JoinCurves(profiles, 0.01, true)[0]);
            }
            return curves;
        }

        public Curve buildRack(Line L, double M, double Teeth, double Angle, double addendum, double dedendum)
        {
            double Pitch = Math.PI * M;
            int time = (int)(L.Length / Pitch);

            Vector3d dir = L.Direction; dir.Unitize();
            Vector3d dirA = dir;
            dirA.Rotate(toRadian(90.0 - Angle), Vector3d.ZAxis);
            Vector3d dirB = dir;
            dirB.Rotate(toRadian(-90.0 + Angle), Vector3d.ZAxis);

            addendum = addendum * M;
            dedendum = dedendum * M;

            double slope = (addendum + dedendum) / Math.Cos(toRadian(Angle));
            double tip = (Pitch * 0.5) - Math.Tan(Angle) * addendum * 2;
            double buttom = (Pitch * 0.5) - Math.Tan(Angle) * dedendum;

            List<Curve> profiles = new List<Curve>();
            profiles.Add(new LineCurve(new Line(L.From, dirA * slope)));
            profiles.Add(new LineCurve(new Line(profiles[0].PointAtEnd, dir * tip)));
            profiles.Add(new LineCurve(new Line(profiles[1].PointAtEnd, dirB * slope)));
            profiles.Add(new LineCurve(new Line(profiles[2].PointAtEnd, dir * buttom)));

            Transform move = Transform.Translation(Vector3d.CrossProduct(dir, Vector3d.ZAxis) * dedendum);
            Curve rack = Curve.JoinCurves(profiles, 0.01, true)[0];
            rack.Transform(move);

            //Display
            locations.Add(rack.PointAtNormalizedLength(0.5));
            texts.Add(Math.Round(tip, 2).ToString());
            sizes.Add(Pitch * 4);

            List<Curve> racks = new List<Curve>();
            for (int i = 0; i < time + 1; i++)
            {
                Transform duplicate = Transform.Translation(dir * Pitch * i);
                Curve thisC = rack.DuplicateCurve();
                thisC.Transform(duplicate);
                racks.Add(thisC);
            }

            return Curve.JoinCurves(racks, 1, true)[0];
        }

        public Curve buildRack(Line L, Circle C, double Teeth, double Angle, double addendum, double dedendum)
        {
            double M = (2 * C.Radius) / Teeth;
            double Pitch = Math.PI * M;
            int time = (int)(L.Length / Pitch);

            Vector3d dir = L.Direction; dir.Unitize();
            Vector3d dirA = dir;
            dirA.Rotate(toRadian(90.0 - Angle), C.Normal);
            Vector3d dirB = dir;
            dirB.Rotate(toRadian(-90.0 + Angle), C.Normal);

            addendum = addendum * M;
            dedendum = dedendum * M;

            double slope = (addendum + dedendum) / Math.Cos(toRadian(Angle));
            double tip = (Pitch * 0.50) - Math.Tan(Angle) * addendum * 2.0;
            double buttom = (Pitch * 0.5) - Math.Tan(Angle) * dedendum;

            List<Curve> profiles = new List<Curve>();
            profiles.Add(new LineCurve(new Line(L.From, dir * buttom * 0.5)));
            profiles.Add(new LineCurve(new Line(profiles[0].PointAtEnd, dirA * slope)));
            profiles.Add(new LineCurve(new Line(profiles[1].PointAtEnd, dir * tip)));
            profiles.Add(new LineCurve(new Line(profiles[2].PointAtEnd, dirB * slope)));
            profiles.Add(new LineCurve(new Line(profiles[3].PointAtEnd, dir * buttom * 0.5)));

            Transform move = Transform.Translation(Vector3d.CrossProduct(dir, C.Normal) * dedendum);
            Curve rack = Curve.JoinCurves(profiles, 0.01, true)[0];
            rack.Transform(move);

            //Display
            locations.Add(rack.PointAtNormalizedLength(0.5));
            texts.Add(Math.Round(tip, 2).ToString());
            sizes.Add(Pitch * 4);

            List<Curve> racks = new List<Curve>();
            for (int i = 0; i < time + 1; i++)
            {
                Transform duplicate = Transform.Translation(dir * Pitch * i);
                Curve thisC = rack.DuplicateCurve();
                thisC.Transform(duplicate);
                racks.Add(thisC);
            }

            //align
            var ccx = Rhino.Geometry.Intersect.Intersection.CurveCurve(new LineCurve(L), new ArcCurve(C), 0.1, 0.1);
            double align_d = dedendum * Math.Tan(toRadian(Angle)) + tip + addendum / Math.Tan(toRadian(90 - Angle)) * 2 + buttom * 0.5;
            align_d += Math.Floor(ccx[0].PointA.DistanceTo(L.From + dir * align_d) / Pitch) * Pitch;
            Point3d closet = L.From + dir * align_d;
            Transform align = Transform.Translation(ccx[0].PointA - closet);

            rack = Curve.JoinCurves(racks, 1, true)[0];
            rack.Transform(align);
            return rack;
        }

        public Brep buildHelicalRack(Line L, double M, double Teeth, double Angle, double HAngle, double H, double addendum, double dedendum)
        {
            double Pitch = Math.PI * M;
            int time = (int)(L.Length / Pitch);

            Vector3d dir = L.Direction; dir.Unitize();
            Vector3d dirA = dir;
            dirA.Rotate(toRadian(90.0 - Angle), Vector3d.ZAxis);
            Vector3d dirB = dir;
            dirB.Rotate(toRadian(-90.0 + Angle), Vector3d.ZAxis);

            addendum = addendum * M;
            dedendum = dedendum * M;

            double slope = (addendum + dedendum) / Math.Cos(toRadian(Angle));
            double tip = (Pitch * 0.5) - Math.Tan(Angle) * addendum * 2;
            double buttom = (Pitch * 0.5) - Math.Tan(Angle) * dedendum;

            List<Curve> profiles = new List<Curve>();
            profiles.Add(new LineCurve(new Line(L.From, dirA * slope)));
            profiles.Add(new LineCurve(new Line(profiles[0].PointAtEnd, dir * tip)));
            profiles.Add(new LineCurve(new Line(profiles[1].PointAtEnd, dirB * slope)));
            profiles.Add(new LineCurve(new Line(profiles[2].PointAtEnd, dir * buttom)));

            Transform move = Transform.Translation(Vector3d.CrossProduct(dir, Vector3d.ZAxis) * dedendum);
            Curve rack = Curve.JoinCurves(profiles, 0.01, true)[0];
            rack.Transform(move);

            //Display
            locations.Add(rack.PointAtNormalizedLength(0.5));
            texts.Add(Math.Round(tip, 2).ToString());
            sizes.Add(Pitch * 4);

            List<Curve> racks = new List<Curve>();
            for (int i = 0; i < time + 1; i++)
            {
                Transform duplicate = Transform.Translation(dir * Pitch * i);
                Curve thisC = rack.DuplicateCurve();
                thisC.Transform(duplicate);
                racks.Add(thisC);
            }

            //build helical
            double hShift = H / (Math.Tan(toRadian(90.0 - HAngle)) * 2 * Math.PI); //helical shift
            double hTime = Math.Ceiling(hShift / Pitch);
            Transform shift = Transform.Translation(dir * hShift);
            Transform zH = Transform.Translation(Vector3d.ZAxis * H);

            List<Curve> helicals = new List<Curve>();
            if (hTime / Math.Abs(hTime) == 1)
            {
                for (int i = -1; i < time + 1; i++)
                {
                    Transform duplicate = Transform.Translation(dir * Pitch * i);
                    Curve thisC = rack.DuplicateCurve();
                    thisC.Transform(duplicate);
                    helicals.Add(thisC);
                }
            }
            else
            {
                for (int i = 0; i < time + 2; i++)
                {
                    Transform duplicate = Transform.Translation(dir * Pitch * i);
                    Curve thisC = rack.DuplicateCurve();
                    thisC.Transform(duplicate);
                    helicals.Add(thisC);
                }
            }
            Curve helical = Curve.JoinCurves(helicals, 1, true)[0];

            rack = Curve.JoinCurves(racks, 1, true)[0];
            helical.Transform(shift);
            helical.Transform(zH);

            List<Curve> sections = new List<Curve> { rack, helical };
            Brep profile = Brep.CreateFromLoft(sections, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0];

            return profile;
        }

        public Brep buildHelicalRack(Line L, Circle C, double Teeth, double Angle, double HAngle, double H, double addendum, double dedendum)
        {
            double M = (2 * C.Radius) / Teeth;
            double Pitch = Math.PI * M;
            int time = (int)(L.Length / Pitch);

            Vector3d dir = L.Direction; dir.Unitize();
            Vector3d dirA = dir;
            dirA.Rotate(toRadian(90.0 - Angle), C.Normal);
            Vector3d dirB = dir;
            dirB.Rotate(toRadian(-90.0 + Angle), C.Normal);

            addendum = addendum * M;
            dedendum = dedendum * M;

            double slope = (addendum + dedendum) / Math.Cos(toRadian(Angle));
            double tip = (Pitch * 0.50) - Math.Tan(Angle) * addendum * 2.0;
            double buttom = (Pitch * 0.5) - Math.Tan(Angle) * dedendum;

            List<Curve> profiles = new List<Curve>();
            profiles.Add(new LineCurve(new Line(L.From, dir * buttom * 0.5)));
            profiles.Add(new LineCurve(new Line(profiles[0].PointAtEnd, dirA * slope)));
            profiles.Add(new LineCurve(new Line(profiles[1].PointAtEnd, dir * tip)));
            profiles.Add(new LineCurve(new Line(profiles[2].PointAtEnd, dirB * slope)));
            profiles.Add(new LineCurve(new Line(profiles[3].PointAtEnd, dir * buttom * 0.5)));

            Transform move = Transform.Translation(Vector3d.CrossProduct(dir, C.Normal) * dedendum);
            Curve rack = Curve.JoinCurves(profiles, 0.01, true)[0];
            rack.Transform(move);

            //Display
            locations.Add(rack.PointAtNormalizedLength(0.5));
            texts.Add(Math.Round(tip, 2).ToString());
            sizes.Add(Pitch * 4);

            //build helical
            //(H / Math.Tan(toRadian(Deg))) / (C.Radius * 2 * Math.PI) / Math.PI *2 * (C.Radius * 2 * Math.PI)
            double hShift = H / (Math.Tan(toRadian(90.0 - HAngle)) * 2 * Math.PI); //helical shift
            double hTime = Math.Ceiling(hShift / Pitch);
            Transform shift = Transform.Translation(dir * hShift);
            Transform zH = Transform.Translation(C.Normal * H);

            List<Curve> racks = new List<Curve>();
            List<Curve> helicals = new List<Curve>();
            if (hTime / Math.Abs(hTime) == 1)
            {
                // base rack
                for (int i = -1; i < time + 1; i++)
                {
                    Transform duplicate = Transform.Translation(dir * Pitch * i);
                    Curve thisC = rack.DuplicateCurve();
                    thisC.Transform(duplicate);
                    racks.Add(thisC);
                }
                // helical
                for (int i = -1; i < time + 1; i++)
                {
                    Transform duplicate = Transform.Translation(dir * Pitch * i);
                    Curve thisC = rack.DuplicateCurve();
                    thisC.Transform(duplicate);
                    helicals.Add(thisC);
                }
            }
            else
            {
                // base rack
                for (int i = 0; i < time + 2; i++)
                {
                    Transform duplicate = Transform.Translation(dir * Pitch * i);
                    Curve thisC = rack.DuplicateCurve();
                    thisC.Transform(duplicate);
                    racks.Add(thisC);
                }
                //helical
                for (int i = 0; i < time + 2; i++)
                {
                    Transform duplicate = Transform.Translation(dir * Pitch * i);
                    Curve thisC = rack.DuplicateCurve();
                    thisC.Transform(duplicate);
                    helicals.Add(thisC);
                }
            }
            Curve helical = Curve.JoinCurves(helicals, 1, true)[0];

            //align
            var ccx = Rhino.Geometry.Intersect.Intersection.CurveCurve(new LineCurve(L), new ArcCurve(C), 0.1, 0.1);
            double align_d = dedendum * Math.Tan(toRadian(Angle)) + tip + addendum / Math.Tan(toRadian(90 - Angle)) * 2 + buttom * 0.5;
            align_d += Math.Floor(ccx[0].PointA.DistanceTo(L.From + dir * align_d) / Pitch) * Pitch;
            Point3d closet = L.From + dir * align_d;
            Transform align = Transform.Translation(ccx[0].PointA - closet);

            rack = Curve.JoinCurves(racks, 1, true)[0];
            rack.Transform(align);
            helical.Transform(align);
            helical.Transform(shift);
            helical.Transform(zH);

            List<Curve> sections = new List<Curve> { rack, helical };
            Brep profile = Brep.CreateFromLoft(sections, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0];

            return profile;
        }

        public Brep buildHelical(Curve Sectioin, Circle C, double Deg, double H, bool Solid)
        {
            double angle = (H / Math.Tan(toRadian(Deg))) / (C.Radius * 2 * Math.PI);
            List<Curve> gears = new List<Curve>();
            double step = 3;
            for (int i = 0; i < (int)step + 1; i++)
            {
                Curve thisC = Sectioin.DuplicateCurve();
                Transform rotate = Transform.Rotation(angle * (double)i / step, C.Normal, C.Center);
                Transform move = Transform.Translation(C.Normal * H * (double)i / step);
                thisC.Transform(rotate);
                thisC.Transform(move);
                gears.Add(thisC);
            }
            Brep profile = Brep.CreateFromLoft(gears, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0];
            if (Solid)
            {
                Brep top = Brep.CreatePlanarBreps(gears[0], 1)[0];
                Brep buttom = Brep.CreatePlanarBreps(gears[(int)step], 1)[0];
                List<Brep> breps = new List<Brep> { buttom, profile, top };
                return Brep.CreateSolid(breps, 1.0)[0];
            }
            else
            {
                return profile;
            }
        }
        double toRadian(double Angle)
        {
            return ((Angle % 360.0) / 360.0) * Math.PI * 2;
        }

        ArcCurve smallestCircle(List<Circle> C)
        {
            double r = -1;
            int id = -1;
            for (int i = 0; i < C.Count; i++)
            {
                if (id == -1) r = C[i].Radius;
                if (r >= C[i].Radius)
                {
                    r = C[i].Radius;
                    id = i;
                }
            }
            return new ArcCurve(C[id]);
        }

        //show ratio
        public class GCD
        {
            public double[] numbers;
            public GCD(List<double> elements)
            {
                numbers = new double[elements.Count];
                for (int i = 0; i < elements.Count; i++)
                {
                    numbers[i] = (int)elements[i];
                }
            }
            public double gcd(double a, double b)
            {
                if (a == 0)
                    return b;
                return gcd(b % a, a);
            }
            public String findGCD()
            {
                String ratio = "";
                double result = numbers[0];
                int id = 0;
                for (int i = 0; i < numbers.Length; i++)
                {
                    if (numbers[i] < result) { result = numbers[i]; id = i; }
                }

                for (int i = 0; i < numbers.Length; i++)
                {
                    if (i < numbers.Length - 1)
                    {
                        result = gcd(numbers[i], result);
                        ratio += (numbers[i] / result).ToString();
                        ratio += ":";
                    }
                    else
                    {
                        result = gcd(numbers[i], result);
                        ratio += (numbers[i] / result).ToString();
                    }
                }
                return ratio;
            }
        }
    }
}
