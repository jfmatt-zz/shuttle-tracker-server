using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Tracker
{
	
	public class Bus {
		
		//=================================================
		//===                Constants                  ===
		//=================================================
		
		public static readonly int MIN_BUS_ID = 1;
		public static readonly int MAX_BUS_ID = 9;
		
		//=================================================
		//===                 Members                   ===
		//=================================================

		[XmlAttribute]
		public String name;
		
		[XmlIgnore]
		public bool running;
	
		[XmlElement("running")]
		public string runningPretty{
			get{return running ? "1":"0";}
			set{running = XmlConvert.ToBoolean(value);}
		}
		
		public long lastUpdated;
		
		public int route;
		public double latitude;
		public double longitude;
	
		public double lastLatitude;
		public double lastLongitude;
	
		public int heading;
		public int lastStop;
		public int nextStop;
		public long timeSinceLastStop;
		public String status;
		public String address;
		
		[XmlIgnore]
		public SerializableDictionary<int,long> stopCheckInTimes;
		
		//=================================================
		//===                 Methods                   ===
		//=================================================
	
		public Bus() {
			stopCheckInTimes = new SerializableDictionary<int, long>();
			foreach (int id in Stop.STOP_IDS) {
				stopCheckInTimes.Add (id,0L);	//Start out with not having visited any stops
			}
		}
		
		public void setRoute(int r) {
			if (r == this.route || r == 0) return;
			
			this.route = r;
			
			//Wipe out saved trip times, as they don't apply to this route
			this.stopCheckInTimes = new SerializableDictionary<int, long>();
			foreach (int id in Stop.STOP_IDS) {
				stopCheckInTimes.Add (id,0L);
			}
		}
		
	}
}