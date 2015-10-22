// Debug.cs
// Copyright 2015
// 
// Project Lead: Need
// Contact:      
//     Mail:     mailto:needdragon@gmail.com 
//     Twitter: https://twitter.com/NeedDragon

using System;

namespace Network_Library.Network {
    internal class Debug {
        public static void WriteLine(object o, String text) {
            if (o == null) return;
            System.Diagnostics.Debug.WriteLine(o.GetType().Name+"| "+text);
        } 
    }
}