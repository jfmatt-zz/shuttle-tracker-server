using System;
using System.Collections.Generic;

namespace Tracker {
	public class TrackerHandler : System.Web.IHttpHandler {
	
		public void ProcessRequest (System.Web.HttpContext context) {
			if (context.Request.RequestType.Equals("get",System.StringComparison.OrdinalIgnoreCase))
				doGet(context);
			else if (context.Request.RequestType.Equals("post",System.StringComparison.OrdinalIgnoreCase))
				doPost(context);
		}
	
		public void doGet(System.Web.HttpContext context) {
			
			context.Response.ContentType = "text/xml";
			
			Global.xmlLock.EnterReadLock();
			try {
				context.Response.Write(Global.getInfo());
			} finally { Global.xmlLock.ExitReadLock(); }
		
		}
	
		public void doPost(System.Web.HttpContext context) {
	
			String longString = context.Request.Form["bus-tracker-longitude"];
			String latString  = context.Request.Form["bus-tracker-latitude"];
			String timeString = context.Request.Form["bus-tracker-timestamp"];
			String idString   = context.Request.Form["bus-tracker-id"];

			//Check that all fields are present
			if (longString == null || latString == null || idString == null) {
				context.Response.StatusCode = 400; 	//Bad request
				return;
			}
			
			double longitude;
			double latitude;
			long timestamp;
			int id;
			
			//Check that all fields are in proper format
			if (!double.TryParse(longString, out longitude) || !double.TryParse(latString, out latitude) 
				|| !int.TryParse(idString, out id) || !long.TryParse(timeString, out timestamp)) {
				context.Response.StatusCode = 415;	//Unsupported media
				return;
			}
			
//			int userLastStop;
			context.Response.ContentType = "text/plain";

			context.Response.Write("ok");
			
			Global.userLock.EnterWriteLock();
			try {
	//			if (users.Try
			} finally { Global.userLock.ExitWriteLock(); }
	
		}
	
		public bool IsReusable {
			get {
				return false;
			}
		}
	
	}
}