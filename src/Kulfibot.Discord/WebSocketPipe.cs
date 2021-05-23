//inspired by https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/clients/csharp/Http.Connections.Client/src/Internal/WebSocketsTransport.cs

namespace Kulfibot.Discord
{
    using System;
    using System.Buffers;
    using System.IO.Pipelines;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    //TODO: kinda don't care in this case, but thread safeify it anyway
    //TODO: how do exceptions from within make it to the caller? we probably want to retry a connection, for instance.
    //  the Complete methods of readers and writers take exceptions, so probably via that.
    //  otherwise, can expose the exception via property or method (where the method awaits the faulted task).
    //the reference seems to prefer logging, which won't do for this scenario.
    //regarding that, it looks like completing the serverSidePipe with exceptions causes the next clientSidePipe ops to throw
    //oh wait im blind; the reference does this, as well, though just for receives.
    internal sealed class WebSocketPipe : IDuplexPipe
    {
        private readonly IDuplexPipe clientSidePipe;
        private readonly IDuplexPipe serverSidePipe;
        private ClientWebSocket? webSocket;
        private Task processing = Task.CompletedTask;
        private bool stopping;

        //the reference implementation tracked whether or not a purposeful abort happened.
        //if it did, it would ignore exceptions that happened in SendLoop or ReceiveLoop.
        //I couldn't figure out why, so leaving it for now.
        //
        //another case is where StopAsync is called (stops the send loop), and the receive loop throw.
        //  on one hand, it's not supposed to throw, so it should be interesting.
        //  on the other hand, if StopAsync was called, then the caller perhaps doesn't care about it after all.
        //one case is where one loop throw, triggers abortion, and the other loop also manages to throw.
        //  i dont yet see a reason why both can't have exceptions.
        //  any sort of abortion, even across both ClientSidePipe.Input and Output, are interesting.
        //
        //given that, we'll track a stopping state, for suppressing exceptions only with StopAsync.

        public WebSocketPipe()
        {
            PipeOptions pipeOptions = new(
                readerScheduler: PipeScheduler.ThreadPool,
                writerScheduler: PipeScheduler.ThreadPool,
                useSynchronizationContext: false);
            (clientSidePipe, serverSidePipe) = ConnectionPipeline.Create(
                pipeOptions,
                pipeOptions
            );
        }

        public PipeReader Input => this.clientSidePipe.Input;

        public PipeWriter Output => this.clientSidePipe.Output;

        public async Task StartAsync(Uri endpoint, CancellationToken cancellationToken)
        {
            if (!this.processing.IsCompleted) throw new InvalidOperationException("The WebSocket has already started.");

            ClientWebSocket webSocket = new();

            try
            {
                await webSocket.ConnectAsync(endpoint, cancellationToken);
            }
            catch
            {
                webSocket.Dispose();
                throw;
            }

            this.webSocket = webSocket;
            this.processing = ProcessAsync();
        }

        public async Task StopAsync()
        {
            if (this.processing.IsCompleted) throw new InvalidOperationException("The WebSocket has already stopped.");

            this.stopping = true;

            await this.clientSidePipe.Input.CompleteAsync();
            await this.clientSidePipe.Output.CompleteAsync();

            //if I understand the referenced code correctly, we can cancel either one.
            //the point is to get ProcessAsync to finish,
            //which has a WhenAny on reading (serverSidePipe.Output) and writing (serverSidePipe.Input).
            //still, since cancelling the reading causes ProcessAsync to potentially wait, this seems more optimal.
            //also, the send loop is the only one potentially calling CloseOutputAsync, which is preferable to Abort.
            this.serverSidePipe.Input.CancelPendingRead();

            //the reference code had a catch-all, but I dont see how ProcessAsync can really throw.
            //maybe via webSocket.Abort(), but I feel like it might be worth propagating.
            try
            {
                await this.processing;
            }
            finally
            {
                //wondering why the reference has a dispose, if ProcessAsync should successfully do it anyway.
                //granted, it can only help, and we logically want it disposed at this point anyway.
                this.webSocket!.Dispose();
                this.stopping = false;
            }
        }

        private async Task ProcessAsync()
        {
            //while StopAsync will also dispose, we need to do it here anyway, since it could stop on its own
            using ClientWebSocket webSocket = this.webSocket
                ?? throw new InvalidOperationException("Somehow no websocket.");

            //mainly passing around the websocket, versus using the field, for NRT convenience
            Task sendLoop = SendLoopAsync(webSocket);
            Task receiveLoop = ReceiveLoopAsync(webSocket);
            Task trigger = await Task.WhenAny(sendLoop, receiveLoop);

            if (trigger == sendLoop)
            {
                //if this was a StopAsync, CloseOutputAsync was likely called.
                //the reference code Aborts anyway, so it's presumably fine.
                //
                //if it was some error, CloseOutputAsync still may have been called.
                //
                //I interpret the reference code to be basically falling back to Abort
                this.webSocket?.Abort();

                //not sure if reference code specifically prefers aborting before flushing; will keep it.
                this.serverSidePipe.Output.CancelPendingFlush();
            }
            else
            {
                this.serverSidePipe.Input.CancelPendingRead();

                //the reference code used a cts to cancel the task.delay for some reason
                //not sure why, so leaving out
                if (Task.WhenAny(sendLoop, Task.Delay(10000)) != receiveLoop)
                {
                    //aka the websocket hasnt closed
                    this.webSocket?.Abort();
                }
            }
        }

