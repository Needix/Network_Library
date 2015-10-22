// ACommand.cs
// Copyright 2015
// 
// Project Lead: Need
// Contact:      
//     Mail:     mailto:needdragon@gmail.com 
//     Twitter: https://twitter.com/NeedDragon

using System;

namespace Network_Library.Network.Messaging {
    public abstract class ACommand {
        public String Name { get; set; } //TODO: Think of a good way to implement command classes

        protected ACommand() { }
        protected ACommand(String name) {
            Name = name;
        }

        protected abstract void Execute(ASocket socket, object[] parameter);
    }
}