//TO DO:
//add fault feedback
//add snapshot for party modes
//change the way expanders are defined, research dictionary
//#define IncludeSampleCode

using System;
using System.Collections.Generic;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       				// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    		// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.EthernetCommunication;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharpPro.AudioDistribution;

namespace AV
{
    //public delegate EventHandler(object sender, EventArgs e);

    public delegate void volumeRoutingHandler();//??DELETe?


    public class ControlSystem : CrestronControlSystem
    {
        ushort[] zoneNumberArray = new ushort[75];//??? is this necessary?

        public event volumeRoutingHandler volumeRoutingEvent;//clean up?
        SimplSharpTimer pressHold = new SimplSharpTimer();
        string[] zoneNameArray = new string[75];
        string[] sourceNameArray = new string[75];
        string[] swampExpanderTypes = new string[10];
        ushort numberOfZones, numberOfExpanders;
        ushort maxNumberOfZones = 10;
        uint[] expanderIDs = new uint[8];
        uint[] expanderLastZone = new uint[8];
        uint[] expanderFirstZone = new uint[8];
        bool[] groupUngroupArray = new bool[50];
        bool[] onMusicMenuArray = new bool[50];
        string[] groupNames = new string[10];
        UItoZone[] currentZone = new UItoZone[20];
        public ThreeSeriesTcpIpEthernetIntersystemCommunications myEISC;
        public Swamp swamp;
        public Swamp24x8 swampA;
        SwampE8[] expanderE8 = new SwampE8[8];
        SwampE4[] expanderE4 = new SwampE4[8];
        Swe8[] expanderSWE8 = new Swe8[8];
        ZoneEventHandler[] zoneEventHandlerz = new ZoneEventHandler[8];
        Dictionary<SwampE8, int> expand = new Dictionary<SwampE8, int>();//DELETE?????
        partyModes partyModeSettings = new partyModes();
        public void triggerEventHandler()
        { //NOT FINISHED!!
            volumeRoutingEvent();
        }

