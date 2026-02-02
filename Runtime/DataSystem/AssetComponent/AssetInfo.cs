namespace MUFramework.DataSystem
{
    public class AssetInfo
    {
        /// <summary>
        /// AB：AB 包中的目标资源名称
        /// Resources：资源别名
        /// AssetDatabase：资源别名
        /// </summary>
        public readonly string Name;
        
        /// <summary>
        /// 当前 UI 的路径，根据 LoadType 不同存储不同的格式的路径
        /// </summary>
        public readonly string Path;
        
        /// <summary>
        /// 资源类型
        /// </summary>
        public readonly AssetLoadType LoadType;

        public AssetInfo(string name, AssetLoadType loadType, string path)
        {
            this.Name = name;
            this.Path = path;
            this.LoadType = loadType;
        }

        private AssetInfo() { }
    }
}