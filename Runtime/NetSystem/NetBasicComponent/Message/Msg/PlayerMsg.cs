using UnityEngine;

namespace MUFramework.NetSystem
{
    public class PlayerMsg : BaseMsg
    {
        public const int PLAYER_MSG_ID = 1002;
        public PlayerData PlayerData;

        public PlayerMsg(int playerID) : base(playerID)
        {
            
        }

        public override int GetBytesNum()
        {
            // ID + 消息体长度 + 玩家ID + Vector3 
            return sizeof(int) * 3 + PlayerData.GetBytesNum();
        }

        public override byte[] Writing()
        {
            int index = 0;
            byte[] bytes = new byte[GetBytesNum()];

            // 1. 写入 ID
            WriteInt(bytes, GetID(), ref index);
            
            // 2. 写入消息体长度
            WriteInt(bytes, GetBytesNum() - 8, ref index);

            // 3. 写入玩家 ID
            WriteInt(bytes, PlayerID, ref index);

            // 4. 写入玩家数据
            WriteData(bytes, PlayerData, ref index);
            
            return bytes;
        }

        public override int Reading(byte[] bytes, int beginIndex = 0)
        {
            int index = 0;
            
            int MsgId = ReadInt(bytes, ref index);
            int MsgLength = ReadInt(bytes, ref index);
            int id =  ReadInt(bytes, ref index);
            PlayerData = ReadData<PlayerData>(bytes, ref index);
            
            return index - 12;
        }
        
        public override int GetID()
        {
            return PLAYER_MSG_ID;
        }
    }
}