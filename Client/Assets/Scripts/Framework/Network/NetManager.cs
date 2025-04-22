using System.Collections.Generic;
using UnityEngine;

[XLua.LuaCallCSharp]
public class NetManager : MonoBehaviour
{
	NetClient Client;
	Queue<KeyValuePair<int, string>> MessageQueue = new Queue<KeyValuePair<int, string>>();
	XLua.LuaFunction XLuaFunction_ReceiveMessage;

	public void Initialize()
	{
		Client = new NetClient();
		XLuaFunction_ReceiveMessage = FrameworkManager.Lua.LuaEnvironmentParser.Global.Get<XLua.LuaFunction>("LuaReceiveMessage");
	}


	// Call from lua
	public void SendMessage(int MessageID, string Message)
	{ 
		Client.SendMessage(MessageID, Message);
	}

	// Call from lua
	public void ConnectServer(string host, int port)
	{
		Client.OnConnectToServer(host, port);
	}

	public void OnConnected()
	{
		
	}

	public void OnDisconnected()
	{
		
	}

	public void ReceiveMessage(int MessageID, string Message)
	{
		MessageQueue.Enqueue(new KeyValuePair<int, string>(MessageID, Message));
	}

	private void Update()
	{
		if(MessageQueue.Count > 0)
		{
			//Dequeue Message Queue
			KeyValuePair<int, string> Message = MessageQueue.Dequeue();

			//Call Lua Function
			XLuaFunction_ReceiveMessage?.Call(Message.Key, Message.Value);
		}
	}
}
