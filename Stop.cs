using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Tracker
{
	public class Stop
	{
		
		public static double segmentPointDistance(double startx, double starty, double endx, double endy, double px, double py) {
			double dx = endx - startx;	//Line segment vector
			double dy = endy - starty;
			double d = Math.Sqrt(dx*dx+dy*dy);
			double ux = dx/d;	//Unit vector
			double uy = dy/d;
			double cx = px - startx;	//vector from start to center of circles
			double cy = py - starty;
			double proj = cx*ux + cy*uy;	//Dot product gives length of projection

			if (proj <= 0)	//Start point is closest point
				return Math.Sqrt(cx*cx + cy+cy);
			if (proj >= d) {	//End point is closest point
				double mindx = endx - px;
				double mindy = endy - py;
				return Math.Sqrt(mindx*mindx + mindy*mindy);
			}
			else {	//Closest point is somewhere in between
				double closex = proj * ux + startx;	//|proj| * u = proj
				double closey = proj * uy + starty;
				double mindx = px - closex;
				double mindy = py - closey;
				return Math.Sqrt(mindx*mindx + mindy*mindy);
			}
		}
		
		
		public static readonly int[] STOP_IDS = {1,2,3,4,5,6,8,9,-3,-4,-5,-7,-9};
		public static readonly int[] RED_ROUTE = {8,9,3,4,5,6,-5,-4,-3,-9,8};
		public static readonly int[] BLUE_ROUTE = {1,2,3,4,5,6,-5,-4,-3,-7,1};
		public static readonly double[] STOP_LATS = {38.935204,38.938373,38.938985,38.943341,38.945713,38.948096,38.944829,38.938424,38.938534,38.942874,38.945213,38.936897,38.938879};
		public static readonly double[] STOP_LONGS = {-77.090913,-77.088017,-77.084873,-77.081161,-77.079262,-77.078753,-77.09435,-77.086751,-77.085366,-77.081703,-77.079735,-77.086837,-77.087126};
		public static readonly string[] STOP_NAMES = {"Southside", "Northside", "Nebraska N", "Nebraska S", "Van Ness N", "Van Ness S", "Tenley Campus N", "Tenley Campus S", "Metro", "Ward", "WCL", "Katzen N", "Katzen S"};
		public static readonly int[] STOP_MINHEADS = {  0,  0,  0, 180,  0, 180,  0, 180,   0, 180,   0,  90, 270};
		public static readonly int[] STOP_MAXHEADS = {360, 90, 90, 270, 90, 270, 90, 270, 360, 270, 360, 180,   0};
		
//		public static Dictionary<int,Stop> stops;

		public static int getNextStop(int stopNumber, int route) {
			//This requires that the route listings be circular (first stop is also last) 
			//but otherwise have no repeats
			int index;
			if (route == 1) {
				index = Array.IndexOf(BLUE_ROUTE, stopNumber);
				return BLUE_ROUTE[index + 1];
			}
			if (route == 2) {
				index = Array.IndexOf(RED_ROUTE, stopNumber);
				return RED_ROUTE[index + 1];
			}
			if (route == 0) {
				//Nebraska part is shared
				if (stopNumber == 3 || stopNumber == 4 || stopNumber == -5 || stopNumber == -4)
					return stopNumber + 1;
			}
			
			//Can't tell
			return 0;

		}
		
		//Prediction info
//		[XmlIgnore]
		public int busesComing;	//how many are in the segment before this stop
		[XmlIgnore]
		public SerializableDictionary<int, List<long> > travelTimes;	//how long it's taken to go from one stop to another
		public static readonly int RECORD_QUEUE_LENGTH = 5;

		//Single stop info
		public double longitude;
		public double latitude;
		public string name;
		public int id;
		public int minHeading;
		public int maxHeading;
		public long timeToNextBus;
		
		public Stop() {}
		
		public Stop (double lo, double la, string na, int i, int minh, int maxh)
		{
			longitude = lo;
			latitude = la;
			name = na;
			id = i;
			minHeading = minh;
			maxHeading = maxh;
		}
		
		//Mark new travel time coming in from bus
		public void logTime(int stopFrom, long time) {
			travelTimes[stopFrom].Add (time);
			while (travelTimes[stopFrom].Count > RECORD_QUEUE_LENGTH)
				travelTimes[stopFrom].RemoveAt(0);
		}
	
	}
}
