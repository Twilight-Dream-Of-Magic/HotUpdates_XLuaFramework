using System.Collections.Generic;
using UnityEngine;

public class NetManager : MonoBehaviour
{
	NetClient Client;
	Queue<KeyValuePair<int, string>> MessageQueue = new Queue<KeyValuePair<int, string>>();
	XLua.LuaFunction XLuaFunction_ReceiveMessage;

	public void Initialize()
	{
		Client = new NetClient();
		XLuaFunction_ReceiveMessage = FrameworkManager.Lua.LuaEnvironmentParser.Global.Get<XLua.LuaFunction>("ReceiveMessage");
	}

	public void SendMessage(int MessageID, string Message)
	{ 
		Client.SendMessage(MessageID, Message);
	}

	public void ConnectedServer(string host, int port)
	{
		Client.OnConnectToServer(host, port);
	}

	public void OnConnected()
	{
		
	}

	public void OnDisconnected()
	{
		
	}

	private void ReceiveMessage(int MessageID, string Message)
	{
		MessageQueue.Enqueue(new KeyValuePair<int, string>(MessageID, Message));
	}

	private void Update()
	{
		if(MessageQueue.Count > 0)
		{
			KeyValuePair<int, string> Message = MessageQueue.Dequeue();
			XLuaFunction_ReceiveMessage?.Call(Message.Key, Message.Value);
		}
	}
}
