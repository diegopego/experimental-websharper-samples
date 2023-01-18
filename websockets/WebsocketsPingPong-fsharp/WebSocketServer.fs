namespace WebsocketsPingPong_fsharp

open System
open WebSharper.AspNetCore.WebSocket.Server

module WebSocketServer = 

    module wsm =
        open System
        open System.Threading
        open WebSharper.AspNetCore.WebSocket.Server
        let genWebSocketAgent<'S2CMessage, 'C2SMessage, 'State>  
            (genState: WebSocketClient<'S2CMessage, 'C2SMessage> -> 'State)
            (msgHandler: WebSocketClient<'S2CMessage, 'C2SMessage> -> 'State -> Message<'C2SMessage> -> Async<'State>)
            : StatefulAgent<'S2CMessage, 'C2SMessage, 'State> =        
            fun client -> async {
                let clientIp = client.Connection.Context.Connection.RemoteIpAddress.ToString()            
                let clientId = client.Connection.Context.Connection.Id
    
                return genState client, fun state c2sMsg -> async {
                    let tid = Threading.Thread.CurrentThread.ManagedThreadId
                    printfn "[tid: %d] Received message %A (state: %A) from %s (%s)" tid c2sMsg state clientId clientIp
        
                    return! msgHandler client state c2sMsg
                }
            }

    open wsm

    let initState = fun (client:WebSocketClient<_, _>) -> 
         printfn "##########connected##########"
         "connected"

    let prop = 
         fun (client:WebSocketClient<string, string>) state c2sMsg -> 
             async {
                 printfn "ws received: %A" c2sMsg
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
                 | Error msgOmg ->
                     return sprintf "disconnected, reason: %s" msgOmg.Message
                 | Close ->
                     return "disconnected"
             }


    let echoWebSocketAgent : StatefulAgent<string, string, string> = 
         genWebSocketAgent<string, string, string> initState prop