//ripped from https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/common/Shared/DuplexPipe.cs
//minor changes to confuse me less, plus for repo analyzer settings
//
//unsure yet of the reason for doing it this way, versus managing two pipes manually,
//  but plan to try out both implementations to see how they look
//
//
//my guess is semantics, and they seem interesting to me.
//https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/clients/csharp/Http.Connections.Client/src/Internal/WebSocketsTransport.cs
//this class exposes both Input and Output as Transport's input and output,
//  and Application is used privately within that class.
//for writes to the network:
// * the client effectively writes to Transport.Output
// * data is read via Application.Input privately
// * that data is then SendAsync'd thru the websocket
//
//similar, for reads:
// * data is ReceiveAsync'd from the websocket
// * then written into Application.Output
// * which is read by the client via Transport.Input
//
//I've attempted to change some names up to better describe this behavior.
//i suspect it would take anyone a bit of time to get the hang of the semantics, in any case,
//  but hopefully this shortens the process.

namespace Kulfibot.Discord
{
    using System.IO.Pipelines;

    internal sealed class ConnectionPipeline
    {
        //ClientSidePipe's input and output are meant to be exposed to clients reading and writing to some server
        //ServerSidePipe's input and output are meant to be used privately,
        //  to facilitate ClientSidePipe's usefulnessto the client, in reading from the server and writing to the server
        //
        //sure, this doesn't have to be used in a client-server architecture,
        //  but it perhaps better illustrates the purpose.
        //was also thinking maybe MainPipe, InternalPipe
        public static (IDuplexPipe ClientSidePipe, IDuplexPipe ServerSidePipe) Create(
            PipeOptions inputOptions,
            PipeOptions outputOptions)
        {
            Pipe inputFromServer = new(inputOptions);
            Pipe outputToServer = new(outputOptions);

            DuplexPipe serverSidePipe = new(outputToServer.Reader, inputFromServer.Writer);
            //so writes to the client-side pipe can be read by the server-side pipe (ultimately written to server)
            //and writes to the server-side pipe (ultimately read from server) can be read from the client-side pipe
            DuplexPipe clientSidePipe = new(inputFromServer.Reader, outputToServer.Writer);

            return (ClientSidePipe: clientSidePipe, ServerSidePipe: serverSidePipe);
        }

        private class DuplexPipe : IDuplexPipe
        {
            public PipeReader Input { get; }
            public PipeWriter Output { get; }

            public DuplexPipe(PipeReader input, PipeWriter output)
            {
                Input = input;
                Output = output;
            }
        }
    }
}
