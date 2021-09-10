using Grpc.Net.Client;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var channel = GrpcChannel.ForAddress("http://localhost:5000");
var client = new TodoPlayground.Todo.TodoClient(channel);

var createReply = await client.CreateTodoAsync(new TodoPlayground.CreateTodoRequest { IsComplete = false, Title = "My First Title" });

Console.WriteLine("Created: {0}", createReply.Id);
var searchByIdReply = await client.GetTodoByIdAsync(new TodoPlayground.TodoByIdRequest { Id = createReply.Id });
Console.WriteLine(searchByIdReply.Title);

var updateReply = await client.UpdateTodoAsync(new TodoPlayground.UpdateTodoRequest { Id = createReply.Id, Todo = new TodoPlayground.TodoRequest { Title = "My new title" } });
var  searchByIdReplyAgain = await client.GetTodoByIdAsync(new TodoPlayground.TodoByIdRequest { Id = createReply.Id });
Console.WriteLine(searchByIdReplyAgain.Title);

Console.WriteLine("Press any key to exit...");
Console.ReadKey();