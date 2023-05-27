using System;

namespace NetStat
{
    [Serializable]
    public class NetStatObject 
    {
        public string Message { get; set; }
        public RequestList RequestList { get; set; }

        public NetStatObject() 
        {
            RequestList = RequestList.GET_TCP_DATA;
        }
    }
}
