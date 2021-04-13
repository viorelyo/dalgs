# Dalgs - Distributed algorithms

## dalgs commands
`dalgs.exe 127.0.0.1 5000 127.0.0.1 5001 5002 5003`
`127.0.0.1 5000 127.0.0.1 5004 5005 5006 gvsd` - second instance

## ProtocolBuffers command
`protoc.exe -I=C:\Users\viorel\Desktop\amcds\NewDalgs\NewDalgs\NewDalgs\proto --csharp_out=C:\Users\viorel\Desktop\amcds\NewDalgs\NewDalgs\NewDalgs\proto C:\Users\viorel\Desktop\amcds\NewDalgs\NewDalgs\NewDalgs\proto\communication-protocol.proto`

## C# references
#### Graceful Stop for TcpListener
- [Async Socket](https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-server-socket-example)
- [Graceful stop for TcpClient - Async](https://codereview.stackexchange.com/questions/151228/asynchronous-tcp-server)
- [Graceful stop Part2](https://github.com/avgoncharov/how_to/blob/master/how_to/SimpleTcpServer/TcpServer.cs)

#### .NET Logging
- [Custom logging msg - Microsoft.Extension.Logging](https://stackoverflow.com/questions/45015660/how-to-format-the-output-of-logs-in-the-consolemicrosoft-extensions-logging)
- [Example logging](https://www.blinkingcaret.com/2018/02/14/net-core-console-logging/)
- [NLog](https://github.com/NLog/NLog/wiki/Tutorial#configure-nlog-targets-for-output)
- [NLog features](https://blog.elmah.io/nlog-tutorial-the-essential-guide-for-logging-from-csharp/)

#### Self contained EXE .NET Core
- [Self-Contained EXE](https://dotnetcoretutorials.com/2019/06/20/publishing-a-single-exe-file-in-net-core-3-0/)

#### Observer vs. events/delegates in C# (Pub-Sub)
- [Plublisher/Subscriber vs. Observer](https://dev.to/absjabed/publisher-subscriber-vs-observer-pattern-with-c-3gpc)

#### Deadlock - Do not try to stop yourself from same thread
- [Handle task exceptions on continuation tasks](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/chaining-tasks-by-using-continuation-tasks)
- [Working solution](https://stackoverflow.com/questions/27896613/continuewith-taskcontinuationoptions-onlyonfaulted-does-not-seem-to-catch-an-exc)
- [Main idea](https://stackoverflow.com/questions/5983779/catch-exception-that-is-thrown-in-different-thread)

