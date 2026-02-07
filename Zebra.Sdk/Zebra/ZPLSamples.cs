using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Services.Zebra
{
    public static class ZPLSamples
    {
        public static readonly string TemplateWithRFID = "^XA^FO6,10^AQN,15,0^FB160,2,,L,,^FD{Line1}^FS^FO6,40^AQN,15,0^FB160,2,,L,,^FD{Line2}^FS^FO6,70^AQN,15,0^FB160,2,,L,,^FD{Line3}^FS^FO6,100^AQN,15,0^FB276,2,,L,,^FDSize                Colour^FS^FO6,130^AQN,15,0^FB101,2,,L,,^FD{Size}^FS^FO121,130^AQN,15,0^FB161,2,,L,,^FD{Color}^FS^FO6,160^AQN,15,0^FB231,2,,L,,^FD{Line4}^FS^FO243,160^AQN,15,0^FB198,2,,L,,^FD{Line5}^FS^BY3^FO51,190^BEN,83,Y,N^FD8433718839693^FS^RS8^FN3^RFW,H^FD000000000000000000000001^HV3,12,RFID[,]_0D_0A,L^FS^XZ";
        public static readonly string TemplateNoRFID = "^XA^FO6,10^AQN,15,0^FB160,2,,L,,^FD{Line1}^FS^FO6,40^AQN,15,0^FB160,2,,L,,^FD{Line2}^FS^FO6,70^AQN,15,0^FB160,2,,L,,^FD{Line3}^FS^FO6,100^AQN,15,0^FB276,2,,L,,^FDSize                Colour^FS^FO6,130^AQN,15,0^FB101,2,,L,,^FD{Size}^FS^FO121,130^AQN,15,0^FB161,2,,L,,^FD{Color}^FS^FO6,160^AQN,15,0^FB231,2,,L,,^FD{Line4}^FS^FO243,160^AQN,15,0^FB198,2,,L,,^FD{Line5}^FS^BY3^FO51,190^BEN,83,Y,N^FD8433718839693^FS^XZ";
		public static readonly string HostVerificationExample = "^XA^FO6,10^A0N,15,0^FB160,2,,L,,^FDHost Verification^FS^FN3^FDABCDEFGHIJKL^HV3,12,Code[,]_0D_0A,L^FS^XZ";
		public static readonly string Calibration = "^XA^FO10,10^GB100,10,1,B,0^FS^FO10,10^GB10,100,1,B,0^FS^FO10,270^GB100,10,1,B,0^FS^FO10,180^GB10,100,1,B,0^FS^FO340,10^GB100,10,1,B,0^FS^FO430,10^GB10,100,1,B,0^FS^FO340,270^GB100,10,1,B,0^FS^FO430,180^GB10,100,1,B,0^FS^FO27,240^A0B,15,0^FB100,1,,L,,^FDAbcde^FS^FO24,85^A0B,15,0^FB100,1,,L,,^FDFghij^FS^FO330,240^A0B,15,0^FB100,1,,L,,^FDKlmno^FS^FO380,85^A0B,15,0^FB60,1,,L,,^FDPqrst^FS^XZ";

	}
}
