namespace WebsocketsPingPong_fsharp

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.AspNetCore.WebSocket

[<JavaScript>]
module WebSocketClient =

    let pongCounterStartsAt = 1
    let maximumPongs = 10

    let clientHandler
        (server: Client.WebSocketServer<string, string>)
        (clientState: int)
        (serverToClientMsg: Client.Message<string>)
        : Async<int> =
        async {
            Console.Log $"client's current state: {clientState}"
            Console.Log "websocket received message:"
            Console.Log serverToClientMsg

            match serverToClientMsg with
            | Client.Open ->
                server.Post "connect"
                return clientState
            | Client.Message "welcome" ->
                // try connecting again just for fun!
                server.Post "connect"
                return clientState
            | Client.Message "welcome back!" ->
                // finally, do some pinging
                server.Post "ping"
                return clientState
            | Client.Message "pong" ->
                if clientState < maximumPongs then
                    server.Post "ping"
                    return (clientState + 1)
                else
                    return clientState
            | _ -> return clientState
        }

    let ws (wsEndpoint: WebSocketEndpoint<string, string>) =
        let initState (server: Client.WebSocketServer<string, string>) =
            Console.Log $"server ready state, before connection: {server.Connection.ReadyState}"
            pongCounterStartsAt

        let clientAgent: Client.StatefulAgent<string, string, int> =
            fun server ->
                async {
                    return
                        initState server,
                        (fun clientState msg -> async { return! clientHandler server clientState msg })
                }

        async {
            let! server = Client.ConnectStateful wsEndpoint clientAgent
            Console.Log $"server ready state, after connection: {server.Connection.ReadyState}"
            ()
        }
        |> Async.Start

        pre [] []
