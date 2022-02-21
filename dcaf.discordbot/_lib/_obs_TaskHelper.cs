// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using TetraPak.XP;
//
// namespace DCAF.DiscordBot._lib obsolete
// {
// public static class TaskHelper
//     {
//         /// <summary>
//         ///   Examines the status of a <see cref="TaskCompletionSource"/> and awaits its
//         ///   completion when applicable (the TCS might have already ran to completion) and then returns it.
//         /// </summary>
//         /// <param name="tcs">
//         ///    The <see cref="TaskCompletionSource{TResult}"/> to be awaited.
//         /// </param>
//         /// <typeparam name="T">
//         ///   The task completion source's result type.
//         /// </typeparam>
//         /// <returns>
//         ///   The specified <see cref="TaskCompletionSource{TResult}"/> (<paramref name="tcs"/>). See remarks.
//         /// </returns>
//         /// <remarks>
//         ///   The method always returns the specified <see cref="TaskCompletionSource{TResult}"/> <paramref name="tcs"/>.
//         ///   Caller's should <i>not</i> rely on the instance being assigned after completion as it is a common
//         ///   pattern for many asynchronous operations to create the TCS while initiating and then removing is
//         ///   upon completion.
//         /// </remarks>
//         public static async Task<TaskCompletionSource<T>> AwaitCompletionAsync<T>(this TaskCompletionSource<T> tcs)
//         {
//             if (tcs.Task.Status < TaskStatus.RanToCompletion)
//             {
//                 await tcs.Task.ConfigureAwait(false);
//             }
//
//             return tcs;
//         }
//
//         /// <summary>
//         ///   Blocks the thread while waiting for a result.
//         /// </summary>
//         /// <param name="task">
//         ///   The task to be awaited.
//         /// </param>
//         /// <param name="timeout">
//         ///   (optional)<br/>
//         ///   Specifies a timeout. If operation times our a default result will be sent back.
//         /// </param>
//         /// <param name="cts">
//         ///   (optional)<br/>
//         ///   A cancellation token source, allowing operation cancellation (from a different thread).
//         /// </param>
//         /// <returns>
//         ///   <c>true</c> if <paramref name="task"/> ran to completion; otherwise <c>false</c>.
//         /// </returns>
//         public static bool Await(
//             this Task task,
//             TimeSpan? timeout = null, 
//             CancellationTokenSource? cts = null)
//         {
//             var awaiter = task.ConfigureAwait(false).GetAwaiter();
//             var useTimeout = timeout.HasValue ? DateTime.Now.Add(timeout.Value) : DateTime.MaxValue;
//             var isTimedOut = false;
//             var isCancelled = false;
//             while (!awaiter.IsCompleted && !isTimedOut && !isCancelled)
//             {
//                 Task.Delay(10);
//                 isTimedOut = DateTime.Now >= useTimeout;
//                 isCancelled = cts?.IsCancellationRequested ?? false;
//             }
//
//             return task.Status >= TaskStatus.RanToCompletion;
//         }
//         
//         /// <summary>
//         ///   Blocks the thread while waiting for a result.
//         /// </summary>
//         /// <param name="tcs">
//         ///   The <see cref="TaskCompletionSource{TResult}"/> in use for signalling result is available.
//         /// </param>
//         /// <param name="timeout">
//         ///   (optional)<br/>
//         ///   Specifies a timeout. If operation times our a default result will be sent back.
//         /// </param>
//         /// <param name="cts">
//         ///   (optional)<br/>
//         ///   A cancellation token source, allowing operation cancellation (from a different thread).
//         /// </param>
//         /// <typeparam name="T">
//         ///   The type of result being requested.
//         /// </typeparam>
//         /// <returns>
//         ///   An <see cref="Outcome"/> value, signalling success/failure while also carrying the requested
//         ///   result on success; otherwise an <see cref="Exception"/>.
//         /// </returns>
//         public static Outcome<T> AwaitResult<T>(
//             this TaskCompletionSource<T> tcs, 
//             TimeSpan? timeout = null, 
//             CancellationTokenSource? cts = null)
//         {
//             if (tcs.Task.Status >= TaskStatus.RanToCompletion) 
//                 return Outcome<T>.Success(tcs.Task.Result);
//             
//             var awaiter = tcs.Task.ConfigureAwait(false).GetAwaiter();
//             var useTimeout = timeout.HasValue ? DateTime.Now.Add(timeout.Value) : DateTime.MaxValue;
//             var isTimedOut = false;
//             var isCancelled = false;
//             while (!awaiter.IsCompleted && !isTimedOut && !isCancelled)
//             {
//                 Task.Delay(10);
//                 isTimedOut = DateTime.Now >= useTimeout;
//                 isCancelled = cts?.IsCancellationRequested ?? false;
//             }
//
//             return tcs.Task.Status >= TaskStatus.RanToCompletion
//                 ? Outcome<T>.Success(tcs.Task.Result)
//                 : Outcome<T>.Fail(
//                     isTimedOut
//                         ? new Exception("Result could not be created before operation timed out")
//                         : new Exception("Result could not be created. Operation was cancelled"));
//         }
//     }
// }