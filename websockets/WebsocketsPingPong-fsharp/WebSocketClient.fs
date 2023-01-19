namespace WebsocketsPingPong_fsharp

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Templating
open WebSharper.UI.Notation
open WebSharper.AspNetCore.WebSocket

[<JavaScript>]
module WebSocketClient =
    let clientHandler
        (server: Client.WebSocketServer<string, string>)
        state
        (serverToClientMsg: Client.Message<string>)
        =
        async {
            Console.Log "websocket received message:"
            Console.Log serverToClientMsg
            Console.Log $"websocket received state: {state}"

            match serverToClientMsg with
            | Client.Open ->
                server.Post "connect"
                return "connected to the server"
            | Client.Message "welcome" ->
                // try connecting again just for fun!
                server.Post "connect"
                return "connected to server 2nd time"
            | Client.Message "welcome back!" ->
                // finally, do some pinging
                server.Post "ping"
                return "pinging"
            | Client.Message "pong" ->
                    server.Post "ping"
                    return "pinging"
            | _ -> return "disconnected"
        }

    let ws (wsEndpoint: WebSocketEndpoint<string, string>) =
        let initState (server: Client.WebSocketServer<string, string>) = 
            Console.Log $"server ready state before connection: {server.Connection.ReadyState}"
            "client initial state"

        let clientAgent: Client.StatefulAgent<string, string, string> =
            fun server ->
                async { return initState server, (fun state msg -> async { return! clientHandler server state msg }) }

        async {
            let! server = Client.ConnectStateful wsEndpoint clientAgent
            Console.Log $"server ready state after connection: {server.Connection.ReadyState}"
            ()
        }
        |> Async.Start

        pre [] []
