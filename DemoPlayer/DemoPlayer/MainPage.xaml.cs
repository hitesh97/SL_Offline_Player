using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
//Additional Namespace
using Microsoft.Web.Media.SmoothStreaming;
using System.IO.IsolatedStorage;

using System.IO;
using System.Threading;

namespace DemoPlayer
{
    public partial class MainPage : UserControl
    {
        ISO_StorageCache cache = null;

        public MainPage()
        {
            InitializeComponent();
            SmoothPlayer.CurrentStateChanged += new RoutedEventHandler(SmoothPlayer_CurrentStateChanged);
            SmoothPlayer.Loaded += new RoutedEventHandler(SmoothPlayer_Loaded);
        }


        private void clearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            cache.keyUrls.Clear();
            IsolatedStorageSettings.ApplicationSettings.Clear();
            IsolatedStorageSettings.SiteSettings.Clear();

            IsolatedStorageFile isoFileArea = IsolatedStorageFile.GetUserStoreForApplication();
            string[] names = isoFileArea.GetFileNames();

            foreach (string file in names)
            {
                isoFileArea.DeleteFile(file);
            }

        }

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
            IsolatedStorageFile isoFileArea = IsolatedStorageFile.GetUserStoreForApplication();

            foreach (KeyValuePair<string, object> pair in IsolatedStorageSettings.ApplicationSettings)
            {

              //if (!keyUrls.ContainsValue((string) pair.Value) && isoFileArea.FileExists((string) pair.Value))
              if (!keyUrls.ContainsValue((string)pair.Value) && File.Exists(rootFolderPath + System.IO.Path.DirectorySeparatorChar + (string)pair.Value))
                keyUrls.Add(pair.Key, ((string) pair.Value));
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

	            if (((CacheAsyncResult) ar).Result != null)
	            {
		            IsolatedStorageFile isoFileArea = IsolatedStorageFile.GetUserStoreForApplication();
		            if (isoFileArea.AvailableFreeSpace < 1024)
			            isoFileArea.IncreaseQuotaTo(isoFileArea.Quota + 2048);

		            

		            if (!Directory.Exists(rootFolderPath))
			            Directory.CreateDirectory(rootFolderPath);

		            string fileGuid = Guid.NewGuid().ToString();

	              string resourceUrl = ((CacheAsyncResult) ar).strUrl;
	              if (resourceUrl.ToLower().Contains("/manifest"))
	              {
	                fileGuid = manifestFileName;
	              }
                  if (!keyUrls.ContainsValue(fileGuid) && !keyUrls.ContainsKey(resourceUrl))
		            {

			            //IsolatedStorageFileStream isoFile = isoFileArea.CreateFile(fileGuid);


			            var fileName = string.Format("{0}{1}{2}",
			                                         rootFolderPath,
			                                         System.IO.Path.DirectorySeparatorChar,
			                                         fileGuid);

			            var file = File.Create(fileName);
			            ((CacheResponse) (((CacheAsyncResult) ar).Result)).WriteTo(file);
			            file.Close();

			            keyUrls.Add(((CacheAsyncResult) ar).strUrl, fileGuid);
			            // Save key/value pairs for playback after application restarts.

			            var exists =
				            IsolatedStorageSettings.ApplicationSettings.Any(
					            x => x.Key == ((CacheAsyncResult) ar).strUrl);
			            if (!exists)
			            {
				            IsolatedStorageSettings.ApplicationSettings.Add(((CacheAsyncResult) ar).strUrl, fileGuid);
			            }
			            else
			            {
				            IsolatedStorageSettings.ApplicationSettings[((CacheAsyncResult) ar).strUrl] = fileGuid;
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
						HttpWebResponse wresp = (HttpWebResponse) wreq.EndGetResponse(ar);

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
										IsolatedStorageFile isoFileArea = IsolatedStorageFile.GetUserStoreForApplication();
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

            public class CacheAsyncResult : IAsyncResult
            {
                public string strUrl { get; set; }

                public object AsyncState { get; private set; }

                public WaitHandle AsyncWaitHandle { get { return _completeEvent; } }

                public bool CompletedSynchronously { get; private set; }

                public bool IsCompleted { get; private set; }

                // Contains all the output result of the GetChunk API
                public Object Result { get; private set; }

                internal TimeSpan Timestamp { get; private set; }

                /// <summary>
                /// Callback function when GetChunk is completed. Used in asynchronous mode only.
                /// Should be null for synchronous mode.
                /// </summary>
                private AsyncCallback _callback;

                /// <summary>
                /// Event is used to signal the completion of the operation
                /// </summary>
                private ManualResetEvent _completeEvent = new ManualResetEvent(false);

                /// <summary>
                /// Called when the operation is completed
                /// </summary>
                public void Complete(Object result, bool completedSynchronously)
                {
                    Result = result;
                    CompletedSynchronously = completedSynchronously;

                    IsCompleted = true;
                    _completeEvent.Set();

                    if (null != _callback) { ;  }
                }

            }

            //When an ISmoothStreamingCache object is started and a request for data is issued, the Silverlight Smooth Streaming Client will call each of the methods in the order: BeginRetrieve, EndRetrieve, BeginPersist, EndPersist
                
            void SmoothPlayer_Loaded(object sender, RoutedEventArgs e)
            {
                cache = new ISO_StorageCache();
                SmoothPlayer.SmoothStreamingCache = cache;
            }

            void SmoothPlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
            {
                switch (SmoothPlayer.CurrentState)
                {
                    case SmoothStreamingMediaElementState.Playing:
                        PlayButton.Content = "Pause";
                        break;

                    case SmoothStreamingMediaElementState.Paused:
                        PlayButton.Content = "Play";
                        break;

                    case SmoothStreamingMediaElementState.Stopped:
                        PlayButton.Content = "Play";
                        break;
                }
            }

            private void PlayButton_Click(object sender, RoutedEventArgs e)
            {
                //Monitor the state of the content to determine the right action to take on this button being clicked
                //and then change the text to reflect the next action
                switch (SmoothPlayer.CurrentState)
                {
                    case SmoothStreamingMediaElementState.Playing:
                        SmoothPlayer.Pause();
                        PlayButton.Content = "Play";
                        break;
                    case SmoothStreamingMediaElementState.Stopped:
                    case SmoothStreamingMediaElementState.Paused:
                        SmoothPlayer.Play();
                        PlayButton.Content = "Pause";
                        //       CacheSize.Text = "Cache size: " + tempSize.ToString();
                        break;
                }
            }

            private void StopButton_Click(object sender, RoutedEventArgs e)
            {
                //This should simply stop the playback
                SmoothPlayer.Stop();
                //We should also reflect the chang on the play button
                PlayButton.Content = "Play";


            }

            private void PlayButton_Loaded(object sender, RoutedEventArgs e)
            {
                //We need to prepopulate the value of Play/Pause button content, we need to check AutoPlay
                switch (SmoothPlayer.AutoPlay)
                {
                    case false:
                        PlayButton.Content = "Play";
                        break;
                    case true:
                        PlayButton.Content = "Pause";
                        break;
                }
            }
        }


    }

