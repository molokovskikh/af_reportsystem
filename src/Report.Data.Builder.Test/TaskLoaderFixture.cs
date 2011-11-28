using System;
using System.Linq;
using NUnit.Framework;

namespace Report.Data.Builder.Test
{
	[TestFixture]
	public class TaskLoaderFixture
	{
		[Test]
		public void Load_items()
		{
			var items = Enumerable.Range(1, 5).ToList();
			var result = TaskLoader.ParallelLoader(items, i => i, 10).ToArray();
			Assert.That(result.Length, Is.EqualTo(5));
			var tuples = Enumerable.Range(1, 5).Select(i => Tuple.Create(i, i)).ToArray();
			Assert.That(result, Is.EquivalentTo(tuples));
		}
	}
}