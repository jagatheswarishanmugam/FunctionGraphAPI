using System.Collections.Generic;
using Microsoft.Graph;

namespace FunctionGraphAPI
{
    public class GraphNotification
    {
        public List<ChangeNotification> value { get; set; }
    }
}