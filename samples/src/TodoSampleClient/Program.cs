using Grpc.Net.Client;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    Console.WriteLine("Cancelling...");
    cts.Cancel();
    e.Cancel = true;
};

var channel = GrpcChannel.ForAddress("http://localhost:5000");
var client = new TodoPlayground.Todo.TodoClient(channel);

var createReply = await client.CreateTodoAsync(new TodoPlayground.CreateTodoRequest { IsComplete = false, Title = "My First Title" }, cancellationToken: cts.Token);

Console.WriteLine("Created: {0}", createReply.Id);
var searchByIdReply = await client.GetTodoByIdAsync(new TodoPlayground.TodoByIdRequest { Id = createReply.Id },cancellationToken: cts.Token);
Console.WriteLine(searchByIdReply.Title);

var updateReply = await client.UpdateTodoAsync(new TodoPlayground.UpdateTodoRequest { Id = createReply.Id, Todo = new TodoPlayground.TodoRequest { Title = "My new title" } }, cancellationToken: cts.Token);
var  searchByIdReplyAgain = await client.GetTodoByIdAsync(new TodoPlayground.TodoByIdRequest { Id = createReply.Id }, cancellationToken : cts.Token);
Console.WriteLine(searchByIdReplyAgain.Title);
var all =  client.GetAllTodos(new Google.Protobuf.WellKnownTypes.Empty(), cancellationToken: cts.Token);
while(await all.ResponseStream.MoveNext(cts.Token))
{
    Console.WriteLine(all.ResponseStream.Current.Title);
}
Console.WriteLine("Press any key to exit...");
Console.ReadKey();