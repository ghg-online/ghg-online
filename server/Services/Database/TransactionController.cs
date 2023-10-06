using LiteDB;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using static server.Services.Database.ITransactionController;

namespace server.Services.Database
{
    public class TransactionController : ITransactionController
    {
        private readonly ILogger<ITransactionController> logger;
        private readonly IDbHolder dbHolder;

        public TransactionController(IDbHolder dbHolder, ILogger<ITransactionController> logger)
        {
            this.dbHolder = dbHolder;
            this.logger = logger;
        }

        public ITransaction BeginTrans()
        {
            return new Transaction(dbHolder, logger);
        }

        private class Transaction : ITransaction
        {
            private readonly Thread thread;
            private readonly AutoResetEvent _event = new(true);
            private readonly ILogger<ITransactionController> logger;
            private enum State
            {
                Working,
                Committing,
                Rollbacking,
                End,
                Error,
            }
            private State state;
            private readonly IDbHolder dbHolder;
            private Exception? exception = null; // not null when state == State.Error

            // This should be excuted in a seperate thread
            // since LiteDB transaction depends on thread id
            private void Work()
            {
                try
                {
                    while (true)
                    {
                        _event.WaitOne();
                        switch (state)
                        {
                            case State.Working:
                                // do nothing
                                break;

                            case State.Committing:
                                dbHolder.Commit();
                                state = State.End;
                                return;

                            case State.Rollbacking:
                                dbHolder.Rollback();
                                state = State.End;
                                return;

                            default:
                                throw new Exception("This is impossible!");
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Transaction failed with an exception.", state);
                    state = State.Error;
                    exception = e;
                }
            }

            public Transaction(IDbHolder dbHolder, ILogger<ITransactionController> logger)
            {
                state = State.Working;
                this.dbHolder = dbHolder;
                this.logger = logger;
                thread = new Thread(Work);
                thread.Start();
            }

            public void Commit()
            {
                state = State.Committing;
                _event.Set();
                thread.Join();
                Debug.Assert(thread.ThreadState == System.Threading.ThreadState.Stopped);
                if (state == State.Error)
                    ExceptionDispatchInfo.Throw(exception!);
            }

            public void Rollback()
            {
                state = State.Rollbacking;
                _event.Set();
                thread.Join();
                Debug.Assert(thread.ThreadState == System.Threading.ThreadState.Stopped);
                if (state == State.Error)
                    ExceptionDispatchInfo.Throw(exception!);
            }

            public void Dispose()
            {
                Rollback();
            }
        }
    }
}
