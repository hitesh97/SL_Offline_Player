using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using Microsoft.Web.Media.SmoothStreaming;
using Path = System.IO.Path;

namespace DemoPlayer
{
	public class ISO_StorageCache : ISmoothStreamingCache
	{
		private string rootFolderPath = string.Format("{0}{1}{2}",
																									@"C:\Users\Hitesh\Videos",
																									System.IO.Path.DirectorySeparatorChar,
																									"BigBuckBunny");

		private string manifestFileName = "big_Buck_Bunny.manifest";

		// Dictionary to track URL/filename pairs of data in cache.
		public Dictionary<string, string> keyUrls = new Dictionary<string, string>(50);

		public ISO_StorageCache()
		{
			foreach (KeyValuePair<string, object> pair in IsolatedStorageSettings.ApplicationSettings)
			{

				//if (!keyUrls.ContainsValue((string) pair.Value) && isoFileArea.FileExists((string) pair.Value))
				if (!keyUrls.ContainsValue((string)pair.Value) && File.Exists(rootFolderPath + System.IO.Path.DirectorySeparatorChar + (string)pair.Value))
					keyUrls.Add(pair.Key, ((string)pair.Value));
			}
		}

		public IAsyncResult BeginPersist(CacheRequest request, CacheResponse response, AsyncCallback callback, object state)
		{
			state = false;
			CacheAsyncResult ar = new CacheAsyncResult();

			//Manipulate the URI
			String tempUri = request.CanonicalUri.ToString();

			if (!keyUrls.ContainsKey(tempUri))
			{
				//state = true;
				ar.strUrl = tempUri;
				ar.Complete(response, true);
				return ar;
			}

			ar.Complete(null, true);
			return ar;
		}

		public bool EndPersist(IAsyncResult ar)
		{
			ar.AsyncWaitHandle.WaitOne();

			if (((CacheAsyncResult)ar).Result != null)
			{
				IsolatedStorageFile isoFileArea = IsolatedStorageFile.GetUserStoreForApplication();
				if (isoFileArea.AvailableFreeSpace < 1024)
					isoFileArea.IncreaseQuotaTo(isoFileArea.Quota + 2048);



				if (!Directory.Exists(rootFolderPath))
					Directory.CreateDirectory(rootFolderPath);

				string fileGuid = Guid.NewGuid().ToString();

				string resourceUrl = ((CacheAsyncResult)ar).strUrl;
				if (resourceUrl.ToLower().Contains("/manifest"))
				{
					fileGuid = manifestFileName;
				}
				if (!keyUrls.ContainsValue(fileGuid) && !keyUrls.ContainsKey(resourceUrl))
				{

					var fileName = string.Format("{0}{1}{2}",
																			 rootFolderPath,
																			 System.IO.Path.DirectorySeparatorChar,
																			 fileGuid);

					var file = File.Create(fileName);
					((CacheResponse)(((CacheAsyncResult)ar).Result)).WriteTo(file);
					file.Close();

					keyUrls.Add(((CacheAsyncResult)ar).strUrl, fileGuid);
					// Save key/value pairs for playback after application restarts.

					var exists =
						IsolatedStorageSettings.ApplicationSettings.Any(
							x => x.Key == ((CacheAsyncResult)ar).strUrl);
					if (!exists)
					{
						IsolatedStorageSettings.ApplicationSettings.Add(((CacheAsyncResult)ar).strUrl, fileGuid);
					}
					else
					{
						IsolatedStorageSettings.ApplicationSettings[((CacheAsyncResult)ar).strUrl] = fileGuid;
					}
					IsolatedStorageSettings.ApplicationSettings.Save();

					return true;
				}
			}
			return false;
		}

		public void OpenMedia(Uri manifestUri)
		{
			//manifest file doesnt exist on file system hence retrive it!
			if (!Directory.Exists(rootFolderPath) && (!File.Exists(rootFolderPath + Path.DirectorySeparatorChar + manifestFileName)))
			{
				WebRequest request = WebRequest.Create(manifestUri);
				request.BeginGetResponse(onmanifestRetrieved, request);
			}

		}

