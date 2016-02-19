using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace AV
{
    public class UIButtonNumberCalc 
    {

        public bool VOLUP, MASTER;
        public ushort ramp;
        public uint zoneNumber;
        public uint UINum(uint buttonNum)
        {
            uint UIresult;
            if (buttonNum <= 400)//zone select
            {
                UIresult = UIfromBtn(buttonNum);
                zoneNumber = zoneNum(buttonNum);
            }
            else if (buttonNum > 400 && buttonNum <= 420)//Master VOL UP
            {
                UIresult = buttonNum - 400;
                VOLUP = true;
                ramp = 65535;//VOL 100%
            }
            else if (buttonNum >= 421 && buttonNum <= 440)//Master VOL DOWN
            { 
                UIresult = buttonNum - 420;
                VOLUP = false;
                ramp = 0;//VOL 0%
            }
            else if (buttonNum > 440 && buttonNum <= 460)
            {
                MASTER = true;
                UIresult = buttonNum - 440;
            }
            else if (buttonNum > 500 && buttonNum <= 900) {//SINGLE VOL UP
                buttonNum -= 500;
                VOLUP = true;
                ramp = 65535;//VOL 100%
                UIresult = UIfromBtn(buttonNum);
                zoneNumber = zoneNum(buttonNum);
            }
            else if (buttonNum >= 901 && buttonNum <= 1300) {//SINGLE VOL DOWN
                VOLUP = false;
                ramp = 0;//VOL 0%
                buttonNum -= 900;
                UIresult = UIfromBtn(buttonNum);
                zoneNumber = zoneNum(buttonNum);
            }
            else if (buttonNum >= 1301 && buttonNum <= 1700)
            {
                MASTER = false;
                buttonNum -= 1300;
                UIresult = UIfromBtn(buttonNum);
                zoneNumber = zoneNum(buttonNum);
            }
            else if (buttonNum >= 1701 && buttonNum <= 2100)
            {
                buttonNum -= 1700;
                UIresult = UIfromBtn(buttonNum);
                zoneNumber = zoneNum(buttonNum);
            }
            else if (buttonNum >= 2301 && buttonNum <= 2700) {
                buttonNum -= 2300;
                UIresult = UIfromBtn(buttonNum);
                zoneNumber = zoneNum(buttonNum);
            }
            else UIresult = buttonNum;

            return UIresult;
        }
        private uint UIfromBtn(uint btn) {
            uint UI;
            if (btn % 20 > 0)
            {
                UI = btn / 20 + 1;
            }
            else
            {
                UI = btn / 20;
            }
            return UI;
        }
        private uint zoneNum(uint btn) {
            uint zone;
            if (btn % 20 > 0)
            {
                zone = btn % 20;
            }
            else
            {
                zone = 20;
            }
            return zone;
        }
        
    }
}