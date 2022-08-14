using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;

using Xunit.Abstractions;

using ConcurrencyTestAttribute = Microsoft.Coyote.SystematicTesting.TestAttribute;

namespace AsyncOrderedQueue.UnitTests;

public class ConcurrencyTests
{
    private readonly ITestOutputHelper _outputHelper;

    public ConcurrencyTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    [Trait("Category", "ConcurrencyTest")]
    public void ConcurrentEnququeDequeue()
    {
        var config = Configuration.Create();

        var engine = TestingEngine.Create(config, ConcurrentEnqueueDequeueCore);

        engine.Run();

        var report = engine.TestReport;

        _outputHelper.WriteLine(engine.GetReport());

        Assert.True(report.NumOfFoundBugs == 0, $"Coyote found {report.NumOfFoundBugs} bugs");
    }

    [ConcurrencyTest]
    public static async Task ConcurrentEnqueueDequeueCore()
    {
        // Arrange
        var sut = new OrderedQueue<int>();

        int count = 100;
        var tasks = new List<Task<int>>(100);
        var seqNos = Enumerable.Range(0, count)
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        // Act
        var dequeueThread = Task.Run(async () =>
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Delay(Random.Shared.Next(5, 10));
                tasks.Add(sut.DequeueAsync());
            }
        });

        var enqueueThread = Task.Run(async () =>
        {
            foreach (var seqNo in seqNos)
            {
                await Task.Delay(Random.Shared.Next(5, 10));
                sut.Enqueue(seqNo, seqNo);
            }
        });

        await Task.WhenAll(enqueueThread, dequeueThread);

        var items = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(count, items.Length);
        for (int i = 0; i < count; i++)
        {
            Assert.Equal(i, items[i]);
        }
    }
}