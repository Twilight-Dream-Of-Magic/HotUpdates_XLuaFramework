using ServerXLuaFreamwork.Network;
using System.Collections.Concurrent;

namespace ServerXLuaFreamwork.Server
{
	/// <summary>
	/// 全服唯一的线程安全消息处理管理器，其余地方均使用静态只读字典或局部变量。
	/// </summary>
	public static class MessageHandlerManager
	{
		// 全局唯一的线程安全容器：
		private static readonly ConcurrentDictionary<Type, Delegate[]> _messageCallbacks = new ConcurrentDictionary<Type, Delegate[]>();

		/// <summary>
		/// 为 T 类型消息订阅回调处理函数
		/// </summary>
		public static void Subscribe<T>(Action<Connection, T> callback)
		{
			var messageType = typeof(T);
			_messageCallbacks.AddOrUpdate(
				messageType,
				_ => new Delegate[] { callback },
				(_, existingCallbacks) => existingCallbacks.Append(callback).ToArray()
			);
		}

		/// <summary>
		/// 为 T 类型消息退订回调处理函数
		/// </summary>
		public static void Unsubscribe<T>(Action<Connection, T> callback)
		{
			var messageType = typeof(T);
			_messageCallbacks.AddOrUpdate(
				messageType,
				_ => Array.Empty<Delegate>(),
				(_, existingCallbacks) => existingCallbacks.Where(d => !d.Equals(callback)).ToArray()
			);

			// 如果回调列表为空，则移除该键
			if (_messageCallbacks.TryGetValue(messageType, out var callbacks) && callbacks.Length == 0)
				_messageCallbacks.TryRemove(messageType, out _);
		}

		/// <summary>
		/// 将 T 类型消息发送给所有订阅者（无锁实现）
		/// </summary>
		public static void Dispatch<T>(Connection sender, T message)
		{
			if (_messageCallbacks.TryGetValue(typeof(T), out var subscribers))
				foreach (Action<Connection, T> callback in subscribers)
					callback(sender, message);
		}
	}
}
