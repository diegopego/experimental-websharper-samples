open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open WebSharper.AspNetCore
open WebSharper.AspNetCore.WebSocket
open WebSharper.AspNetCore.WebSocket.Server
open WebsocketsPingPong_fsharp

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


[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    
    // Add services to the container.
    builder.Services.AddWebSharper()
        .AddAuthentication("WebSharper")
        .AddCookie("WebSharper", fun options -> ())
    |> ignore

    let app = builder.Build()

    // Configure the HTTP request pipeline.
    if not (app.Environment.IsDevelopment()) then
        app.UseExceptionHandler("/Error")
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            .UseHsts()
        |> ignore

    app.UseHttpsRedirection()
        .UseAuthentication()
        .UseStaticFiles()
        .UseWebSockets()
        .UseWebSharper(fun ws -> 
            ws.Sitelet(Site.Main) |> ignore
            ws.UseWebSocket(
                            "commbus"
                            , fun wsws ->                                 
                                wsws.Use(echoWebSocketAgent).JsonEncoding(JsonEncoding.Readable) |> ignore
                        )
            )
    |> ignore

    app.Run()

    0 // Exit code
