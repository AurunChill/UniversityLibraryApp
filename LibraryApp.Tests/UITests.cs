using LibraryApp.UI.Helpers;
using System.Windows.Forms;
using Xunit;

namespace LibraryApp.Tests;

public class UITests
{
    [Fact]
    public void AtExtensionSetsPositionAndWidth()
    {
        var btn = new Button();
        btn.At(10, 20, 200);
        Assert.Equal(10, btn.Left);
        Assert.Equal(20, btn.Top);
        Assert.Equal(200, btn.Width);
    }
}
