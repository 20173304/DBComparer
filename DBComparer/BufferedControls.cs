using System.Windows.Forms;

namespace DBComparer
{
    internal sealed class BufferedLayoutPanel : TableLayoutPanel
    {
        public BufferedLayoutPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    internal sealed class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    internal sealed class BufferedGrid : DataGridView
    {
        public BufferedGrid()
        {
            DoubleBuffered = true;
        }
    }

    // Keep native controls visually enabled while an asynchronous operation runs.
    // Only input inside the settings area is blocked; painting continues normally.
    internal sealed class BusyInputFilter : IMessageFilter
    {
        readonly Control root;
        public BusyInputFilter(Control root)
        {
            this.root = root;
        }
        public bool PreFilterMessage(ref Message message)
        {
            bool keyboard = message.Msg >= 0x100 && message.Msg <= 0x109;
            bool mouse = message.Msg >= 0x201 && message.Msg <= 0x20E;
            if (!keyboard && !mouse && message.Msg != 0x7B)
                return false;
            var target = Control.FromChildHandle(message.HWnd);
            return target != null && (target == root || root.Contains(target) ||
                (keyboard && target == root.FindForm() && root.ContainsFocus));
        }
    }
}
