/*
 * MS	15-01-10	initial version
 * 
 * 
 * 
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace WebExample
{
	public class MyWebAPI : MS.Web.WebAPI
	{
		protected override bool ProcessRequest(HttpContext context, DateTime now, string view, string propertySet, string action, string eTag, out string timeStamp)
		{
			timeStamp = null;

			var fileName = context.Server.MapPath("~/App_Data/data.txt");
			var fi = new FileInfo(fileName);

			switch(view)
			{
				case "db":
					{
						switch(context.Request.HttpMethod)
						{
							case "GET":

								if(!String.IsNullOrEmpty(eTag) && eTag == fi.LastWriteTime.Ticks.ToString())
								{
									ReturnNotChanged(context);
									return true;
								}

								timeStamp = fi.LastWriteTime.Ticks.ToString();

								ReturnValue(context, new
								{
									Text = File.ReadAllText(fileName),
									LastWriteTime = fi.LastWriteTime

								}, timeStamp);

								return true;

							case "POST":

								if (action == "save")
								{
									File.WriteAllText(fileName, GetPostData<string>(context) ?? "");

									ReturnValue(context, true, null);
								}
								else
									throw new Exception("This action is not yet supported.");

								return true;
						}
					}

					break;
			}

			return false;
		}
	}
}