using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raydon.CommonData.SharedThreadInfo
{
    public class SharedThreadInfo : TableEntity
    {
        public string venueID { get; set; }
        public string venueName { get; set; }
        public int threadID { get; set; }
        public DateTime updateUtcTime { get; set; }

        public static string threadPartitionKey = "threadIDs";
        public static string SharedStorageTableName = "SharedThreadInfoTable";

        public SharedThreadInfo(string venID, string venName)
        {
            venueID = venID;
            PartitionKey = threadPartitionKey;
            RowKey = venueID;
            venueName = venName;
        }

        public SharedThreadInfo() { }
    };
}
