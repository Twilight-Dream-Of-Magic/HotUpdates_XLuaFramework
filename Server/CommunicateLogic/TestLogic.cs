using ServerXLuaFreamwork.MessageData;
using ServerXLuaFreamwork.Protocol;
using ServerXLuaFreamwork.Network;
using ServerXLuaFreamwork.Server;

namespace ServerXLuaFreamwork.CommunicateLogic
{
	public class TestLogic : Singleton<TestLogic>, IDisposable
	{
		public void Initialize()
		{
			MessageProtocolDistributer.Instance.Subscribe<TestRequest>(OnTest);
		}

		public void Dispose()
		{
			MessageProtocolDistributer.Instance.Unsubscribe<TestRequest>(OnTest);
		}

		private void OnTest(Connection sender, TestRequest request)
		{
			Console.WriteLine($"[Test] user={request.user} Protocol={request}");
			foreach (var i in request.listTest)
				Console.WriteLine($"  list={i}");

			Protocol.Message message = new Protocol.Message();
			TestResponse test_response = new TestResponse
			{
				result_code = ResultCode.None,
				vector3 = new Vector3(1, 1, 1),
				user = request.user,
				password = "********",
				listTest = new List<int> { 4, 5, 6 }
			};

			if(!message.Responses.ContainsKey((int)Protocol.MessageID.TestResponse))
				message.Responses.Add((int)Protocol.MessageID.TestResponse, test_response);
			_ = sender.SendMessage(message);
		}
	}
}
