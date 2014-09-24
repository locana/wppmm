using Kazyx.WPPMM.Controls;
using Kazyx.WPPMM.Utils;

namespace Kazyx.WPPMM.DataModel
{
    class VisualSelectorData : ItemGroup
    {
        public int Width
        {
            get
            {
                int horizontal_count = Group.Count >= 3 ? 3 : Group.Count % 3;
                int width = horizontal_count * 84 + 32;
                DebugUtil.Log("Selector width: " + width);
                return width;
            }
        }

        public int Height
        {
            get
            {
                int vertical_count = (Group.Count - 1) / 3 + 1;
                var height = vertical_count * 84 + 16 * 2;
                DebugUtil.Log("Selector height: " + height);
                return height;
            }
        }
    }
}
