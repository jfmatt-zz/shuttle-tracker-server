using System;
using System.Web;
using System.Web.UI;

namespace Tracker
{
	public class Kill : System.Web.IHttpHandler
	{
		
		public bool IsReusable {
			get {
				return false;
			}
		}
		
		public void ProcessRequest (HttpContext context)
		{
			Global.kill();
			context.Response.ContentType = "text/plain";
			context.Response.Write ("App killed.");
			
		}
	}
}

