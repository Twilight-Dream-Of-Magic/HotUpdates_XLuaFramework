#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using XLua;
using System.Collections.Generic;


namespace XLua.CSObjectWrap
{
    using Utils = XLua.Utils;
    public class FrameworkConstantWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(FrameworkConstant);
			Utils.BeginObjectRegister(type, L, translator, 0, 0, 0, 0);
			
			
			
			
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 7, 2, 2);
			
			
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "BundleExtension", FrameworkConstant.BundleExtension);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "VersionFileList", FrameworkConstant.VersionFileList);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "VersionFileHashList", FrameworkConstant.VersionFileHashList);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ProtectedVersionFileList", FrameworkConstant.ProtectedVersionFileList);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "ProtectedVersionFileHashList", FrameworkConstant.ProtectedVersionFileHashList);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "HotUpdateRootURL", FrameworkConstant.HotUpdateRootURL);
            
			Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "AllowLogging", _g_get_AllowLogging);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "GDM", _g_get_GDM);
            
			Utils.RegisterFunc(L, Utils.CLS_SETTER_IDX, "AllowLogging", _s_set_AllowLogging);
            Utils.RegisterFunc(L, Utils.CLS_SETTER_IDX, "GDM", _s_set_GDM);
            
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            return LuaAPI.luaL_error(L, "FrameworkConstant does not have a constructor!");
        }
        
		
        
		
        
        
        
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_AllowLogging(RealStatePtr L)
        {
		    try {
            
			    LuaAPI.lua_pushboolean(L, FrameworkConstant.AllowLogging);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_GDM(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.Push(L, FrameworkConstant.GDM);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_AllowLogging(RealStatePtr L)
        {
		    try {
                
			    FrameworkConstant.AllowLogging = LuaAPI.lua_toboolean(L, 1);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_GDM(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			GameDeploymentMode gen_value;translator.Get(L, 1, out gen_value);
				FrameworkConstant.GDM = gen_value;
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
		
		
    }
}
