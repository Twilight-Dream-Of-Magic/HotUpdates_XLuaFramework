using System;

public class CustomSceneLogic : LuaBehaviour
{
	public string SceneName;

	private Action LuaScriptOnActive;
	private Action LuaScriptOnInactive;
	private Action LuaScriptOnEnter;
	private Action LuaScriptOnExit;

	public override void Initialize(string LuaFilePath)
	{
		base.Initialize(LuaFilePath);
		this.LuaScriptEnvironment.Get("OnActiveScene", out this.LuaScriptOnActive);
		this.LuaScriptEnvironment.Get("OnInactiveScene", out this.LuaScriptOnInactive);
		this.LuaScriptEnvironment.Get("OnEnterScene", out this.LuaScriptOnEnter);
		this.LuaScriptEnvironment.Get("OnExitScene", out this.LuaScriptOnExit);
	}

	public void OnActive()
	{
		this.LuaScriptOnActive?.Invoke();
	}

	public void OnInactive()
	{
		this.LuaScriptOnInactive?.Invoke();
	}

	public void OnEnter()
	{
		this.LuaScriptOnEnter?.Invoke();
	}

	public void OnExit()
	{
		this.LuaScriptOnExit?.Invoke();
	}

	public override void Uninitialize()
	{
		this.LuaScriptOnActive = null;
		this.LuaScriptOnInactive = null;
		this.LuaScriptOnEnter = null;
		this.LuaScriptOnExit = null;
		base.Uninitialize();
	}
}
