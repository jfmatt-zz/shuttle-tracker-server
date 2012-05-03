using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Threading;
using System.Net;

namespace Tracker
{
	
	public class Global : System.Web.HttpApplication
	{
		
		public static readonly double DISTANCE_THRESHOLD = 30;

		public static DateTime EPOCH;
		
		//Utilities
		public static double longitudeToFeet(double val) {
			return val * 280571;
		}
		public static double latitudeToFeet(double val) {
			return val * 364280;
		}
		public static string getIntSegment(string str){
			int len=0;
			for (int i = 0; i< str.Length; i++) {
				if (Char.IsDigit(str[i]))
					len++;
				else
					if (len > 0) return str.Substring(i-len,len);
			}
			if (len > 0) return str.Substring(str.Length - len, len);
			return null;
		}

		public static long unixTime(DateTime dt) {
			return (long)(dt - EPOCH).TotalMilliseconds;
		}
		
		public static int getRoute(int stopNumber) {
			switch(stopNumber) {
			//Red-only stops
			case 1: return 1;
			case 2: return 1;
			case -7: return 1;
			//Blue-only stops
			case 8: return 2;
			//Otherwise, can't tell
			default: return 0;
			}
		}

		public static long average(IEnumerable<long> list) {
			long total = 0;
			long count = 0;
			
			foreach (long t in list) {
				total = total + t;
				count++;
			}
			
			if (count == 0) return 0;
			return total / count;
		}
		
		public static String getInfo() {
			return swrapper.getStr();
		}

		//Multithreaded data
		public static ReaderWriterLockSlim xmlLock;
		public static StringWrapper swrapper;
		
		public static ReaderWriterLockSlim userLock;
		public static Dictionary<int,int> users;

		private static Updater updater;
		
		//Init
		protected virtual void Application_Start (Object sender, EventArgs e)
		{
			EPOCH = new DateTime(1970,1,1);

			//Stop.init();
			
			userLock = new ReaderWriterLockSlim();
			users = new Dictionary<int, int>();
			
			xmlLock = new ReaderWriterLockSlim();
			swrapper = new StringWrapper();
			
			updater = new Updater(swrapper);
			updater.start();
		}
		
		public static void kill() {
			updater.stop ();
		}
		
		protected virtual void Application_End (Object sender, EventArgs e)
		{
			kill();
		}
	
	}
	
	//Because I don't know how to deal with pointers in C#
	public class StringWrapper {
		private volatile String s;
		public StringWrapper() {s = "";}
		public void setStr(String str) {s = str;}
		public String getStr() { return s; }
	}
	
	//Thread wrapper that behaves like Java Runnables
	public class Updater {

		//Updater info
		private static readonly int UPDATE_INTERVAL = 5000;
		private static readonly String GPS_URL = "http://busdata.streeteagleweb.com/Service.svc";
		private static readonly String REQUEST_XML =
		 				  "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
						+ "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">"
						+ "<s:Body>"
						+ "<Get_BusesByVehicleIdString xmlns=\"http://tempuri.org/\">"
						+ "<strUserName>americanuniversityBB</strUserName>"
						+ "<lngCustID>808</lngCustID>"
						+ "<strVehicleIdString>1,2,3,4,5,6,7,8,9</strVehicleIdString>"
						+ "</Get_BusesByVehicleIdString>"
						+ "</s:Body>"
						+ "</s:Envelope>";
		private static readonly String SOAP_PROPERTY = "SOAPAction";
		private static readonly String SOAP_VALUE = "\"http://tempuri.org/IService/Get_BusesByVehicleIdString\"";
		
		//Bus Info
		private CombinedInfo info;
		private String idNumbers;
		
		private XmlSerializer serializer;
		private StringWrapper sw;
		
		//Timing functions
		private Thread thread;
		private volatile bool _abort;
		private int counter;
		private long lastCycle;

