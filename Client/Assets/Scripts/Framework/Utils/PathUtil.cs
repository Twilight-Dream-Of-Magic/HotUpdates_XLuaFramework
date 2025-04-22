using System.IO;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PathUtil
{
	/// <summary>
	/// The primary base directory (relative) under which all AssetBundles are stored.
	/// </summary>
	public static string InRootFolderPath = "/AssetBundles";

	/*   Roots   */

	/// <summary>
	/// The full read-only root path for AssetBundles, combining
	/// <see cref="Application.streamingAssetsPath"/> with <see cref="InRootFolderPath"/>.
	/// </summary>
	private static string ReadOnlyRoot = Application.streamingAssetsPath + InRootFolderPath;

	/// <summary>
	/// The full read-write root path for AssetBundles (e.g. persistent data), 
	/// as returned by <see cref="GetReadableAndWriteableRoot"/>.
	/// </summary>
	private static string ReadWriteRoot => GetReadableAndWriteableRoot();


	/*   Common asset paths   */

	/// <summary>
	/// The absolute file system path to the Unity project’s Assets folder.
	/// </summary>
	public static readonly string AssetsPath = Application.dataPath;

	// Bundle/Build input assets directory
	public static readonly string BuildResourcesInputPath = AssetsPath + "/BuildResources";
	// Bundle output directory
	public static readonly string BundleOutputPath = Application.streamingAssetsPath + InRootFolderPath;
	
	/// <summary>
	/// The directory under BuildResourcesInputPath for Lua scripts.
	/// Combines <see cref="BuildResourcesInputPath"/> with "/LuaScripts".
	/// </summary>
	public static readonly string LuaPath = BuildResourcesInputPath + "/LuaScripts";

	public enum DevicePlatformType
	{
		Windows,
		Linux,
		OSX,
		iOS,
		Android,
		WebGL,
		Other
	}

	private static DevicePlatformType DetectPlatform()
	{
#if UNITY_EDITOR
		switch (EditorUserBuildSettings.activeBuildTarget)
		{
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return DevicePlatformType.Windows;
			case BuildTarget.StandaloneLinux64:
				return DevicePlatformType.Linux;
			case BuildTarget.StandaloneOSX:
				return DevicePlatformType.OSX;
			case BuildTarget.iOS:
				return DevicePlatformType.iOS;
			case BuildTarget.Android:
				return DevicePlatformType.Android;
			case BuildTarget.WebGL:
				return DevicePlatformType.WebGL;
			default:
				return DevicePlatformType.Other;
		}
#else
			switch (Application.platform)
			{
				case RuntimePlatform.WindowsPlayer:
					return DevicePlatformType.Windows;
				case RuntimePlatform.LinuxPlayer:
					return DevicePlatformType.Linux;
				case RuntimePlatform.OSXPlayer:
					return DevicePlatformType.OSX;
				case RuntimePlatform.IPhonePlayer:
					return DevicePlatformType.iOS;
				case RuntimePlatform.Android:
					return DevicePlatformType.Android;
				case RuntimePlatform.WebGLPlayer:
					return DevicePlatformType.WebGL;
				default:
					return DevicePlatformType.Other;
			}
#endif
	}

	private static readonly Lazy<DevicePlatformType> lazyCurrentPlatform = new Lazy<DevicePlatformType>(DetectPlatform);
	public static DevicePlatformType CurrentPlatform => lazyCurrentPlatform.Value;

	// 2 Read-only path builder
	// Android have prefix jar:file//
	public static string GetReadOnlyRoot() => ReadOnlyRoot;

	public static string GetReadOnlyPath(string relativePath) => Path.Combine(GetReadOnlyRoot(), relativePath);

	// 3 Read-write path builder

	public static string GetReadableAndWriteableRoot()
	{
		if (CurrentPlatform == DevicePlatformType.iOS)
			return UnityEngine.Application.temporaryCachePath + InRootFolderPath;
		else
			return UnityEngine.Application.persistentDataPath + InRootFolderPath;
	}

#if UNITY_IOS

		// iOS: small ¡ú Documents (persistent), large ¡ú Caches (temp)
		private const long IOS_LargeFileThreshold = 5 * 1024 * 1024;

		public static string GetReadableAndWriteableFolderPath(string folderName, FileInfo fileInfo)
		{
			if (fileInfo.Length >= IOS_LargeFileThreshold)
			{
				var fullPath = Path.Combine(UnityEngine.Application.temporaryCachePath, folderName);
				if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
				return fullPath;
			}
			else
			{
				var fullPath = Path.Combine(UnityEngine.Application.persistentDataPath, folderName);
				if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
				return fullPath;
			}
		}

		public static string GetReadableAndWriteableFilePath(string fileName, FileInfo fileInfo)
		{
			if (fileInfo.Length >= IOS_LargeFileThreshold)
			{
				var fullPath = Path.Combine(UnityEngine.Application.temporaryCachePath, fileName);
				if (!File.Exists(fullPath))
					Debug.LogWarning($"[PathUtil] File path not found: {fullPath});
				return fullPath;
			}
			else
			{
				var fullPath = Path.Combine(UnityEngine.Application.persistentDataPath, fileName);
				if (!File.Exists(fullPath))
					Debug.LogWarning($"[PathUtil] File path not found: {fullPath});
				return fullPath;
			}
		}

		[Obsolete("Must use for iOS API!!!")]
		public static string GetReadableAndWriteableFolderPath(string folderName)
		{
			return string.Empty;
		}

		[Obsolete("Must use for iOS API!!!")]
		public static string GetReadableAndWriteableFilePath(string fileName)
		{
			return string.Empty;
		}
#else
	public static string GetReadableAndWriteableFolderPath(string folderName)
	{
		var fullPath = GetReadableAndWriteableRoot() + folderName;
		if (!Directory.Exists(fullPath))
			Directory.CreateDirectory(fullPath);
		return fullPath;
	}

	public static string GetReadableAndWriteableFilePath(string fileName)
	{
		var fullPath = GetReadableAndWriteableRoot() + fileName;
		if (!File.Exists(fullPath))
			Debug.LogWarning($"[PathUtil] File path not found: {fullPath}");
		return fullPath;
	}
#endif

	public static string BuildResourcesPath
	{
		get
		{
			if (FrameworkConstant.GDM == GameDeploymentMode.UpdateMode)
				return ReadWriteRoot;
			return ReadOnlyRoot;
		}
	}

	public static string GetUnityRelativePath(string fullPath)
	{
		if (string.IsNullOrEmpty(fullPath))
			return string.Empty;
		var idx = fullPath.IndexOf("Assets");
		return idx >= 0 ? fullPath.Substring(idx) : fullPath;
	}

	public static string GetStandardPath(string path)
	{
		if (string.IsNullOrEmpty(path))
			return string.Empty;
		return path.Trim().Replace("\\", "/");
	}

	public static string GetLuaScriptPath(string fileName)
	{
		return string.Format("Assets/BuildResources/LuaScripts/{0}.bytes", fileName);
	}

	public static string GetUIPrefabPath(string fileName)
	{
		return string.Format("Assets/BuildResources/UI/Prefabs/{0}.prefab", fileName);
	}

	public static string GetMusicPath(string fileName)
	{
		return string.Format("Assets/BuildResources/Audio/Music/{0}", fileName);
	}

	public static string GetSoundPath(string fileName)
	{
		return string.Format("Assets/BuildResources/Audio/Sound/{0}", fileName);
	}

	public static string GetEffectPrefabPath(string fileName)
	{
		return string.Format("Assets/BuildResources/Effect/Prefabs/{0}.prefab", fileName);
	}

	public static string GetModelPrefabPath(string fileName)
	{
		return string.Format("Assets/BuildResources/Models/Prefabs/{0}.prefab", fileName);
	}

	public static string GetSpritePath(string fileName)
	{
		return string.Format("Assets/BuildResources/Sprites/{0}", fileName);
	}

	public static string GetTexturePath(string fileName)
	{
		return string.Format("Assets/BuildResources/Textures/{0}", fileName);
	}

	public static string GetMaterialPath(string fileName)
	{
		return string.Format("Assets/BuildResources/Materials/{0}.mat", fileName);
	}

	public static string GetScenePath(string fileName)
	{
		return string.Format("Assets/BuildResources/Scenes/{0}.unity", fileName);
	}

	public static string GetPhysicsMaterialPath(string fileName)
	{
		return string.Format("Assets/BuildResources/PhysicsMaterials/{0}", fileName);
	}
}
