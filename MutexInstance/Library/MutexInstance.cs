using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Bancada.Library
{
    public class MutexInstance : IDisposable
    {
        private bool _hasAcquired;

        private Mutex _mutex;

        public MutexInstance(string mutexName, TimeSpan timeOut, int times = 0)
        {
            CreateMutex(mutexName);

            RetryAction.Retry(times, timeOut, () => { TryAcquire(mutexName, timeOut); });
        }

        private void TryAcquire(string mutexName, TimeSpan timeOut)
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
}
