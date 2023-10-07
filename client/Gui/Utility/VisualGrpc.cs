/*  
 *  Namespace   :   client.Gui
 *  Filename    :   VisualGrpc.cs
 *  Class       :   VisualGrpc
 *  
 *  Creator     :   Nictheboy
 *  Create at   :   2023/08/22
 *  Last Modify :   2023/10/07
 *  
 *  Change Log:
 *      2023/09/24   fix a double close bug when user click the cancel button
 *      2023/10/05   add support for synchronous invoke and its error handling
 *      2023/10/06   changed default time to show waiting dialog to 1000ms
 *      2023/10/07   add support for status-bar based visualization
 *  
 */

using client.Gui.StatusBar;
using Google.Protobuf;
using Grpc.Core;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using Terminal.Gui;

namespace client.Gui
{
    /// <summary>
    /// This static class is used to invoke gRPC methods with some sort of visualization.
    /// </summary>
    public static class VisualGrpc
    {
        /// <summary>
        /// When the invoke takes longer than the timeout span, this exception is thrown.
        /// </summary>
        public class InvokeTimeoutException : System.Exception
        {
            public InvokeTimeoutException() : base("Invoke timeout") { }
        }

        /// <summary>
        /// When user clicks "cancel" button during an invoke, this exception is thrown.
        /// </summary>
        public class UserCancelledException : System.Exception
        {
            public UserCancelledException() : base("Operation cancelled by user") { }
        }

        public static bool ShowDialog
        {
            get
            {
                return false == (Application.Top.StatusBar?.Visible ?? false);
            }
        }

        /// <summary>
        /// Invoke a gRPC method and show a waiting dialog.
        /// </summary>
        /// <typeparam name="TRequest">Type name of your gRPC request</typeparam>
        /// <typeparam name="TRespond">Type name of your gRPC respond</typeparam>
        /// <param name="func">Async version of your gRPC procedure, such as LoginAsync</param>
        /// <param name="request">The request you will send</param>
        /// <returns>A task that will contain your respond</returns>
        /// <example>
        /// <code>var loginRespond = await VisualGrpc.InvokeAsync(LoginAsync, loginRequest);</code>
        /// is equivalent to
        /// <code>var loginRespond = await LoginAsync(loginRequest);</code>
        /// except that we provide with some sort of visualization.
        /// </example>
        /// <remarks>
        /// <para>
        /// This method is equivalent to <code>InvokeAsync(func, request, 1000, -1);</code>,
        /// which means the waiting dialog will be shown after 1000 milliseconds, and the invoke will not timeout.
        /// </para>
        /// <para>If you have called <c>LoadToken</c>, the token will be carried in your invoke by metadata (in http header)</para>
        /// </remarks>
        public static async Task<TRespond> InvokeAsync<TRequest, TRespond>(Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TRespond>> func, TRequest request)
            where TRequest : IBufferMessage
            where TRespond : IBufferMessage, new()
        {
            return await InvokeAsync(func, request, 1000, -1);
        }


        /// <summary>
        /// Invoke a gRPC method and show a waiting dialog.
        /// </summary>
        /// <typeparam name="TRequest">Type name of your gRPC request</typeparam>
        /// <typeparam name="TRespond">Type name of your gRPC respond</typeparam>
        /// <param name="func">Async version of your gRPC procedure, such as LoginAsync</param>
        /// <param name="request">The request you will send</param>
        /// <returns>A task that will contain your respond</returns>
        /// <remarks>
        /// <para>
        /// This method is equivalent to <code>Invoke(func, request, 1000, -1);</code>,
        /// which means the waiting dialog will be shown after 1000 milliseconds, and the invoke will not timeout.
        /// </para>
        /// <para>If you have called <c>LoadToken</c>, the token will be carried in your invoke by metadata (in http header)</para>
        /// </remarks>

