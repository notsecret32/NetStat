using System;

namespace NetStat
{
    [Serializable]
    public class NetStatObject 
    {
        public string Message { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public int BytesReveived { get; set; }
        public int BytesSended { get; set; }
        public string ProtocolType { get; set; }
        public RequestList RequestList { get; set; }

        public NetStatObject() 
        {
            RequestList = RequestList.GET_TCP_DATA;
            IPAddress   = string.Empty;
            Port        = 0;
            BytesReveived = 0;
            BytesSended   = 0;
            ProtocolType  = string.Empty;
        }
    }
}
