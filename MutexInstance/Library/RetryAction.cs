using System;
using System.Threading.Tasks;

namespace Bancada.Library
{
    public static class RetryAction
    {
        public static void Retry(int times, TimeSpan timeOut, Action action)
        {
            var attempts = 1;
            while (true)
            {
                try
                {
                    action();
                    break;
                }
                catch
                {
                    if (attempts > times) throw;

                    Console.WriteLine($"Exception caught attempt: {attempts} - will retry after: {timeOut}");

                    Task.Delay(timeOut).Wait();

                    attempts++;
                }
            }
        }
    }
}
