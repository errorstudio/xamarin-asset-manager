using System;
using System.IO;
using System.Net;
using System.ComponentModel;

using AssetManager.Droid;

using Android.App;

using SharpCompress;
using SharpCompress.Reader;

using Xamarin.Forms;
using XLabs.Forms;
using XLabs.Forms.Controls;

[assembly: Xamarin.Forms.Dependency(typeof(WebAssetManager))]

namespace AssetManager.Droid
{
	public class WebAssetManager : IWebAssetManager
	{
		private WebClient webClient = new WebClient();
		private string tempFilename;

		public WebAssetManager()
		{
			webClient.DownloadFileCompleted += assetsDownloaded;
		}

		public Boolean assetsCopied()
		{
			var writablePath = this.getWritableAssetPath();

			if (File.Exists(writablePath))
			{
				return true;
			}

			return false;
		}

		public void ensureAssetsArePresent()
		{
			string archiveName = "Latest.zip";
			string storagePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			string contentPath = Path.Combine(storagePath, "..", "Library", "Content");
			var storedAssets = Path.Combine(contentPath, archiveName);

			if (!File.Exists(storedAssets))
			{
				//copy from bundle
				this.copyAssets(this.getBundledAssetPath());
			}
		}

		/*
		 * Given a local file path (string), open the file and extract
		 * each file to the local writable filesystem;
		 */
		public void copyAssets(string fromAssetsPath)
		{
			var destination = this.getWritableAssetPath();
			string path;

			Stream stream;

			if (fromAssetsPath == "web.zip")
			{
				stream = Android.App.Application.Context.Assets.Open(fromAssetsPath);
			}
			else if (File.Exists(fromAssetsPath))
			{
				stream = File.OpenRead(fromAssetsPath);
			}
			else {
				return;
			}

			using (stream)
			{
				using (var reader = ReaderFactory.Open(stream))
				{
					while (reader.MoveToNextEntry())
					{
						path = Path.Combine(destination, reader.Entry.Key);

						if (!reader.Entry.IsDirectory)
						{
							using (Stream sw = File.Create(path))
							{
								reader.WriteEntryTo(sw);
							}
						}
						else if (reader.Entry.IsDirectory && !Directory.Exists(path))
						{
							Directory.CreateDirectory(path);
						}
					}
				}
			}

		}

		public String getWritableAssetPath()
		{
			string docPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			string webPath = Path.Combine(docPath, "web");

			return webPath;
		}

		public String getLocalAssetPath()
		{
			return this.getWritableAssetPath();
		}

		public String getBundledAssetPath()
		{
			return "web.zip";
		}

		public string getFilePath(string tab, string slug)
		{
			var root = this.getWritableAssetPath();
			var path = Path.Combine(root, "local-content", tab, slug + ".html");

			if (File.Exists(path))
			{
				var filePath = path;
				return filePath;
			}

			return "no file: " + path;
		}

		public string getFileContent(string path)
		{
			return File.ReadAllText(path);
		}

		/*
		 *
		 * Check the status code of the events endpoint.
		 *
		 * If the request returns a 304 (NotModified) or times out (user is offline), we can skip downloading the
		 *
		 */
		public bool isUpdateRequired(DateTime lastUpdated)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create("client.tld/ping-url");
			request.Method = "GET";
			request.Headers.Add("lastUpdated", String.Format("{0:s}", lastUpdated));
			request.Timeout = 4000; // assume we're offline if there's no response within 4 seconds

			try
			{
				HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
				if (resp.StatusCode == HttpStatusCode.OK)
				{
					return true;
				}
			}
			catch (WebException ex)
			{
				if (ex.Status == WebExceptionStatus.NameResolutionFailure || ex.Status == WebExceptionStatus.ConnectFailure || ex.Status == WebExceptionStatus.Timeout)
				{
					return false;
				}

				if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotModified)
				{
					return false;
				}
			}

			return false;
		}

		/*
		 * Perform an async network read of our /assets endpoint.
		 * The response handler (checkAssetsStatus) will either do nothing (check for a 304 not modified)
		 * or perform a 2nd async task which downloads the assets
		 */
		public void fetchAssets()
		{
			string archiveName = "Latest.zip";
			string storagePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			string contentPath = Path.Combine(storagePath, "..", "Library", "Content");

			if (!Directory.Exists(contentPath))
			{
				Directory.CreateDirectory(contentPath);
			}
			tempFilename = Path.Combine(contentPath, archiveName);

			if (!webClient.IsBusy)
			{
				webClient.DownloadFileAsync(new Uri("client.tld/download-url"), tempFilename);
			}
		}

		/*
		 * Todo: move this out of the WebAssetManager at some point - UI stuff
		 * shouldn't be in here but since the download is happening Asynchonously,
		 * we need a way to update the tab content immediately after the archive is
		 * extracted
		 *
		 */
		public void assetsDownloaded(object sender, AsyncCompletedEventArgs args)
		{
			this.copyAssets(tempFilename);

			foreach (var page in (App.Current.MainPage as TabbedPage).Children)
			{
				TabRenderer tab;
				if (page.GetType() == typeof(NavigationPage))
				{
					tab = ((page as NavigationPage).CurrentPage as TabRenderer);
				}
				else
				{
					tab = (page as TabRenderer);
				}

				var hwebView = (tab.hwebView as HybridWebView);
				hwebView.CallJsFunction("reloadPage");
			}
		}
	}
}
