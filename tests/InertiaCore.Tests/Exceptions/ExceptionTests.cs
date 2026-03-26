using InertiaCore.Exceptions;

namespace InertiaCore.Tests.Exceptions;

[Trait("Class", "Exceptions")]
public class ExceptionTests
{
    [Fact]
    public void InertiaException_default_constructor()
    {
        var ex = new InertiaException();
        Assert.NotNull(ex);
    }

    [Fact]
    public void InertiaException_message_constructor()
    {
        var ex = new InertiaException("test error");
        Assert.Equal("test error", ex.Message);
    }

    [Fact]
    public void InertiaException_message_and_inner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new InertiaException("outer", inner);
        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ComponentNotFoundException_default_constructor()
    {
        var ex = new ComponentNotFoundException();
        Assert.NotNull(ex);
    }

    [Fact]
    public void ComponentNotFoundException_message_constructor()
    {
        var ex = new ComponentNotFoundException("not found");
        Assert.Equal("not found", ex.Message);
    }

    [Fact]
    public void ComponentNotFoundException_message_and_inner()
    {
        var inner = new Exception("inner");
        var ex = new ComponentNotFoundException("outer", inner);
        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ComponentNotFoundException_is_InertiaException()
    {
        var ex = new ComponentNotFoundException("test");
        Assert.IsAssignableFrom<InertiaException>(ex);
    }
}
