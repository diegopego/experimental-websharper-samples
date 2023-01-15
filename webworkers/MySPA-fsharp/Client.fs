namespace MySPA

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Templating

[<JavaScript>]
module Client =
    // The templates are loaded from the DOM, so you just can edit index.html
    // and refresh your browser, no need to recompile unless you add or remove holes.
    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    let People =
        ListModel.FromSeq [
            "John"
            "Paul"
        ]


    [<SPAEntryPoint>]
    let Main () =
        // --- begin myWorker
        Console.Log "The main thread wrote: main thread was initialized."

        let myWorker = new Worker(fun self ->
            Console.Log "This was written from the worker!"
            self.PostMessage("This worker's job is done, it can be terminated.")
        )
        myWorker.Onmessage <- fun event ->
            Console.Log event.Data
            myWorker.Terminate()
        // --- end myWorker

        // --- begin echoWorker
        // Create and start a new Web Worker.
        // It will start an asynchronous process that will execute the entry point passed as a function on another thread.
        let echoWorker = new Worker(fun self ->
            // This code runs in the worker.
            Console.Log "echoWorker says: echoWorker has been initialized."

            let doHeavyWorkAsync (event:MessageEvent) = async {
                do! Async.Sleep 1000
                // Here we're assuming we'll only ever receive strings
                let msg = event.Data :?> string
                let reversed = System.String(Array.rev(msg.ToCharArray()))
                do self.PostMessage($"Hello, main thread. Here is your reversed message: {reversed}")
                // You can post more messages to the main thread here
            }

            // Sends an asynchronous message to be processed by the main thread. see echoWorker.OnMessage
            self.PostMessage("Hello, main thread. This happens once during echoWorker initialization.")

            // Receives messages from the main thread.
            // This only runs when the main thread calls echoWorker.PostMessage
            self.Onmessage <- fun event ->
                Console.Log $"echoWorker says: Received message from main thread: {event.Data}"
                doHeavyWorkAsync event |> Async.Start
        )

        // This code runs in the main thread. It receives messages from the worker.
        // It only runs when the worker calls self.PostMessage
        echoWorker.Onmessage <- fun event ->
            Console.Log $"Main thread says: Received message from echoWorker thread: {event.Data}"
        // --- end echoWorker

        let newName = Var.Create ""

        IndexTemplate.Main()
            .ListContainer(
                People.View.DocSeqCached(fun (name: string) ->
                    IndexTemplate.ListItem().Name(name).Doc()
                )
            )
            .Name(newName)
            .Add(fun _ ->
                People.Add(newName.Value)
                
                // Sends an asynchronous message to be processed by the worker self.OnMessage function, on a separate thread
                Console.Log $"Main thread says: will post to worker!"
                echoWorker.PostMessage($"Hello, worker! {newName.Value} was added.")

                newName.Value <- ""
            )
            .Doc()
        |> Doc.RunById "main"
