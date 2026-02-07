using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mail;
using System.Numerics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.Database;
using Service.Contracts.PrintLocal;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
    public class SendDuplicatedEPCEmail : EQEventHandler<DuplicatedEPCEvent>
    {
        private ICLSNotificationService clsNotification;

        public SendDuplicatedEPCEmail(
            ICLSNotificationService clsNotification)
        {
            this.clsNotification = clsNotification;
        }

        public override EQEventHandlerResult HandleEvent(DuplicatedEPCEvent e)
        {
            var response = clsNotification.SendDuplicatedEPCEMail(e.FactyoryName, e.OrderId, e.EPCList);

            return new EQEventHandlerResult() { Success = response };
        }
    }
}