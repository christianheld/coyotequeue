using PublicApiGenerator;

namespace AsyncOrderedQueue.UnitTests;

public class PublicApiSurfaceTests
{
    [Fact]
    public async Task PublicApiIsUnchanged()
    {
        // Arrange
        const string FileName = "../../../ApprovedApi.txt";
        var currentApi = typeof(OrderedQueue<>).Assembly.GeneratePublicApi(new()
        {
            ExcludeAttributes = new[]
            {
                "Microsoft.Coyote.Rewriting.RewritingSignatureAttribute"
            }
        });

        var assemblyVersion = typeof(OrderedQueue<>).Assembly.GetName().Version!;

        // Act
        if (assemblyVersion.Major > 0 && File.Exists(FileName))
        {
            var approvedApi = await File.ReadAllTextAsync(FileName);
            Assert.Equal(approvedApi, currentApi);
        }
        else
        {
            await File.WriteAllTextAsync(FileName, currentApi);
        }
    }
}
