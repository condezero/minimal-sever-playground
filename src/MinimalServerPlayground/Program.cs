using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TodoPlayground;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("TodoDb") ?? "Data Source=todos.db";
builder.Services.AddSqlite<TodoDb>(connectionString)
                .AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddGrpc();

var app = builder.Build();

await EnsureDb(app.Services, app.Logger);

app.MapGrpcService<TodoService>();
app.Run();


async Task EnsureDb(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Ensuring database exists and is up to date at connection string '{connectionString}'", connectionString);

    using var db = services.CreateScope().ServiceProvider.GetRequiredService<TodoDb>();

    db.Database.EnsureCreated();

    await db.Database.MigrateAsync();
}

class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options)
        : base(options) { }

    public DbSet<TodoPlayground.TodoReply> Todos => Set<TodoPlayground.TodoReply>();
}
class TodoService : Todo.TodoBase
{
    private readonly TodoDb _db;

    private static Status _notFoundStatus = new Status(StatusCode.NotFound, "Not found");
    public TodoService(TodoDb database)
    {
        _db = database;
    }

    public override async Task<CreateTodoReply> CreateTodo(CreateTodoRequest request, ServerCallContext context)
    {
        var res = _db.Todos.Add(new TodoReply { IsComplete = request.IsComplete, Title = request.Title });
        await _db.SaveChangesAsync();


        return new CreateTodoReply
        {
            Id = res.Entity.Id
        };


    }

    public override async Task<Empty> DeleteTodo(TodoByIdRequest request, ServerCallContext context)
    {
        if (await _db.Todos.FindAsync(request.Id) is TodoReply todo)
        {
            _db.Todos.Remove(todo);
            await _db.SaveChangesAsync();
            return new Empty();
        }
        throw new RpcException(_notFoundStatus);
    }

    public override async Task GetAllTodos(Empty request, IServerStreamWriter<TodoReply> responseStream, ServerCallContext context)
    {
        await foreach (var todo in _db.Todos.AsAsyncEnumerable())
        {
            await responseStream.WriteAsync(todo);
        }
    }

    public override async Task GetCompleteTodos(Empty request, IServerStreamWriter<TodoReply> responseStream, ServerCallContext context)
    {
        await foreach (var todo in _db.Todos.Where(t => t.IsComplete).AsAsyncEnumerable())
        {
            await responseStream.WriteAsync(todo);
        }
    }

    public override async Task GetIncompleteTodos(Empty request, IServerStreamWriter<TodoReply> responseStream, ServerCallContext context)
    {
        await foreach (var todo in _db.Todos.Where(t => !t.IsComplete).AsAsyncEnumerable())
        {
            await responseStream.WriteAsync(todo);
        }
    }

    public override async Task<TodoReply> GetTodoById(TodoByIdRequest request, ServerCallContext context)
    {
        return await _db.Todos.FindAsync(request.Id) is TodoReply todo ? todo : new TodoReply();
    }

    public override async Task<UpdateTodoReply> MarkComplete(TodoByIdRequest request, ServerCallContext context)
    {
        var todo = await _db.Todos.FindAsync(request.Id);
        if (todo is null) throw new RpcException(_notFoundStatus);

        todo.IsComplete = true;
        await _db.SaveChangesAsync();
        return new();
    }

    public override async Task<UpdateTodoReply> MarkInComplete(TodoByIdRequest request, ServerCallContext context)
    {
        var todo = await _db.Todos.FindAsync(request.Id);
        if (todo is null) throw new RpcException(_notFoundStatus);

        todo.IsComplete = false;
        await _db.SaveChangesAsync();
        return new();
    }

    public override async Task<UpdateTodoReply> UpdateTodo(UpdateTodoRequest request, ServerCallContext context)
    {
        var todo = await _db.Todos.FindAsync(request.Id);
        if (todo is null) throw new RpcException(_notFoundStatus);

        todo.Title = request.Todo.Title;
        todo.IsComplete = request.Todo.IsComplete;
        await _db.SaveChangesAsync();
        return new();
    }

}
