
namespace ServerXLuaFreamwork
{
	public abstract class Singleton<T> where T : new()
	{
		private static readonly Lazy<T> _lazy = new(() => new T());
		public static T Instance => _lazy.Value;
	}
}
