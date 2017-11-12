using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// this file is used to both send to and receive commands in the babbler
namespace Raydon.CommonData.AdjustmentCommands
{
    // for all enums, users sending commands use enum.ToString to avoid copy/paste/typing errors
    public enum CommandTypes
    {
        Stop,
        Change,
        Reset
    }

    public enum entityTypes
    {
        None,
        Venue,
        TrainingSystem
    };

    public enum entitySubTypes
    {
        None,
        CPUTemp,
        CPUFan,
        GPUTemp,
        GPUFan,
        MainboardTemp,
        MainboardFan,

        InternalTemp,
        InternalHumidity,

        InputPowerFrequency,
        InputPowerVoltage
    };

    // the "header", Example:

    // instanceID "ALL"  -- command for all instances
    // or
    // instanceID "'GUID'"  - command for a specific instance
    public class ControlCommand
    {
        public string queuedID;    // the ID of the queue element we received, used for de-duping messages
        public DateTime arrivalTime;
        public string instanceID;  // a particular instance ID or "ALL" for all
        public double probability;  // if instanceID was "ALL", then operate on the command with this probability, where 0 is none and 1 is always
        public List<ControlMessage> messages;

        public void RemoveCompletedCommands()
        {
            if( messages == null )
            {
                return;
            }

            messages = messages.Where(p => p.goalReached != true).ToList();
        }
    }

    // the command itself.  Example:
    
    // commandString "Stop"  // stops the venue.
    // or 
    // commandString "IncreaseTo"   // increase the internal temp at 10 degrees per minute
    // commandEntityString "Venue"
    // commandSubEntityString "InternalTemp"
    // changeRatePerMinute = 10.0
    // targetValue = 40.0
    // or
    // commandString "Reset"        // reset the Venue Internal temp to normal
    // commandEntityString "Venue"
    // commandSubEntityString "InternalTemp"
    // changeRatePerMinute = 10.0
    // targetValue = 22.0

    public class ControlMessage
    {
        public string commandString; // matches enum commandEnum.ToString()
        public string commandEntityString;  // the name of the thing we're going to operate on, like a VENUE
        public string commandSubEntityString; // the name of the subentity to act on
        public double changeRatePerMinute;  // change per minute
        public double targetValue;

        public int applyToSystemIndex;  // the system index to which the command has been applied, in
                                        // case of application at a "maint" level (as opposed to venue)
        public bool goalReached;        // the goal has been reached, so we're ready to remove the command
    }
}
