using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discouser
{
    class SingletonTask
    {
        private Tuple<string, Task, CancellationTokenSource> _task;
        private Logger _logger;

        internal SingletonTask(Logger logger)
        {
            _logger = logger;
        }

        internal async Task SetTask(string taskName, Func<CancellationToken, Task> action)
        {
            var newSource = new CancellationTokenSource();
            var newTask = Tuple.Create(taskName, Task.Run(async () => await action(newSource.Token), newSource.Token), newSource);

            var oldTask = Interlocked.Exchange(ref _task, newTask);

            if (oldTask == null) return;
            try
            {
                oldTask.Item3.Cancel();
                await oldTask.Item2;
                oldTask.Item3.Dispose();
            }
            catch (AggregateException e)
            {
                foreach (var inner in e.InnerExceptions)
                {
                    _logger.Log(inner);
                }
            }
        }
    }
}