        public ControlSystem()
            : base()
        {
            for (int i = 0; i < 20; i++)
            {
                currentZone[i] = new UItoZone();
            }
            //currentZone[0].volumeRoutingEvent += new UItoZone.volumeRoutingHandler(UItoZone.routeVolumeToUI);

            readConfig(@"\NVRAM\AVCONFIG.xml");

            if (this.SupportsEthernet)
            {
                myEISC = new ThreeSeriesTcpIpEthernetIntersystemCommunications(0x90, "127.0.0.2", this);
                myEISC.SigChange += new SigEventHandler(MySigChangeHandler);
                swampA = new Swamp24x8(0x30, this);
                //swampA.SourcesChangeEvent += new SourceEventHandler(swampSpdifEvent);
                swampA.BaseEvent += new BaseEventHandler(swampBaseEvent);
                swampA.ZoneChangeEvent += new ZoneEventHandler(SwampZoneEvent);
                swampA.OnlineStatusChange += new OnlineStatusChangeEventHandler(swampA_OnlineStatusChange);
                if (myEISC.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("myEISC failed registration. Cause: {0}", myEISC.RegistrationFailureReason);
                if (swampA.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    ErrorLog.Error("SWAMP failed registration {0}", swampA.RegistrationFailureReason);
            }
            for (ushort i = 0; i < 5; i++)
            {
                myEISC.StringInput[(ushort)(801 + i)].StringValue = groupNames[i];
            }
            for (ushort i = 1; i <= 24; i++)
            {
                myEISC.StringInput[(ushort)(810 + i)].StringValue = sourceNameArray[i];
            }

            myEISC.UShortInput[151].UShortValue = (ushort)UItoZone.numberOfGroups;
            /*expand.Add(expanderE8[0], 0);
            expand.Add(expanderE8[1], 1);
            expand.Add(expanderE8[2], 2);
            foreach (var expander in expand)
            {
             * expanderE8[i].ZoneChangeEvent += new ZoneEventHandler(zoneEvent);
                if (expander.Key == device)
                {
                    ErrorLog.Notice("expander.Key, args.z.n {0} {1}", expander.Key, args.Zone.Number);
                    
                }
            }*/

            if (numberOfExpanders > 0)
            {
                int numberOfSwampE8 = 0;
                int numberOfSwampE4 = 0;
                int numberOfSwe8 = 0;
                for (int i = 0; i < numberOfExpanders; i++)
                {

                    switch (swampExpanderTypes[i])
                    {
                        case "swampE8":
                            numberOfSwampE8++;
                            maxNumberOfZones += 8;
                            expanderE8[i] = new SwampE8(expanderIDs[i], swampA);
                            expanderE8[i].ZoneChangeEvent += new ZoneEventHandler(E8ZoneEvent);
                            //expand.Add(expanderE8[i], i);
                            //expanderE8[i].ZoneChangeEvent += new zoneEventHandlerz[i](zoneEvent);
                            break;
                        case "swampE4":
                            numberOfSwampE4++;
                            maxNumberOfZones += 4;
                            expanderE4[i] = new SwampE4(expanderIDs[i], swampA);
                            expanderE4[i].ZoneChangeEvent += new ZoneEventHandler(E4ZoneEvent);
                            break;
                        case "SWE8":
                            numberOfSwe8++;
                            maxNumberOfZones += 8;
                            expanderSWE8[i] = new Swe8(expanderIDs[i], swampA);
                            //expanderSWE8[i].ZoneChangeEvent += new ZoneEventHandler(SWE8ZoneEvent);
                            break;
                        default:
                            break;
                    }
                }
                //ErrorLog.Notice("E8, E4, SWE8 {0},{1},{2}", numberOfSwampE8, numberOfSwampE4, numberOfSwe8);
                //ErrorLog.Notice("max#zones {0}", maxNumberOfZones);
                //ErrorLog.Notice("expanders.count {0}", swampA.Expanders.Count);
                //ErrorLog.Notice("expanderNumber type {0} {1}", swampA.Expanders[1].Number, swampA.Expanders[1].ExpanderType);
            }
            // Set the number of threads which you want to use in your program - At this point the threads cannot be created but we should
            // define the max number of threads which we will use in the system.
            // the right number depends on your project; do not make this number unnecessarily large
            Thread.MaxNumberOfUserThreads = 20;


        }




        public override void InitializeSystem()
        {
            // This should always return   
        }
        /*private void volumeChanged(object sender, EventArgs e)
        {
            CrestronConsole.PrintLine("This is called when the event fires.");
            
        }*/
        void MySigChangeHandler(GenericBase currentDevice, SigEventArgs args)
        {
            if (args.Event == eSigEvent.UShortChange)
            {
                if (args.Sig.Number < 20) { floorSelect(args.Sig.Number, args.Sig.UShortValue); }
                if (args.Sig.Number <= 40 && args.Sig.Number > 20) { sendSource(args.Sig.Number, args.Sig.UShortValue); }
                if (args.Sig.Number == 100) { swampIO(2, args.Sig.UShortValue); }//in, out
            }

            if (args.Event == eSigEvent.BoolChange)
            {
                if (args.Sig.Number <= 400)
                {
                    if (args.Sig.BoolValue == true)
                    {
                        zoneSelect(args.Sig.Number);
                    }
                }
                else if (args.Sig.Number > 400 && args.Sig.Number <= 440)// Master volume up/dowon
                {
                    masterVolume(args.Sig.Number, args.Sig.BoolValue);
                }
                else if (args.Sig.Number > 440 && args.Sig.Number <= 460 && args.Sig.BoolValue == true) //master mute
                {
                    mute(args.Sig.Number);
                }
                else if (args.Sig.Number > 500 && args.Sig.Number <= 1300)
                { //individual vol up/down
                    singleVolUpDown(args.Sig.Number, args.Sig.BoolValue);
                }
                else if (args.Sig.Number > 1300 && args.Sig.Number <= 1700 && args.Sig.BoolValue == true)
                {//individual mute
                    mute(args.Sig.Number);
                }
                else if (args.Sig.Number > 1700 && args.Sig.Number <= 2100 && args.Sig.BoolValue == true)
                {//individual off

                    sendOff(args.Sig.Number);
                }
                else if (args.Sig.Number > 2100 && args.Sig.Number < 2200 && args.Sig.BoolValue == true) //select / deselect all
                {
                    selectDeselectAll(args.Sig.Number);
                }
                else if (args.Sig.Number > 2200 && args.Sig.Number <= 2220) //group ungroup
                {
                    groupUngroup(args.Sig.Number, args.Sig.BoolValue);
                }
                else if (args.Sig.Number > 2230 && args.Sig.Number < 2235)
                {
                    //pressHold.presetNum = (ushort)(args.Sig.Number - 2230);
                    //pressAndHold(args.Sig.BoolValue);
                    saveCurrentStatus(args.Sig.Number - 2230);
                }
                else if (args.Sig.Number > 2234 && args.Sig.Number < 2238)
                {
                    if (args.Sig.BoolValue == true)
                    {
                        recallPartyMode(args.Sig.Number - 2234);
                    }
                }
                else if (args.Sig.Number > 2250 && args.Sig.Number < 2300)
                {
                    onMusicMenu(args.Sig.Number, args.Sig.BoolValue);//THIS NEEDS UPDATING!!!!!!!
                }
                else if (args.Sig.Number > 2300 && args.Sig.Number < 2700)
                {
                    pressAndHold(args.Sig.Number, args.Sig.BoolValue);
                }
            }
        }
        void swampA_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            if (swampA.IsOnline)
            {
                for (ushort i = 1; i < 9; i++)
                {
                    swampA.Zones[i].Name.StringValue = zoneNameArray[i - 1];
                }
                for (ushort i = 1; i < 25; i++)
                {
                    swampA.Sources[i].Name.StringValue = sourceNameArray[i];
                }
                if (numberOfExpanders > 0)
                {
                    ushort zonePlaceHolder = 10;
                    for (ushort i = 1; i < numberOfExpanders; i++)
                    {
                        if (swampA.Expanders[i].OnlineFeedback)
                        {
                            for (ushort j = 1; j < swampA.Expanders[i].NumberOfZones; j++)
                            { swampA.Expanders[i].Zones[j].Name.StringValue = zoneNameArray[zonePlaceHolder + j - 1]; }
                        }
                        zonePlaceHolder += (ushort)swampA.Expanders[i].NumberOfZones;//NEEDS TESTING
                    }
                }
            }
        }
        void swampBaseEvent(GenericBase device, BaseEventArgs args)
        {
            uint callingZone = 0;
            switch (args.EventId)
            {
                case Swamp.SPDIFOut9SourceFeedbackEventId:
                    callingZone = 9;
                    break;
                case Swamp.SPDIFOut10SourceFeedbackEventId:
                    callingZone = 10;
                    break;
                default:
                    break;
            }
            myEISC.UShortInput[40 + callingZone].UShortValue = swampA.SpdifOuts[callingZone].SPDIFOutSourceFeedback.UShortValue;
            UItoZone.zoneCurrentSourceNumber[callingZone] = swampA.SpdifOuts[callingZone].SPDIFOutSourceFeedback.UShortValue;
            UItoZone.zoneCurrentSourceText[callingZone] = sourceNameArray[swampA.SpdifOuts[callingZone].SPDIFOutSourceFeedback.UShortValue];
            if (swampA.SpdifOuts[callingZone].SPDIFOutSourceFeedback.UShortValue == 0)
            {
                UItoZone.zoneCurrentOnOffStatus[callingZone] = false;
            }
            else UItoZone.zoneCurrentOnOffStatus[callingZone] = true;
            updateUI(callingZone, "sources");
            updateUI(callingZone, "onOffStatus");
        }
        void SwampZoneEvent(Object device, ZoneEventArgs args)
        {
            switch (args.EventId)
            {

                case ZoneEventIds.SourceFeedbackEventId:
                    myEISC.UShortInput[args.Zone.Number + 40].UShortValue = args.Zone.Source.UShortValue;//UPDATE XSIG
                    UItoZone.zoneCurrentSourceNumber[args.Zone.Number] = args.Zone.Source.UShortValue;//UPDATE ZONE STATUS
                    UItoZone.zoneCurrentSourceText[args.Zone.Number] = sourceNameArray[args.Zone.SourceFeedback.UShortValue];//UPDATE SOURCE TEXT

                    if (args.Zone.SourceFeedback.UShortValue == 0)
                    {
                        UItoZone.zoneCurrentOnOffStatus[args.Zone.Number] = false;//ZONE IS OFF
                    }
                    else
                    {
                        UItoZone.zoneCurrentOnOffStatus[args.Zone.Number] = true;//ZONE IS ON
                    }
                    updateUI(args.Zone.Number, "sources");
                    updateUI(args.Zone.Number, "onOffStatus");
                    break;
                case ZoneEventIds.VolumeFeedbackEventId:
                    myEISC.UShortInput[args.Zone.Number + 100].UShortValue = args.Zone.VolumeFeedback.UShortValue;
                    UItoZone.zoneCurrentVolumes[args.Zone.Number] = args.Zone.VolumeFeedback.UShortValue;
                    updateUI(args.Zone.Number, "volumes");
                    break;
                case ZoneEventIds.MuteOnFeedbackEventId:
                    UItoZone.zoneCurrentMuteStatus[args.Zone.Number] = args.Zone.MuteOnFeedback.BoolValue;
                    updateUI(args.Zone.Number, "muteStatus");
                    break;
                case ZoneEventIds.StartupVolumeFeedbackEventId:
                    UItoZone.zoneCurrentStartupVols[args.Zone.Number] = args.Zone.StartupVolumeFeedback.UShortValue;
                    decimal vol = args.Zone.StartupVolumeFeedback.UShortValue;
                    CrestronConsole.PrintLine("startupVolFB {0} {1:P} ", args.Zone.Number, vol / 65535);
                    break;
                case ZoneEventIds.OverCurrentFeedbackEventId:
                    myEISC.StringInput[810].StringValue = "Over Current Zone " + args.Zone.Number;
                    break;
                case ZoneEventIds.OverOrUnderVoltageFeedbackEventId:
                    myEISC.StringInput[810].StringValue = "Over / Under Voltage " + args.Zone.Number;
                    break;
                default:
                    break;
            }
        }
        void E8ZoneEvent(Object device, ZoneEventArgs args)
        {
            var ex = device as SwampE8;
            string deviceString = Convert.ToString(ex.ExpanderType);
            uint expNum = ex.Number;
            uint callingZone = (args.Zone.Number + (ushort)(expanderFirstZone[expNum - 1]) - 1);
            switch (args.EventId)
            {

                case ZoneEventIds.SourceFeedbackEventId:
                    myEISC.UShortInput[callingZone + 40].UShortValue = args.Zone.Source.UShortValue;
                    UItoZone.zoneCurrentSourceNumber[callingZone] = args.Zone.Source.UShortValue;//UPDATE ZONE STATUS
                    UItoZone.zoneCurrentSourceText[callingZone] = sourceNameArray[args.Zone.Source.UShortValue];
                    if (args.Zone.Source.UShortValue == 0)
                    {
                        UItoZone.zoneCurrentOnOffStatus[callingZone] = false;
                    }
                    else UItoZone.zoneCurrentOnOffStatus[callingZone] = true;
                    updateUI(callingZone, "sources");
                    updateUI(callingZone, "onOffStatus");
                    break;
                case ZoneEventIds.VolumeFeedbackEventId:
                    myEISC.UShortInput[callingZone + 100].UShortValue = args.Zone.VolumeFeedback.UShortValue;
                    UItoZone.zoneCurrentVolumes[callingZone] = args.Zone.VolumeFeedback.UShortValue;
                    updateUI(callingZone, "volumes");
                    break;
                case ZoneEventIds.MuteOnFeedbackEventId:
                    UItoZone.zoneCurrentMuteStatus[callingZone] = args.Zone.MuteOnFeedback.BoolValue;
                    updateUI(callingZone, "muteStatus");
                    break;
                case ZoneEventIds.StartupVolumeFeedbackEventId:
                    UItoZone.zoneCurrentStartupVols[callingZone] = args.Zone.StartupVolumeFeedback.UShortValue;
                    decimal vol = args.Zone.StartupVolumeFeedback.UShortValue;
                    CrestronConsole.PrintLine("startupVolFB {0} {1:P} ", callingZone, vol / 65535);
                    break;
                default:
                    break;
            }
        }
        void E4ZoneEvent(Object device, ZoneEventArgs args)//THIS NEEDS TESTING AND PROBABLY UPDATING
        {
            var ex = device as SwampE4;
            string deviceString = Convert.ToString(ex.ExpanderType);
            uint expNum = ex.Number;
            uint callingZone = (args.Zone.Number + (ushort)(expanderFirstZone[expNum - 1]) - 1);
            switch (args.EventId)
            {
                case ZoneEventIds.SourceFeedbackEventId:
                    myEISC.UShortInput[args.Zone.Number + 39 + (ushort)(expanderFirstZone[expNum - 1])].UShortValue = args.Zone.Source.UShortValue;
                    UItoZone.zoneCurrentSourceNumber[callingZone] = args.Zone.Source.UShortValue;//UPDATE ZONE STATUS
                    break;
                case ZoneEventIds.StartupVolumeFeedbackEventId:
                    UItoZone.zoneCurrentStartupVols[callingZone] = args.Zone.StartupVolumeFeedback.UShortValue;
                    break;
                default:
                    break;
            }
        }
        void swampIO(ushort input, ushort output)
        {
            if (input < 25)
            {
                swampA.Zones[output].Source.UShortValue = input;
            }
        }
        void updateUI(uint callingZone, string propertyToUpdate)
        {
            for (int i = 0; i < UItoZone.numberOfGroups; i++)
            {
                for (int j = 0; j < UItoZone.groupSizes[i]; j++)
                {
                    if (callingZone == UItoZone.zoneNumbersInGroups[i, j])
                    {
                        switch (propertyToUpdate)
                        {
                            case "muteStatus":
                                {
                                    UItoZone.groupMuteStatus[i, j] = UItoZone.zoneCurrentMuteStatus[UItoZone.zoneNumbersInGroups[i, j]];
                                    for (int k = 0; k < 20; k++)
                                    {
                                        myEISC.BooleanInput[(ushort)(1301 + k * 20 + j)].BoolValue = UItoZone.groupMuteStatus[i, j];//UPDATE XSIG
                                    }
                                }
                                break;
                            case "onOffStatus":
                                {
                                    UItoZone.groupOnOffStatus[i, j] = UItoZone.zoneCurrentOnOffStatus[UItoZone.zoneNumbersInGroups[i, j]];
                                    for (int k = 0; k < 20; k++)
                                    {
                                        myEISC.BooleanInput[(ushort)(1701 + k * 20 + j)].BoolValue = UItoZone.groupOnOffStatus[i, j];//UPDATE XSIG
                                    }
                                }
                                break;
                            case "sources":
                                {
                                    UItoZone.groupSources[i, j] = UItoZone.zoneCurrentSourceText[UItoZone.zoneNumbersInGroups[i, j]];
                                    UItoZone.groupOnOffStatus[i, j] = UItoZone.zoneCurrentOnOffStatus[UItoZone.zoneNumbersInGroups[i, j]];
                                    for (int k = 0; k < 20; k++)//UPDATE TO TEST IF UI IS CURRENTLY ON AUDIO MENU
                                    {
                                        myEISC.StringInput[(ushort)(401 + k * 20 + j)].StringValue = UItoZone.groupSources[i, j];
                                        myEISC.BooleanInput[(ushort)(1701 + k * 20 + j)].BoolValue = UItoZone.groupOnOffStatus[i, j];
                                    }
                                }
                                break;
                            case "volumes":
                                {
                                    if (callingZone == UItoZone.zoneNumbersInGroups[i, j])
                                    {
                                        UItoZone.groupVolumes[i, j] = UItoZone.zoneCurrentVolumes[UItoZone.zoneNumbersInGroups[i, j]];
                                        for (int k = 0; k < 20; k++)//UPDATE TO TEST IF UI IS CURRENTLY ON AUDIO MENU
                                        {
                                            myEISC.UShortInput[(ushort)(201 + (k * 20) + j)].UShortValue = UItoZone.groupVolumes[i, j];
                                        }
                                    }
                                }
                                break;
                            default:
                                break;

                        }
                    }
                }
            }
        }
        void zoneSelect(uint buttonNumber)
        {
            bool bit;
            UIButtonNumberCalc UICalc = new UIButtonNumberCalc();
            uint UINumber = UICalc.UINum(buttonNumber);
            int zoneNumber = Convert.ToInt32(UICalc.zoneNumber);//calculate zone number

            if (groupUngroupArray[UINumber])//multizone mode
            {
                currentZone[UINumber - 1].zoneSelectButtons ^= 1 << zoneNumber;//toggle bit 
                bit = (currentZone[UINumber - 1].zoneSelectButtons & (1 << zoneNumber)) != 0;//check bit
                myEISC.BooleanInput[Convert.ToUInt16((UINumber - 1) * 20) + (uint)zoneNumber].BoolValue = bit;//update button fb
            }
            else //singlezone mode
            {
                currentZone[UINumber - 1].zoneSelectButtons = (2 << (zoneNumber - 1));//set bit
                for (int i = 0; i < currentZone[UINumber - 1].currentGroupSize; i++)
                {
                    bit = (currentZone[UINumber - 1].zoneSelectButtons & (1 << (i + 1))) != 0;//check bit
                    myEISC.BooleanInput[Convert.ToUInt16((UINumber - 1) * 20 + (i + 1))].BoolValue = bit;
                }
            }
        }
        void masterVolume(uint buttonNum, bool pressRelease)
        {
            double VolumeRampSeconds = 700;//X10ms
            double rampTime;
            ushort exp = 1;
            UIButtonNumberCalc UICalc = new UIButtonNumberCalc();
            uint UINumber = UICalc.UINum(buttonNum);
            bool volUp = UICalc.VOLUP;
            ushort rampValue = UICalc.ramp;
            for (int i = 0; i < (currentZone[UINumber - 1].currentGroupSize); i++)//test all zones in group
            {
                if ((currentZone[UINumber - 1].zoneSelectButtons & (1 << (i + 1))) != 0)//if zone is selected
                {
                    ushort output = UItoZone.zoneNumbersInGroups[currentZone[UINumber - 1].currentGroupNumber - 1, i];//select zone number to change vol
                    if (volUp)
                    {
                        rampTime = (65535 - (double)UItoZone.zoneCurrentVolumes[output]) * VolumeRampSeconds / 65535;//set relative ramping time
                    }
                    else
                    {
                        rampTime = ((double)UItoZone.zoneCurrentVolumes[output]) * VolumeRampSeconds / 65535;
                    }
                    if (output > 0 && output < 9)//SWAMP24X8
                    {
                        if (pressRelease)
                        {
                            if (swampA.Zones[output].SourceFeedback.UShortValue != 0)
                            {
                                swampA.Zones[output].MuteOff();
                                swampA.Zones[output].Volume.UShortValue = UItoZone.zoneCurrentVolumes[output];
                                swampA.Zones[output].Volume.CreateRamp(rampValue, (uint)rampTime);//SEND RAMP COMMAND
                            }
                        }
                        else
                        {
                            swampA.Zones[output].Volume.StopRamp();
                        }
                    }
                    else if (output >= 11)
                    {
                        ushort expOutput = (ushort)(output + 1);//calculate expander zone
                        for (int j = 0; j < numberOfExpanders; j++)
                        {
                            if (output <= expanderLastZone[j])
                            {
                                exp = (ushort)(j + 1);//expander number
                                expOutput -= (ushort)expanderFirstZone[j];
                                break;
                            }
                        }
                        if (pressRelease)
                        {
                            if (swampA.Expanders[exp].Zones[expOutput].SourceFeedback.UShortValue != 0)
                            {
                                swampA.Expanders[exp].Zones[expOutput].MuteOff();
                                swampA.Expanders[exp].Zones[expOutput].Volume.UShortValue = UItoZone.zoneCurrentVolumes[output];
                                swampA.Expanders[exp].Zones[expOutput].Volume.CreateRamp(rampValue, (uint)rampTime);
                            }
                        }
                        else
                        {
                            swampA.Expanders[exp].Zones[expOutput].Volume.StopRamp();
                        }
                    }
                }
            }
        }
        void singleVolUpDown(uint buttonNum, bool pressRelease)
        {
            double VolumeRampSeconds = 700;//X10ms
            double rampTime;
            ushort exp = 1;
            UIButtonNumberCalc UICalc = new UIButtonNumberCalc();
            uint UINumber = UICalc.UINum(buttonNum);
            bool volUp = UICalc.VOLUP;//up or down
            ushort rampValue = UICalc.ramp;//0 or 65535
            uint zoneNumber = UICalc.zoneNumber;//calculate zone number

            ushort output = UItoZone.zoneNumbersInGroups[currentZone[UINumber - 1].currentGroupNumber - 1, zoneNumber - 1];
            if (volUp)
            {
                rampTime = (65535 - (double)UItoZone.zoneCurrentVolumes[output]) * VolumeRampSeconds / 65535;//set relative ramping time
            }
            else
            {
                rampTime = ((double)UItoZone.zoneCurrentVolumes[output]) * VolumeRampSeconds / 65535;
            }
            if (output > 0 && output < 9)//SWAMP24X8
            {
                if (pressRelease)
                {
                    if (swampA.Zones[output].SourceFeedback.UShortValue != 0)//if not off
                    {
                        swampA.Zones[output].MuteOff();
                        swampA.Zones[output].Volume.UShortValue = UItoZone.zoneCurrentVolumes[output];
                        swampA.Zones[output].Volume.CreateRamp(rampValue, (uint)rampTime);//SEND RAMP COMMAND
                    }
                }
                else
                {
                    swampA.Zones[output].Volume.StopRamp();
                }
            }
            else if (output >= 11)//send to expander
            {
                ushort expOutput = calculateExpanderOutput(output);
                if (pressRelease)
                {
                    if (swampA.Expanders[exp].Zones[expOutput].SourceFeedback.UShortValue != 0)
                    {
                        swampA.Expanders[exp].Zones[expOutput].MuteOff();
                        swampA.Expanders[exp].Zones[expOutput].Volume.UShortValue = UItoZone.zoneCurrentVolumes[output];
                        swampA.Expanders[exp].Zones[expOutput].Volume.CreateRamp(rampValue, (uint)rampTime);
                    }
                }
                else
                {
                    swampA.Expanders[exp].Zones[expOutput].Volume.StopRamp();
                }
            }
        }
        void groupUngroup(uint buttonNum, bool boolValue)
        {
            buttonNum -= 2200;
            currentZone[buttonNum - 1].zoneSelectButtons = 0;//clear zone feedback 
            groupUngroupArray[buttonNum] = boolValue;
            for (uint i = 0; i < 20; i++)
            {
                myEISC.BooleanInput[(buttonNum - 1) * 20 + i + 1].BoolValue = false;
            }
        }
        void onMusicMenu(uint buttonNum, bool boolValue)
        {
            buttonNum -= 2250;
            onMusicMenuArray[buttonNum - 1] = boolValue;
        }
        void selectDeselectAll(uint buttonNum)
        {//UPDATE FB TO XSIG
            buttonNum -= 2100;
            uint UINumber;
            if (buttonNum < 51) //select all
            {
                UINumber = buttonNum;
                currentZone[UINumber - 1].zoneSelectButtons = 2097150;//set all bits
                for (uint i = 0; i < 20; i++)
                {
                    myEISC.BooleanInput[(UINumber - 1) * 20 + i + 1].BoolValue = true;
                }
            }
            else
            { //deselect all
                UINumber = buttonNum - 50;
                currentZone[UINumber - 1].zoneSelectButtons = 0;
                for (uint i = 0; i < 20; i++)
                {
                    myEISC.BooleanInput[(UINumber - 1) * 20 + i + 1].BoolValue = false;
                }
            }
        }
        void mute(uint buttonNum)
        {
            UIButtonNumberCalc UICalc = new UIButtonNumberCalc();
            uint UINumber = UICalc.UINum(buttonNum);
            uint zoneNumber = UICalc.zoneNumber;
            bool master = UICalc.MASTER;
            if (master)
            {
                for (int i = 0; i < (currentZone[UINumber - 1].currentGroupSize); i++)//test all zones in group
                {
                    if ((currentZone[UINumber - 1].zoneSelectButtons & (1 << (i + 1))) != 0)//if zone is selected
                    {
                        ushort output = UItoZone.zoneNumbersInGroups[currentZone[UINumber - 1].currentGroupNumber - 1, i];//select zone number to send source to
                        muter(output);
                    }
                }
            }
            else
            {
                ushort output = UItoZone.zoneNumbersInGroups[currentZone[UINumber - 1].currentGroupNumber - 1, zoneNumber - 1];//select zone number to send source to
                muter(output);
            }
            for (ushort i = 1; i < 9; i++)
            {
                swampA.Zones[i].Name.StringValue = zoneNameArray[i - 1];
                //swampA.Expanders[i].Zones[i].Name.StringValue = zoneNameArray[i - 1];
            }
        }
        void muter(ushort output)
        {
            ushort exp;
            if (output > 0)
            {
                if (output < 9)//SEND TO MAIN
                {
                    if (swampA.Zones[output].MuteOnFeedback.BoolValue)
                    {
                        swampA.Zones[output].MuteOff();
                    }
                    else swampA.Zones[output].MuteOn();
                }
                else if (output >= 11)//Send to expander
                {
                    for (exp = 0; exp < numberOfExpanders; exp++)
                    {
                        if (output <= expanderLastZone[exp])
                        {
                            output -= (ushort)(expanderFirstZone[exp]);
                            output++;
                            break;
                        }
                    }
                    if (swampA.Expanders[(ushort)(exp + 1)].Zones[output].MuteOnFeedback.BoolValue)
                    {
                        swampA.Expanders[(ushort)(exp + 1)].Zones[output].MuteOff();
                    }
                    else
                        swampA.Expanders[(ushort)(exp + 1)].Zones[output].MuteOn();
                }
            }
        }
        void floorSelect(uint UINumber, uint floorNumber)
        {
            uint size = UItoZone.groupSizes[floorNumber - 1];
            myEISC.UShortInput[UINumber].UShortValue = (ushort)size;//CURRENT PAGE SIZE TO XSIG
            currentZone[UINumber - 1].currentGroupNumber = Convert.ToInt32(floorNumber);//update class group number
            currentZone[UINumber - 1].zoneSelectButtons = 0; //clear zone buttons when new floor selected
            for (uint i = 0; i < size; i++) //send room names/volumes/sources to xsig
            {
                myEISC.StringInput[(UINumber - 1) * 20 + i + 1].StringValue = zoneNameArray[UItoZone.zoneNumbersInGroups[floorNumber - 1, i] - 1];
                myEISC.StringInput[(UINumber - 1) * 20 + i + 401].StringValue = UItoZone.groupSources[currentZone[UINumber - 1].currentGroupNumber - 1, i];
                myEISC.BooleanInput[(UINumber - 1) * 20 + i + 1].BoolValue = false;
                myEISC.BooleanInput[(UINumber - 1) * 20 + i + 1701].BoolValue = UItoZone.groupOnOffStatus[currentZone[UINumber - 1].currentGroupNumber - 1, i];
                myEISC.UShortInput[(UINumber - 1) * 20 + i + 201].UShortValue = UItoZone.groupVolumes[currentZone[UINumber - 1].currentGroupNumber - 1, i];
            }
        }
        void sendSource(uint UINumber, ushort sourceNumber)
        {
            UINumber -= 20;//starts at analog in 20
            for (int i = 0; i < (currentZone[UINumber - 1].currentGroupSize); i++)//test all zones in group
            {
                if ((currentZone[UINumber - 1].zoneSelectButtons & (1 << (i + 1))) != 0)//if zone is selected
                {
                    ushort output = UItoZone.zoneNumbersInGroups[currentZone[UINumber - 1].currentGroupNumber - 1, i];//select zone number to send source to

                    sourcer(output, sourceNumber);

                }
            }
        }
        void sendOff(uint buttonNum)
        {
            UIButtonNumberCalc UICalc = new UIButtonNumberCalc();
            uint UINumber = UICalc.UINum(buttonNum);
            uint zoneNumber = UICalc.zoneNumber;
            ushort output = UItoZone.zoneNumbersInGroups[currentZone[UINumber - 1].currentGroupNumber - 1, zoneNumber - 1];
            sourcer(output, 0);
        }
        void sourcer(ushort output, ushort sourceNumber)
        {
            ushort exp;
            if (output > 0)
            {
                if (output < 9)//SEND TO MAIN
                {
                    swampA.Zones[output].Source.UShortValue = sourceNumber;
                }
                else if (output < 11)
                {//SEND TO SPDIF
                    swampA.SpdifOuts[output].SPDIFOutSource.UShortValue = sourceNumber;
                }

                else//Send to expander
                {
                    for (exp = 0; exp < numberOfExpanders; exp++)//calculate expander
                    {
                        if (output <= expanderLastZone[exp])
                        {
                            output -= (ushort)(expanderFirstZone[exp]);
                            output++;
                            break;
                        }
                    }
                    swampA.Expanders[(ushort)(exp + 1)].Zones[output].Source.UShortValue = sourceNumber;
                }
            }
        }
        void readConfig(string path)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            uint lastZoneTemp = 10;
            uint firstZoneTemp;
            XmlTextReader reader = new XmlTextReader(path);
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                reader.ReadToFollowing("expander");//Configure Expanders
                                do
                                {
                                    swampExpanderTypes[i] = reader.GetAttribute("Type");
                                    expanderIDs[i] = Convert.ToUInt16(reader.GetAttribute("ID"));

                                    if (swampExpanderTypes[i] == "swampE4")
                                    {
                                        lastZoneTemp += 4;
                                        firstZoneTemp = lastZoneTemp - 3;
                                    }
                                    else
                                    {
                                        lastZoneTemp += 8;
                                        firstZoneTemp = lastZoneTemp - 7;
                                    }
                                    expanderLastZone[i] = lastZoneTemp;
                                    expanderFirstZone[i] = firstZoneTemp;
                                    i++;
                                } while (reader.ReadToNextSibling("expander"));
                                numberOfExpanders = (ushort)i;
                                i = 0;

                                reader.ReadToFollowing("audioZone"); //Audio Zone Names & Numbers
                                do
                                {
                                    zoneNameArray[i] = reader.GetAttribute("Name");
                                    zoneNumberArray[i] = Convert.ToUInt16(reader.GetAttribute("zoneNumber"));
                                    i++;
                                    if (zoneNameArray[i] != " ")
                                    {
                                        k++;
                                    }
                                } while (reader.ReadToNextSibling("audioZone"));
                                numberOfZones = (ushort)k;

                                i = 1;
                                reader.ReadToFollowing("audioSource"); //Audio Source Names & Numbers
                                sourceNameArray[0] = "Off";
                                do
                                {
                                    sourceNameArray[i] = reader.GetAttribute("Name");
                                    i++;
                                } while (reader.ReadToNextSibling("audioSource"));

                                i = 0;
                                reader.ReadToFollowing("group");//group numbers
                                do
                                {
                                    groupNames[i] = reader.GetAttribute("Name");
                                    reader.ReadToFollowing("zone");
                                    do
                                    {
                                        UItoZone.zoneNumbersInGroups[i, j] = Convert.ToUInt16(reader.GetAttribute("zoneNumber"));//add zone numbers per floor/group
                                        j++;
                                    } while (reader.ReadToNextSibling("zone"));
                                    UItoZone.groupSizes[i] = (ushort)j;//Number of zones in groups
                                    j = 0;
                                    i++;
                                } while (reader.ReadToNextSibling("group"));
                                UItoZone.numberOfGroups = i;
                                break;
                        } break;

                    }

                }
            }
        }
        void saveCurrentStatus(uint presetNumber)
        {
            for (int i = 1; i <= numberOfZones; i++)
            {
                partyModeSettings.partyVolumes[presetNumber, i] = UItoZone.zoneCurrentVolumes[i];
                decimal vol = Convert.ToDecimal(UItoZone.zoneCurrentVolumes[i]);
                CrestronConsole.PrintLine("partySave{0:P} {1}", vol / 65535, UItoZone.zoneCurrentVolumes[i]);
                partyModeSettings.partySources[presetNumber, i] = UItoZone.zoneCurrentSourceNumber[i];
            }

        }
        void recallPartyMode(uint presetNumber)
        {
            ushort[] tempVol = new ushort[100];
            ushort exp;
            for (ushort i = 1; i <= numberOfZones; i++)
            {
                tempVol[i] = UItoZone.zoneCurrentStartupVols[i];//temporarily store the original startup volume
                if (i < 9)
                {
                    swampA.Zones[i].StartupVolume.UShortValue = partyModeSettings.partyVolumes[presetNumber, i];//SET STARTUP VOL
                }
                else if (i <= 10)
                {
                    //don't do anything
                }
                else
                {//EXPANDER STARTUP VOL
                    ushort output = i;
                    CrestronConsole.PrintLine("partyoutput{0}", output);
                    for (exp = 0; exp < numberOfExpanders; exp++)//calculate expander   
                    {
                        if (output <= expanderLastZone[exp])
                        {
                            output -= (ushort)(expanderFirstZone[exp]);
                            output++;

                            break;
                        }
                    }
                    swampA.Expanders[(ushort)(exp + 1)].Zones[output].StartupVolume.UShortValue = partyModeSettings.partyVolumes[presetNumber, i];
                    decimal vol = partyModeSettings.partyVolumes[presetNumber, i];
                    CrestronConsole.PrintLine("partyvol {0:P} exp{1} output{2}", vol / 65535, exp, output);
                }

            }
            Thread.Sleep(500);
            for (ushort i = 1; i <= numberOfZones; i++)
            { sourcer(i, partyModeSettings.partySources[presetNumber, i]); }//send source
            Thread.Sleep(500);
            for (ushort i = 1; i < numberOfZones; i++)
            {
                if (i < 9)
                {
                    swampA.Zones[i].StartupVolume.UShortValue = tempVol[i];//restore startup volume
                }
                else if (i > 10)
                {
                    ushort output = i;
                    for (exp = 0; exp < numberOfExpanders; exp++)//calculate expander   
                    {
                        if (output <= expanderLastZone[exp])
                        {
                            output -= (ushort)(expanderFirstZone[exp]);
                            output++;
                            break;
                        }
                    }
                    swampA.Expanders[(ushort)(exp + 1)].Zones[output].StartupVolume.UShortValue = tempVol[i];

                }
            }
        }
        void saveStartupVol(ushort zoneNumber)
        {
            ushort exp = 1;
            if (zoneNumber < 9) { swampA.Zones[zoneNumber].StartupVolume.UShortValue = swampA.Zones[zoneNumber].VolumeFeedback.UShortValue; }
            else if (zoneNumber >= 11)//send to expander
            {
                ushort expOutput = calculateExpanderOutput(zoneNumber);
                swampA.Expanders[exp].Zones[expOutput].StartupVolume.UShortValue = swampA.Expanders[exp].Zones[expOutput].VolumeFeedback.UShortValue;
            }
        }
        void volumeFromExtAmp()
        {
            //!!!CONTINUE!!!
        }
        void pressAndHold(uint buttonNumber, bool status)
        {
            UIButtonNumberCalc UICalc = new UIButtonNumberCalc();
            uint UINumber = UICalc.UINum(buttonNumber);
            int zoneNumber = Convert.ToInt32(UICalc.zoneNumber);//calculate UIZone number
            ushort output = UItoZone.zoneNumbersInGroups[currentZone[UINumber - 1].currentGroupNumber - 1, zoneNumber - 1];//calculate SwampZone number

            pressHold.presetNum = output;
            if (status)
            {
                pressHold.InitializeTimer(2000);
                pressHold.TimerHasFinished += new EventHandler(pressHold_TimerHasFinished);

            }
            else
            {
                pressHold.StopTimer();
            }
        }

        void pressHold_TimerHasFinished(object sender, EventArgs e)
        {
            //saveCurrentStatus(pressHold.presetNum);
            saveStartupVol(pressHold.presetNum);
            myEISC.StringInput[800].StringValue = "zone " + pressHold.presetNum + " startup volume changed";
            throw new NotImplementedException();
        }
        ushort calculateExpanderOutput(ushort zoneNumber)
        {
            ushort exp;
            ushort expOutput = (ushort)(zoneNumber + 1);//calculate expander zone
            for (int j = 0; j < numberOfExpanders; j++)
            {
                if (zoneNumber <= expanderLastZone[j])
                {
                    exp = (ushort)(j + 1);//expander number
                    expOutput -= (ushort)expanderFirstZone[j];
                    break;
                }
            }
            return expOutput;
        }
    }
}
