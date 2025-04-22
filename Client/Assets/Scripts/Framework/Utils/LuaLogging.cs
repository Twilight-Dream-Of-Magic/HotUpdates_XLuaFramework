using UnityEngine;

/*
	Call from lua script LogModule.bytes
*/
public static class LuaLogging
{
	private const string Prefix = "[LuaLogging]";

	public static void Info(string message)
	{
		if (!FrameworkConstant.AllowLuaLogging)
			return;
		Debug.LogFormat("{0}: {1}", Prefix, message);
	}

	public static void Warn(string message)
	{
		if (!FrameworkConstant.AllowLuaLogging)
			return;
		Debug.LogWarningFormat("{0}: {1}", Prefix, message);
	}

	public static void Error(string message)
	{
		if (!FrameworkConstant.AllowLuaLogging)
			return;
		Debug.LogErrorFormat("{0}: {1}", Prefix, message);
	}
}
