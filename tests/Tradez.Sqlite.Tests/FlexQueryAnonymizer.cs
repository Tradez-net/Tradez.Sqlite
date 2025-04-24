namespace Tradez.Sqlite.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    public static class FlexQueryAnonymizer
    {
        private static readonly Dictionary<string, string> SensitiveAttributes = new()
    {
            { "accountId", "1" }, {"acctAlias", "2" }, { "conid", "3" }, { "tradeID", "4" }, { "transactionID", "5" },
        { "ibOrderID", "6" }, { "ibExecID", "7" }, { "brokerageOrderID", "8" }, { "orderReference", "9" },
        { "clientReference", "10" }, { "origOrderID", "11" }, { "origTradeID", "12" }, { "clearingFirmID", "13" }
    };

        public static string AnonymizeXml(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            AnonymizeNodes(doc.DocumentElement);

            using var sw = new StringWriter();
            using var xw = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true });
            doc.WriteTo(xw);
            xw.Flush();
            return sw.ToString();
        }

        private static void AnonymizeNodes(XmlNode node)
        {
            if (node.Attributes != null)
            {
                foreach (XmlAttribute attr in node.Attributes)
                {
                    if (SensitiveAttributes.ContainsKey(attr.Name))
                    {
                        // crappy anonymizer
                        attr.Value = string.Join("",
                            Enumerable.Repeat(SensitiveAttributes[attr.Name],
                            attr.Value.Length / SensitiveAttributes[attr.Name].Length));
                    }
                }
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                AnonymizeNodes(child);
            }
        }
    }

}
