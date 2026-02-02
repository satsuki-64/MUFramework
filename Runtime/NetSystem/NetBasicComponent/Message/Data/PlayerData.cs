using UnityEngine;

namespace MUFramework.NetSystem
{
    public class PlayerData : BaseData
    {
        public Vector3 position;
        
        public override int GetBytesNum()
        {
            return sizeof(float) * 3;
        }

        public override byte[] Writing()
        {
            int index = 0;
            byte[] bytes = new byte[GetBytesNum()];
            
            WriteFloat(bytes, position.x, ref index);
            WriteFloat(bytes, position.y, ref index);
            WriteFloat(bytes, position.z, ref index);

            return bytes;
        }

        public override int Reading(byte[] bytes, int beginIndex = 0)
        {
            int index = beginIndex;
            
            position = new Vector3(
                ReadInt(bytes,ref index),
                ReadInt(bytes,ref index),
                ReadInt(bytes,ref index)
                );
            
            return index - beginIndex;
        }
    }
}