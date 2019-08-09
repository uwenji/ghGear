using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

using System.Diagnostics;

namespace ghGear.Util
{
    class BevelGear: Gears
    {
        public List<List<NurbsCurve>> bevelProfiles;
        public List<List<Circle>> coneCircles;
        public List<List<Brep>> pitchCones;
        public List<Mesh> bevelMeshes;

        public List<NurbsCurve> outBevelProfiles;
        public List<Circle> profileCircles;
        public List<Brep> profileBreps;

        public BevelGear()
        {
            pitchCones = new List<List<Brep>>();
            coneCircles = new List<List<Circle>>();
            bevelProfiles = new List<List<NurbsCurve>>();
            profileCircles = new List<Circle>();
            profileBreps = new List<Brep>();
            bevelMeshes = new List<Mesh>();
            outBevelProfiles = new List<NurbsCurve>();
        }

        public void PitchCones(List<Circle> C, Polyline Poly, List<double> shifts, double Depth, List<double> Hole)
        {
            pitchCones = new List<List<Brep>>();
            coneCircles = new List<List<Circle>>();
            for (int i = 0; i < C.Count; i++)
            {
                if (Hole.Count <= i)
                    Hole.Add(Hole[Hole.Count - 1]);
            }
            #region Trigonometry
            var ccx = Rhino.Geometry.Intersect.Intersection.CurveCurve(new ArcCurve(C[0]), new ArcCurve(C[1]), 1.0, 1.0);
            Point3d pitch = ccx[0].PointA;
            /*[0]  b     [1] c _C_ a
             *    |\           \  |
             *   C| \A         A\ |B
             *    | _\           \|
             *   a  B  c = pitch = b
             */
            //Triangle: [0]_A, [1]_B = r, [2]_C = h, [3]_alpha = 90, [4]_beta, [5]_gamma 
            List<double> TA = Trigonometry(C[0].Radius, Math.PI * 0.5, C[0].Center.DistanceTo(Poly[1]), TrigLaw.SAS_B_alpha_C, Traingle.ALL);
            List<double> TB = Trigonometry(C[1].Radius, Math.PI * 0.5, C[1].Center.DistanceTo(Poly[1]), TrigLaw.SAS_B_alpha_C, Traingle.ALL);
            List<List<double>> TT = new List<List<double>> { TA, TB };

            double map = (TA[2] - Depth) / TA[2]; //TA[2] is C = Heigh
            List<double> ta = new List<double> { TA[0] * map, TA[1] * map, TA[2] * map, TA[3], TA[4], TA[5] }; //456 is angle
            List<double> tb = new List<double> { TB[0] * map, TB[1] * map, TB[2] * map, TB[3], TB[4], TB[5] };
            List<List<double>> tt = new List<List<double>> { ta, tb };
            //compute back cone angle
            //TCone: [0]_A = profile, [1]_B = shiftA, [2]_C = shiftB, [3]_a = sumBeta, [4]_b = c1A, [5]_r = c2A 
            double sumBeta = TA[4] + TB[4];
            List<double> TCone = Trigonometry(shifts[0], sumBeta, shifts[1], TrigLaw.SAS_B_alpha_C, Traingle.ALL);
            #endregion

            //cone surface
            
            for (int i = 0; i < C.Count; i++)
            {
                List<Circle> thisConeCircles = new List<Circle>();
                List<Brep> thisPitchCones = new List<Brep>();
                //lower circle
                Circle BC = new Circle(C[i].Plane, C[i].Radius - shifts[i]);
                double h = shifts[i] * 3 * Math.Sin(TCone[4]);
                double r = shifts[i] * 3 * Math.Cos(TCone[4]);
                Circle TC = new Circle(new Plane(C[i].Center + C[i].Plane.ZAxis * h, C[i].Plane.XAxis, C[i].Plane.YAxis), BC.Radius + r);

                Circle BChole = new Circle(BC.Plane, Hole[i]);
                thisConeCircles.Add(BC);
                thisConeCircles.Add(BChole);
                List<Curve> Circles = new List<Curve> { new ArcCurve(BC), new ArcCurve(TC) };
                thisPitchCones.Add(Brep.CreateFromLoft(Circles, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0]);

                //upper circle
                Plane hPln = new Plane(C[i].Center + (C[i].Plane.ZAxis * (TT[i][2] - tt[i][2])), C[i].Plane.XAxis, C[i].Plane.YAxis);
                double hshift = shifts[i] * map;
                Circle bc = new Circle(hPln, tt[i][1] - hshift);
                double hh = h * map;
                double hr = r * map;
                Circle tc = new Circle(new Plane((bc.Center + bc.Plane.ZAxis * hh), C[i].Plane.XAxis, C[i].Plane.YAxis), bc.Radius + hr);

                Circle bchole = new Circle(bc.Plane, Hole[i]);
                thisConeCircles.Add(bchole);
                thisConeCircles.Add(bc);
                List<Curve> circles = new List<Curve> { new ArcCurve(bc), new ArcCurve(tc) };
                thisPitchCones.Add(Brep.CreateFromLoft(circles, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0] );

                pitchCones.Add(thisPitchCones);
                coneCircles.Add(thisConeCircles);

                //getBevelParameterOut
                profileCircles.AddRange(thisConeCircles);
                profileBreps.AddRange(thisPitchCones);
            }
            
        }

