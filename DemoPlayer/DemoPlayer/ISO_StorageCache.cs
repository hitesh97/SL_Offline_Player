using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using Microsoft.Web.Media.SmoothStreaming;
using Newtonsoft.Json;
using Path = System.IO.Path;

namespace DemoPlayer
{
	public class IsoStorageCache : ISmoothStreamingCache
	{
		private readonly string _rootFolderPath = string.Format("{0}{1}{2}",
			@"C:\Users\Hitesh\Videos",
			Path.DirectorySeparatorChar,
			"BigBuckBunny");

		private const string ManifestFileName = "big_Buck_Bunny.manifest";
		private const string SettingsFilename = "bigBuckBunny.settings";

		public string SettingsFilePath
		{
			get
			{
				return RootFolderPath + Path.DirectorySeparatorChar + SettingsFilename;
			}
		}


		public string ManifestFilePath
		{
			get
			{
				return RootFolderPath + Path.DirectorySeparatorChar + ManifestFileName;
			}
		}

		// Dictionary to track URL/filename pairs of data in cache.
		private readonly Dictionary<string, string> _keyUrls = new Dictionary<string, string>(50);

		public IsoStorageCache()
		{
			//foreach (KeyValuePair<string, object> pair in IsolatedStorageSettings.ApplicationSettings)
			//{

			//	//if (!keyUrls.ContainsValue((string) pair.Value) && isoFileArea.FileExists((string) pair.Value))
			//	if (!keyUrls.ContainsValue((string) pair.Value) &&
			//			File.Exists(RootFolderPath + Path.DirectorySeparatorChar + (string) pair.Value))
			//		keyUrls.Add(pair.Key, ((string) pair.Value));
			//}

			if (File.Exists(SettingsFilePath))
			{
				string settings;
				using (var reader = new StreamReader(SettingsFilePath))
				{
					settings = reader.ReadToEnd();
				}
				_keyUrls = JsonConvert.DeserializeObject<Dictionary<string, string>>(settings);
			}
		}

		public Dictionary<string, string> KeyUrls
		{
			get { return _keyUrls; }
		}

		public string RootFolderPath
		{
			get { return _rootFolderPath; }
		}

		public IAsyncResult BeginPersist(CacheRequest request, CacheResponse response, AsyncCallback callback, object state)
		{
			state = false;
			var ar = new CacheAsyncResult();

			//Manipulate the URI
			String tempUri = request.CanonicalUri.ToString();

			if (!_keyUrls.ContainsKey(tempUri))
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

			if (((CacheAsyncResult) ar).Result != null)
			{
				IsolatedStorageFile isoFileArea = IsolatedStorageFile.GetUserStoreForApplication();
				if (isoFileArea.AvailableFreeSpace < 1024)
					isoFileArea.IncreaseQuotaTo(isoFileArea.Quota + 2048);



				if (!Directory.Exists(RootFolderPath))
					Directory.CreateDirectory(RootFolderPath);

				var fileGuid = Guid.NewGuid().ToString();

				var resourceUrl = ((CacheAsyncResult) ar).strUrl;
				if (resourceUrl.ToLower().Contains("/manifest"))
				{
					fileGuid = ManifestFileName;
				}
				if (!_keyUrls.ContainsValue(fileGuid) && !_keyUrls.ContainsKey(resourceUrl))
				{

					var fileName = string.Format("{0}{1}{2}",
						RootFolderPath,
						Path.DirectorySeparatorChar,
						fileGuid);

					var file = File.Create(fileName);
					((CacheResponse) (((CacheAsyncResult) ar).Result)).WriteTo(file);
					file.Close();

					_keyUrls.Add(((CacheAsyncResult) ar).strUrl, fileGuid);
					return true;
				}
			}
			return false;
		}

		public void OpenMedia(Uri manifestUri)
		{
			//manifest file doesnt exist on file system hence retrive it!
			if (!Directory.Exists(RootFolderPath) &&
					(!File.Exists(ManifestFilePath)))
			{
				WebRequest request = WebRequest.Create(manifestUri);
				request.BeginGetResponse(OnManifestResponse, request);
			}

		}

		private void OnManifestResponse(IAsyncResult ar)
		{
			var wreq = ar.AsyncState as WebRequest;
			if (wreq != null)
			{
				var wresp = (HttpWebResponse) wreq.EndGetResponse(ar);

				if (wresp.StatusCode != HttpStatusCode.OK)
					throw new Exception(wresp.StatusDescription);

				Stream respStream = wresp.GetResponseStream();

				if (!Directory.Exists(RootFolderPath))
					Directory.CreateDirectory(RootFolderPath);
				//open a filestream
				using (var fs = new FileStream(ManifestFilePath, FileMode.Create, FileAccess.ReadWrite))
				{
					//create a CacheResponse
					var cacheResp = new CacheResponse(respStream.Length, wresp.ContentType, null,
						respStream, wresp.StatusCode, wresp.StatusDescription,
						DateTime.UtcNow);
					//serialize to the file
					cacheResp.WriteTo(fs);
					fs.Flush();
					fs.Close();
				}
			}
		}

		public void CloseMedia(Uri manifestUri)
		{

		}

		public IAsyncResult BeginRetrieve(CacheRequest request, AsyncCallback callback, object state)
		{
			CacheResponse response = null;
			var ar = new CacheAsyncResult {strUrl = request.CanonicalUri.ToString()};
			// ar.strUrl = "http://mediadl.microsoft.com/mediadl/iisnet/smoothmedia/Experience/BigBuckBunny_720p.ism/Manifest";

			ar.Complete(response, true);
			return ar;
		}

		public CacheResponse EndRetrieve(IAsyncResult ar)
		{
			ar.AsyncWaitHandle.WaitOne();

			CacheResponse response = null;

			if ((((CacheAsyncResult) ar).strUrl).ToLower().EndsWith("/manifest"))
			{
				//manifest file exists on file system hence retrive it!
				if (Directory.Exists(RootFolderPath) && (File.Exists(ManifestFilePath)))
				{
					using (FileStream fs = new FileStream(ManifestFilePath, FileMode.Open, FileAccess.Read))
					{
						return new CacheResponse(fs);
					}
				}
			}
			else
			{
				if (_keyUrls.ContainsKey(((CacheAsyncResult) ar).strUrl))
				{
					string filename = _keyUrls[((CacheAsyncResult) ar).strUrl];

					var chunkFilename = RootFolderPath + Path.DirectorySeparatorChar + filename;

					if (Directory.Exists(RootFolderPath) && (File.Exists(chunkFilename)))
					{
						var stream = File.OpenRead(chunkFilename);
						response = new CacheResponse(stream);
					}
				}
			}
			return response;
			//if (response != null)
				//return response;
			//else
				//return
					//response = new CacheResponse(0, null, null, null, HttpStatusCode.NotFound, "Not Found", DateTime.Now);
		}
	}
}
