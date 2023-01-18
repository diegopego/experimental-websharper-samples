namespace WebsocketsPingPong_fsharp

open System
open WebSharper.AspNetCore.WebSocket
open WebSharper.AspNetCore.WebSocket.Server

module WebSocketServer =

    let serverRoute = "ws"

    let genWebSocketAgent<'ServerToClientMessage, 'ClientToServerMessage, 'State>
        (genState: WebSocketClient<'ServerToClientMessage, 'ClientToServerMessage> -> 'State)
        (msgHandler: WebSocketClient<'ServerToClientMessage, 'ClientToServerMessage> -> 'State -> Message<'ClientToServerMessage> -> Async<'State>)
        : StatefulAgent<'ServerToClientMessage, 'ClientToServerMessage, 'State> =
        fun client ->
            async {
                let clientIp = client.Connection.Context.Connection.RemoteIpAddress.ToString()
                let clientId = client.Connection.Context.Connection.Id

                return
                    genState client,
                    fun state clientToServerMsg ->
                        async {
                            let tid = Threading.Thread.CurrentThread.ManagedThreadId

                            printfn
                                $"[tid: {tid}] Received message {clientToServerMsg} (state: {state}) from {clientId} ({clientIp})"

                            return! msgHandler client state clientToServerMsg
                        }
            }

    let initState =
        fun (client: WebSocketClient<_, _>) ->
            printfn "##########connected##########"
            "connected"

    let serverHandler =
        fun (client: WebSocketClient<string, string>) state clientToServerMsg ->
            async {
                printfn $"ws received: {clientToServerMsg}"

                match clientToServerMsg with
                | Message "serve" ->
                    if state = "connected" then
                        do! client.PostAsync "welcome"
                        return "first blood"
                    else
                        do! client.PostAsync "ping"
                        return "running"
                | Message s ->
                    do! client.PostAsync "ping"
                    return "running"
                | Error msgOmg -> return sprintf $"disconnected, reason: {msgOmg.Message}"
                | Close -> return "disconnected"
            }


    let pingPongSocketAgent: StatefulAgent<string, string, string> =
        genWebSocketAgent<string, string, string> initState serverHandler

    let CreateEndpoint baseUrl =
        WebSocketEndpoint.Create(baseUrl, $"/{serverRoute}", JsonEncoding.Readable)
