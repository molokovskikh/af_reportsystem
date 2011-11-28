using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Report.Data.Builder
{
	public class TaskLoader
	{
		public static IEnumerable<Tuple<T, TResult>> ParallelLoader<T, TResult>(List<T> items, Func<T, TResult> taskFunc, int taskCount)
		{
			if (taskCount <= 0)
				throw new ArgumentOutOfRangeException("taskCount", "количество ниток должно быть больше нуля");

			if (taskCount > items.Count())
				taskCount = items.Count();

			var tasks = new Task<TResult>[taskCount];
			for(var i = 0; i < taskCount; i++)
			{
				tasks[i] = RunTask(items[i], taskFunc);
			}

			while (items.Count() > 0)
			{
				var index = Task.WaitAny(tasks);
				var task = tasks[index];
				if (items.Count() > taskCount)
				{
					tasks[index] = RunTask(items.Skip(taskCount).First(), taskFunc);
				}
				else
				{
					tasks[index] = null;
					tasks = tasks.Where(t => t != null).ToArray();
				}
				var item = (T) task.AsyncState;
				items.Remove(item);
				yield return Tuple.Create(item, task.Result);
			}
		}

		public static Task<TResult> RunTask<T, TResult>(T item, Func<T, TResult> taskFunc)
		{
			var task = new Task<TResult>(
				state => taskFunc((T)state),
				item);

			task.Start();
			return task;
		}
	}
}