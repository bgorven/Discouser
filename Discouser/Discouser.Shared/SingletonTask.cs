using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discouser
{
    class SingletonTask
    {
        private Tuple<Task, CancellationTokenSource> _task;
        private Logger _logger;

        internal SingletonTask(Logger logger)
        {
            _logger = logger;
        }

        internal async Task SetTask(Func<CancellationToken, Task> action)
        {
            var newSource = new CancellationTokenSource();
            var newTask = Tuple.Create(Task.Run(async () => await action(newSource.Token), newSource.Token), newSource);

            var oldTask = Interlocked.Exchange(ref _task, newTask);

            if (oldTask == null) return;
            try
            {
                oldTask.Item2.Cancel();
                await oldTask.Item1;
                oldTask.Item2.Dispose();
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
