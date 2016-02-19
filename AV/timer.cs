using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace AV
{
    public class SimplSharpTimer
    {
        //The timer
        CTimer Timer;
        public ushort presetNum;
        //Event for signaling when the timer has finished
        public event EventHandler TimerHasFinished;

        //Default constructor
        public SimplSharpTimer()
        {
        }

        public void InitializeTimer(int lengthOfTime)
        {
            //Initialize timer with callback function and interval
            Timer = new CTimer(TimeHasElapsed, lengthOfTime);
        }

        public void ResetTimer(int interval, int repeatInterval)
        {
            //Set the timer to run for the interval amount of time
            //and to then repeat at the repeat interval
            Timer.Reset(interval, repeatInterval);
        }
        
        public void StopTimer()
        {
            //Stop the timer
            Timer.Stop();
            CrestronConsole.Print("The timer has been stopped.\r\n");
        }
        
       
        //Callback method that is called when the timers interval has finished
        public void TimeHasElapsed(Object obj)
        {
            //Send an event to the calling program to inform it that the timer's interval has finished
            TimerHasFinished(this, new EventArgs());
        }

        public void WriteMessage()
        {
            //Write a message to the console when the timer's interval has finished
            CrestronConsole.Print("The timer is finished.\r\n");
            
        }
    }
}