using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System;

#if UNITY_EDITOR
public class BuildTool : UnityEditor.Editor
{
	[MenuItem("FrameworkTool/Build Windows Bundle")]
	public static void BuildWindowsBundle()
	{
		BuildBundle(UnityEditor.BuildTarget.StandaloneWindows);
	}

	[MenuItem("FrameworkTool/Build Android Bundle")]
	public static void BuildAndroidBundle()
	{
		BuildBundle(UnityEditor.BuildTarget.Android);
	}

	[MenuItem("FrameworkTool/Build iPhone Bundle")]
	public static void BuildiPhoneBundle()
	{
		BuildBundle(UnityEditor.BuildTarget.iOS);
	}

	private static void BuildBundle(UnityEditor.BuildTarget buildTarget)
	{
		List<AssetBundleBuild> assetBundleBuilds = new List<AssetBundleBuild>();
		string[] files = Directory.GetFiles(PathUtil.BuildResourcesInputPath, "*", SearchOption.AllDirectories);
		List<string> bundleInfos = new List<string>();

		// Prepare a list to hold ¡°relative path | hash¡± entries
		List<string> fileHashs = new List<string>();

		for (int i = 0; i < files.Length; i++)
		{
			if (files[i].EndsWith(".meta"))
				continue;
			AssetBundleBuild assetBundleBuild = new AssetBundleBuild();

			string fileName = PathUtil.GetStandardPath(files[i]);
			Debug.Log("file: " + fileName);

			string assetName = PathUtil.GetUnityRelativePath(fileName);
			assetBundleBuild.assetNames = new string[] { assetName };
			
			string bundleName = fileName.Replace(PathUtil.BuildResourcesInputPath, "").ToLower();
			if (bundleName.StartsWith("/"))
				bundleName = bundleName.Substring(1);
			assetBundleBuild.assetBundleName = bundleName + FrameworkConstant.BundleExtension;
			assetBundleBuilds.Add(assetBundleBuild);


			List<string> dependencies = GetDependenciesFilePaths(assetName);
			string bundleInfo = assetName + "|" + bundleName + FrameworkConstant.BundleExtension;

			if(dependencies.Count > 0)
				bundleInfo = bundleInfo + "|" + string.Join("|", dependencies);

			bundleInfos.Add(bundleInfo);
		}

		if (files.Length > 0)
		{
			if (Directory.Exists(PathUtil.BundleOutputPath))
				Directory.Delete(PathUtil.BundleOutputPath, true);
			Directory.CreateDirectory(PathUtil.BundleOutputPath);

			BuildPipeline.BuildAssetBundles(PathUtil.BundleOutputPath, assetBundleBuilds.ToArray(), BuildAssetBundleOptions.None, buildTarget);
			File.WriteAllLines(PathUtil.BundleOutputPath + FrameworkConstant.VersionFileList, bundleInfos);

			// Generate a list of all files in the bundle output directory (recursively),
			// compute each file¡¯s MD5 hash, and write entries in the form ¡°relative/path|md5hash¡±

			// Get every file under the bundle output directory
			string[] bundleFilePaths = Directory.GetFiles(PathUtil.BundleOutputPath, "*",SearchOption.AllDirectories);

			for (int index = 0; index < bundleFilePaths.Length; index++)
			{
				string filePath = bundleFilePaths[index];

				// Skip Unity meta files
				if (filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
					continue;

				// Compute the path of this file relative to the bundle output directory
				string relativeFilePath = Path.GetRelativePath(PathUtil.BundleOutputPath, filePath);

				// Normalize to forward slashes so it¡¯s consistent across platforms
				relativeFilePath = relativeFilePath.Replace('\\', '/');

				// Read the entire file into memory
				byte[] fileBytes = File.ReadAllBytes(filePath);

				// Compute the MD5 checksum of the file
				string fileMd5Hash = FileHashTool.ComputeMD5(fileBytes);

				// Store one entry per line: "relative/path/to/file|md5checksum"
				fileHashs.Add($"{relativeFilePath}|{fileMd5Hash}");
			}
			File.WriteAllLines(PathUtil.BundleOutputPath + FrameworkConstant.VersionFileHashList, fileHashs);
		}

		UnityEditor.AssetDatabase.Refresh();
	}

	private static List<string> GetDependenciesFilePaths(string currentAssetName)
	{
		List<string> dependenceFilePaths = new List<string>();
		string[] files = UnityEditor.AssetDatabase.GetDependencies(currentAssetName);
		dependenceFilePaths = files.Where(file => !file.EndsWith(".cs") && !file.Equals(currentAssetName)).ToList();
		return dependenceFilePaths;
	}
}
#endif
