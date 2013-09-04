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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DemoPlayer
{
	public partial class MainPage : UserControl
	{
		private IsoStorageCache cache = null;

		public MainPage()
		{
			InitializeComponent();
			SmoothPlayer.CurrentStateChanged += new RoutedEventHandler(SmoothPlayer_CurrentStateChanged);
			SmoothPlayer.Loaded += new RoutedEventHandler(SmoothPlayer_Loaded);
		}


		private void clearCacheButton_Click(object sender, RoutedEventArgs e)
		{
			cache.KeyUrls.Clear();
			IsolatedStorageSettings.ApplicationSettings.Clear();
			IsolatedStorageSettings.SiteSettings.Clear();

			IsolatedStorageFile isoFileArea = IsolatedStorageFile.GetUserStoreForApplication();
			string[] names = isoFileArea.GetFileNames();

			foreach (string file in names)
			{
				isoFileArea.DeleteFile(file);
			}

		}


		//When an ISmoothStreamingCache object is started and a request for data is issued, the Silverlight Smooth Streaming Client will call each of the methods in the order: BeginRetrieve, EndRetrieve, BeginPersist, EndPersist

		private void SmoothPlayer_Loaded(object sender, RoutedEventArgs e)
		{
			cache = new IsoStorageCache();
			SmoothPlayer.SmoothStreamingCache = cache;
		}

		private void SmoothPlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
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

			if (File.Exists(cache.SettingsFilePath))
			{
				File.Delete(cache.SettingsFilePath);
			}
			string json = JsonConvert.SerializeObject(cache.KeyUrls, new KeyValuePairConverter());
			using (StreamWriter outfile = new StreamWriter(cache.SettingsFilePath))
			{
				outfile.Write(json);
			}
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