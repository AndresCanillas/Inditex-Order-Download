using Newtonsoft.Json;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
    /// <summary>
    /// Monoprix only require encode EANS 
    /// </summary>
    public class MonoprixRFIDReportGeneratorService : IMonoprixRFIDReportGeneratorService
    {
        
        private readonly IProjectRepository projectRepo;
        private readonly IAppConfig config;
        private readonly ILogSection log;
        private readonly IEncodedLabelRepository encodeLabelRepo;
        private readonly IEncryptionService encryp;
        private readonly IOrderRepository orderRepo;
        private readonly ITempFileService tempFileService;

        public MonoprixRFIDReportGeneratorService(IAppConfig config, ILogService log, IEncodedLabelRepository encodeLabelRepo, IEncryptionService encryp, IProjectRepository projectRepo, IOrderRepository orderRepo, ITempFileService tempFileService)
        {
            this.config = config;
            this.log = log.GetSection("MonoprixEans"); ;
            this.encodeLabelRepo = encodeLabelRepo;
            this.encryp = encryp;
            this.projectRepo = projectRepo;
            this.orderRepo = orderRepo;
            this.tempFileService = tempFileService;
        }


        public void SendHistory(int companyID, DateTime from, DateTime to)
        {
            var startDate = from.Date;
            var maxEndDate = to.AddDays(1).AddTicks(-1);

            while(startDate < maxEndDate)
            {
                log.LogMessage($"day : [{startDate}]");
                var endDate = startDate.AddDays(1).AddTicks(-1);
                SendReport(companyID, startDate, endDate);
                startDate = startDate.AddDays(1).Date;
            }

        }

        public void SendReport(int companyID, DateTime from, DateTime to)
        {
            try
            {
                var projects = projectRepo.GetByCompanyID(companyID, false).ToList();

                projects.ToList().ForEach(project =>
                {
                    var data = GetReportData(project, from, to);

                    if (data.Count() < 1)
                    {
                        return;
                    }
                    var filePath = CreateFile(data);

                    PutFile(project, filePath, $"EAN.RFID.IndetGroup.{to.ToString("yyyyMMdd")}.csv");
                });
            }catch (Exception ex)
            {
                log.LogException($"Erro from: [{from}] to: [{to}]", ex);
                throw;
            }

            
        }
        
        public IEnumerable<string> GetReportData(IProject project, DateTime from, DateTime to)
        {
            var orderStatus = new List<OrderStatus> { OrderStatus.Completed };

            List<string> eans = new List<string>();
            
            var closedOrders = orderRepo.GetEncodedByProjectInStatusBetween(project.ID, orderStatus, from, to).ToList();

            closedOrders.ForEach(co =>
            {

                var found = encodeLabelRepo
               .GetByOrderId(new List<int>() { co.ID }, project.ID)
               //.GroupBy(g => g.Barcode)
               .Select(s => s.Barcode)
               .Distinct();

                eans.AddRange(found);

            });
            

            return eans;
            
        }

        public void PutFile(IProject project, string filePath, string targetFilename)
        {

            var decrypted = encryp.DecryptString(project.FTPClients);
            var configuredClients = JsonConvert.DeserializeObject<List<FtpClientConfig>>(decrypted);


            foreach (var cli in configuredClients)
            {
                
                SendViaFTP(cli, filePath, targetFilename);
                tempFileService.RegisterForDelete(filePath);
               
            }
        }

        /// <summary>
        /// return created file full path
        /// </summary>
        /// <param name="eans"></param>
        /// <returns></returns>
        public string CreateFile(IEnumerable<string> eans)
        {
            var filePath = tempFileService.GetTempFileName("monoprixean-report", true);
           
            using (StreamWriter csvFile = new StreamWriter(filePath, false))
            {
                eans.ToList().ForEach(line => csvFile.WriteLine(line));
            }

            return filePath;
            
        }

        private void SendViaFTP(FtpClientConfig cli, string filePath, string targetFilename)
        {
            var targetDirectory = config.GetValue<string>("CustomSettings.Monoprix.FTPTargetDirectory", "/arr");

            var ftpClient = new RebexFtpLib.Client.FtpClient();

            byte[] fileContent = tempFileService.ReadTempFile(filePath);
         
            ftpClient.Initialize(0,
                "ftpConnectionName-monoprix",
                cli.Server, cli.Port, cli.User, cli.Password,
                (RebexFtpLib.Client.FTPMode)((int)cli.Mode), cli.AllowInvalidCert, null, null,
                RebexFtpLib.Client.SFTPKeyAlgorithm.None);

            ftpClient.Connect();

            ftpClient.ChangeDirectory(targetDirectory); // hardcode directory for monoprix

            ftpClient.SendFile(fileContent, targetFilename);
        }

    }
    
}
