
[XLua.LuaCallCSharp]
public static class UnityExtension
{
	public static void OnClickSet(this UnityEngine.UI.Button @Button, object Callback)
	{
		XLua.LuaFunction function = Callback as XLua.LuaFunction;
		@Button.onClick.RemoveAllListeners();
		@Button.onClick.AddListener
		(
			() =>
			{
				function?.Call();
			}
		);
	}

	public static void OnValueChangedSet(this UnityEngine.UI.Slider @Slider, object Callback)
	{
		XLua.LuaFunction function = Callback as XLua.LuaFunction;
		@Slider.onValueChanged.RemoveAllListeners();
		@Slider.onValueChanged.AddListener
		(
			(float value) =>
			{
				function?.Call(value);
			}
		);
	}
}
