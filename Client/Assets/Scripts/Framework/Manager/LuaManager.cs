using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using XLua;

public class LuaManager : MonoBehaviour
{
	public List<string> LuaFileNameList = new List<string>();
	private Dictionary<string, byte[]> CacheLuaScripts;

	public LuaEnv LuaEnvironmentParser;
	public DateTime LuaGCAndUpdateTick = DateTime.Now;
	public const float LuaGCInterval = 5.0f;

	private XLua.LuaFunction UpdateFunction;

	private bool IsDisposedValue = false;

	public void Initialize()
	{
		LuaEnvironmentParser = new LuaEnv();
		LuaEnvironmentParser.AddBuildin("rapidjson", XLua.LuaDLL.Lua.LoadRapidJson);
		LuaEnvironmentParser.AddLoader(Loader);
		CacheLuaScripts = new Dictionary<string, byte[]>();

#if UNITY_EDITOR
		if (FrameworkConstant.GDM == GameDeploymentMode.EditorMode)
			EditorLoadLuaScript();
		else
#endif
			LoadLuaScript();
	}

	public object[] ExecuteLuaCode(string LuaCodeString)
	{
		if (LuaEnvironmentParser != null)
		{
			try
			{
				return LuaEnvironmentParser.DoString(LuaCodeString);
			}
			catch (System.Exception except)
			{
				string message = string.Format("xLua core exception : {0}\n {1}", except.Message, except.StackTrace);
				Debug.LogError(message, null);
			}
		}

		return null;
	}

	public void LoadLuaScript(string LuaCodeFileName)
	{
		ExecuteLuaCode(string.Format("require('{0}')", LuaCodeFileName));
	}

	public void ReloadLuaScript(string LuaCodeFileName)
	{
		ExecuteLuaCode(string.Format("package.loaded['{0}'] = nil", LuaCodeFileName));
		ExecuteLuaCode(string.Format("require('{0}')", LuaCodeFileName));
	}

	private byte[] Loader(ref string Name)
	{
		return LuaLibraryFileCustomLoader(Name);
	}

	/// <summary>
	/// Custom loader for XLua: maps a module name or path to a file path and loads its byte content from cache.
	/// </summary>
	/// <param name="name">
	/// The Lua module identifier, which may include dots (e.g. "foo.bar.baz") or file extensions
	/// (".lua" or ".bytes"). Extensions will be stripped before path normalization.
	/// </param>
	/// <returns>
	/// The raw byte content of the Lua script if found in <c>CacheLuaScripts</c>; otherwise, <c>null</c>.
	/// </returns>
	public byte[] LuaLibraryFileCustomLoader(string name)
	{
		// Remove ".bytes" or ".lua" suffix, case-insensitive
		string moduleName = name;
		const string bytesExtensionName = ".bytes";
		const string luaExtensionName = ".lua";
		const string textExtensionName = ".txt";

		if (moduleName.EndsWith(bytesExtensionName, StringComparison.OrdinalIgnoreCase))
		{
			moduleName = moduleName.Substring(0, moduleName.Length - bytesExtensionName.Length);
		}
		else if (moduleName.EndsWith(luaExtensionName, StringComparison.OrdinalIgnoreCase))
		{
			moduleName = moduleName.Substring(0, moduleName.Length - luaExtensionName.Length);
		}
		else if (moduleName.EndsWith(textExtensionName, StringComparison.OrdinalIgnoreCase))
		{
			moduleName = moduleName.Substring(0, moduleName.Length - textExtensionName.Length);
		}

		// Replace dots with slashes to form a relative path
		// e.g. "foo.bar.baz" -> "foo/bar/baz"
		string relativePath = moduleName.Replace('.', '/');

		// Build the full file path using your project-specific PathUtil
		string filePath = PathUtil.GetLuaScriptPath(relativePath);

		// Attempt to retrieve the script from cache
		if (!CacheLuaScripts.TryGetValue(filePath, out byte[] luaBytes))
		{
			Debug.LogError($"Lua script not found: {filePath}");
			return null;
		}

		return luaBytes;
	}

	private void LoadLuaScript()
	{
		foreach (string name in LuaFileNameList)
		{
			FrameworkManager.Resource.LoadLuaScript
			(
				name,
				(UnityEngine.Object UnityObject) =>
				{
					AddLuaScript(name, (UnityObject as TextAsset).bytes);
					if(CacheLuaScripts.Count >= LuaFileNameList.Count)
					{
						FrameworkManager.Event.Invoke((int)GameEvent.LuaScriptsInitialize);

						LuaFileNameList.Clear();
						LuaFileNameList = null;
					}
				}
			);
		}
	}

	private void AddLuaScript(string AssetsName, byte[] LuaScript)
	{
		CacheLuaScripts[AssetsName] = LuaScript;
	}

	private void Update()
	{
		if (!System.Object.ReferenceEquals(LuaEnvironmentParser, null))
		{
			if ((DateTime.Now - LuaGCAndUpdateTick) >= TimeSpan.FromSeconds(LuaGCInterval))
			{
				LuaEnvironmentParser.Tick();
				LuaGCAndUpdateTick = DateTime.Now;

				if (UpdateFunction is null)
				{
					UpdateFunction = LuaEnvironmentParser.Global.Get<XLua.LuaFunction>("Update");
				}
				if (UpdateFunction is not null)
				{
					UpdateFunction.Call();
				}
			}
		}
	}

	public bool IsLuaEnvironmentDisposed()
	{
		if (System.Object.ReferenceEquals(LuaEnvironmentParser, null))
			return true;

		return this.LuaEnvironmentParser.rawL == System.IntPtr.Zero && this.IsDisposedValue;
	}

	public void DoFinalize()
	{
		if (!System.Object.ReferenceEquals(UpdateFunction, null))
		{
			UpdateFunction.Dispose();
			UpdateFunction = null;
		}

		if (!System.Object.ReferenceEquals(LuaEnvironmentParser, null))
		{
			LuaEnvironmentParser.ForceReleaseAllDelegateBridges();
			LuaEnvironmentParser.Dispose();
			LuaEnvironmentParser = null;
		}

		CacheLuaScripts.Clear();
		this.IsDisposedValue = true;
	}

#if UNITY_EDITOR
	private void EditorLoadLuaScript()
	{
		string[] luaFiles = Directory.GetFiles(PathUtil.LuaPath, "*.bytes", SearchOption.AllDirectories);
		for (int i = 0; i < luaFiles.Length; i++)
		{
			string fileName = PathUtil.GetStandardPath(luaFiles[i]);
			byte[] file = File.ReadAllBytes(fileName);
			AddLuaScript(PathUtil.GetUnityRelativePath(fileName), file);
		}
		FrameworkManager.Event.Invoke((int)GameEvent.LuaScriptsInitialize);
	}
#endif
}
