using System.Text;
using System.IO;
using UnityEngine;
using XLua;

public class SingletonLuaPE
{
	private LuaEnv LuaEnvironmentParser = null;

	private SingletonLuaPE()
	{
		LuaEnvironmentParser = new LuaEnv();
		LuaEnvironmentParser.AddLoader(LuaLibraryFileCustomLoader);
	}

	private static SingletonLuaPE _Instance = null;

	public static SingletonLuaPE Instance
	{
		get
		{
			if (_Instance == null)
			{
				_Instance = new SingletonLuaPE();
			}

			return _Instance;
		}
	}

	//Custom Lua library file loaders are executed in preference to the default system built-in loader.
	//Subsequent Lua library file loaders will not execute if and when the custom Lua library file loader loads the appropriate file.
	private byte[] LuaLibraryFileCustomLoader(ref string FilePath)
	{
		string path = Application.dataPath;
		StringBuilder stringBuilder = new StringBuilder();

		//Go back one level above the subdirectory "asset".
		string backedpath = path.Substring(0, path.Length - 7);
		//Constructing a new file path
		stringBuilder.Append(backedpath).Append("/LuaScriptPath/").Append(FilePath).Append(".lua");
		path = stringBuilder.ToString();
		Debug.Log(path);

		if (File.Exists(path))
		{
			//Returns file byte data to the Lua parser environment.
			return File.ReadAllBytes(path);
		}
		else
		{
			return null;
		}
	}

	public void ManualDispose()
	{
		if (LuaEnvironmentParser != null)
		{
			LuaEnvironmentParser.Dispose();
			LuaEnvironmentParser = null;
		}
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

	public void LoadLuaCode(string LuaCodeFileName)
	{
		ExecuteLuaCode(string.Format("require('{0}')", LuaCodeFileName));
	}

	public void ReloadLuaCode(string LuaCodeFileName)
	{
		ExecuteLuaCode(string.Format("package.loaded['{0}'] = nil", LuaCodeFileName));
		ExecuteLuaCode(string.Format("require('{0}')", LuaCodeFileName));
	}

	public LuaTable Global
	{
		get { return LuaEnvironmentParser.Global; }
	}
}
