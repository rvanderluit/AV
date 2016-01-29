using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro; 
using Crestron.SimplSharpPro.AudioDistribution;
//public delegate void volumeRoutingHandler();
        
namespace AV
{
    public class UItoZone
    {
        //VARIABLES
        
        public int currentGroupNumber = 1;
        //public ushort[] currentGroupVolumes = new ushort[20];
        public int zoneSelectButtons = 0;//bits to toggle zones
        public static int numberOfGroups;
        public static ushort[,] zoneNumbersInGroups = new ushort[10, 20];//GROUP# , ZONE NUMBERS
        public static ushort[,] groupVolumes = new ushort[10, 20];
        public static string[,] groupSources = new string[10, 20];
        public static bool[,] groupMuteStatus = new bool[10, 20];
        public static bool[,] groupOnOffStatus = new bool[10, 20];
        public static ushort[] groupSizes = new ushort[10];//array starts from 0; group1 = array[0]
        //public static ushort[] currentZoneVolumes = new ushort[100];//volume levels of all zones 1-100 sequentially
        public int currentGroupSize {
            get { return groupSizes[currentGroupNumber-1]; }  
        }
        //public static ushort[] routeVolumeToUI = new ushort[20];
        //public delegate void volumeRoutingHandler(object sender, EventArgs e);
        
        /*public void OnChanged()
        {
            volumeRoutingHandler handler = volumeRoutingEvent;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }*/


        private static ushort[] _zoneCurrentVolumes = new ushort[100];
        private static string[] _zoneCurrentSources = new string[100];
        private static bool[] _zoneCurrentMuteStatus = new bool[100];
        private static bool[] _zoneCurrentOnOffStatus = new bool[100];
        public static ushort[] zoneCurrentVolumes
        {
            get { return _zoneCurrentVolumes; }
            set 
            {
                _zoneCurrentVolumes = value;
                volumeChanged();//THIS ISN'T USED OR WORKING

            }
        }
        public static bool[] zoneCurrentMuteStatus
        { 
            get {return _zoneCurrentMuteStatus; }
            set { _zoneCurrentMuteStatus = value; }
        }
        public static bool[] zoneCurrentOnOffStatus
        {
            get { return _zoneCurrentOnOffStatus; }
            set { _zoneCurrentOnOffStatus = value; }
        }
        public static string[] zoneCurrentSources {
            get { return _zoneCurrentSources; }
            set {
                _zoneCurrentSources = value;
            }
        }
        
        public static void volumeChanged()//THIS ISN'T USED OR WORKING
        {
            for (int i = 0; i < numberOfGroups; i++)
            {
                for (int j = 0; j < groupSizes[i]; j++)
                {
                    groupVolumes[i, j] = zoneCurrentVolumes[zoneNumbersInGroups[i, j]];
                    CrestronConsole.PrintLine("vol {0}", groupVolumes[i, j]);
                }
            }
        }
        
        
        //public event System.EventHandler durpChanged;
        public void handleVolumeRoutingEvent() {
            currentGroupNumber = 1;
        
        }



    }
 }

