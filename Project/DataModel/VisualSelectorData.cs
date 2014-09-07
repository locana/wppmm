using Kazyx.WPMMM.Controls;
using System.Diagnostics;

namespace Kazyx.WPMMM.DataModel
{
    class VisualSelectorData : ItemGroup
    {
        public int Width
        {
            get
            {
                int horizontal_count = Group.Count >= 3 ? 3 : Group.Count % 3;
                int width = horizontal_count * 84 + 32;
                Debug.WriteLine("Selector width: " + width);
                return width;
            }
        }

        public int Height
        {
            get
            {
                int vertical_count = (Group.Count - 1) / 3 + 1;
                var height = vertical_count * 84 + 16 * 2;
                Debug.WriteLine("Selector height: " + height);
                return height;
            }
        }
    }
}
