using Xunit.Abstractions;

namespace Xboard.Tests.Framework;

public class UnitTest4(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Test1()
    {
        testOutputHelper.WriteLine("Hello World");
        Thread.Sleep(1000);
    }

    [Fact]
    public void Test2()
    {
        Assert.Equal(1, 2);
    }


    [Fact]
    public void Test3()
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "Not required.")]
    public void Test4()
    {

    }

}
