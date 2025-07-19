
using ServerXLuaFreamwork.Protocol;

namespace ServerXLuaFreamwork.Network
{
	public sealed class ConnectionPool : Singleton<ConnectionPool>
	{
		private readonly HashSet<Connection> _set = new();
		private readonly object _lock = new();

		public event Action<int>? ConnectionCountChanged;

		/// <summary>
		/// ��ǰ��Ծ������
		/// </summary>
		public int AliveCount
		{
			get
			{
				lock (_lock)
					return _set.Count;
			}
		}

		public void Add(Connection connection)
		{
			int newCount;
			lock (_lock)
			{
				_set.Add(connection);
				newCount = _set.Count;
			}
			ConnectionCountChanged?.Invoke(newCount);
		}

		public void Remove(Connection connection)
		{
			int newCount;
			lock (_lock)
			{
				_set.Remove(connection);
				newCount = _set.Count;
			}
			ConnectionCountChanged?.Invoke(newCount);
		}

		/// <summary>
		/// �㲥һ�������� Protocol.Message����ϰ���
		/// </summary>
		public Task Broadcast(Protocol.Message bundle)
		{
			List<Task> tasks = new();
			lock (_lock)
			{
				foreach (var c in _set)
					tasks.Add(c.SendMessage(bundle));
			}
			return Task.WhenAll(tasks);
		}

		/// <summary>
		/// �㲥������Ӧ���ڲ��Զ���װ�� Protocol.Message ���������
		/// </summary>
		public Task Broadcast<T>(T response) where T : ResponseBase
		{
			List<Task> tasks = new();
			lock (_lock)
			{
				foreach (var c in _set)
					tasks.Add(c.Send(response));
			}
			return Task.WhenAll(tasks);
		}
	}
}
