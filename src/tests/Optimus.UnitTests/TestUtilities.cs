using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Optimus.UnitTests
{
    internal static class TestUtilities
    {
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }

        public static async Task TimeoutAfter(this Task task, TimeSpan timeout) =>
            await task.ToGeneric<bool>().TimeoutAfter(timeout);

        public static async Task<T> TimeoutAfter<T>(this ValueTask<T> task, TimeSpan timeout) =>
            await task.AsTask().TimeoutAfter(timeout);

        private static async Task<T?> ToGeneric<T>(this Task task)
        {
            await task;
            return default(T);
        }
    }
}
