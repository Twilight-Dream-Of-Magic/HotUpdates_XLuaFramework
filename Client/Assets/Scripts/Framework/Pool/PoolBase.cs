using System.Collections.Generic;
using UnityEngine;

public class PoolBase : UnityEngine.MonoBehaviour
{
	//The time interval, in seconds, at which the object is automatically released.
	//自动释放对象的时间间隔，单位是秒。
	protected float AutoReleaseTimeSpan;

	//The last time this resource was released, in milliseconds. 1 second = 10000000 milliseconds
	//上一次释放该资源的时间，单位是毫微秒。1秒 = 10000000毫微秒
	protected long LastReleaseTime = 0;

	//Object pool structure
	//对象池结构
	protected List<InPoolObject> Objects = null;

	public void Start()
	{
		this.LastReleaseTime = System.DateTime.Now.Ticks;
	}

	public void Initialize(float AutoReleaseTimeSpan)
	{
		this.AutoReleaseTimeSpan = AutoReleaseTimeSpan;
		this.Objects = new List<InPoolObject>();
	}

	public void Uninitialize()
	{
		this.ReleaseAll();
		this.Objects = null;
		this.AutoReleaseTimeSpan = 0.0f;
	}

	//取出对象
	public virtual UnityEngine.Object Spawn(string Name)
	{
		foreach (InPoolObject item in Objects)
		{
			if (item.Name == Name)
			{
				this.Objects.Remove(item);
				return item.Object;
			}
		}
		return null;
	}

	//放入对象
	public virtual void Unspawn(string Name, UnityEngine.Object @Object)
	{
		InPoolObject item = new InPoolObject(Name, @Object);
		this.Objects.Add(item);
	}

	public virtual void Release(UnityEngine.Object @Object)
	{
		
	}

	public virtual void ReleaseByTimer()
	{
		
	}

	public virtual void ReleaseAll()
	{

	}

	private void Update()
	{
		if (System.DateTime.Now.Ticks - LastReleaseTime >= AutoReleaseTimeSpan * 10000000)
		{
			LastReleaseTime = System.DateTime.Now.Ticks;
			ReleaseByTimer();
		}
	}
}
