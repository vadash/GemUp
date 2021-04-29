using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace GemUp
{
    public class GemUpSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
    }
}