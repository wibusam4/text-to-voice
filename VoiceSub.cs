using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATextToVoice
{
    public class VoiceSub
    {
        public int id { get; set; }
        public string sub { get; set; }
        public TimeSpan start { get; set; }
        public TimeSpan end { get; set; }
        public string status { get; set; }

        public VoiceSub(int id, string sub, TimeSpan start, TimeSpan end, string status)
        {
            this.id = id;
            this.sub = sub;
            this.start = start;
            this.end = end;
            this.status = status;
        }

        public static List<VoiceSub> listVoiceSub = new List<VoiceSub>();
    }
}
