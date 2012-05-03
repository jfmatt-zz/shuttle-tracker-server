using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Tracker
{

	public class BusInfo {
		
			public int count;
		
			public List<Bus> Buses;
			
			public BusInfo() { count = 0; Buses = new List<Bus>();  }
			
			public void add(Bus b) {
				Buses.Add(b);
				count++;
			}
			
			public void clear() {
				Buses.Clear();
			}
		
	}
	
	public class StopInfo {
		
		public int count;
		
//		[XmlIgnore]
		public ValueSerializableDictionary<int,Stop> stops;
		
		public StopInfo() {
			stops = new ValueSerializableDictionary<int,Stop>();
			for (int n = 0; n < Stop.STOP_IDS.Length; n++) {
				Stop s = new Stop(Stop.STOP_LONGS[n], Stop.STOP_LATS[n],
					Stop.STOP_NAMES[n], Stop.STOP_IDS[n], Stop.STOP_MINHEADS[n], Stop.STOP_MAXHEADS[n]);
				s.busesComing = 0;	//Start with no buses coming to this stop

				s.travelTimes = new SerializableDictionary<int, List<long>>(); //time to get TO this stop FROM each other
				foreach (int id in Stop.STOP_IDS) {
					s.travelTimes.Add (id, new List<long>());
				}
				this.count++;
				stops.Add (Stop.STOP_IDS[n],s);
			}
		}
		
		public int getStop(Bus b) {
			int closestStop = 0;
			double minDistance = double.PositiveInfinity;
			double thisDistance = 0;
			foreach (Stop st in stops.Values) {
				//If going the wrong way, skip
				if (b.heading < st.minHeading || b.heading > st.maxHeading)
					continue;
				
				//Check if nearby				
				if ((thisDistance = Stop.segmentPointDistance(
					Global.latitudeToFeet(b.lastLatitude),
					Global.longitudeToFeet(b.lastLongitude),
					Global.latitudeToFeet(b.latitude),
					Global.longitudeToFeet(b.longitude),
					Global.latitudeToFeet(st.latitude),
					Global.longitudeToFeet(st.longitude)
						)) <= minDistance) {
					
					closestStop = st.id;
					minDistance = thisDistance;
				}
			}
			
			if ((minDistance <= Global.DISTANCE_THRESHOLD)
				&& (
							(b.route == 0) 
						|| ((b.route == 1) && (Array.IndexOf(Stop.BLUE_ROUTE, closestStop) != -1))
						|| ((b.route == 2) && (Array.IndexOf(Stop.RED_ROUTE,  closestStop) != -1))
				)
			)
				return closestStop;
			else
				return 0;
		}

		
	}
	
	[XmlRootAttribute("TrackerInfo", Namespace="AUBusTracker",IsNullable=false)]
	public class CombinedInfo {
		[XmlElement("BusInfo")]
		public BusInfo busInfo;
		
		[XmlElement("StopInfo")]
		public StopInfo stopInfo;
		
		public long timestamp;
		
		public CombinedInfo() {
			timestamp = 0;
			busInfo = new BusInfo();
			stopInfo = new StopInfo();
		}
	}

}

