using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

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

			File.WriteAllLines(PathUtil.BundleOutputPath + "/" + FrameworkConstant.VersionFileListName, bundleInfos);
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
