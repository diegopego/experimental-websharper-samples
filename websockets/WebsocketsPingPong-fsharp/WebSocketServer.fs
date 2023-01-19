namespace WebsocketsPingPong_fsharp

open System
open WebSharper.AspNetCore.WebSocket
open WebSharper.AspNetCore.WebSocket.Server

module WebSocketServer =

    let serverRoute = "ws"



    let serverMsgHandler =
        fun (client: WebSocketClient<string, string>) state clientToServerMsg ->
            async {
                printfn $"ws received message: {clientToServerMsg}"
                printfn $"ws received state .: {state}"

                match clientToServerMsg with
                | Message "connect" ->
                    if state = "server initial state" then
                        do! client.PostAsync "welcome"
                    else
                        do! client.PostAsync "welcome back!"

                    return "client connected"
                | Message s ->
                    if state = "server initial state" then
                        do! client.PostAsync "connect is required"
                        return state
                    else
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
                    fun state clientToServerMsg ->
                        async {
                            let tid = Threading.Thread.CurrentThread.ManagedThreadId

                            printfn
                                $"[tid: {tid}] Received message {clientToServerMsg} (state: {state}) from {clientId} ({clientIp})"

                            return! serverMsgHandler client state clientToServerMsg
                        }
            }

    let CreateEndpoint baseUrl =
        WebSocketEndpoint.Create(baseUrl, $"/{serverRoute}", JsonEncoding.Readable)
