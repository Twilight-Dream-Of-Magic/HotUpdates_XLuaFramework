
public enum GameDeploymentMode
{
	EditorMode,
	PackageBundle,
	UpdateMode
}

public enum GameEvent
{
	LuaScriptsInitialize = 1000,
}

/// <summary>
/// Holds framework-wide constants and controls for logging behavior.
/// </summary>
[XLua.LuaCallCSharp]
public static class FrameworkConstant
{
	/// <summary>
	/// When <c>true</c>, Lua-side logging (via <c>LuaLogging</c>) is permitted.
	/// Do not modify directlyâ€”use <see cref="AllowLogging"/> instead.
	/// </summary>
	internal static bool AllowLuaLogging = true;

	/// <summary>
	/// Backing field for <see cref="AllowLogging"/>.
	/// Records the last value assigned.
	/// </summary>
	private static bool _AllowLoggingValue = false;

	/// <summary>
	/// Global logging switch.  
	/// <para>
	/// Setting this will:
	/// <list type="bullet">
	///   <item><description>Record the new value in a private backing field.</description></item>
	///   <item><description>
	///     Control Unity C# logging:
	///     <list type="bullet">
	///       <item><description>Editor: <c>Debug.unityLogger.logEnabled</c> follows the set value directly.</description></item>
	///       <item><description>
	///         Runtime: only enable <c>Debug.unityLogger</c> if the value is <c>true</c> <b>and</b>
	///         <c>Debug.isDebugBuild</c> is <c>true</c>.
	///       </description></item>
	///     </list>
	///   </description></item>
	///   <item><description>Synchronize Lua-side logging by updating <see cref="AllowLuaLogging"/>.</description></item>
	/// </list>
	/// Getting this property returns the last value that was set.
	/// </para>
	/// </summary>
	public static bool AllowLogging
	{
		get => _AllowLoggingValue;
		set
		{
			_AllowLoggingValue = value;

#if UNITY_EDITOR
			UnityEngine.Debug.unityLogger.logEnabled = value;
#else
			UnityEngine.Debug.unityLogger.logEnabled = (_AllowLoggingValue && UnityEngine.Debug.isDebugBuild);
#endif

			AllowLuaLogging = value;
		}
	}

	/// <summary>
	/// Current deployment mode (Editor, Development build, or Release build).
	/// </summary>
	public static GameDeploymentMode GDM = GameDeploymentMode.EditorMode;

	/// <summary>
	/// File extension used for AssetBundles.
	/// </summary>
	public const string BundleExtension = ".assetbundle";

	/// <summary>
	/// Name of the text file listing all versioned resources.
	/// </summary>
	public const string VersionFileList = "/versionfile_list.txt";

	/// <summary>
	/// Name of the text file hash listing all versioned resources.
	/// </summary>
	public const string VersionFileHashList = "/versionfilehash_list.txt";

	/// <summary>
	/// Name of the JSON file containing protected version file entries.
	/// </summary>
	public const string ProtectedVersionFileList = "/protected_versionfile_list.json";

	/// <summary>
	/// Name of the JSON file containing protected version file hash entries.
	/// </summary>
	public const string ProtectedVersionFileHashList = "/protected_versionfilehash_list.json";

	/// <summary>
	/// Gets the root URL used by the hot-update AssetBundle downloader.
	/// See <see cref="PathUtil.InRootFolderPath"/> for how local paths are mapped to URLs.
	/// In a production environment, replace this with the real HTTP/HTTPS file server address.
	/// </summary>
	public const string HotUpdateRootURL = "http://127.0.0.1";
}

