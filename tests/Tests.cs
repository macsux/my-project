using FluentAssertions;

namespace MyProject.Tests;

public class Tests
{
    [Test]
    public void HelloWorld()
    {
        var sut = new MyClass();
        sut.HelloWorld().Should().Be("Hello World");
    }

   
}