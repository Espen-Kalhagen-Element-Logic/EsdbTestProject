# EsdbTestProject

A minimum example that displays the behavior of the esdb clients growth in memory usage. It is meant to have two running instances, one master and one slave. It creates very many streams and sends ping pong between the services.

The master create a stream by publishing a ping event and then subscribes to this stream. The slave subscribes to the stream waits for a ping event and publishes a pong event. When the master receives a pong it cancels the subscription and starts the process again.

This can be found here: Test project reproducing problem. Specifically look at Orchestrator.cs for behavior.

The memory grows linearly with the number of streams ![picture](https://github.com/Espen-Kalhagen-Element-Logic/EsdbTestProject/blob/main/Memory%20usage%20high%20number%20of%20streams.PNG?raw=true)

## The repo also contains two dotnet-dumps that showcase the state at the beginning and after a high number of streams [here](https://github.com/Espen-Kalhagen-Element-Logic/EsdbTestProject/tree/main/dotnet-dumps)
