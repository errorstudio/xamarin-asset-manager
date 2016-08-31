using System;

namespace AssetManager
{
	public interface IWebAssetManager
	{
		void openUrlInBrowser(string url);

		Boolean assetsCopied();
		void copyAssets(string fromAssetsPath);
		String getWritableAssetPath();
		String getLocalAssetPath();
		String getBundledAssetPath();
		string getFilePath(string tab, string slug);
		string getFileContent(string path);

		void ensureAssetsArePresent();

		bool isUpdateRequired(DateTime lastUpdated);
		void fetchAssets();
	}
}
