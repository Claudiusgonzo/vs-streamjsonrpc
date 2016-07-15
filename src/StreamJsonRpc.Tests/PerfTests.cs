﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Nerdbank;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;

public class PerfTests
{
    private readonly ITestOutputHelper logger;

    public PerfTests(ITestOutputHelper logger)
    {
        this.logger = logger;
    }

    [Fact]
    public async Task ChattyPerf_OverNamedPipes()
    {
        string pipeName = Guid.NewGuid().ToString();
        var serverPipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var connectTask = serverPipe.WaitForConnectionAsync();
        var clientPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        clientPipe.Connect();
        await ChattyPerfAsync(serverPipe, clientPipe);
    }

    [Fact]
    public async Task ChattyPerf_OverFullDuplexStream()
    {
        var streams = FullDuplexStream.CreateStreams();
        await ChattyPerfAsync(streams.Item1, streams.Item2);
    }

    private async Task ChattyPerfAsync(Stream serverStream, Stream clientStream)
    {
        JsonRpc.Attach(serverStream, new Server());
        var client = JsonRpc.Attach(clientStream);

        const int iterations = 10000;
        var timer = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await client.InvokeAsync("NoOp");
        }

        timer.Stop();
        this.logger.WriteLine($"{iterations} iterations completed in {timer.ElapsedMilliseconds} ms.");
        this.logger.WriteLine($"Rate: {iterations / timer.Elapsed.TotalSeconds} invocations per second.");
        this.logger.WriteLine($"Overhead: {(double)timer.ElapsedMilliseconds / iterations} ms per invocation.");
    }

    public class Server
    {
        public void NoOp() { }
    }
}
