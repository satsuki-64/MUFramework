namespace MUFramework.NetSystem
{
    public class HeartMsg : BaseMsg
    {
        public const int HEART_MSG_ID = 1000;

        public HeartMsg(int playerID) : base(playerID)
        {
            
        }

        public override int GetBytesNum()
        {
            // ID长度 + 消息体长度 + 玩家 ID 长度
            return sizeof(int) * 3;
        }

        /// <summary>
        /// 心跳消息，无数据需要读取
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="beginIndex"></param>
        /// <returns></returns>
        public override int Reading(byte[] bytes, int beginIndex = 0)
        {
            return 0;
        }

        public override byte[] Writing()
        {
            int index = 0;
            byte[] bytes = new byte[GetBytesNum()];

            WriteInt(bytes, GetID(), ref index);
            WriteInt(bytes, 0, ref index);
            WriteInt(bytes, PlayerID, ref index);
            
            return bytes;
        }

        public override int GetID()
        {
            return HEART_MSG_ID;
        }
    }
}