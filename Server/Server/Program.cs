using System.CommandLine;
using System.Diagnostics;
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

return await rootCommand.Parse(args).InvokeAsync();

async Task<int> RunProgram(ParseResult parseResult, CancellationToken arg2) {
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

	ServerSetup.CreateAndInitialize(clientListener);

	if (parseResult.GetValue(fileOption) is { } parsedFile) {
		Console.WriteLine($"WorldFile: {parsedFile.Name}");
	}

	clientListener.Start();
	Console.WriteLine($"Started listening on port: {parseResult.GetValue(portOption)}");

	var stopwatch = Stopwatch.StartNew();
	while (running) {
		SRVR.Update(stopwatch.Elapsed.TotalSeconds);
		await Task.Yield();
	}

	clientListener.Stop();
	Console.WriteLine();

	ServerSetup.Destroy();
	Console.WriteLine($"Exited after {stopwatch.Elapsed.TotalSeconds:F0} seconds.");

	return 0;
}
