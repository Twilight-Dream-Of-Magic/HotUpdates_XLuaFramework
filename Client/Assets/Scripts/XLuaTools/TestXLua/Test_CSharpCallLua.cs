using UnityEngine;

public class Test_CSharpCallLua : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
		SingletonLuaPE.Instance.ExecuteLuaCode("print(\"Hello CSharp !\")");
	}

	private void OnDestroy()
	{
		SingletonLuaPE.Instance.ManualDispose();
	}
}