        // This function is added on 2023/10/05
        public static TRespond Invoke<TRequest, TRespond>(Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TRespond>> func, TRequest request)
            where TRequest : IBufferMessage
            where TRespond : IBufferMessage, new()
        {
            var task = InvokeAsync(func, request);
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

        /// <summary>
        /// Invoke a gRPC method, show a waiting dialog, and cancel the invoke automatically if timeout.
        /// </summary>
        /// <typeparam name="TRequest">Type name of your gRPC request</typeparam>
        /// <typeparam name="TRespond">Type name of your gRPC respond</typeparam>
        /// <param name="func">Async version of your gRPC procedure, such as LoginAsync</param>
        /// <param name="request">The request you will send</param>
        /// <param name="showWait">How many milliseconds before the dialog is shown. Zero means immediately. Negative number means never.</param>
        /// <param name="timeOut">How many milliseconds before the invoke timeout. Negative number means never.</param>
        /// <returns>A task that will contain your respond</returns>
        /// <example>
        /// <code>var loginRespond = await VisualGrpc.Invoke(LoginAsync, loginRequest, -1, -1);</code>
        /// is equivalent to
        /// <code>var loginRespond = await LoginAsync(loginRequest);</code>
        /// </example>
        /// <remarks>
        /// <para>If you have called <c>LoadToken</c>, the token will be carried in your invoke by metadata (in http header)</para>
        /// </remarks>
        public static async Task<TRespond> InvokeAsync<TRequest, TRespond>(
            Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TRespond>> func,
            TRequest request,
            int showWait,
            int timeOut
        ) where TRequest : IBufferMessage
            where TRespond : IBufferMessage, new()
        {
            var handler = new InvokeHandler(showWait, timeOut);
            TRespond respond = new();
            await handler.Invoke(async (cancellationToken) =>
            {
                respond = await func(request, entries, null, cancellationToken);
            });
            return respond;
        }

        /// <summary>
        /// Invoke a gRPC method, show a waiting dialog, and cancel the invoke automatically if timeout.
        /// </summary>
        /// <typeparam name="TRequest">Type name of your gRPC request</typeparam>
        /// <typeparam name="TRespond">Type name of your gRPC respond</typeparam>
        /// <param name="func">Async version of your gRPC procedure, such as LoginAsync</param>
        /// <param name="request">The request you will send</param>
        /// <param name="showWait">How many milliseconds before the dialog is shown. Zero means immediately. Negative number means never.</param>
        /// <param name="timeOut">How many milliseconds before the invoke timeout. Negative number means never.</param>
        /// <returns>A task that will contain your respond</returns>
        /// <remarks>
        /// <para>If you have called <c>LoadToken</c>, the token will be carried in your invoke by metadata (in http header)</para>
        /// </remarks>

        // This function is added on 2023/10/05
        public static TRespond Invoke<TRequest, TRespond>(
            Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TRespond>> func,
            TRequest request,
            int showWait,
            int timeOut
        ) where TRequest : IBufferMessage
            where TRespond : IBufferMessage, new()
        {
            var task = InvokeAsync(func, request, showWait, timeOut);
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

        /// <summary>
        /// Load a token to be carried in your invoke by metadata (in http header),
        /// and all gRPC methods that require authorization will succeed.
        /// Role information is also decoded from the token and stored in <c>ConnectionInfo</c>.
        /// </summary>
        /// <param name="token">
        /// Your jwt token. It's obtained by creating <c>client.Gui.LoginDialog</c> object and wait for user to login.
        /// </param>
        public static void LoadToken(string token)
        {
            ClearToken();
            entries.Add(new Metadata.Entry("Authorization", $"Bearer {token}"));
            JwtSecurityTokenHandler handler = new();
            JwtSecurityToken jwt = handler.ReadJwtToken(token);
            ConnectionInfo.LoadRoleCode(jwt.Claims.First(c => c.Type == "Role").Value);
        }

        /// <summary>
        /// Delete your token. You need to call <c>LoadToken</c> again to gain your token.
        /// All gRPC methods that require authorization will fail until you call <c>LoadToken</c> again.
        /// </summary>
        /// <remarks>
        /// In our design, the only place that a token is stored is in <c>VisualGrpc</c>.
        /// So, you don't need to delete your token in other places.
        /// We hope that this design can be obeyed both for security and convenience.
        /// <para>
        /// A similar place that holds user's termColors is ConnectionInfo,
        /// but information there is less sensitive.
        /// </para>
        /// </remarks>
        public static void ClearToken()
        {
            Metadata.Entry? entry = entries.FirstOrDefault(e => e.Key.ToLower() == "Authorization".ToLower());
            if (entry == default)
                return;
            entries.Remove(entry);
        }

        readonly static Metadata entries = new();

        private class InvokeHandler
        {
            readonly int noticeTime;
            readonly int timeOut;
            readonly CancellationTokenSource cancellationTokenSource = new();
            object? noticeTimer = null;
            object? timeOutTimer = null;
            CancelableProcedureDialogue? dialog;
            bool userCanceled = false;

            public InvokeHandler(int noticeTime, int timeOut)
            {
                this.noticeTime = noticeTime;
                this.timeOut = timeOut;
            }

            public async Task Invoke(Func<CancellationToken, Task> func)
            {
                BeginTiming();
                VisualGrpcStatusItem.Instance.Increase();
                try
                {
                    await func(cancellationTokenSource.Token);
                }
                catch (RpcException e) when (e.Status.StatusCode == Status.DefaultCancelled.StatusCode)
                {
                    if (userCanceled)
                    {
                        throw new UserCancelledException();
                    }
                    else
                    {
                        //Application.MainLoop.Invoke(() =>
                        //{
                        MessageBox.ErrorQuery("Server timeout", "Server timeout, please try again later.", "Abort");
                        //});
                        throw new InvokeTimeoutException();
                    }
                }
                finally
                {
                    VisualGrpcStatusItem.Instance.Decrease();

                    Application.MainLoop.RemoveTimeout(noticeTimer);
                    Application.MainLoop.RemoveTimeout(timeOutTimer);
                    //Application.MainLoop.Invoke(() => {

                    // ## Before change on 2023/9/24:
                    // dialog?.RequestStop();
                    // ## After change on 2023/9/24:
                    if (dialog is not null && dialog.IsCanceled == false)
                        dialog.RequestStop();
                    // ## End of change on 2023/9/24
                    // ## This change is to fix a double close bug when user click the cancel button.

                    await Task.Delay(1); // wait for the dialog to close
                    //});
                }
            }

            void BeginTiming()
            {
                if (noticeTime >= 0)
                {
                    noticeTimer = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(noticeTime), (MainLoop) =>
                    {
                        OnShowNoticeTimerElapsed(null, new EventArgs());
                        return false;
                    });
                }
                //else if (noticeTime == 0)
                //{
                //    OnShowNoticeTimerElapsed(null, new EventArgs());
                //}

                if (timeOut >= 0)
                {
                    timeOutTimer = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(timeOut), (MainLoop) =>
                    {
                        OnTimeOutTimerElapsed(null, new EventArgs());
                        return false;
                    });
                }
                //else if (timeOut == 0)
                //{
                //    OnTimeOutTimerElapsed(null, new EventArgs());
                //}
            }

            void OnShowNoticeTimerElapsed(object? sender, EventArgs e)
            {
                if (ShowDialog)
                {
                    dialog = new("Wait", "Waiting for server respond...", UserCancelRequest);
                    //Application.MainLoop.Invoke(() =>
                    //{
                    Application.Run(dialog);
                    //});
                }
            }

            void OnTimeOutTimerElapsed(object? sender, EventArgs e)
            {
                //Application.MainLoop.Invoke(() => {
                dialog?.RequestStop();
                //});
                cancellationTokenSource.Cancel();
            }

            void UserCancelRequest()
            {
                userCanceled = true;
                cancellationTokenSource.Cancel();
            }
        }
    }
}
