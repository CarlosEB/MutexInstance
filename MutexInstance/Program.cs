using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Bancada
{
    class Program
    {
        private static List<string> _runned;

        static void Main(string[] args)
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


    public class MutexInstance : IDisposable
    {
        private bool _hasAcquired;

        private Mutex _mutex;

        public MutexInstance(string mutexName, TimeSpan timeOut, int times = 0)
        {
            CreateMutex(mutexName);

            RetryAction.Retry(times, timeOut, () => { TryHandle(mutexName, timeOut); });
        }

        private void TryHandle(string mutexName, TimeSpan timeOut)
        {
            try
            {
                _hasAcquired = _mutex.WaitOne(timeOut, false);

                if (_hasAcquired == false)
                    throw new TimeoutException($"Timeout trying to get exclusive Mutex: {mutexName}.");
            }
            catch (AbandonedMutexException)
            {
                _hasAcquired = true;
            }
        }

        public void Dispose()
        {
            if (_mutex == null) return;

            if (_hasAcquired) _mutex.ReleaseMutex();

            _mutex.Close();
        }

        private void CreateMutex(string mutexName)
        {
            mutexName = mutexName.Replace("\\", "_");

            var mutexId = $"Global\\{{{mutexName}}}";
            _mutex = new Mutex(false, mutexId);

            var rules = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var security = new MutexSecurity();

            security.AddAccessRule(rules);

            _mutex.SetAccessControl(security);
        }
    }

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