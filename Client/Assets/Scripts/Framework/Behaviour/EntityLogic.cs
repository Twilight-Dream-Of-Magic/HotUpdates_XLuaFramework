using System;

public class EntityLogic : LuaBehaviour
{
	private Action LuaScriptOnShow;
	private Action LuaScriptOnHide;

	public override void Initialize(string LuaFilePath)
	{
		base.Initialize(LuaFilePath);
		this.LuaScriptEnvironment.Get("OnShowEntity", out this.LuaScriptOnShow);
		this.LuaScriptEnvironment.Get("OnHideEntity", out this.LuaScriptOnHide);
	}

	public void OnShow()
	{
		this.LuaScriptOnShow?.Invoke();
	}

	public void OnHide()
	{
		this.LuaScriptOnHide?.Invoke();
	}

	public override void Uninitialize()
	{
		this.LuaScriptOnShow = null;
		this.LuaScriptOnHide = null;
		base.Uninitialize();
	}
}
