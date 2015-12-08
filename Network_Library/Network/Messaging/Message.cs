// Message.cs
// Copyright 2015
// 
// Project Lead: Need
// Contact:      
//     Mail:     mailto:needdragon@gmail.com 
//     Twitter: https://twitter.com/NeedDragon

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Network_Library.Network.Messaging {
    [XmlRoot(Namespace = DEFAULT_NAMESPACE, ElementName = ELEMENT_NAME)]
    public class Message {
        public const string DEFAULT_NAMESPACE = "Network_Library.Network.Messaging";
        public const string ELEMENT_NAME = "Message";

        //public ACommand Command { get; set; }
        public String Command { get; set; }

        [XmlArray("Parameters")]
        [XmlArrayItem("Parameter", typeof(Object))]
        public object[] Parameters { get; set; }

        private readonly XmlSerializer _serializer;
        public XmlSerializer Serializer { get { return _serializer; } }

        public Message() { }
        public Message(String command, params object[] parameters) {
            Command = command;
            Parameters = parameters;

            _serializer = new XmlSerializer(typeof(Message), GetParameterClassList(), GetParameterTypes(), new XmlRootAttribute(ELEMENT_NAME), DEFAULT_NAMESPACE);
        }

        internal void Send(Stream remote, StreamWriter writer) {
            foreach (object o in Parameters) {
                writer.WriteLine(o.GetType().AssemblyQualifiedName);
            }
            writer.WriteLine("END_TYPES");
            writer.Flush();

            Serializer.Serialize(remote, this);

            writer.Write('\n');
            writer.WriteLine("END_OBJECT");
            writer.Flush();
        }

        internal Type[] GetParameterTypes() {
            List<Type> typeList = new List<Type>();
            foreach (object o in Parameters) {
                Type t = o.GetType();
                if(!typeList.Contains(t)) typeList.Add(t);
            }
            return typeList.ToArray();
        }
        internal XmlAttributeOverrides GetParameterClassList() {
            XmlAttributeOverrides aor = new XmlAttributeOverrides();
            XmlAttributes listAttribs = new XmlAttributes();
            for (int i = 0; i < Parameters.Length; i++) {
                object curParameter = Parameters[i];
                listAttribs.XmlElements.Add(new XmlElementAttribute(curParameter.GetType()));
            }
            aor.Add(typeof(Message), "ListOfParameter", listAttribs);
            return aor;
        }

        public override string ToString() { return string.Format("Command: {0}, Parameters: {1}", Command, ArrayToString(Parameters, "  ")); }

        public static string ArrayToString(object[] para, char sep) { return ArrayToString(para, sep + ""); }
        public static string ArrayToString(object[] para, string sep) {
            if(para == null) return "";
            String result = "";
            for(int i = 0; i < para.Length; i++) {
                result += para[i];
                if(i + 1 < para.Length) result += sep;
            }
            return result;
        }
    }
}