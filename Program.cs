using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using TestChat;

AutoResetEvent awaiter = new AutoResetEvent(false);
Mutex mutex = new Mutex();
User static_user = null;
MessageId.LastMessageId = -1;

var dbContext = new ChatContext();
dbContext.Database.EnsureCreated();
dbContext.SaveChanges();
readCommmand: Console.WriteLine("LogIn or Register");
string command = Console.ReadLine();

if (command == "LogIn")
    LogIn();
else if (command == "Register")
    Register();
else goto readCommmand;

Console.Clear();
OpenChat();

var syncronizer = new Thread(UpdateContiniously);
syncronizer.Start();

while (true)
{
    var c = Console.ReadKey(true);
    var key = c.Key;
    if (key == ConsoleKey.S)
        SendMessage(static_user);
}

void UpdateContiniously()
{
    while(true)
    {
        Thread.Sleep(500);
        SynchronizeData();
    }
}

void Register()
{
getUsername: var username = GetUsername();

    if (dbContext.Users.ToList().FirstOrDefault(x => x.Username == username) != null)
    {
        Console.WriteLine($"User with '{username}' already exists");
        goto getUsername;
    }

    var newUser = new User();
    newUser.Username = username;
    dbContext.Add(newUser);
    dbContext.SaveChanges();

    static_user = newUser;
}

void LogIn()
{
getUsername: var username = GetUsername();
    var user = dbContext.Users.ToList().FirstOrDefault(x => x.Username == username);

    if (user == null)
    {
        Console.WriteLine($"User with '{username}' doesn't exist");
        goto getUsername;
    }

    static_user = user;
}

void SendMessage(User user)
{
    string content = Console.ReadLine();
    var position = Console.GetCursorPosition();
    Console.SetCursorPosition(position.Left = 0, position.Top - 1);
    var message = new Message();
    message.Content = content;
    message.Sender = user;
    message.SentAt = DateTime.Now;

    dbContext.Add(message);
    dbContext.SaveChanges();
    SynchronizeData();
}


void OpenChat()
{
    var messages = dbContext.Messages.Include(x => x.Sender).ToList();
    if (messages.Count == 0)
        return;
    MessageId.LastMessageId = messages[messages.Count - 1].Id;
    foreach (var message in messages)
        new Cell(message).PlaceCell();
        //Console.WriteLine(message);
}

void SynchronizeData()
{
    var messagesCount = dbContext.Messages.ToList().Count();
    if (messagesCount == 0)
        return;
    var lastmessage = dbContext.Messages.AsEnumerable().ElementAt(messagesCount - 1);
    dbContext.Entry(lastmessage).Reference(x => x.Sender);


    if (lastmessage.Id != MessageId.LastMessageId)
    {
        new Cell(lastmessage).PlaceCell();
        //Console.WriteLine(lastmessage);
        MessageId.LastMessageId = lastmessage.Id;
    }
}

string GetUsername()
{
    Console.WriteLine("Enter username: ");
    string username = Console.ReadLine();

    return username;
}


class User
{
    [Key]
    public int Id { get; set; }
    [MaxLength(20)]
    public string Username { get; set; }

    public override string ToString()
    {
        return $"{Id} {Username}";
    }
}

class Message
{
    [Key]
    public int Id { get; set; }
    public string Content { get; set; }
    [ForeignKey("SenderId")]
    public User Sender { get; set; }
    public DateTime SentAt { get; set; }

    //public override string ToString()
    //{
    //    var credentials = Sender.Username + " " + SentAt.ToString();
    //    var contentStr = $"| {Content}" + new string(' ', 10);
    //    var alligningSpace = new string(' ', Console.WindowWidth - 1 - contentStr.Length - credentials.Length);
    //    var messageStr = contentStr + alligningSpace + credentials + "|";
    //    var divider = new string('-', Console.WindowWidth);

    //    return divider + '\n' + messageStr + '\n' + divider;
    //}
}

class Cell
{
    [Key]
    public int Id { get; set; }
    public int Height { get => height; }
    public ReadField ReadField { get; set; }

    private int height;
    [ForeignKey("MessageId")]
    private Message _message;

    public Cell(Message message)
    {
        _message = message;
    }
    public void PlaceCell()
    {
        height = Console.GetCursorPosition().Top + 1;
        Console.WriteLine(BuildCell());
        ReadField.PutField();
    }

    private string BuildCell()
    {
        var credentials = _message.Sender.Username + " " + _message.SentAt.ToString();
        var contentStr = $"| {_message.Content}" + new string(' ', 10);
        var alligningSpace = new string(' ', Console.WindowWidth - 1 - contentStr.Length - credentials.Length);
        var messageStr = contentStr + alligningSpace + credentials + "|";
        var divider = new string('-', Console.WindowWidth);

        var xField = 25;
        var yField = Height;
        ReadField = new ReadField((xField, yField));
        ReadField.CellId = Id;

        return divider + '\n' + messageStr + '\n' + divider;
    }
}

class ReadField
{
    public int CellId { get; set; }
    public (int x, int y) Position { get; set; }
    public string Indicator { get; set; }

    private object _lockObject;

    public ReadField((int x, int y) position)
    {
        Position = position;
        Indicator = "v";
    }

    public void PutField()
    {
        Console.SetCursorPosition(Position.x, Position.y);
        Console.Write(Indicator);
        Console.SetCursorPosition(0, Position.y + 2);
    }

    public void ChangeIndicator()
    {
        Console.SetCursorPosition(Position.x + 1, Position.y);
        Console.Write("v");
    }
}

class ChatContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.UseSqlite("Filename=ChatDb");
    }
}