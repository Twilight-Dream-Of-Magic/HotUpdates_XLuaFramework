using System.Text;
using ServerXLuaFreamwork.Server;

namespace ServerXLuaFreamwork.Network
{
	public class MessageHandler : IDisposable
	{
		private readonly Connection clientConnection;
		private readonly MemoryStream _memoryStream = new MemoryStream();
		private readonly BinaryWriter _binaryWriter;

		public MessageHandler(Connection connection)
		{
			clientConnection = connection;
			// leaveOpen: true 保证我们自己控制 _ms 生命周期
			_binaryWriter = new BinaryWriter(_memoryStream, Encoding.UTF8, leaveOpen: true);
		}

		public void Dispose()
		{
			_binaryWriter.Close();
			_memoryStream.Close();
		}

		/// <summary>
		/// 打包成 [ID:int32][Len:int32][Body:UTF8 bytes]
		/// </summary>
		public byte[] PackMessage(int message_id, byte[] body_data)
		{
			// 1) 清空残留数据
			_memoryStream.SetLength(0);
			// 2) 写头
			_binaryWriter.Write(message_id);
			_binaryWriter.Write(body_data.Length);
			// 3) 写体
			_binaryWriter.Write(body_data);
			_binaryWriter.Flush();
			// 4) 拷贝
			var result = new byte[_memoryStream.Length];
			Array.Copy(_memoryStream.GetBuffer(), result, result.Length);
			return result;
		}

		/// <summary>
		/// 解析一个完整帧（Assume: data 包含完整的一个 [head+body]）
		/// </summary>
		public void UnpackMessageAndParse(byte[] data)
		{
			if (data.Length < 8)
				return;
			using (MemoryStream memoryStream = new MemoryStream(data))
			{
				using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
				{
					int message_id = binaryReader.ReadInt32();
					int body_data_length = binaryReader.ReadInt32();
					var body_data = binaryReader.ReadBytes(body_data_length);
					string json = Encoding.UTF8.GetString(body_data);

					// ... 拆包得到 id, json ...
					if (!MessageProtocolDistributer.Instance.TryDispatch(message_id, json, clientConnection))
						Console.WriteLine($"[Warn] No handler for MessageID={message_id}");
				}
			}
		}
	}
}
