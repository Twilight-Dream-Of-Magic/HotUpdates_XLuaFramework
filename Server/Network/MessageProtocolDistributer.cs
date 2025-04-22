using System.Reflection;
using ServerXLuaFreamwork.Network;
using ServerXLuaFreamwork.Protocol;
using Newtonsoft.Json;

namespace ServerXLuaFreamwork.Server
{
	/// <summary>
	/// 单例——负责：
	///  1) 扫描 [MessageId] 建立 ID↔Type ↔ FastDispatch 委托
	///  2) 把 Subscribe/Unsubscribe 泛型调用 转给 HandlerManager
	///  3) 在 ParseMessage 后调用 TryDispatch 将 JSON→T + 发布给 HandlerManager
	/// </summary>
	public class MessageProtocolDistributer : Singleton<MessageProtocolDistributer>
	{
		// —— 只读字典，线程安全因不再修改 —— 
		private readonly Dictionary<int, Type> _id2Type;
		private readonly Dictionary<Type, int> _type2Id;
		private readonly Dictionary<int, Action<Connection, string>> _fastDispatch;

		// 私有构造，Singleton 基类会调用
		public MessageProtocolDistributer()
		{
			_id2Type = new();
			_type2Id = new();
			_fastDispatch = new();

			var asm = Assembly.GetExecutingAssembly();
			foreach (var t in asm.DefinedTypes)
			{
				var attr = t.GetCustomAttribute<MessageIdAttribute>();
				if (attr == null) continue;
				int id = (int)attr.Id;
				var type = t.AsType();

				_id2Type[id] = type;
				_type2Id[type] = id;
				_fastDispatch[id] = BuildFastDelegate(type);
			}
		}

		/// <summary>
		/// 业务层调用：订阅 T 类型的消息
		/// </summary>
		public void Subscribe<T>(Action<Connection, T> cb) where T : class
			=> MessageHandlerManager.Subscribe(cb);

		/// <summary>
		/// 业务层调用：退订 T 类型的消息
		/// </summary>
		public void Unsubscribe<T>(Action<Connection, T> cb) where T : class
			=> MessageHandlerManager.Unsubscribe(cb);

		/// <summary>
		/// 网络层在拆包后调用，将 rawId/json → 强类型 T，然后发布到 MessageHandlerManager
		/// </summary>
		public bool TryDispatch(int rawId, string json, Connection sender)
		{
			if (_fastDispatch.TryGetValue(rawId, out var fast))
			{
				fast(sender, json);
				return true;
			}
			return false;
		}

		/// <summary>
		/// 如果你想在底层自动附 ID（如 Send&lt;T&gt; 重载中），可用它拿 ID。
		/// </summary>
		public int GetIdOf<T>() where T : class
			=> _type2Id[typeof(T)];

		// 内部：为每个消息类型生成一个直接调用 HandlerManager.Dispatch<T> 的委托
		// 此方法使用反射来找到以下两种泛型方法：
		// 1. JsonConvert.DeserializeObject<T>(string json) —— 用于将 JSON 字符串反序列化成消息对象 T
		// 2. MessageHandlerManager.Dispatch<T>(Connection conn, T msg) —— 用于通知所有订阅者接收到消息对象 T
		private static Action<Connection, string> BuildFastDelegate(Type msgType)
		{
			// 找到 Newtonsoft.Json.JsonConvert 中的泛型反序列化方法 DeserializeObject<T>(string)
			var jsonDeserializeMethodDefinition = typeof(JsonConvert)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(method =>
			method.Name == nameof(JsonConvert.DeserializeObject) &&
			method.IsGenericMethodDefinition &&
			method.GetParameters().Length == 1 &&
			method.GetParameters()[0].ParameterType == typeof(string)
			)
			.Single(); // 确保只找到唯一的定义

			// 用具体的消息类型替换泛型参数 T
			var jsonDeserializeMethod = jsonDeserializeMethodDefinition.MakeGenericMethod(msgType);

			// 找到 MessageHandlerManager 类中负责分发消息的泛型方法 Dispatch<T>(Connection, T)
			var messageDispatchMethodDefinition = typeof(MessageHandlerManager)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(method =>
			method.Name == nameof(MessageHandlerManager.Dispatch) &&
			method.IsGenericMethodDefinition
			)
			.Single(); // 确保只找到唯一的定义

			// 用具体的消息类型替换泛型参数 T
			var messageDispatchMethod = messageDispatchMethodDefinition.MakeGenericMethod(msgType);

			// 返回一个委托，它负责：
			// 1. 使用 jsonDeserializeMethod 将 JSON 字符串转换为消息对象
			// 2. 调用 messageDispatchMethod 将转换后的消息对象发布给所有订阅者
			return (connection, jsonData) =>
			{
				// 反序列化 JSON 字符串为消息对象
				var messageObject = jsonDeserializeMethod.Invoke(null, new object[] { jsonData });
				// 通过 MessageHandlerManager 分发消息对象
				messageDispatchMethod.Invoke(null, new object[] { connection, messageObject! });
			};
		}
	}
}