        public void buildBevelGear(List<Circle> C, Polyline L, double Teeth, double Angle, double profileShift, double addendum, double dedendum, out List<int> teethNumber)
        {
            ArcCurve sC = new ArcCurve(C[0]);
            if (C.Count > 1)
                sC = smallestCircle(C);

            double m = (2 * sC.Radius) / Teeth;
            addendum = addendum * m;
            dedendum = dedendum * m;

            teethNumber = new List<int>();
            //each circle to gear
            outBevelProfiles = new List<NurbsCurve>();
            bevelProfiles = new List<List<NurbsCurve>>();
            
            for (int i = 0; i < C.Count; i++)
            {
                List<NurbsCurve> thisBevelProfiles = new List<NurbsCurve>();

                Circle baseCir = new Circle(C[i].Plane, C[i].Radius * Math.Cos(toRadian(Angle)));
                Circle addCir = new Circle(C[i].Plane, C[i].Radius + addendum);
                Circle dedCir = new Circle(C[i].Plane, C[i].Radius - dedendum);
                Curve profile = involute(baseCir, Angle, addCir, dedCir);

                #region build teeth

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
                #endregion

                //teeth
                mirProfile.Reverse();
                List<Curve> CS = new List<Curve> { profile, tip, mirProfile, buttom };
                Curve teethC = Curve.JoinCurves(CS, 0.1, true)[0];

                //bevel: teeth project to one tip find intersection on cone.
                NurbsCurve nurbsT = teethC.ToNurbsCurve();
                NurbsCurve nurbs_t = teethC.ToNurbsCurve();
                List<Point3d[]> rayPts = new List<Point3d[]>();

                //intersect with cone
                Line displayTip = new Line();
                for (int j = 0; j < nurbsT.Points.Count; j++)
                {
                    Point3d thisP = nurbsT.Points[j].Location;
                    Point3d lsx_A = Rhino.Geometry.Intersect.Intersection.RayShoot(new Ray3d(thisP, L[1] - thisP), new List<GeometryBase> { pitchCones[i][0] }, (int)thisP.DistanceTo(L[1]))[0];
                    Point3d lsx_B = Rhino.Geometry.Intersect.Intersection.RayShoot(new Ray3d(thisP, L[1] - thisP), new List<GeometryBase> { pitchCones[i][1] }, (int)thisP.DistanceTo(L[1]))[0];
                    Point3d[] lsx = new Point3d[2] { lsx_A, lsx_B };
                    rayPts.Add(lsx);

                    //========================display=====================
                    if (thisP == tip.PointAtStart)
                    {
                        displayTip.From = lsx[1];
                    }
                    if (thisP == tip.PointAtEnd)
                    {
                        displayTip.To = lsx[1];
                    }

                }
                //show upper gear tip
                locations.Add(displayTip.From); //tip
                texts.Add(Math.Round(displayTip.Length, 2).ToString());
                sizes.Add((addendum + dedendum) * 5);

                //teeth project to point on cone
                for (int j = 0; j < nurbsT.Points.Count; j++)
                {
                    nurbsT.Points.SetPoint(j, rayPts[j][0].X, rayPts[j][0].Y, rayPts[j][0].Z, nurbsT.Points[j].Weight);
                    nurbs_t.Points.SetPoint(j, rayPts[j][1].X, rayPts[j][1].Y, rayPts[j][1].Z, nurbs_t.Points[j].Weight);
                }

                //polar array
                List<Curve> Profiles = new List<Curve>();
                List<Curve> profiles = new List<Curve>();
                for (int j = 0; j < teethCount; j++)
                {
                    Curve presentProfile = nurbsT.DuplicateCurve();
                    Curve presentProfile_s = nurbs_t.DuplicateCurve();
                    double step = ((double)j / (double)teethCount) * Math.PI * 2;
                    Transform rotate = Transform.Rotation(step, C[i].Normal, C[i].Center);
                    presentProfile.Transform(rotate);
                    presentProfile_s.Transform(rotate);

                    Profiles.Add(presentProfile);
                    profiles.Add(presentProfile_s);
                }


                thisBevelProfiles.Add(Curve.JoinCurves(Profiles, 1, true)[0].ToNurbsCurve());
                thisBevelProfiles.Add(Curve.JoinCurves(profiles, 1, true)[0].ToNurbsCurve());

                //output
                bevelProfiles.Add(thisBevelProfiles);
                outBevelProfiles.AddRange(thisBevelProfiles);
                //debug
                //Debug.WriteLine("============================debug=============================");
                //Debug.WriteLine("bevelProfiles:" + bevelProfiles.Count.ToString() + "," + bevelProfiles[0].Count.ToString());
            }
            
        }

