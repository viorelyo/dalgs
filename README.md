# Dalgs - Distributed algorithms
Discovering some distributed algorithms. `Introduction to Reliable and Secure Distributed Programming. Second Edition` book by Christian Cachin, Rachid Guerraoui, Luis Rodrigues, used as main reference.

### Implemented Algorithms
* Best-Effort Broadcast (__*3.1 Basic Broadcast*__) - [goto](/NewDalgs/Abstractions/BestEffortBroadcast.cs)
* (N,N)-Atomic Register (__*4.10-4.11 Read-Impose Write-Consult-Majority*__) - [goto](/NewDalgs/Abstractions/NNAtomicRegister.cs)
* Eventually Perfect Failure Detector (__*2.7 Increasing Timeout*__) - [goto](/NewDalgs/Abstractions/EventuallyPerfectFailureDetector.cs)
* Eventual Leader Detector (__*2.8 Monarchical Eventual Leader Detection*__) - [goto](/NewDalgs/Abstractions/EventualLeaderDetector.cs)
* Epoch-Change (__*5.5 Leader-Based Epoch-Change*__) - [goto](/NewDalgs/Abstractions/EpochChange.cs)
* Epoch Consensus (__*5.6 Read/Write Epoch Consensus*__) - [goto](/NewDalgs/Abstractions/EpochConsensus.cs)
* Uniform Consensus (__*5.7 Leader-Driven Consensus (Paxos)*__) - [goto](/NewDalgs/Abstractions/UniformConsensus.cs)

### About
NewDalgs communicates via TCP with the Hub and other nodes (both `dalgs` and `NewDalgs` run 3 separate nodes - simulating a distributed environment).
* The Core uses `Asynchronous Server Socket` for networking (supports graceful stop)
* The algorithms are based on `Event-driven programming`, so the Core uses a BlockingQueue in order to process all proto-defined messages.

### Built with
* .NET Core
* Protocol Buffers
* NLog

### Build/Run 
* `protoc.exe -I=\NewDalgs\proto --csharp_out=\NewDalgs\proto NewDalgs\proto\communication-protocol.proto` - Generate proto
* `dotnet publish -c Release -r win10-x64 /p:PublishSingleFile=true` - Compile app
* `dalgs.exe 127.0.0.1 5000 127.0.0.1 5001 5002 5003` - Run [reference binaries (containing the Hub)](/dalgs/dalgs-reference-binaries.7z)
`NewDalgs.exe 127.0.0.1 5000 127.0.0.1 5004 5005 5006 alias` - Run second instance

### Demo
* **Broadcast**
<img src="/samples/broadcast.gif" width="800">  
  
* **(N,N)-Atomic Register**
<img src="/samples/nnar.gif" width="800">  
  
<img src="/samples/storm.gif" width="800">  
  
* **Consensus**
<img src="/samples/consensus.gif" width="800">
