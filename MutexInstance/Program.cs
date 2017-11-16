using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bancada.Library;

namespace Bancada
{
    class Program
    {
        private static List<string> _runned;

        static void Main()
        {
            _runned = new List<string>();

            var tasks = new List<Task>();
            var random = new Random();

            for (var i = 0; i < 10; i++)
            {
                var item = i + 1;
                tasks.Add(
                    Task.Factory.StartNew(() =>
                    {
                        var timeout = TimeSpan.FromSeconds(random.Next(1, 10));
                        var retry = random.Next(0, 3);

                        try
                        {
                            using (new MutexInstance("Clearing", timeout, retry))
                                RunSomeStuff($"Task {item}");
                        }
                        catch
                        {
                            Console.WriteLine($"Task {item} - TimeOut: {timeout} - Retry: {retry} - TIMEOUT!!!");
                        }
                    })
                );
            }

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine("--------------------------------------------------------------------");

            foreach (var item in _runned.OrderBy(o => o)) Console.WriteLine(item);

            Console.WriteLine("--------------------------------------------------------------------");

            Console.ReadKey();
        }

        private static void RunSomeStuff(string task)
        {
            _runned.Add(task);

            Console.WriteLine($"Begin {task}");
            Task.Delay(TimeSpan.FromSeconds(new Random().Next(1, 10))).Wait();
            Console.WriteLine($"End {task}");
        }
    }
}