using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Services.Zebra.Commands
{
    public class AlertConfigurationCommand : BaseCommand
    {
        //baseAddress example: "http://81.25.127.175/Weblink/Alert"

        public AlertConfigurationCommand(string baseUrl)
        {
            IsOneWay = false;
            SetMessage(String.Format(@"! U setvar ""alerts.add"" ""PAPER OUT, HTTP - POST, Y, Y, {0}/PaperOut,0,N,""
setvar ""alerts.add"" ""RIBBON OUT,HTTP-POST,Y,Y,{0}/RibbonOut,0,N,""
setvar ""alerts.add"" ""HEAD TOO HOT,HTTP-POST,Y,Y,{0}/HeadTooHot,0,N,""
setvar ""alerts.add"" ""HEAD COLD,HTTP-POST,Y,Y,{0}/HeadCold,0,N,""
setvar ""alerts.add"" ""HEAD OPEN,HTTP-POST,Y,Y,{0}/HeadOpen,0,N,""
setvar ""alerts.add"" ""SUPPLY TOO HOT,HTTP-POST,Y,Y,{0}/SupplyTooHot,0,N,""
setvar ""alerts.add"" ""RIBBON IN,HTTP-POST,Y,Y,{0}/RibbonIn,0,N,""
setvar ""alerts.add"" ""REWIND,HTTP-POST,Y,Y,{0}/Rewind,0,N,""
setvar ""alerts.add"" ""CUTTER JAMMED,HTTP-POST,Y,Y,{0}/CutterJammed,0,N,""
setvar ""alerts.add"" ""PRINTER PAUSED,HTTP-POST,Y,Y,{0}/PrinterPaused,0,N,""
setvar ""alerts.add"" ""PQ JOB COMPLETED,HTTP-POST,Y,Y,{0}/PQJobCompleted,0,N,""
setvar ""alerts.add"" ""LABEL READY,HTTP-POST,Y,Y,{0}/LabelReady,0,N,""
setvar ""alerts.add"" ""HEAD ELEMENT BAD,HTTP-POST,Y,Y,{0}/HeadElementBad,0,N,""
setvar ""alerts.add"" ""POWER ON,HTTP-POST,Y,Y,{0}/PowerOn,0,N,""
setvar ""alerts.add"" ""CLEAN PRINTHEAD,HTTP-POST,Y,Y,{0}/CleanPrintHead,0,N,""
setvar ""alerts.add"" ""MEDIA LOW,HTTP-POST,Y,Y,{0}/MediaLow,0,N,""
setvar ""alerts.add"" ""RIBBON LOW,HTTP-POST,Y,Y,{0}/RibbonLow,0,N,""
setvar ""alerts.add"" ""REPLACE HEAD,HTTP-POST,Y,Y,{0}/RepleaceHead,0,N,""
setvar ""alerts.add"" ""BATTERY LOW,HTTP-POST,Y,Y,{0}/BatteryLow,0,N,""
setvar ""alerts.add"" ""RFID ERROR,HTTP-POST,Y,Y,{0}/RFIDError,0,N,""
setvar ""alerts.add"" ""MOTOR OVERTEMP,HTTP-POST,Y,Y,{0}/MotorOverTemp,0,N,""
setvar ""alerts.add"" ""PRINTHEAD SHUTDOWN,HTTP-POST,Y,Y,{0}/PrinterHeadShutdown,0,N,""
setvar ""alerts.add"" ""COLD START,HTTP-POST,Y,Y,{0}/ColdStart,0,N,""
setvar ""alerts.add"" ""SHUTTING DOWN,HTTP-POST,Y,Y,{0}/ShuttingDown,0,N,""
setvar ""alerts.add"" ""RESTARTING,HTTP-POST,Y,Y,{0}/Restarting,0,N,""
setvar ""alerts.add"" ""NO READER PRESENT,HTTP-POST,Y,Y,{0}/NoReaderPresent,0,N,""
setvar ""alerts.add"" ""THERMISTOR FAULT,HTTP-POST,Y,Y,{0}/ThermistorFault,0,N,""
setvar ""alerts.add"" ""INVALID HEAD,HTTP-POST,Y,Y,{0}/InvalidHead,0,N,""
setvar ""alerts.add"" ""RIBBON AUTH ERROR,HTTP-POST,Y,Y,{0}/RibbonAuthError,0,N,""
setvar ""alerts.add"" ""ODOMETER TRIGGERED,HTTP-POST,Y,Y,{0}/OdometerTriggered,0,N,""
getvar ""alerts.configured""
END
", baseUrl));
        }
    }
}
