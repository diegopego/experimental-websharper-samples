namespace WebsocketsPingPong_fsharp

open WebSharper
open WebSharper.Sitelets

open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Server

open WebSharper.Web
open WebSharper.Sitelets
open WebSharper.AspNetCore
open WebSharper.AspNetCore.WebSocket
open WebSharper.AspNetCore.WebSocket.Server
open WebSharper.AspNetCore.Sitelets

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

open Client

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/about">] About
    | [<EndPoint "/home">] Commbus

type CommbusSite(logger: ILogger<CommbusSite>, config: IConfiguration) =
    inherit SiteletService<EndPoint>()

    override this.Sitelet = 
        Application.MultiPage (fun (ctx: Context<EndPoint>) (endpoint: EndPoint) ->                
            let urlStr = ctx.RequestUri.ToString()
            printfn "MultiPage WebSocketEndpoint: %s" urlStr
            let connPort = 
                WebSocketEndpoint.Create(urlStr, "/commbus", JsonEncoding.Readable)
            let content = 
                match endpoint with
                | EndPoint.Commbus -> 
                    let pg = {
                        Page.Default with
                            Title = Some "commbus"
                            Body = ([
                                div [] [Doc.WebControl (InlineControl<_> (ws connPort initState prop serverOp)) ]
                            ])
                        }
                    Content.Page pg
            content
            )

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





module Templating =
    open WebSharper.UI.Html

    // Compute a menubar where the menu item for the given endpoint is active
    let MenuBar (ctx: Context<EndPoint>) endpoint : Doc list =
        let ( => ) txt act =
             li [if endpoint = act then yield attr.``class`` "active"] [
                a [attr.href (ctx.Link act)] [text txt]
             ]
        [
            "Home" => EndPoint.Home
            "About" => EndPoint.About
        ]

    let Main ctx action (title: string) (body: Doc list) =
        Content.Page(
            Templates.MainTemplate()
                .Title(title)
                .MenuBar(MenuBar ctx action)
                .Body(body)
                .Doc()
        )

module Site =
    open WebSharper.UI.Html

    open type WebSharper.UI.ClientServer

    let HomePage ctx =
        Templating.Main ctx EndPoint.Home "Home" [
            h1 [] [text "Say Hi to the server!"]
            div [] [client (Client.Main())]
        ]

    let AboutPage ctx =
        Templating.Main ctx EndPoint.About "About" [
            h1 [] [text "About"]
            p [] [text "This is a template WebSharper client-server application."]
        ]

    [<Website>]
    let Main =
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | EndPoint.Home -> HomePage ctx
            | EndPoint.About -> AboutPage ctx
            | EndPoint.Commbus -> 
                let urlStr = ctx.RequestUri.ToString()
                printfn "MultiPage WebSocketEndpoint: %s" urlStr
                let connPort = 
                    WebSocketEndpoint.Create(urlStr, "/commbus", JsonEncoding.Readable)
                let pg = {
                            Page.Default with
                                Title = Some "commbus"
                                Body = ([
                                    div [] [Doc.WebControl (InlineControl<_> (ws connPort initState prop serverOp)) ]
                                ])
                            }
                Content.Page pg
        )