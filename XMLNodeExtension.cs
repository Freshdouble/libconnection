using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace libconnection
{
    public static class XMLNodeExtension
    {
        public static IEnumerable<XmlNode> GetChildsOrdered(this XmlNode node)
        {
            var childcount = node.ChildNodes.Count;
            for (var i = 0; i < childcount; i++)
            {
                yield return node.ChildNodes[i];
            }
        }
    }
}
