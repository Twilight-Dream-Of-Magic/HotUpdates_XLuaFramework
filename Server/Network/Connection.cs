using System.Net.Sockets;
using System.Text;
using ServerXLuaFreamwork.Protocol;
using ServerXLuaFreamwork.Server;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ServerXLuaFreamwork.Network
{
	public class Connection
	{
		private readonly TcpClient tcpClient;
		private readonly NetworkStream networkStream;
		private readonly byte[] buffer = new byte[8192];
		private readonly MemoryStream dataCache = new MemoryStream();
		private readonly MessageHandler messageHandler;

		public DateTime LastReceivedTimeUTC = DateTime.MinValue;
		public readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

		public bool isWorked = false;

		public Connection(TcpClient client)
		{
			tcpClient = client;
			networkStream = client.GetStream();
			messageHandler = new MessageHandler(this);
			ConnectionPool.Instance.Add(this);
		}

		/// <summary>
		/// 由外部调用
		/// </summary>
		public void OpenReceiveLoop()
		{
			Task.Run
			(
				async () =>
				{
					isWorked = await ReceiveLoop();
				}
			);
		}

		private async Task<bool> ReceiveLoop()
		{
			try
			{
				while (!cancellationTokenSource.IsCancellationRequested)
				{
					int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
					if (bytesRead == 0)
					{
						return true;
					}

					LastReceivedTimeUTC = DateTime.UtcNow;

					// 1) 累积数据
					dataCache.Seek(0, SeekOrigin.End);
					dataCache.Write(buffer, 0, bytesRead);
					dataCache.Seek(0, SeekOrigin.Begin);

					// 2) 循环切包，只要缓存中至少有 8 字节，则读取消息头取出长度，若数据充足再进行切包
					while (dataCache.Length - dataCache.Position >= 8)
					{
						long initialPosition = dataCache.Position;
						using var reader = new BinaryReader(dataCache, Encoding.UTF8, leaveOpen: true);
						int messageId = reader.ReadInt32();
						int dataLength = reader.ReadInt32();
						if (dataCache.Length - dataCache.Position < dataLength)
						{
							// 半包，回退到包头
							dataCache.Position = initialPosition;
							break;
						}
						// 3) 读取一整包数据（头 + 体）
						dataCache.Position = initialPosition;
						byte[] packet = new byte[8 + dataLength];
						dataCache.Read(packet, 0, packet.Length);

						// 4) 交由 MessageHandler 解析消息包
						messageHandler.UnpackMessageAndParse(packet);
					}

					// 5) 清理已处理的数据，将剩余数据移至流头
					long remainingBytes = dataCache.Length - dataCache.Position;
					if (remainingBytes > 0)
					{
						var tempData = dataCache.GetBuffer().AsSpan((int)dataCache.Position, (int)remainingBytes).ToArray();
						dataCache.SetLength(0);
						dataCache.Write(tempData, 0, tempData.Length);
					}
					else
					{
						dataCache.SetLength(0);
					}
				}
			}
			catch (OperationCanceledException)
			{
				/* 正常取消 */
				return true;
			}
			catch (IOException io_except)
			{
				dataCache.SetLength(0);
				networkStream.Close();
				tcpClient.Close();
				Console.WriteLine($"ReceiveLoop IO异常: {io_except.Message}");
				return false;
			}
			catch (Exception except)
			{
				Console.WriteLine($"ReceiveLoop 异常: {except.Message}");
				return false;
			}
			finally
			{
				isWorked = true;
			}

			return true;
		}

		/// <summary>
		/// 发送 “组合消息” Protocol.Message
		/// </summary>
		public Task SendMessage(Protocol.Message bundle)
		{
			var tasks = new List<Task>();
			foreach (var kv in bundle.Responses)
			{
				int messageId = kv.Key;
				string json = JsonConvert.SerializeObject(kv.Value);
				byte[] body = Encoding.UTF8.GetBytes(json);
				var packet = messageHandler.PackMessage(messageId, body);
				tasks.Add(networkStream.WriteAsync(packet, 0, packet.Length));
			}
			return Task.WhenAll(tasks);
		}

		/// <summary>
		/// 发送单个响应
		/// </summary>
		public Task Send<T>(T response) where T : ResponseBase
		{
			int id = MessageProtocolDistributer.Instance.GetIdOf<T>();
			var bundle = new Protocol.Message();
			bundle.Responses[id] = response;
			return SendMessage(bundle);
		}

		public void Close()
		{
			Console.WriteLine($"Client: {tcpClient.Client.RemoteEndPoint} disconnected.");
			tcpClient.Close();
			ConnectionPool.Instance.Remove(this);
		}
	}
}
