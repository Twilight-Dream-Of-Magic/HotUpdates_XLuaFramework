using System.Net;
using System.Net.Sockets;
using ServerXLuaFreamwork.Network;

namespace ServerXLuaFreamwork.Services
{
	class NetworkService : IDisposable
	{
		private readonly TcpListener _listener;
		private readonly ManualResetEventSlim _stopped = new(false);
		private volatile bool _running;

		public bool IsRunning
		{
			get { return _running; }
		}

		public NetworkService(IPAddress address, int port)
		{
			_listener = new TcpListener(address, port);

			InitializeCommunicateLogics();
		}

		/// <summary>
		/// 启动并阻塞，直到 Stop() 被调用。
		/// </summary>
		public void Start()
		{
			_listener.Start();
			_running = true;
			Console.WriteLine("NetworkService Started");
			Console.WriteLine($"Listening on {_listener.LocalEndpoint}");
			ConnectionPool.Instance.ConnectionCountChanged += PrintConnected;

			// 在 ThreadPool 中接收客户端
			for (int i = 0; i < 2; i++)
				ThreadPool.QueueUserWorkItem(_ => AcceptLoop());
		}

		/// <summary>
		/// 通知服务停止并释放阻塞
		/// </summary>
		public void Stop()
		{
			if (!_running)
				return;
			_listener.Stop();
			ConnectionPool.Instance.ConnectionCountChanged -= PrintConnected;
			Console.WriteLine("NetworkService Stoped");
			_running = false;
		}

		private void PrintConnected(int count)
		{
			Console.WriteLine($">> Alive connected count: {count}");
		}

		private void InitializeCommunicateLogics()
		{
			CommunicateLogic.TestLogic.Instance.Initialize();
		}

		private void AcceptLoop()
		{
			while (_running)
			{
				try
				{
					TcpClient client = _listener.AcceptTcpClient();
					Console.WriteLine($"Accept connection: {client.Client.RemoteEndPoint}");
					HandleClient(client);
				}
				catch (SocketException)
				{
					break;
				}
			}
		}

		private void HandleClient(TcpClient client)
		{
			Connection connection = new Connection(client);
		
			Console.WriteLine(">> New client connection, waiting for it to finish…");

			connection.OpenReceiveLoop();

			while (!connection.isWorked)
			{
				if (connection.LastReceivedTimeUTC == DateTime.MinValue)
					connection.LastReceivedTimeUTC = DateTime.UtcNow;
				else if (DateTime.UtcNow - connection.LastReceivedTimeUTC > TimeSpan.FromSeconds(60))
				{
					//Timeout by idle
					Console.WriteLine(">> Timeout 60 seconds by idle！");
					connection.cancellationTokenSource.Cancel();
					break;
				}

				Thread.Sleep(TimeSpan.FromSeconds(1));
			}

			connection.Close();

			Console.WriteLine(">> Current Client Connection fully cleaned up");

			if (ConnectionPool.Instance.AliveCount <= 0)
			{
				Stop();
			}
		}

		public void Dispose() => Stop();
	}
}