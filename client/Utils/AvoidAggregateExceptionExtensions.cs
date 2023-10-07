using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace client.Utils
{
    public static class AvoidAggregateExceptionExtensions
    {
        public static TResult GetResultWithoutAggregateException<TResult>(this Task<TResult> task)
        {
            try
            {
                return task.Result;
            }
            catch (AggregateException e)
            {
                Debug.Assert(e.InnerExceptions.Count == 1);
                ExceptionDispatchInfo.Throw(e.InnerExceptions[0]);
                throw; // Unreachable
            }
        }

        public static void WaitWithoutAggregateException(this Task task)
        {
            try
            {
                task.Wait();
            }
            catch (AggregateException e)
            {
                Debug.Assert(e.InnerExceptions.Count == 1);
                ExceptionDispatchInfo.Throw(e.InnerExceptions[0]);
                throw; // Unreachable
            }
        }
    }
}
