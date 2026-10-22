using System;
using Xunit;
using Tedd.TUI.Controls;

namespace Tedd.TUI.Tests
{
    public class LabelTests
    {
        [Fact]
        public void Label_HasTargetProperty()
        {
            var label = new Label();
            var target = new TextBox();
            label.Target = target;

            Assert.Same(target, label.Target);
            Assert.False(label.Focusable);
        }
    }
}
