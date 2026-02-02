namespace MUFramework.NetSystem
{
    public class ConnectMsg : BaseMsg
    {
        public const int CONNECT_MSG_ID = 999;

        public ConnectMsg(int playerID) : base(playerID)
        {
            
        }

        public override int GetBytesNum()
        {
            // ID + 消息体长度 + 玩家ID + Vector3 
            return sizeof(int) * 3;
        }

        public override byte[] Writing()
        {
            int index = 0;
            byte[] bytes = new byte[GetBytesNum()];

            // 客户端无需写入
            return bytes;
        }

        public override int Reading(byte[] bytes, int beginIndex = 0)
        {
            int index = 0;
            
            int MsgId = ReadInt(bytes, ref index);
            int MsgLength = ReadInt(bytes, ref index);
            int id =  ReadInt(bytes, ref index);
            
            return 0;
        }
        
        public override int GetID()
        {
            return CONNECT_MSG_ID;
        }
    }
}