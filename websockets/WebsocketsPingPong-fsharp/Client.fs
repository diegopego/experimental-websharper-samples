namespace WebsocketsPingPong_fsharp

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Templating
open WebSharper.UI.Notation
open WebSharper.AspNetCore.WebSocket

[<JavaScript>]
module Templates =

    type MainTemplate = Templating.Template<"Main.html", ClientLoad.FromDocument, ServerLoad.WhenChanged>

[<JavaScript>]
module Client =

    let ws (connPort:WebSocketEndpoint<string, string>) 
        gs mh (clientProp: Client.WebSocketServer<string, string> -> unit) = 
        let clientAgent
            (genState: Client.WebSocketServer<string, string> -> 'State)
            (msgHandler: Client.WebSocketServer<string, string> -> 'State -> Client.Message<string> -> Async<'State>)
            : Client.StatefulAgent<string, string, 'State> =
            fun server -> async {
                return genState server, fun state msg -> async {
                    return! msgHandler server state msg
                    }
                }
        async {
            let! server = Client.ConnectStateful connPort (clientAgent gs mh)
            clientProp server
        } |> Async.Start
        pre [] []

    let initState server = "connected"

    let prop (server: Client.WebSocketServer<string, string>) state (s2cMsg:Client.Message<string>) = async {
        Console.Log "websocket recieved:"
        Console.Log s2cMsg
        match s2cMsg with
        | Client.Message msg ->
            server.Post "pong"
            return "pong"
        | Client.Open ->
            server.Post "mega killed"
            return "connected"
        | _ ->
            return "diconnected"
    }

    let serverOp server = ()
    let Main () =
        let rvReversed = Var.Create ""
        Templates.MainTemplate.MainForm()
            .OnSend(fun e ->
                async {
                    let! res = Server.DoSomething e.Vars.TextToReverse.Value
                    rvReversed := res
                }
                |> Async.StartImmediate
            )
            .Reversed(rvReversed.View)
            .Doc()
