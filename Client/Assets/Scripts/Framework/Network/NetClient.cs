using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class NetClient
{
	private TcpClient TCP_Client;
	private NetworkStream TCP_NetworkStream;
	private const int BufferSize = 1024 * 8;
	private byte[] Buffer;
	private MemoryStream LocalMemoryStream;
	private BinaryReader BinaryReader;

	public NetClient()
	{
		LocalMemoryStream = new MemoryStream();
		BinaryReader = new BinaryReader(LocalMemoryStream);
		Buffer = new byte[BufferSize];
	}

	public void OnConnectToServer(string host, int port)
	{
		try
		{
			IPAddress[] IPAddresses = Dns.GetHostAddresses(host);
			if (IPAddresses.Length == 0)
			{
				Debug.LogError("Host Or IP is Invaild!");
				return;
			}

			if (IPAddresses[0].AddressFamily == AddressFamily.InterNetworkV6)
			{
				TCP_Client = new TcpClient(AddressFamily.InterNetworkV6);
			}
			else
			{
				TCP_Client = new TcpClient(AddressFamily.InterNetwork);
			}

			TCP_Client.SendTimeout = 1000;
			TCP_Client.ReceiveTimeout = 1000;
			TCP_Client.NoDelay = true;
			TCP_Client.BeginConnect(host, port, OnConnect, null);
		}
		catch (System.Exception except)
		{
			Debug.LogError(except.Message);
		}
	}

	private void OnConnect(IAsyncResult ar)
	{
		if (TCP_Client == null || !TCP_Client.Connected)
		{
			Debug.LogError("Client Connect To Server Is Error!");
			return;
		}

		FrameworkManager.Net.OnConnected();
		TCP_NetworkStream = TCP_Client.GetStream();
		TCP_NetworkStream.BeginRead(Buffer, 0, BufferSize, OnReadStream, null);
	}

	private void OnReadStream(IAsyncResult ar)
	{
		try
		{
			if (TCP_Client == null || TCP_NetworkStream == null)
				return;

			int MessageLength = TCP_NetworkStream.EndRead(ar);

			if (MessageLength < 1)
			{
				OnDisconnected();
				return;
			}

			ReceiveData(MessageLength);

			lock (TCP_NetworkStream)
			{
				Array.Clear(Buffer, 0, Buffer.Length);
				TCP_NetworkStream.BeginRead(Buffer, 0, BufferSize, OnReadStream, null);
			}
		}
		catch (System.Exception except)
		{
			Debug.LogError(except.Message);
			OnDisconnected();
		}
	}

	private int RemainingByteLength() { return (int)(LocalMemoryStream.Length - LocalMemoryStream.Position); }

	private void ReceiveData(int ReceiveMessageLength)
	{
		//Append Buffer Data
		LocalMemoryStream.Seek(0, SeekOrigin.End);
		LocalMemoryStream.Write(Buffer, 0, ReceiveMessageLength);
		LocalMemoryStream.Seek(0, SeekOrigin.Begin);
		while (RemainingByteLength() > 8)
		{
			int MessageID = BinaryReader.ReadInt32();
			int MessageLength = BinaryReader.ReadInt32();

			if (RemainingByteLength() >= MessageLength)
			{
				byte[] ByteData = BinaryReader.ReadBytes(MessageLength);
				string Message = System.Text.Encoding.UTF8.GetString(ByteData);

				//Enqueue Message Queue
				FrameworkManager.Net.ReceiveMessage(MessageID, Message);
			}
			else
			{
				LocalMemoryStream.Position = LocalMemoryStream.Position - 8;
				break;
			}
		}

		byte[] RemainingByteData = BinaryReader.ReadBytes(RemainingByteLength());
		LocalMemoryStream.SetLength(0);
		LocalMemoryStream.Write(RemainingByteData, 0, RemainingByteData.Length);
	}

	public void SendMessage(int MessageID, string Message)
	{
		using (MemoryStream TemporaryMemoryStream = new MemoryStream())
		{
			TemporaryMemoryStream.Position = 0;
			BinaryWriter BinaryWriter = new BinaryWriter(TemporaryMemoryStream);
			byte[] ByteData = System.Text.Encoding.UTF8.GetBytes(Message);

			//ID
			BinaryWriter.Write(MessageID);
			//Message Length
			BinaryWriter.Write((int)ByteData.Length);
			//Message Content
			BinaryWriter.Write(ByteData);
			BinaryWriter.Flush();

			if (TCP_Client != null && TCP_Client.Connected)
			{
				byte[] SendByteData = TemporaryMemoryStream.ToArray();
				TCP_NetworkStream.BeginWrite(SendByteData, 0, SendByteData.Length, OnWritedStream, null);
			}
			else
			{
				Debug.LogError("Client Connect To Server Is Error! (On Send Message)");
			}
		}
	}

	private void OnWritedStream(IAsyncResult ar)
	{
		try
		{
			TCP_NetworkStream.EndWrite(ar);
		}
		catch (System.Exception except)
		{
			Debug.LogError(except.Message);
			OnDisconnected();
		}
	}

	private void OnDisconnected()
	{
		if(TCP_Client != null && TCP_Client.Connected)
		{
			TCP_Client.Close();
			TCP_Client = null;

			TCP_NetworkStream.Close();
			TCP_NetworkStream = null;
		}

		FrameworkManager.Net.OnDisconnected();
	}
}