		private void onmanifestRetrieved(IAsyncResult ar)
		{
			WebRequest wreq = ar.AsyncState as WebRequest;
			HttpWebResponse wresp = (HttpWebResponse)wreq.EndGetResponse(ar);

			if (wresp.StatusCode != HttpStatusCode.OK)
				throw new Exception(wresp.StatusDescription);

			Stream RespStream = wresp.GetResponseStream();

			if (!Directory.Exists(rootFolderPath))
				Directory.CreateDirectory(rootFolderPath);

			var manifestPath = rootFolderPath + Path.DirectorySeparatorChar + manifestFileName;

			//open a filestream
			using (FileStream fs = new FileStream(manifestPath, FileMode.Create, FileAccess.ReadWrite))
			{
				//create a CacheResponse
				CacheResponse cacheResp = new CacheResponse(RespStream.Length, wresp.ContentType, null,
																										RespStream, wresp.StatusCode, wresp.StatusDescription,
																										DateTime.UtcNow);
				//serialize to the file
				cacheResp.WriteTo(fs);
				fs.Flush();
				fs.Close();
			}

			//SaveClientManifest(wresp, RespStream);
		}


		//internal void SaveClientManifest(HttpWebResponse wresp, Stream RespStream)
		//{
		//	var manifestPath = rootFolderPath + System.IO.Path.DirectorySeparatorChar + "big_Buck_Bunny.manifest";


		//	//open a filestream
		//	using (FileStream fs = new FileStream(manifestPath, FileMode.Create, FileAccess.ReadWrite))
		//	{
		//		//create a CacheResponse
		//		CacheResponse cacheResp = new CacheResponse(RespStream.Length, wresp.ContentType, null,
		//																								RespStream, wresp.StatusCode, wresp.StatusDescription,
		//																								DateTime.UtcNow);
		//		//serialize to the file
		//		cacheResp.WriteTo(fs);
		//		fs.Flush();
		//		fs.Close();
		//	}
		//}

		public void CloseMedia(Uri manifestUri)
		{

		}

		public IAsyncResult BeginRetrieve(CacheRequest request, AsyncCallback callback, object state)
		{
			CacheResponse response = null;
			CacheAsyncResult ar = new CacheAsyncResult();
			ar.strUrl = request.CanonicalUri.ToString();
			// ar.strUrl = "http://mediadl.microsoft.com/mediadl/iisnet/smoothmedia/Experience/BigBuckBunny_720p.ism/Manifest";

			ar.Complete(response, true);
			return ar;
		}

		public CacheResponse EndRetrieve(IAsyncResult ar)
		{
			ar.AsyncWaitHandle.WaitOne();

			CacheResponse response = null;

			if ((((CacheAsyncResult)ar).strUrl).ToLower().EndsWith("/manifest"))
			{
				var manifestFileName = rootFolderPath + System.IO.Path.DirectorySeparatorChar + "big_Buck_Bunny.manifest";

				//manifest file exists on file system hence retrive it!
				if (Directory.Exists(rootFolderPath) && (File.Exists(manifestFileName)))
				{
					using (FileStream fs = new FileStream(manifestFileName, FileMode.Open, FileAccess.Read))
					{
						return new CacheResponse(fs);
					}
				}
			}
			else
			{
				if (keyUrls.ContainsKey(((CacheAsyncResult)ar).strUrl))
				{
					string filename = keyUrls[((CacheAsyncResult)ar).strUrl];

					var chunkFilename = rootFolderPath + System.IO.Path.DirectorySeparatorChar + filename;

					if (Directory.Exists(rootFolderPath) && (File.Exists(chunkFilename)))
					{
						var stream = File.OpenRead(chunkFilename);
						response = new CacheResponse(stream);
					}
				}
			}
			if (response != null)
				return response;
			else
				return response = new CacheResponse(0, null, null, null, System.Net.HttpStatusCode.NotFound, "Not Found", DateTime.Now);
		}
	}
}
