namespace WebsocketsPingPong_fsharp

open System
open WebSharper.AspNetCore.WebSocket
open WebSharper.AspNetCore.WebSocket.Server

module WebSocketServer =

    let serverRoute = "ws"

    let serverMsgHandler =
        fun (client: WebSocketClient<string, string>) serverState clientToServerMsg ->
            async {
                printfn $"server's current state : {serverState}"
                printfn $"ws received message ...: {clientToServerMsg}"

                match clientToServerMsg with
                | Message "connect" ->
                    if serverState = "server initial state" then
                        do! client.PostAsync "welcome"
                    else
                        do! client.PostAsync "welcome back!"

                    return "client connected"
                | Message s ->
                    if serverState = "server initial state" then
                        do! client.PostAsync "connect is required"
                        return serverState
                    else
                        do! Async.Sleep 100
                        do! client.PostAsync "pong"
                        return "ponging"
                | Error msgOmg -> return sprintf $"disconnected, reason: {msgOmg.Message}"
                | Close -> return "disconnected"
            }

    let pingPongSocketAgent: StatefulAgent<string, string, string> =
        let initState =
            fun (client: WebSocketClient<_, _>) ->
                printfn $"connected to this client: {client.Connection.Context.Connection.RemoteIpAddress.ToString()}"
                "server initial state"

        fun client ->
            async {
                let clientIp = client.Connection.Context.Connection.RemoteIpAddress.ToString()
                let clientId = client.Connection.Context.Connection.Id

                return
                    initState client,
                    fun serverState clientToServerMsg ->
                        async {
                            let tid = Threading.Thread.CurrentThread.ManagedThreadId

                            printfn
                                $"[tid: {tid}] Received message {clientToServerMsg} (state: {serverState}) from {clientId} ({clientIp})"

                            return! serverMsgHandler client serverState clientToServerMsg
                        }
            }

    let CreateEndpoint baseUrl =
        WebSocketEndpoint.Create(baseUrl, $"/{serverRoute}", JsonEncoding.Readable)
