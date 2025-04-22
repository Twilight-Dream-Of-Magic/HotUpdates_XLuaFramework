using System;
using System.Collections.Generic;
using System.Reflection;
using XLua;

namespace Utilities
{
	public static class LuaEnvironmentHackUtil
	{
		/// <summary>
		/// Forces release of all residual C# delegate bridges in the given LuaEnv.
		/// Must be called after all Lua¡úC# callbacks have been unbound (e.g., after Uninitialize).
		/// </summary>
		/// <param name="luaEnvironment">The LuaEnvironment instance whose delegate bridges should be cleared.</param>
		public static void ForceReleaseAllDelegateBridges(this LuaEnv luaEnvironment)
		{
			luaEnvironment.Tick();
			luaEnvironment.Tick();
			luaEnvironment.Tick();

			// Access the internal ObjectTranslator
			var translator = luaEnvironment.translator;

			// Get private 'delegate_bridges' dictionary
			var bridgesField = translator.GetType().GetField("delegate_bridges", BindingFlags.Instance | BindingFlags.NonPublic);
			Dictionary<int, WeakReference> delegate_bridges = bridgesField.GetValue(translator) as Dictionary<int, WeakReference>;
			if (delegate_bridges == null || delegate_bridges.Count == 0)
				return;

			// Copy keys to array to avoid using enumerator directly
			int count = delegate_bridges.Count;
			var keys = new int[count];
			delegate_bridges.Keys.CopyTo(keys, 0);

			// Release each delegate bridge using a normal for loop
			for (int i = 0; i < keys.Length; i++)
			{
				translator.ReleaseLuaBase(luaEnvironment.L, keys[i], true);
			}
		}
	}
}


