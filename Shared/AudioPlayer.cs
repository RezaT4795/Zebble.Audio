﻿namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;
    using Olive;

    public partial class AudioPlayer : IDisposable
    {
        TaskCompletionSource<bool> Ended = new TaskCompletionSource<bool>();

        public readonly AsyncEvent Completed = new AsyncEvent();

        public Task Play(string source, OnError errorAction = OnError.Toast)
        {
            return ExecuteSafe(() => DoPlay(source), errorAction, "Failed to play audio file");
        }

        public Task Stop(OnError errorAction = OnError.Toast)
        {
            return ExecuteSafe(StopPlaying, errorAction, "Failed to stop playing audio.");
        }

        Task ExecuteSafe(Func<Task> execution, OnError errorAction, string errorMessage)
        {
            var task = new TaskCompletionSource<bool>();

            AudioThread.Post(async () =>
            {
                try
                {
                    await execution();
                    task.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    if (errorAction == OnError.Throw) task.TrySetException(ex);
                    else
                    {
                        await errorAction.Apply(ex, "Failed to play audio file");
                        task.TrySetResult(false);
                    }
                }
            });

            return task.Task;
        }

        async Task DoPlay(string file)
        {
            await Stop(OnError.Ignore);

            Ended = new TaskCompletionSource<bool>();

            if (file.IsUrl()) await PlayStream(file);
            else await PlayFile(file);
#if !ANDROID
            await Completed.RaiseOn(Thread.Pool);
#endif
        }
    }
}
