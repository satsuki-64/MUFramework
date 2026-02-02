namespace MUFramework.NetSystem
{
    public enum MsgType
    {
        HeartBeat = 1000,
        Quit = 1001,
    }
    
    public class BaseMsg : BaseData
    {
        protected int PlayerID;

        public BaseMsg(int playerID)
        {
            this.PlayerID = playerID;
        }

        private BaseMsg() { }

        public override int GetBytesNum()
        {
            throw new System.NotImplementedException();
        }

        public override byte[] Writing()
        {
            throw new System.NotImplementedException();
        }

        public override int Reading(byte[] bytes, int beginIndex = 0)
        {
            throw new System.NotImplementedException();
        }

        public virtual int GetID()
        {
            return 0;
        }
        
        public int GetPlayerID()
        {
            return PlayerID;
        }
    }
}