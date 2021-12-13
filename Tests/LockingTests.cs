using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AsyncLockTests
{
    public class LockingTests
    {
        [Fact]
        public void WithLocking()
        {
            var data = new SampleData { Name = "Same Name" };
            var sampleClass = new SampleClass(true);
            var task1 = sampleClass.AddData(data, wait: true);
            var task2 = sampleClass.AddData(data, wait: false);

            Task.WaitAll(task1, task2);
        }

        [Fact]
        public void WithoutLocking()
        {
            try
            {
                var data = new SampleData { Name = "Same Name" };
                var sampleClass = new SampleClass(false);
                var task1 = sampleClass.AddData(data, wait: true);
                var task2 = sampleClass.AddData(data, wait: false);
                Task.WaitAll(task1, task2);
                Assert.True(false, "No Exception is thrown");
            }
            catch(Exception exp)
            {
                Assert.NotNull(exp.InnerException);
                Assert.IsType<ArgumentException>(exp.InnerException);
                Assert.Equal("An item with the same key has already been added. Key: Same Name", exp.InnerException.Message);
            }
        }




    }
}
