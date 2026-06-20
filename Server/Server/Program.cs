using System.CommandLine;
using System.Diagnostics;
using Game.Core;
using Shenanicode.Rollback;
using Shenanicode.Rollback.LiteNetLib;

Option<ushort> portOption = new("--port")
{
	Description = "The port to listen on",
	Arity = ArgumentArity.ExactlyOne,
	Required = true,
};

Option<FileInfo> fileOption = new("--file")
{
	Description = "The save file to populate the world",
	Arity = ArgumentArity.ZeroOrOne,
};

RootCommand rootCommand = new("Game server.");
rootCommand.Options.Add(fileOption);
rootCommand.Options.Add(portOption);

rootCommand.SetAction(RunProgram);

return rootCommand.Parse(args).Invoke();

async Task RunProgram(ParseResult parseResult, CancellationToken arg2) {
	var running = true;
	Console.CancelKeyPress += (_, e) =>
	{
		e.Cancel = true;
		running = false;
	};
	AppDomain.CurrentDomain.ProcessExit += (_, _) =>
	{
		running = false;
	};

	var clientListener = new LiteNetLibRemoteClientListener(parseResult.GetValue(portOption));

	Server.Create(GameSessionSetup.SessionConfig, clientListener);
	GameSessionSetup.Register();
	Server.Initialize();

	GameWorldSetup.Create();
	GameWorldSetup.Register();
	GameWorldSetup.Initialize();

	Console.WriteLine($"Hello, Static World!");

	clientListener.Start();

	var stopwatch = Stopwatch.StartNew();
	while (running) {
		Server.Update(stopwatch.Elapsed.TotalSeconds);
		await Task.Yield();
	}

	clientListener.Stop();

	GameWorldSetup.Destroy();
	Server.Destroy();

	Console.WriteLine($"Exit with {stopwatch.Elapsed.TotalSeconds} seconds");
}

public abstract class Server : Server<GameWorld>;
