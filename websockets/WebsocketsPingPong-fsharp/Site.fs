namespace WebsocketsPingPong_fsharp

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/about">] About
    | [<EndPoint "/websockets-sample">] WebsocketsSample

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
            "Websockets sample" => EndPoint.WebsocketsSample
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

    open WebSharper.Web
    
    open Client

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
            | EndPoint.WebsocketsSample -> 
                let baseUrl = ctx.RequestUri.ToString()
                let wsEndpoint = WebSocketServer.CreateEndpoint baseUrl
                printfn $"MultiPage WebSocket Base Url: {baseUrl}"
                let pg = {
                            Page.Default with
                                Title = Some "Websockets Sample"
                                Body = ([
                                    div [] [Doc.WebControl (InlineControl<_> (WebSocketClient.ws wsEndpoint))]
                                ])
                            }
                Content.Page pg
        )