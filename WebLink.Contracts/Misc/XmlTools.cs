using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace WebLink.Contracts
{
    public class XmlTools
    {

        public static System.Xml.Linq.XDocument ToXml(object o)
        {
            System.Xml.Linq.XDocument xml = System.Xml.Linq.XDocument.Parse(XmlTools.ToXmlString(o));

            return xml;
        }

        public static String ToXmlString(object o)
        {
            return XmlTools._ToXmlString(o);
        }

        public static String ToXmlString(object o, string rootTag)
        {
            return XmlTools._ToXmlString(o, rootTag);
        }

        public static T GetObjectFromXml<T>(String xml)
        {
            T returnedXmlClass = default(T);

            try
            {
                using (System.IO.TextReader reader = new System.IO.StringReader(xml))
                {
                    try
                    {
                        returnedXmlClass = (T)new XmlSerializer(typeof(T)).Deserialize(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex.ToString());
                        // String passed is not XML, simply return defaultXmlClass
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: how to log errors
            }

            return returnedXmlClass;
        }

        public static T GetObjectFromXml<T>(XDocument xml)
        {
            T returnedXmlClass = default(T);

            try
            {
                using (XmlReader reader = xml.CreateReader())
                {
                    try
                    {
                        returnedXmlClass = (T)new XmlSerializer(typeof(T)).Deserialize(reader);
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex.ToString());
                        // String passed is not XML, simply return defaultXmlClass
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: how to log errors
            }

            return returnedXmlClass;
        }

        private static String _ToXmlString(object o, string rootTag = null)
        {


            var sb = new StringBuilder();
            try
            {
                // Begin XML serialization
                System.Xml.Serialization.XmlSerializer xs;

                //System.IO.StringWriter stringwriter = new System.IO.StringWriter();
                if (string.IsNullOrEmpty(rootTag))
                {
                    xs = new System.Xml.Serialization.XmlSerializer(o.GetType());
                }
                else
                {
                    XmlRootAttribute root = new XmlRootAttribute(rootTag);
                    xs = new XmlSerializer(o.GetType(), root);
                }


                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");

                System.Xml.XmlWriterSettings ws = new System.Xml.XmlWriterSettings();
                ws.NewLineHandling = System.Xml.NewLineHandling.Entitize;
                //ws.Encoding = Encoding.UTF8;
                // https://stackoverflow.com/questions/2223882/whats-the-difference-between-utf-8-and-utf-8-without-bom
                // is better avoid using BOM
                ws.Encoding = new UnicodeEncoding(false, false); // no BOM in a .NET string
                ws.OmitXmlDeclaration = true;

                using (var writter = System.Xml.XmlWriter.Create(sb, ws))
                {

                    xs.Serialize(writter, o, ns);
                }

                // End XML Serialization
            }catch(Exception _ex)
            {
                Console.WriteLine(_ex);
            }

            return sb.ToString();

        }

        public static string ExtractFromTag (string xml, string tagName){
            System.Xml.XmlDocument xmlDoc = new XmlDocument();
            
            xmlDoc.LoadXml(xml);

            string tagContent = string.Empty;


            XmlNodeList elemList = xmlDoc.GetElementsByTagName(tagName);
            for (int i = 0; i < elemList.Count; i++)
            {
                tagContent = elemList[i].Value;
            }

            return tagContent;
        }



    }
}
