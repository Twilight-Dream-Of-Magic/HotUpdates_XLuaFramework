
namespace ServerXLuaFreamwork.Protocol
{
	/*── 消息号 ──*/
	public enum MessageID
	{
		TestRequest = 1000,
		TestResponse = 1001,
		PasswordWrongResponse = 1003,
		RoleNameRepeat = 1004,
		RoleIndexError = 1005
	}

	/*── 通用码 ──*/
	public enum ResultCode
	{
		None = 0,
		True = 1,
		False = 2,
		Unknown = 3
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class MessageIdAttribute : Attribute
	{
		public MessageID Id { get; }
		public MessageIdAttribute(MessageID id) => Id = id;
	}

	public class RequsetBase
	{

	}
	public class ResponseBase
	{
		public ResultCode result_code = ResultCode.None;
	}

	/*── 公共基类 ──*/
	public class Message
	{
		public Dictionary<int, ResponseBase> Requsets = new Dictionary<int, ResponseBase>();
		public Dictionary<int, ResponseBase> Responses = new Dictionary<int, ResponseBase>();
	}

	/*── 1000 请求 / 响应 ──*/
	[MessageId(MessageID.TestRequest)]
	public class TestRequest : RequsetBase
	{
		public string user;
		public string password;
		public List<int> listTest;
	}

	[MessageId(MessageID.TestResponse)]
	public class TestResponse : ResponseBase
	{
		public MessageData.Vector3 vector3;
		
		public string user;
		public string password;
		public List<int> listTest;
	}
}
