namespace WebsocketsPingPong_fsharp

open System
open WebSharper.AspNetCore.WebSocket
open WebSharper.AspNetCore.WebSocket.Server

module WebSocketServer =

    let serverRoute = "ws"

    let genWebSocketAgent<'S2CMessage, 'C2SMessage, 'State>
        (genState: WebSocketClient<'S2CMessage, 'C2SMessage> -> 'State)
        (msgHandler: WebSocketClient<'S2CMessage, 'C2SMessage> -> 'State -> Message<'C2SMessage> -> Async<'State>)
        : StatefulAgent<'S2CMessage, 'C2SMessage, 'State> =
        fun client ->
            async {
                let clientIp = client.Connection.Context.Connection.RemoteIpAddress.ToString()
                let clientId = client.Connection.Context.Connection.Id

                return
                    genState client,
                    fun state c2sMsg ->
                        async {
                            let tid = Threading.Thread.CurrentThread.ManagedThreadId

                            printfn
                                $"[tid: {tid}] Received message {c2sMsg} (state: {state}) from {clientId} ({clientIp})"

                            return! msgHandler client state c2sMsg
                        }
            }

    let initState =
        fun (client: WebSocketClient<_, _>) ->
            printfn "##########connected##########"
            "connected"

    let serverHandler =
        fun (client: WebSocketClient<string, string>) state c2sMsg ->
            async {
                printfn $"ws received: {c2sMsg}"

                match c2sMsg with
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
