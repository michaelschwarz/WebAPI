/*
 * MS	13-10-12	initial version
 * 
 * 
 * 
 * 
 * 
 */
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Web;
using Newtonsoft.Json;

namespace MS.Web
{
	public partial class WebAPI : IHttpHandler
	{
		protected JsonSerializerSettings jsonSettings = null;

		public WebAPI()
			: base()
		{
			jsonSettings = new JsonSerializerSettings
			{
				DateTimeZoneHandling = DateTimeZoneHandling.Unspecified,
				Formatting = Newtonsoft.Json.Formatting.None
			};
		}

		#region Helper Methods

		/// <summary>
		/// Returns a DateTime from sortableDateTimePattern.
		/// </summary>
		/// <param name="sortableDate"></param>
		/// <returns></returns>
		protected DateTime GetDateTime(string sortableDate)
		{
			// TODO: date timezone handling correct?
			return DateTime.ParseExact(sortableDate, System.Globalization.DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern, System.Globalization.DateTimeFormatInfo.InvariantInfo);
		}

		/// <summary>
		/// Returns the posted request body deserialized to the specified type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context"></param>
		/// <returns></returns>
		protected T GetPostData<T>(HttpContext context)
		{
			using (StreamReader inputStream = new StreamReader(context.Request.InputStream))
			{
				return JsonConvert.DeserializeObject<T>(inputStream.ReadToEnd(), jsonSettings);
			};
		}

		/// <summary>
		/// Compress the output data.
		/// </summary>
		/// <param name="context"></param>
		protected void AddCompression(HttpContext context)
		{
			context.Response.Filter = new GZipStream(context.Response.Filter, CompressionMode.Compress);
			context.Response.AddHeader("Content-Encoding", "gzip");
		}

		#endregion

		public void ProcessRequest(HttpContext context)
		{
			var sw = Stopwatch.StartNew();

			context.Response.Clear();
			context.Response.Expires = 0;
			context.Response.Cache.SetCacheability(System.Web.HttpCacheability.ServerAndPrivate);
			context.Response.ContentType = "application/json";

			#region Output handling

			if (context.Request["output"] == "text")
			{
				context.Response.ContentType = "text/plain";
				jsonSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
			}

			if (context.Request["minimize"] == "true")
			{
				jsonSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
				jsonSettings.NullValueHandling = NullValueHandling.Ignore;
			}

			#endregion

			try
			{
				var eTag = context.Request.Headers["If-None-Match"];
				var action = context.Request["action"];
				var propertySet = context.Request["propertyset"];
				var view = context.Request["view"];
				var timeStamp = "";

				var now = DateTime.Now;

				#region TimeStamp handling

				if (eTag == null || !String.IsNullOrEmpty(context.Request.Headers["X-IgnoreTimeStamp"]))
					eTag = "";
				else if (eTag.StartsWith("\"")) eTag = eTag.Substring(1, eTag.Length - 2);

				if (!String.IsNullOrEmpty(eTag) && eTag.Contains("|"))
				{
					var parts = eTag.Split('|');
					eTag = parts[0];
				}

				#endregion

				#region Default Handling

				switch (view)
				{
					case "date":
						{
							var sb = new StringBuilder();

							ReturnValue(context, new
							{
								now = now

							}, timeStamp);
						}
				
						break;
				
					default:

						throw new NotImplementedException("The view '" + view + "' is not implemented.");
				}

				#endregion
			}
			catch (SqlException ex)
			{
				context.Response.Clear();
				context.Response.StatusCode = 400;
				context.Response.StatusDescription = ex.Message.Contains("\r") ? ex.Message.Substring(0, ex.Message.IndexOf('\r')) : ex.Message;
				context.Response.Write(JsonConvert.SerializeObject(new { error = new { Type = ex.GetType().FullName, Code = ex.ErrorCode, Message = ex.Message, LineNumber = ex.LineNumber, Number = ex.Number, Procedure = ex.Procedure } }, jsonSettings));
			}
			catch (NullReferenceException ex)
			{
				context.Response.Clear();
				context.Response.StatusCode = 400;
				context.Response.StatusDescription = ex.Message.Contains("\r") ? ex.Message.Substring(0, ex.Message.IndexOf('\r')) : ex.Message;
				context.Response.Write(JsonConvert.SerializeObject(new { error = new { Type = ex.GetType().FullName, Message = ex.Message, StackTrace = ex.StackTrace, Source = ex.Source } }, jsonSettings));
			}
			catch (FileNotFoundException ex)
			{
				context.Response.Clear();
				context.Response.StatusCode = 400;
				context.Response.StatusDescription = ex.Message.Contains("\r") ? ex.Message.Substring(0, ex.Message.IndexOf('\r')) : ex.Message;
				context.Response.Write(JsonConvert.SerializeObject(new { error = new { Type = ex.GetType().FullName, Message = ex.Message, FileName = ex.FileName.Substring(0, ex.FileName.Length - 20) } }, jsonSettings));
			}
			catch (ThreadAbortException)
			{ }
			catch (ThreadInterruptedException)
			{ }
			catch (Exception ex)
			{
				context.Response.Clear();
				context.Response.StatusCode = 400;
				context.Response.StatusDescription = ex.Message.Contains("\r") ? ex.Message.Substring(0, ex.Message.IndexOf('\r')) : ex.Message;
				context.Response.Write(JsonConvert.SerializeObject(new { error = new { Type = ex.GetType().FullName, Message = ex.Message, StackTrace = ex.StackTrace } }, jsonSettings));
			}
			finally
			{
				sw.Stop();
				context.Response.AddHeader("X-Duration", sw.ElapsedMilliseconds.ToString());
			}

			context.Response.End();
		}

		#region Common return data handling

		protected void ReturnNotChanged(HttpContext context)
		{
			context.Response.StatusCode = 304;
		}

		protected void ReturnValue(HttpContext context, object o, string timeStamp)
		{
			if (String.IsNullOrEmpty(timeStamp))
				context.Response.Write(JsonConvert.SerializeObject(new
				{
					value = o
				}, jsonSettings));
			else
			{
				context.Response.Cache.SetETag("\"" + timeStamp + "\"");
				context.Response.Write(JsonConvert.SerializeObject(new
				{
					value = o,
					timeStamp = timeStamp
				}, jsonSettings));
			}
		}

		#endregion

		public bool IsReusable
		{
			get
			{
				return true;			// We can return true as we have to private variables here
			}
		}
	}
}
