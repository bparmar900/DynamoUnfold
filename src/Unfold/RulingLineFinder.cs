﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Runtime;
using Unfold;
using Unfold.Interfaces;

namespace Unfold
{
	public static class RulingLineFinder
	{
		//find max gaussiant curvature stepping through uvs with the specified divisions
		private static double maxGaussianCurvature(Surface surface, int udiv, int vdiv)
		{
			double max = double.NegativeInfinity;
			foreach (double u in Enumerable.Range(0, udiv).Select(i => i / udiv).ToList())
			{
				foreach (double v in Enumerable.Range(0, vdiv).Select(i => i / vdiv).ToList())
				{
					var curvature = Math.Abs(surface.GaussianCurvatureAtParameter(u, v));
					if (curvature > max)
					{
						max = curvature;
					}
				}
			}
			return max;
		}

		public static List<Tuple<Surface,Line>> FindingRulingLines(List<Surface> surfaces, double stepSize)
		{
			var rulingLines = new List<Line>();
			var surfacetoLineTuples = new List<Tuple<Surface, Line>>();
			foreach (var surface in surfaces)
			{
			//find principal curvature directions at this point
			var curvatureatmid = surface.PrincipalCurvaturesAtParameter(.5, .5);

			bool flipUV = false;
			//curvature is greater in the U dir
			if (Math.Abs(curvatureatmid[0]) > Math.Abs(curvatureatmid[1]))
			{
				flipUV = false;
			}
			//curvature greater in the V dir
			else
			{
				flipUV = true;
			}

			
			//TODO march from 0 to 1 by stepsize...might miss one, need to check
			double v = .5;
			double u = 0;
			//TODO change this stepping routine to instead use a stepsize based on curvature at last ruling line
			//we'll make this a while loop
			for (double steppos = 0; steppos <= 1; steppos += stepSize)
			{
				if (flipUV)
				{
					var temp = steppos;
					u = v;
					v = steppos;
				}
				else
				{
					u = steppos;
					
				}
				var rulingcoordsystem = surface.CurvatureAtParameter(u, v);
				Line lineToIntersect;
				var normal = surface.NormalAtParameter(u, v);
				if (flipUV)
				{
					//cross product of normal and curve direction
					var noncurvedir = rulingcoordsystem.YAxis.Cross(normal);
					if (noncurvedir.IsAlmostEqualTo(Vector.ByCoordinates(0, 0, 0)))
					{
						noncurvedir = surface.DerivativesAtParameter(u, v).First(); ;
					}
					lineToIntersect = Line.ByStartPointEndPoint(rulingcoordsystem.Origin.Add(noncurvedir.Scale(-100)),
						rulingcoordsystem.Origin.Add(noncurvedir.Scale(100)));
				}
				else
				{
					//cross product of normal and curve direction
					var noncurvedir = rulingcoordsystem.XAxis.Cross(normal);
					if (noncurvedir.IsAlmostEqualTo(Vector.ByCoordinates(0, 0, 0)))
					{
						noncurvedir = surface.DerivativesAtParameter(u, v)[1];
					}
					lineToIntersect = Line.ByStartPointEndPoint(rulingcoordsystem.Origin.Add(noncurvedir.Scale(-100)),
						rulingcoordsystem.Origin.Add(noncurvedir.Scale(100)));
				}

				rulingLines.Add(lineToIntersect);
				surfacetoLineTuples.Add(Tuple.Create(surface, lineToIntersect));
				}
			}
			return surfacetoLineTuples;

		}

