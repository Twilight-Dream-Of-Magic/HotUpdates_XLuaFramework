using System;
using UnityEngine;

public class Test_LuaCallCSharp : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
		TestFunction_LuaCallCSharp();
		TestFunction_LuaReturnData();
	}

	public void TestFunction_LuaCallCSharp()
	{
		SingletonLuaPE.Instance.ExecuteLuaCode("CS.UnityEngine.Debug.Log(\" Call From Lua! \")");
	}

	public void TestFunction_LuaReturnData()
	{
		object[] LuaDataObjects = SingletonLuaPE.Instance.ExecuteLuaCode("return 100, 'String'");
		var number = LuaDataObjects[0];
		var stringData = LuaDataObjects[1];
		UnityEngine.Debug.LogFormat("LuaDataObjects[0] type is {0}", number.GetType().ToString());
		UnityEngine.Debug.LogFormat("LuaDataObjects[1] type is {0}", stringData.GetType().ToString());
		if (LuaDataObjects[0] is Int64)
		{
			UnityEngine.Debug.LogFormat("LuaDataObjects[0] is {0}", number);
		}
		if (LuaDataObjects[1] is String)
		{
			UnityEngine.Debug.LogFormat("LuaDataObjects[1] is {0}", stringData);
		}
	}

	private void OnDestroy()
	{
		SingletonLuaPE.Instance.ManualDispose();
	}
}
