using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// تعریف کلاس Person
class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {

        using HttpListener server = new();
        server.Prefixes.Add("http://localhost:11231/");
        server.Start();

         var context = await server.GetContextAsync();

          Console.WriteLine("Hello Word");

        // ایجاد لیست افراد
        List<Person> people = new List<Person>
        {
            new Person { FirstName = "Ali", LastName = "Ahmadi", Age = 25 },
            new Person { FirstName = "Sara", LastName = "Moradi", Age = 30 },
            new Person { FirstName = "Reza", LastName = "Hosseini", Age = 28 },
            new Person { FirstName = "Maryam", LastName = "Karimi", Age = 22 }
        };

      

        Console.WriteLine("سرور روی http://localhost:11231/ در حال اجرا است...");
        Console.WriteLine("برای توقف، ENTER بزنید.");

        var cts = new CancellationTokenSource();

        // اجرای سرور به صورت غیرهمزمان
        var serverTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var context = await server.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context, people));
                }
                catch (HttpListenerException)
                {
                    // سرور Stop شده → خروج از حلقه
                    break;
                }
            }
        });

        // منتظر فشار دادن ENTER
        Console.ReadLine();
        cts.Cancel();
        server.Stop();
        server.Close();
        Console.WriteLine("سرور متوقف شد.");

        await serverTask;
    }

    private static async Task HandleRequest(HttpListenerContext context, List<Person> people)
    {
        var request = context.Request;
        var response = context.Response;

        if (request.Url.AbsolutePath == "/users")
        {
            // تبدیل لیست افراد به JSON
            string json = JsonSerializer.Serialize(people);

            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            Console.WriteLine("یک درخواست به /users دریافت شد ✅");
        }
        else
        {
            // اگر مسیر اشتباه بود
            string message = "404 - Not Found";
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            response.StatusCode = 404;
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            Console.WriteLine("درخواست نامعتبر دریافت شد ❌");
        }
    }
}