        public void LoftGearFromCurve()
        {
            bevelMeshes = new List<Mesh>();
            
            for (int i = 0; i < bevelProfiles.Count; i++) 
            {
                Mesh gear = new Mesh();
                for(int j = 0; j < bevelProfiles[i][0].Points.Count ; j++)//each mesh vertices
                {
                    Point3d thisP = bevelProfiles[i][0].Points[j].Location; //S
                    gear.Vertices.Add(thisP);
                    for (int k = 0; k < coneCircles[i].Count; k++)
                    {
                        thisP = coneCircles[i][k].ClosestPoint(thisP);
                        gear.Vertices.Add(thisP);
                    }
                    gear.Vertices.Add(bevelProfiles[i][1].Points[j].Location); //E
                }

                //Debug.WriteLine("============================debug=============================");
                //Debug.WriteLine(bevelProfiles[i][0].Points.Count.ToString());

                int V = 4 + 2;
                List<MeshFace> gearFaces = new List<MeshFace>();
                for (int k = 0; k < bevelProfiles[i][0].Points.Count-1; k++)
                {
                    
                    if (k < bevelProfiles[i][0].Points.Count - 1)
                    {
                        int _this = -1;
                        for (int j = 0; j < V -1; j++)
                        {
                            _this = k * V + j;
                            MeshFace thisF = new MeshFace(_this, _this + V, _this + V + 1, _this + 1);
                            gearFaces.Add(thisF);
                        }
                        _this = k * V + 5;
                        gearFaces.Add(new MeshFace(_this, _this + V, _this + 1, _this - V + 1));
                    }

                    else
                    {
                        //Debug.WriteLine(k.ToString());

                        for (int n = 0; n < V-1; n++)
                        {
                            int _this = k * V + n;
                            MeshFace thisF = new MeshFace(_this, n, n + 1, _this + 1);
                            gearFaces.Add(thisF);
                        }
                        gearFaces.Add(new MeshFace(k*V + V-1, V-1, 0, k+V));
                    }
                }
                gear.Faces.AddFaces(gearFaces);
                bevelMeshes.Add(gear);
            }
        }

    }
}
