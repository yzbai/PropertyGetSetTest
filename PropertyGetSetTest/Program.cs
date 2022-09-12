using System.Diagnostics;
using System.Reflection;

using FastMember;

using Microsoft.Extensions.Internal;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        IList<Book> books = MockBooks();
        Book book = books[0];
        Type bookType = typeof(Book);

        var properties = bookType.GetProperties();

        PropertyAccessor.Set(typeof(Book), book, nameof(Book.Name), "TEst");

        TypeAccessor typeAccessor = TypeAccessor.Create(bookType);
        typeAccessor[book, nameof(Book.Name)] = "TTYYYRYRY";

        PropertyInfo namePropertyInfo = bookType.GetProperty(nameof(Book.Name))!;
        MethodInfo nameGetMethod = namePropertyInfo.GetGetMethod()!;
        MethodInfo nameSetMethod = namePropertyInfo.GetSetMethod()!;

        Action<Book, string?> nameSetDelegate = (Action<Book, string?>)nameSetMethod.CreateDelegate(typeof(Action<Book, string?>));

        Stopwatch stopwatch = new Stopwatch();

        //0
        stopwatch.Restart();
        RunByDirect();
        stopwatch.Stop();

        Console.WriteLine($"Direct Method : {stopwatch.ElapsedMilliseconds}");

        //1
        stopwatch.Restart();
        RunByCachedPropertyInfo();
        stopwatch.Stop();

        Console.WriteLine($"PropertyInfo Method : {stopwatch.ElapsedMilliseconds}");

        //2
        stopwatch.Restart();
        RunByMethodInfo();
        stopwatch.Stop();

        Console.WriteLine($"Getter Setter Method : {stopwatch.ElapsedMilliseconds}");

        //2-1
        stopwatch.Restart();
        RunByMethodInfoDelegate();
        stopwatch.Stop();

        Console.WriteLine($"Getter Setter Delegate Method : {stopwatch.ElapsedMilliseconds}");

        //3
        stopwatch.Restart();
        RunByIL();
        stopwatch.Stop();

        Console.WriteLine($"IL Method : {stopwatch.ElapsedMilliseconds}");

        //4
        stopwatch.Restart();
        RunByIL_without_lookup();
        stopwatch.Stop();

        Console.WriteLine($"IL Without Lookup Method : {stopwatch.ElapsedMilliseconds}");

        //5
        stopwatch.Restart();
        RunByFastMember();
        stopwatch.Stop();

        Console.WriteLine($"Fastmember Method : {stopwatch.ElapsedMilliseconds}");

        //6

        ObjectMethodExecutor methodExecutor = ObjectMethodExecutor.Create(nameSetMethod, bookType.GetTypeInfo());

        stopwatch.Restart();
        RunByObjectMethodExecutor();
        stopwatch.Stop();

        Console.WriteLine($"ObjectMethodExecutor Method : {stopwatch.ElapsedMilliseconds}");

        void RunByDirect()
        {
            //foreach (Book book in books)
            //{
            //    object? obj = book.Name;
            //}

            foreach (Book book in books)
            {
                book.Name = "TTT";
            }

        }

        void RunByCachedPropertyInfo()
        {
            //foreach (Book book in books)
            //{
            //    object? obj = namePropertyInfo.GetValue(book);
            //}

            foreach (Book book in books)
            {
                namePropertyInfo.SetValue(book, "TTT");
            }
        }

        void RunByMethodInfo()
        {
            //foreach (Book book in books)
            //{
            //    object? obj = nameGetMethod.Invoke(book, null);
            //}

            foreach (Book book in books)
            {
                nameSetMethod.Invoke(book, new object[] { "TTTT" });
            }
        }

        void RunByMethodInfoDelegate()
        {

            foreach (Book book in books)
            {
                nameSetDelegate.Invoke(book, "sdfsfs");
            }
        }

        void RunByIL()
        {
            //var action =  PropertyAccessor.GetGetAction<Book>(nameof(Book.Name));

            foreach (Book book in books)
            {
                PropertyAccessor.Set(bookType, (object)book, "Name", (object)"BBBBBBBB");
                PropertyAccessor.Set(bookType, book, nameof(Book.Price), 12.323);
                //action(book, "BBBBB");
            }
        }

        void RunByIL_without_lookup()
        {
            var action = PropertyAccessor.GetSetAction(bookType, nameof(Book.Name));

            foreach (Book book in books)
            {
                //PropertyAccessor.Set(bookType, book, "Name", "BBBBBBBB");
                action(book, "BBBBB");
            }
        }

        void RunByFastMember()
        {
            foreach (Book book in books)
            {
                typeAccessor[book, nameof(Book.Name)] = "sfafasfasf";
            }
        }

        void RunByObjectMethodExecutor()
        {
            foreach (Book book in books)
            {
                methodExecutor.Execute(book, new object[] { "sfasf" });
            }
        }

        static IList<Book> MockBooks(int count = 100000)
        {
            Random random = new Random();
            List<Book> books = new List<Book>();

            for (int i = 0; i < count; ++i)
            {
                books.Add(new Book
                {
                    Name = random.NextInt64().ToString(),
                    Price = random.NextDouble()
                });
            }

            return books;
        }
    }

}

public class Book
{
    public string? Name { get; set; }

    public double Price { get; set; }

    public int[] Indexs { get; set; }

    public IList<string> Lsts { get; set; }

    public object this[int index]
    {
        get
        {
            return "Itttt";
        }
        set
        {

        }
    }
}