		public Updater(StringWrapper wrapper) {

			//Bus info
			info = new CombinedInfo();
			idNumbers = "";
			for (int i = Bus.MIN_BUS_ID; i <= Bus.MAX_BUS_ID; i++) {
				idNumbers += "" + i + ",";
				info.busInfo.add(new Bus());
			}
			idNumbers += Bus.MAX_BUS_ID;
			idNumbers = "1,2,3,4,5,6,7,8,9";
			Console.WriteLine (idNumbers);
			
			//Info passing
			sw = wrapper;
			serializer = new XmlSerializer(typeof(CombinedInfo));
			
			//Thread
			thread = new Thread(this.run);
			_abort = false;
			counter = 0;
		}
		
		public void start() {
			thread.Start();
		}
		
		public void run() {
			while (!_abort) {
				//Start stopwatch
				lastCycle = DateTime.UtcNow.Ticks;
				Console.Write("Updating..." + counter++ + "\n");
				info.timestamp = Global.unixTime(DateTime.Now);

				
				/////////////////////////////////////////////////
				///
				///Update bus info
				///
				/////////////////////////////////////////////////
				
				//Send request to StreetEagle
				HttpWebRequest req = (HttpWebRequest) WebRequest.Create(new Uri(GPS_URL));
				req.Method = "POST";
				req.ContentType = "text/xml; encoding=UTF-8";
				req.Accept = "*/*";
				req.Headers.Add(SOAP_PROPERTY,SOAP_VALUE);
				
				using(StreamWriter sw = new StreamWriter(req.GetRequestStream())) {
					sw.Write(String.Format(REQUEST_XML, idNumbers));
					sw.Close();
				}
				
				//Get response
				HttpWebResponse resp;
				try {
					resp = (HttpWebResponse) req.GetResponse();
				} catch(WebException w) {
					Console.WriteLine ("Error getting StreetEagle data!");
					Console.WriteLine(w.ToString());
					resp = (HttpWebResponse) w.Response;
					//continue;
					
				}

				//Load XML
				XmlDocument doc = new XmlDocument();
				string xmlFromOutside = (new StreamReader(resp.GetResponseStream())).ReadToEnd();
				doc.LoadXml(xmlFromOutside);
				XPathNavigator nav = doc.CreateNavigator();
				
				//Find all buses
				XPathExpression busXpr = nav.Compile("//*[local-name()='List_Bus']");
				XPathNodeIterator busItr = (XPathNodeIterator)nav.Evaluate(busXpr);
				
				//For each bus
				foreach (XPathNavigator n in busItr) {
					//Get id number
					int id = int.Parse(Global.getIntSegment(n.SelectSingleNode("./*[local-name()='VehicleID']").Value));
					Bus bus = info.busInfo.Buses[id - Bus.MIN_BUS_ID];
					
					//Get last update time
					long time = Global.unixTime(DateTime.Parse(n.SelectSingleNode("./*[local-name()='DateTimeLoc']").Value));
					//Skip this bus if it hasn't changed (this will happen a LOT)
					if (bus.lastUpdated == time) continue;
					else bus.lastUpdated = time;
					
					//Check if running - can ignore the rest if not
					bus.running = !n.SelectSingleNode("./*[local-name()='Status']").Value.Equals("Parked");
					if (!bus.running) {
						bus.lastLatitude = 0;
						bus.lastLongitude = 0;
						bus.lastStop = 0;
						bus.nextStop = 0;
						continue;					
					}
					
					//Update location
					bus.lastLatitude = bus.latitude;
					bus.lastLongitude = bus.longitude;
					bus.latitude = double.Parse(n.SelectSingleNode("./*[local-name()='Latitude']").Value);
					bus.longitude = double.Parse(n.SelectSingleNode("./*[local-name()='Longitude']").Value);
					bus.heading = int.Parse(n.SelectSingleNode("./*[local-name()='Heading']").Value);
					bus.address = n.SelectSingleNode("./*[local-name()='Address']").Value;
					bus.name = n.SelectSingleNode("./*[local-name()='Name']").Value;
					if (bus.lastStop != 0)
						bus.timeSinceLastStop = time - bus.stopCheckInTimes[bus.lastStop];
					
					//Update stop
					if (bus.lastLatitude != 0) {
						//If this is not the first update for this bus, look for a stop
						int stopAt = info.stopInfo.getStop(bus);
						if (stopAt != 0 && stopAt != bus.lastStop) {
							//Got to a stop
							bus.lastStop = stopAt;
														
							//Notify travel times
							if (bus.route == 1) {
								foreach (int stopId in Stop.BLUE_ROUTE) {
									if (bus.stopCheckInTimes[stopId] != 0) //0 means haven't been there yet
										info.stopInfo.stops[bus.lastStop].logTime(
											stopId, time - bus.stopCheckInTimes[stopId]);

								}
							}
							else if (bus.route == 2) {
								foreach (int stopId in Stop.RED_ROUTE) {
									if (bus.stopCheckInTimes[stopId] != 0) //0 means haven't been there yet
										info.stopInfo.stops[bus.lastStop].logTime(
											stopId, time - bus.stopCheckInTimes[stopId]);										
								}
																								
							
							}
							
							//Here we are now, entertain us
							bus.stopCheckInTimes[stopAt] = time; 
							bus.timeSinceLastStop = 0; //Time since last being at this stop is now 0
							
						}//at stop
					}

					//Update route if this stop allows differentiation
					bus.setRoute (Global.getRoute (bus.lastStop));

					//Predict next stop
					if (bus.lastStop != 0) {
						int cachedNextStop = bus.nextStop;
						bus.nextStop = Stop.getNextStop(bus.lastStop,bus.route);
						if (bus.nextStop != cachedNextStop) {
							
							if (cachedNextStop != 0 && bus.nextStop != 0) {
								//Update what segments they're in
//								info.stopInfo.stops[bus.nextStop].busesComing += 1;
//								info.stopInfo.stops[cachedNextStop].busesComing -= 1;
								
							}							
							
						}//next stop changed
					}//predictions
				}//for each bus in XML

				//Find out how long until a bus arrives at each stop
				//Time to get there is the time from the last stop to this destination
				//	(as determined by averages)
				//	minus how long it's been since the bus left the last stop
				foreach (int stopId in info.stopInfo.stops.Keys) {
					long bestTime = long.MaxValue;
					foreach (Bus bus in info.busInfo.Buses) {
						//Check that the stop and bus are on the same route
						if (bus.lastStop == 0 || bus.route == 0) continue; //new bus
						if (bus.route == 1 && Array.IndexOf(Stop.BLUE_ROUTE,stopId) == -1) continue; //wrong route
						if (bus.route == 2 && Array.IndexOf(Stop.RED_ROUTE,stopId) == -1) continue; //wrong route
						if (info.stopInfo.stops[stopId].travelTimes[bus.lastStop].Count == 0) continue; //no estimate
						
						long timeToStop = (long)Global.average(info.stopInfo.stops[stopId].travelTimes[bus.lastStop])
											- bus.timeSinceLastStop;
								
						if (timeToStop <= bestTime)
							bestTime = timeToStop;
					}
					
					info.stopInfo.stops[stopId].timeToNextBus = bestTime;
				}
				
				
				//Write info to string - which is then shared, so that this only happens once per update
				using (StringWriter writer = new StringWriter()) {
					serializer.Serialize(writer, info);
					Global.xmlLock.EnterWriteLock();
					try {
						//Only one quick pointer-switch while the lock is held... oh yeah
						sw.setStr(writer.ToString());
//						sw.setStr(xmlFromOutside);
//						sw.setStr(writer.ToString() + xmlFromOutside);
					}finally { Global.xmlLock.ExitWriteLock(); }
				}
				
				//Wait for next update phase
				if (UPDATE_INTERVAL > 0)
					while ((DateTime.UtcNow.Ticks - lastCycle) / 10000 < UPDATE_INTERVAL)
						Thread.Sleep(500);
			}
		}
		
		//Should be called from an outside thread - will wait until the local thread ends
		public void stop() {
			_abort = true;
			thread.Join();
		}
		
	}
	
	

	
}
