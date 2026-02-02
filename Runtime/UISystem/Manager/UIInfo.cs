using MUFramework.DataSystem;

namespace MUFramework.UISystem
{
    public sealed class UIInfo : AssetInfo
    {
        public readonly bool IsActiveAfterLoaded = false;

        public UIInfo(string name, AssetLoadType loadType, string path, bool isActiveAfterLoaded = false) : base(name, loadType, path)
        {
            this.IsActiveAfterLoaded = isActiveAfterLoaded;
        }
        
    }
}