        private async Task ReceiveLoopAsync(ClientWebSocket webSocket)
        {
            try
            {
                while (true)
                {
                    // Do a 0 byte read so that idle connections don't allocate a buffer when waiting for a read
                    //or if i understand correctly, potentially save a GetMemory call
                    ValueWebSocketReceiveResult receiveResult =
                        await webSocket.ReceiveAsync(Memory<byte>.Empty, CancellationToken.None);
                    if (IsClosed(receiveResult))
                    {
                        break;
                    }

                    Memory<byte> payload = this.serverSidePipe.Output.GetMemory();
                    receiveResult = await webSocket.ReceiveAsync(payload, CancellationToken.None);
                    if (IsClosed(receiveResult))
                    {
                        break;
                    }

                    this.serverSidePipe.Output.Advance(receiveResult.Count);

                    FlushResult flushResult = await this.serverSidePipe.Output.FlushAsync();
                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                    {
                        break;
                    }
                }
            }
#pragma warning disable CA1031 //Modify to catch a more specific allowed exception type, or rethrow the exception
            catch (Exception ex) when (!this.stopping)
#pragma warning restore CA1031
            {
                this.serverSidePipe.Output.Complete(ex);
            }
            finally
            {
                this.serverSidePipe.Output.Complete();
            }

            bool IsClosed(ValueWebSocketReceiveResult receiveResult)
            {
                if (receiveResult.MessageType != WebSocketMessageType.Close)
                {
                    return false;
                }

#pragma warning disable IDE0046 //'if' statement can be simplified
                if (webSocket.CloseStatus != WebSocketCloseStatus.NormalClosure)
#pragma warning restore IDE0046
                {
                    throw new InvalidOperationException($"Websocket closed with error: {webSocket.CloseStatus}.");
                }

                return true;
            }
        }

        private async Task SendLoopAsync(ClientWebSocket webSocket)
        {
            bool error = false;
            try
            {
                while (true)
                {
                    ReadResult result = await this.serverSidePipe.Input.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    try
                    {
                        bool isDone =
                            result.IsCanceled
                            || (result.IsCompleted && buffer.IsEmpty)
                            || !CanSend();
                        if (isDone)
                        {
                            break;
                        }

                        if (buffer.IsEmpty)
                        {
                            continue;
                        }

                        await webSocket.SendAsync(buffer, WebSocketMessageType.Text);
                    }
                    finally
                    {
                        this.serverSidePipe.Input.AdvanceTo(buffer.End);
                    }

                }
            }
#pragma warning disable CA1031 //Modify to catch a more specific allowed exception type, or rethrow the exception
            catch (Exception ex) when (!this.stopping)
#pragma warning restore CA1031
            {
                //the reference code doesnt do this; not sure why
                //will try it anyway
                this.serverSidePipe.Input.Complete(ex);
                error = true;
            }
            finally
            {
                //got here from !CanSend, exception, cancelled (from StopAsync or receive completing), or completed
                //if the socket still open (aka can send), gotta close
                if (!CanSend())
                {
                    //TODO: InternalServerError is kinda wrong for a client perhaps
                    await webSocket.CloseOutputAsync(
                        error ? WebSocketCloseStatus.InternalServerError : WebSocketCloseStatus.NormalClosure,
                        "",
                        CancellationToken.None);
                }

                this.serverSidePipe.Input.Complete();
            }

            //the reference code prefers a function.
            //I interpret that to be that a post-send failure may update the state.
            bool CanSend() =>
                this.webSocket?.State is WebSocketState.CloseSent or WebSocketState.Closed or WebSocketState.Aborted;
        }
    }

    //ripped from https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/common/Shared/WebSocketExtensions.cs
    internal static class WebSocketExtensions
    {
        public static ValueTask SendAsync(this WebSocket webSocket, ReadOnlySequence<byte> buffer, WebSocketMessageType webSocketMessageType, CancellationToken cancellationToken = default) =>
            buffer.IsSingleSegment
                ? webSocket.SendAsync(buffer.First, webSocketMessageType, endOfMessage: true, cancellationToken)
                : SendMultiSegmentAsync(webSocket, buffer, webSocketMessageType, cancellationToken);

        private static async ValueTask SendMultiSegmentAsync(WebSocket webSocket, ReadOnlySequence<byte> buffer, WebSocketMessageType webSocketMessageType, CancellationToken cancellationToken = default)
        {
            SequencePosition position = buffer.Start;
            // Get a segment before the loop so we can be one segment behind while writing
            // This allows us to do a non-zero byte write for the endOfMessage = true send
            _ = buffer.TryGet(ref position, out ReadOnlyMemory<byte> prevSegment);
            while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> segment))
            {
                await webSocket.SendAsync(prevSegment, webSocketMessageType, endOfMessage: false, cancellationToken);
                prevSegment = segment;
            }

            // End of message frame
            await webSocket.SendAsync(prevSegment, webSocketMessageType, endOfMessage: true, cancellationToken);
        }
    }
}