		public static List<List<Geometry>> FindingRulingPatches(Surface surface, double stepSize)
		{
			//TODO need to make sure that we're aligned with the direction of curvature on this surface... 
			//will need to look at principal curvature directions max and step that way
			//find principal curvature directions at this point
			var curvatureatmid = surface.PrincipalCurvaturesAtParameter(.5, .5);

			bool flipUV = false;
			//curvature is greater in the U dir
			if (Math.Abs(curvatureatmid[0]) > Math.Abs(curvatureatmid[1]))
			{
				flipUV = false;
			}
			//curvature greater in the V dir
			else
			{
				flipUV = true;
			}

			var rulingLines = new List<Line>();
			var intersectedRulingLines = new List<List<Geometry>>();
			var filteredintersectedRulingLines = new List<Geometry>();
			var finalpatches = new List<List<Geometry>>();
			//TODO march from 0 to 1 by stepsize...might miss one, need to check
			double v = .5;
			double u = 0;
			//TODO change this stepping routine to instead use a stepsize based on curvature at last ruling line
			//we'll make this a while loop
			for (double steppos = 0; steppos < 1; steppos += stepSize)
			{
				if (flipUV)
				{
					var temp = steppos;
					u = v;
					v = steppos;
				}
				else
				{
					u = steppos;
				}
				var rulingcoordsystem = surface.CurvatureAtParameter(u, v);
				Line lineToIntersect;
				var normal = surface.NormalAtParameter(u, v);
				if (flipUV)
				{
					//cross product of normal and curve direction
					var noncurvedir = rulingcoordsystem.YAxis.Cross(normal);
					
					if (noncurvedir.IsAlmostEqualTo(Vector.ByCoordinates(0,0,0)))
					{
						noncurvedir = surface.DerivativesAtParameter(u, v).First(); ;
					}
					lineToIntersect = Line.ByStartPointEndPoint(rulingcoordsystem.Origin.Add(noncurvedir.Scale(-100)),
						rulingcoordsystem.Origin.Add(noncurvedir.Scale(100)));
				}
				else
				{

					var noncurvedir = rulingcoordsystem.XAxis.Cross(normal);
					if (noncurvedir.IsAlmostEqualTo(Vector.ByCoordinates(0, 0, 0)))
					{
						noncurvedir = surface.DerivativesAtParameter(u, v)[1];
					}
					lineToIntersect = Line.ByStartPointEndPoint(rulingcoordsystem.Origin.Add(noncurvedir.Scale(-100)),
						rulingcoordsystem.Origin.Add(noncurvedir.Scale(100)));
				}
				//intersec the very large rule with the surface
				//var intersectionResults = lineToIntersect.Intersect(surface);
				//we get a list of geometry results...if this is developable we'll get just one line
				//intersectedRulingLines.Add(intersectionResults.ToList());
				//filter this list of geo down to the first curvelike thing....
				// filteredintersectedRulingLines.Add(intersectionResults.Where(x => x is Curve || x is NurbsCurve || x is Line).First());
				rulingLines.Add(lineToIntersect);
				// if we have found our first ruling line then we can start finding polygons between
				// ruling lines
				if (rulingLines.Count > 1)
				{
					//get the last two intersected ruling lines
					var lineone = rulingLines[rulingLines.Count - 2];
					var linetwo = rulingLines[rulingLines.Count - 1];
					//draw a line from the center points of the two ruling lines, and then get the center of that line
					var vectorfromone2two = Vector.ByTwoPoints(((Line)lineone).PointAtParameter(.5), (((Line)linetwo).PointAtParameter(.5)));
					var pickpointone = ((Line)lineone).PointAtParameter(.5).Subtract(vectorfromone2two);
					var pickpointtwo = ((Line)linetwo).PointAtParameter(.5).Add(vectorfromone2two);
					try
					{
						var initialTrim = surface.Trim(lineone, pickpointone);
						var finalTrim = initialTrim.First().Trim(linetwo, pickpointtwo);

						finalpatches.Add(finalTrim.ToList());
					}
					catch
					{
						Console.WriteLine("some problem trimming");
					}
				}
			}
			return finalpatches;

		}

		public static List<List<Geometry>> RotateRulesToCeiling(Plane ceilingPlane, List<Surface> surfaces,double stepsize)
		{
			//algorithm draft
			//give a list of ruling lines found either adaptively or by some tolerance
			//take all rules, find the angle between the normal of the ruling line (or original srf along this ruling line) and the ceil plane
			//now rotate this ruling line up to the ceiling plane, it will be rotated around an axis
			//perpendicular to the normal at that rule, going through the vertex V of the cone, we can find V, 
			//by intersecting/extending multiple ruling lines... or more generally, consider the case where a developable
			//begins on the surface of a cone, but then continues off the cone into a cylinder, we can intersect the ruling line
			//with the ceiling plane to find the point that our rotation axis must pass through.
			
			//find some ruling lines, these are very long and will in most cases intersect with the supplied ceil plane

			List<Tuple<Surface,Line>> rules = FindingRulingLines(surfaces,stepsize);
			//TODO check that all rules intersect ceil plane

			//this will hold rules that lie on the surface
			var newRules = new List<List<Geometry>>();
			
			foreach (var tuple in rules)
			{
				var surface = tuple.Item1;
				var rule = tuple.Item2;	
				var ruleNormal = surface.NormalAtPoint(rule.PointAtParameter(.5));
				var ceilplaneNorm = ceilingPlane.Normal;
				
				Vector bxaCrossedNormals = ceilplaneNorm.Cross(ruleNormal);
				var s = (bxaCrossedNormals.Length) * -1.0;
				var planeRotOrigin = Plane.ByOriginNormal(rule.Intersect(ceilingPlane).First() as Point, bxaCrossedNormals);
				var c = ruleNormal.Dot(ceilplaneNorm) *-1.0;

				var radians = Math.Atan2(s, c);
				var degrees = radians * (180.0 / Math.PI);
				degrees = 180.0 - degrees;
# if DEBUG

				Console.WriteLine("about to to rotate" + degrees);
# endif

				var intersectionResults = rule.Intersect(surface).ToList();
				var rotatedRuling = intersectionResults.Select(x => x.Rotate(planeRotOrigin, degrees)).ToList();
				newRules.Add(rotatedRuling);

			}
			return newRules;

		}

	}
}

