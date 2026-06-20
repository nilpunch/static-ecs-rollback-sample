using System.CommandLine;
using System.Diagnostics;
using Game.Core;
using Shenanicode.Rollback;
using Shenanicode.Rollback.LiteNetLib;

Option<FileInfo> fileOption = new("--file")
{
	Description = "The file to read and display on the console",
	Arity = ArgumentArity.ZeroOrOne,
};

Option<ushort> portOption = new("--port")
{
	Description = "The port to listen on",
	Arity = ArgumentArity.ExactlyOne,
	Required = true
};

RootCommand rootCommand = new("Server app");
rootCommand.Options.Add(fileOption);
rootCommand.Options.Add(portOption);

var parseResult = rootCommand.Parse(args);
if (parseResult.Errors.Count != 0)
{
	foreach (var parseError in parseResult.Errors)
	{
		Console.Error.WriteLine(parseError.Message);
	}
	return 1;
}

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

return 0;

public abstract class Server : Server<GameWorld>;
