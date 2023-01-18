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
    let serverOp server = ()

    let initState server = "connected"

    let clientHandler (server: Client.WebSocketServer<string, string>) state (s2cMsg: Client.Message<string>) =
        async {
            Console.Log "websocket recieved:"
            Console.Log s2cMsg

            match s2cMsg with
            | Client.Message msg ->
                server.Post "pong"
                return "pong"
            | Client.Open ->
                server.Post "mega killed"
                return "connected"
            | _ -> return "diconnected"
        }

    let ws (connPort: WebSocketEndpoint<string, string>) =
        let clientAgent
            (genState: Client.WebSocketServer<string, string> -> 'State)
            (msgHandler: Client.WebSocketServer<string, string> -> 'State -> Client.Message<string> -> Async<'State>)
            : Client.StatefulAgent<string, string, 'State> =
            fun server ->
                async { return genState server, (fun state msg -> async { return! msgHandler server state msg }) }

        async {
            let! server = Client.ConnectStateful connPort (clientAgent initState clientHandler)
            serverOp server
        }
        |> Async.Start

        pre [] []
