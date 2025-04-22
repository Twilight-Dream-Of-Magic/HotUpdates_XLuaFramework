using System.Net;

class Program
{
	static void Main()
	{
		Console.Title = "GameServer";
		using var networkService = new ServerXLuaFreamwork.Services.NetworkService(IPAddress.Any, 8000);

		// 捕获 Ctrl+C，优雅地停止服务
		Console.CancelKeyPress += (s, e) =>
		{
			e.Cancel = true;         // 阻止进程立刻终止
			networkService.Stop();   // 通知服务退出
		};

		// 阻塞调用：内部管理所有线程/任务，直到 Stop() 被调用
		networkService.Start();

		while (networkService.IsRunning)
		{
			Thread.Sleep(TimeSpan.FromSeconds(1));
		}

		Thread.Sleep(TimeSpan.FromSeconds(10));

		Console.WriteLine("All Service Stop，Program Exited!");
	}
}
