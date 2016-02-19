using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace AV
{
    public class partyModes
    {
        public ushort[,] partyVolumes = new ushort[10, 100];
        public ushort[,] partySources = new ushort[10, 100];

    }
}