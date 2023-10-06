using System.Diagnostics;

namespace client.Utils
{
    public static class AvoidAggragateExceptionExtensions
    {
        public static TResult GetResultWithoutAggragateException<TResult>(this Task<TResult> task)
        {
            try
            {
                return task.Result;
            }
            catch (AggregateException e)
            {
                Debug.Assert(e.InnerExceptions.Count == 1);
                throw e.InnerExceptions[0];
            }
        }

        public static void WaitWithoutAggragateException(this Task task)
        {
            try
            {
                task.Wait();
            }
            catch (AggregateException e)
            {
                Debug.Assert(e.InnerExceptions.Count == 1);
                throw e.InnerExceptions[0];
            }
        }
    }
}
