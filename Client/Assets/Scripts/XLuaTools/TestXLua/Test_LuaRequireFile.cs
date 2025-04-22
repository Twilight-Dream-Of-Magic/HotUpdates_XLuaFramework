using UnityEngine;

public class Test_LuaRequireFile : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
		SingletonLuaPE.Instance.ExecuteLuaCode("require('TestLuaScript00')");
	}

	private void OnDestroy()
	{
		SingletonLuaPE.Instance.ManualDispose();
	}
}
