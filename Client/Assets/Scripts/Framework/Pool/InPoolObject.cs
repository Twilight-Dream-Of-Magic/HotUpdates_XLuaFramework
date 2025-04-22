public class InPoolObject
{
	//Specific objects
	//具体的对象
	public UnityEngine.Object @Object;

	//Object name
	//对象的名字
	public string Name;

	//Time of last use
	//最后一次使用的时间
	public System.DateTime LastUseTime;

	public InPoolObject(string Name, UnityEngine.Object Object)
	{
		this.Name = Name;
		this.Object = Object;
		LastUseTime = System.DateTime.Now;
	}
}
