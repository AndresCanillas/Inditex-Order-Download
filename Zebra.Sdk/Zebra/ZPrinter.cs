using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Services.Zebra.Commands;

namespace WebLink.Services.Zebra
{
    public class ZPrinter : IZPrinter
    {
        public const int MAX_QUEUED_LABELS = 5;

        private IFactory factory;
        private IEventQueue events;
        private IWSConnection conn;
        private ILogService log;

        private object syncObj = new object();
        private volatile bool executingJob;
        private volatile bool keepAlive = true;
        private volatile IWSConnection rawchannel;
        private volatile PrintJobDetailDTO currentTask;
        private string lastHeaderContent = null;
        private string lastStopContent = null;
        private volatile PrinterState lastSeenState = new PrinterState();
        private ManualResetEvent printerWaitHandle = new ManualResetEvent(false);
        private string eventToken;


        public ZPrinter(IFactory factory)
        {
            this.factory = factory;
            this.log = factory.GetInstance<ILogService>();
            events = factory.GetInstance<IEventQueue>();
            eventToken = events.Subscribe<EntityEvent>(handleDBUpdate);
        }


        private void handleDBUpdate(EntityEvent e)
        {
            if(e.Entity is IPrinter && e.Operation == DBOperation.Update)
            {
                var printer = e.Entity as IPrinter;
                DriverName = printer.DriverName;
            }
        }


        public int ID { get; set; }
        public int LocationID { get; set; }
        public int CompanyID { get; set; }
        public string DeviceID { get; set; }
        public string ProductName { get; set; }
        public string Name { get; set; }
        public string Firmware { get; set; }
        public string DriverName { get; set; }


        public int CurrentJobID
        {
            get
            {
                try
                {
                    lock (syncObj)
                    {
                        return (currentTask != null) ? currentTask.PrinterJobID : 0;
                    }
                }
                catch { return 0; }
            }
        }


        public bool Connected
        {
            get
            {
                try
                {
                    if (rawchannel != null)
                        return rawchannel.IsConnected;
                    else
                        return false;
                }
                catch { return false; }
            }
        }


        public IWSConnection MainChannel
        {
            get
            {
                return conn;
            }
            set
            {
                if (value == null)
                    throw new InvalidOperationException("Cannot set MainChannel to null.");
                if (conn != null)
                    throw new InvalidOperationException("Cannot set MainChannel multiple times.");
                conn = value;
                DeviceID = conn.DeviceProperties["SERIAL_NUMBER"];
                ProductName = conn.DeviceProperties["PRODUCT_NAME"];
                Firmware = conn.DeviceProperties["FIRMWARE_VER"];

                var printerRepo = factory.GetInstance<IPrinterRepository>();
                var printer = printerRepo.GetByDeviceID(DeviceID);
                if (printer != null)
                {
                    ID = printer.ID;
                    Name = printer.Name;
                    LocationID = printer.LocationID;
                    DriverName = printer.DriverName;
                    var locationRepo = factory.GetInstance<ILocationRepository>();
                    var location = locationRepo.GetByID(printer.LocationID);
                    CompanyID = location.CompanyID;
                    var st = new PrinterState() { ID = printer.ID, Online = true };
                    events.Send(new PrinterJobEvent(CompanyID, PrinterJobEventType.PrinterStatus, new CompactPrinterState(st)));
                }
                else
                {
                    log.LogMessage("DeviceID {0} is not registered in the DB", DeviceID);
                }
            }
        }


        // Ensures that all cached printer properties are up to date by reading them from the DB again.
        private void UpdatePrinterProperties()
        {
            var printerRepo = factory.GetInstance<IPrinterRepository>();
            var printer = printerRepo.GetByID(ID);
            if (printer != null)
            {
                Name = printer.Name;
                LocationID = printer.LocationID;
                DriverName = printer.DriverName;
                var locationRepo = factory.GetInstance<ILocationRepository>();
                var location = locationRepo.GetByID(printer.LocationID);
                CompanyID = location.CompanyID;
            }
        }


        public IWSConnection RawChannel
        {
            get { return rawchannel; }
            set { rawchannel = value; }
        }


        public void RegisterChannel(IWSConnection conn)
        {
            if (conn == null)
                throw new ArgumentNullException(nameof(conn));
            switch (conn.ChannelType)
            {
                case ChannelType.Weblink:
                    if (MainChannel != null)
                        throw new InvalidOperationException("The main channel has already been initialized.");
                    MainChannel = conn;
                    break;
                case ChannelType.Raw:
                    if (RawChannel != null)
                        throw new InvalidOperationException("The raw channel has already been initialized.");
                    RawChannel = conn;
                    ResumeWork();
                    break;
                default:
                    throw new NotSupportedException($"Channel of type {conn.ChannelType} is not supported.");
            }
        }


        public bool IsPrinting()
        {
            lock (syncObj)
            {
                return executingJob;
            }
        }


        public void ResumeWork()
        {
            var repo = factory.GetInstance<IPrinterJobRepository>();
            var job = repo.GetNextPrinterJob(ID);
            if(job != null)
                StartJob(job);
        }


        public void StartJob(IPrinterJob job)
        {
            if(job.AssignedPrinter.HasValue && job.AssignedPrinter.Value == ID)
            {
                PauseCurrentJob();
                lock (syncObj)
                {
                    if (executingJob) // NOTE: This ensures only ONE task is created when concurrent requests to start jobs are received at the same time on the same printer. All concurrent requests will receive an exception, but one.
                        throw new Exception("Concurrent access to StartJob detected, this thread cannot continue executing.");
                    executingJob = true;
                    keepAlive = true;
                    var task = new Task(() => ExecutePrinterJob(job), TaskCreationOptions.LongRunning);
                    task.Start();
                }
            }
        }


        public void PauseJob(IPrinterJob job)
        {
            bool stop = false;
            lock (syncObj)
            {
                stop = job.ID == CurrentJobID;
            }
            if (stop) PauseCurrentJob();
            UpdateJobState(job.ID, JobStatus.Paused);
        }


        public async Task PrintSample(int projectid, int articleid, int orderid, int detailid)
        {
            PauseCurrentJob();
            try
            {
                var orderRepo = factory.GetInstance<IOrderRepository>();
                var projectRepo = factory.GetInstance<IProjectRepository>();
                var articleRepo = factory.GetInstance<IArticleRepository>();
                var productRepo = factory.GetInstance<IVariableDataRepository>();
                var printerRepo = factory.GetInstance<IPrinterRepository>();
                var printCommand = factory.GetInstance<IPrintLabelCommand>();
                var order = orderRepo.GetByID(orderid);
                var project = projectRepo.GetByID(projectid);
                var article = articleRepo.GetByID(articleid, true);
                var data = productRepo.GetProductDataFromDetail(project.ID, detailid);
                var settings = printerRepo.GetSettings(ID, articleid);
                await printCommand.PrepareLabel(projectid, article.LabelID.Value, orderid, order.OrderNumber, detailid, data, settings, DriverName, true);
                printCommand.IsLastInBatch = true;
                await WaitForPrinterReady();
                await SendPreamble(printCommand);
                await RawChannel.SendCommand(printCommand);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
            }
        }


        private void PauseCurrentJob()
        {
            bool wait = false;
            lock (syncObj)
            {
                if (executingJob)
                {
                    printerWaitHandle.Reset();
                    keepAlive = false;
                    wait = true;
                }
            }
            if (wait)
            {
                if (!printerWaitHandle.WaitOne(20000))
                    throw new Exception("Print process is not responding");
            }
        }


        private async void ExecutePrinterJob(IPrinterJob job)
        {
            bool error = false, jobCompleted = false;
            try
            {
                lastHeaderContent = null;
                lastStopContent = null;
                UpdateJobState(job.ID, JobStatus.Executing);
                if (!keepAlive) return;
                var jobRepo = factory.GetInstance<IPrinterJobRepository>();
                var detail = jobRepo.GetJobDetails(job.ID, true);
                foreach(var task in detail)
                {
                    lock(syncObj) currentTask = task;
                    if (!keepAlive) break;
                    if (task.Printed < task.Quantity + task.Extras)
                    {
                        await ExecuteTask(job, task);
                        var taskCompleted = task.Printed >= task.Quantity + task.Extras;
                        if (!taskCompleted) break;
                    }
                }
                jobCompleted = !detail.Any(p => p.Printed < p.Quantity + p.Extras);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                events.Send(new PrinterJobEvent(CompanyID, PrinterJobEventType.JobError, new PrinterJobError(ex.Message, job)));
                error = true;
            }
            finally
            {
                if(keepAlive)
                {
                    try
                    {
                        if (error)
                            UpdateJobState(job.ID, JobStatus.Error);
                        else if (jobCompleted)
                        {
                            UpdateJobState(job.ID, JobStatus.Completed);
                            events.Send(new OrderChangeStatusEvent() { OrderID = job.CompanyOrderID, OrderStatus = (int)OrderStatus.Completed });
                        }
                        else
                            UpdateJobState(job.ID, JobStatus.Paused);
                    }
                    catch (Exception ex)
                    {
                        log.LogException(ex);
                    }
                }
                else
                {
                    UpdateJobState(job.ID, JobStatus.Paused);
                }
                lock (syncObj)
                {
                    executingJob = false;
                    currentTask = null;
                    printerWaitHandle.Set();
                }
            }
        }


        private async Task ExecuteTask(IPrinterJob job, PrintJobDetailDTO task)
        {
            int sentLabels = task.Printed;
            if (!keepAlive || (task.Quantity + task.Extras) == 0) return;

            UpdatePrinterProperties();
            var companyRepo = factory.GetInstance<ICompanyRepository>();
            var orderRepo = factory.GetInstance<IOrderRepository>();
            var articleRepo = factory.GetInstance<IArticleRepository>();
            var printerRepo = factory.GetInstance<IPrinterRepository>();
            var encodedLabelRepo = factory.GetInstance<IEncodedLabelRepository>();
            var variableDataRepo = factory.GetInstance<IVariableDataRepository>();
            var printCommand = factory.GetInstance<IPrintLabelCommand>();
            var order = orderRepo.GetByID(job.CompanyOrderID, true);
            var company = companyRepo.GetByID(order.BillToCompanyID, true);
            var article = articleRepo.GetByID(job.ArticleID, true);
            var settings = printerRepo.GetSettings(ID, job.ArticleID);
            IVariableData data = variableDataRepo.GetProductDataFromDetail(order.ProjectID, task.ProductDataID);

            await printCommand.PrepareLabel(order.ProjectID, article.LabelID.Value, order.ID, order.OrderNumber, task.ProductDataID, data, settings, DriverName, false);
            var pendingConfirmations = new ConcurrentQueue2<SentLabelInfo>();

            await WaitForPrinterReady();
            if (!string.IsNullOrWhiteSpace(company.StopFields))
            {
                if (lastStopContent == null)
                {
                    lastStopContent = GetNextHeader(data, company.StopFields);
                }
                else
                {
                    string stop = GetNextHeader(data, company.StopFields);
                    if (lastStopContent != stop)
                    {
                        lastStopContent = stop;
                        if (settings.EnableCut && settings.CutBehavior == CutBehavior.EachStop)
                        {
                            await SendCutCommand();
                            await WaitForPrinterReady();
                            if(!settings.ResumeAfterCut)
                            {
                                await PausePrinter(true);
                                await WaitForPrinterReady();
                            }
                        }
                        else
                        {
                            await PausePrinter(true);
                            await WaitForPrinterReady();
                        }
                    }
                }
            }

            await SendPreamble(printCommand);  //The preamble configures the printer with the selected offset, darkness and speed, however it only needs to be sent once on each task.

            if(!string.IsNullOrWhiteSpace(company.HeaderFields) && task.Printed == 0)
            {
                string header = GetNextHeader(data, company.HeaderFields);
                if (header != lastHeaderContent)
                {
                    await SendHeader(data, company.HeaderFields, settings);
                }
            }

            EventHandler<ReceiveEventArgs> recvHandler = (sender, e) => {
                string tid, epc;
                SentLabelInfo formatInfo;
                if (e.Message.Contains("TID:"))
                {
                    try
                    {
                        if (pendingConfirmations.TryDequeue(out formatInfo))
                        {
                            var encRepo = factory.GetInstance<IEncodedLabelRepository>();
                            if (formatInfo.EncodeRFID)
                            {
                                if (ParsePrinterResponse(e.Message, out tid, out epc))
                                {
                                    if (epc == formatInfo.EPC)
                                    {
                                        encRepo.AddEncodedLabel(
                                            job.ID, task.ID, ID, job.CompanyID, order.ProjectID,
                                            formatInfo.ArticleCode, formatInfo.ProductCode, order.ProductionType, LocationID,
                                            formatInfo.Serial, tid, epc, formatInfo.AccessPassword, formatInfo.KillPassword,
                                            true, null, true, true);
                                        job.IncPrinted();
                                        task.IncPrinted();
                                        job.IncEncoded();
                                        task.IncEncoded();
                                    }
                                    else
                                    {
                                        do
                                        {
                                            if (epc != formatInfo.EPC)
                                            {
                                                encRepo.AddEncodedLabel(
                                                    job.ID, task.ID, ID, job.CompanyID, order.ProjectID,
                                                    formatInfo.ArticleCode, formatInfo.ProductCode, order.ProductionType, LocationID,
                                                    formatInfo.Serial, null, formatInfo.EPC, formatInfo.AccessPassword, formatInfo.KillPassword,
                                                    false, "001", true, true);    //Code001 = "Did not receive RFID command confirmation"
                                            }
                                            else
                                            {
                                                encRepo.AddEncodedLabel(
                                                    job.ID, task.ID, ID, job.CompanyID, order.ProjectID,
                                                    formatInfo.ArticleCode, formatInfo.ProductCode, order.ProductionType, LocationID,
                                                    formatInfo.Serial, tid, epc, formatInfo.AccessPassword, formatInfo.KillPassword,
                                                    true, null, true, true);
                                            }
                                            job.IncPrinted();
                                            task.IncPrinted();
                                            job.IncEncoded();
                                            task.IncEncoded();
                                        } while (epc != formatInfo.EPC && pendingConfirmations.TryDequeue(out formatInfo));
                                        //if (settings.PauseOnError)
                                        //	_ = Task.Run(() => PausePrinter(true));
                                    }
                                }
                            }
                            else
                            {
                                task.IncPrinted();
                                job.IncPrinted();
                                encRepo.AddPrintedLabel(task.ID);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogException(ex);
                    }
                }
            };
            RawChannel.OnReceive += recvHandler;

            string token = events.Subscribe<PrinterJobEvent>((e) =>
            {
                if(e.Type == PrinterJobEventType.ExtrasAdded)
                {
                    var detail = (e.Data as IPrinterJobDetail);
                    if (detail.ID == task.ID)
                        task.Extras = detail.Extras;
                }
            });

            try
            {
                if (settings.EnableCut && settings.CutBehavior == CutBehavior.EachLabel)
                    printCommand.EnableCut = true;
                while (task.Printed < task.Quantity + task.Extras)
                {
                    bool shouldSendLabel = (sentLabels < task.Quantity + task.Extras) && keepAlive && lastSeenState.CanPrint;
                    if (shouldSendLabel)
                    {
                        try
                        {
                            if (sentLabels == (task.Quantity + task.Extras - 1))
                                printCommand.IsLastInBatch = true;
                            await RawChannel.SendCommand(printCommand);
                            pendingConfirmations.Enqueue(new SentLabelInfo(printCommand.EncodeRFID, article.ArticleCode, printCommand.ProductCode, printCommand.LastEPC, printCommand.LastSerial, printCommand.AccessPassword, printCommand.KillPassword));
                            sentLabels++;
                        }
                        catch (Exception ex)
                        {
                            log.LogException(ex);
                            job.IncErrors();
                            task.IncErrors();
                            throw;
                        }
                        await WaitForPrinterBuffer();
                    }
                    else
                    {
                        if (pendingConfirmations.Count == 0)
                        {
                            if (!keepAlive)
                                break;
                            else
                            {
                                await Task.Delay(1000);
                                await GetPrinterState();
                            }
                        }
                        else
                        {
                            await WaitForQueuedLabels(pendingConfirmations);
                            foreach (var item in pendingConfirmations)
                            {
                                if (item.Date.AddSeconds(10) < DateTime.Now)
                                {
                                    pendingConfirmations.Remove(item);

                                    if (item.EncodeRFID)
                                    {
                                        // NOTE: We have identified that the printer some times does not respond to the ^RFR commands that generate a message to the host, in that case some labels that were printed will be marked as errors...
                                        encodedLabelRepo.AddEncodedLabel(
                                            job.ID, task.ID, ID, job.CompanyID, order.ProjectID,
                                            item.ArticleCode, item.ProductCode, order.ProductionType, this.LocationID,
                                            item.Serial, null, item.EPC, item.AccessPassword, item.KillPassword,
                                            false, "001", true, true);
                                        job.IncPrinted();
                                        task.IncPrinted();
                                        job.IncEncoded();
                                        task.IncEncoded();
                                    }
                                    else
                                    {
                                        task.IncPrinted();
                                        job.IncPrinted();
                                        encodedLabelRepo.AddPrintedLabel(task.ID);
                                    }
                                }
                            }
                        }
                    }
                }
                if(settings.EnableCut && settings.CutBehavior == CutBehavior.EachBarcode)
                    await SendCutCommand();
            }
            catch(Exception ex)
            {
                SentLabelInfo item;
                while(pendingConfirmations.TryDequeue(out item))
                {
                    if (item.EncodeRFID)
                    {
                        // NOTE: We have identified that the printer some times does not respond to the ^RFR commands that generate a message to the host, in that case some labels that were printed will be marked as errors...
                        encodedLabelRepo.AddEncodedLabel(
                            job.ID, task.ID, ID, job.CompanyID, order.ProjectID,
                            item.ArticleCode, item.ProductCode, order.ProductionType, this.LocationID,
                            item.Serial, null, item.EPC, item.AccessPassword, item.KillPassword,
                            false, "001", true, true);
                        job.IncPrinted();
                        task.IncPrinted();
                        job.IncEncoded();
                        task.IncEncoded();
                    }
                    else
                    {
                        task.IncPrinted();
                        job.IncPrinted();
                        encodedLabelRepo.AddPrintedLabel(task.ID);
                    }
                }
                throw;
            }
            finally
            {
                RawChannel.OnReceive -= recvHandler;
                events.Unsubscribe<PrinterJobEvent>(token);
            }
        }



        private async Task SendPreamble(IPrintLabelCommand printCommand)
        {
            if (!String.IsNullOrWhiteSpace(printCommand.Preamble))
            {
                BaseCommand cmd = new BaseCommand();
                cmd.SetMessage(printCommand.Preamble);
                cmd.IsOneWay = true;
                await RawChannel.SendCommand(cmd);
                await Task.Delay(100); // Give time for printer to process preamble before sending the labels.
            }
        }


        private string GetNextHeader(IVariableData data, string fieldsNames)
        {
            if (String.IsNullOrWhiteSpace(fieldsNames))
                return "";
            StringBuilder sb = new StringBuilder(50);
            var fields = fieldsNames.Split(',', ';');
            foreach(var field in fields)
            {
                var value = data[field];
                if (value != null)
                    sb.Append(value.ToString()).Append("-");
            }
            return sb.ToString();
        }


        private async Task PausePrinter(bool pause)
        {
            await RawChannel.SendCommand(new PauseCommand(pause));
            await Task.Delay(200).ConfigureAwait(false);
        }


        private async Task SendCutCommand()
        {
            await RawChannel.SendCommand(new CutCommand());
            await Task.Delay(200).ConfigureAwait(false);
        }


        private async Task WaitForPrinterReady()
        {
            bool keepWaiting = true;
            int waitTime = 200;
            do
            {
                if(keepWaiting)
                    await Task.Delay(waitTime).ConfigureAwait(false);
                if(waitTime < 5000)
                    waitTime += 100;
                var printerState = await GetPrinterState();
                keepWaiting = !printerState.Ready;
            } while (keepAlive && keepWaiting);
        }


        private async Task WaitForPrinterBuffer()
        {
            bool keepWaiting = true;
            int waitTime = 200;
            do
            {
                if (keepWaiting)
                    await Task.Delay(waitTime).ConfigureAwait(false);
                if(waitTime < 2000)
                    waitTime += 100;
                var printerState = await GetPrinterState();
                keepWaiting = printerState.FormatsInBuffer >= MAX_QUEUED_LABELS;
            } while (keepAlive && keepWaiting);
        }


        private async Task WaitForQueuedLabels(ConcurrentQueue2<SentLabelInfo> pendingConfirmations)
        {
            bool keepWaiting = true;
            int waitTime = 200;
            do
            {
                if (keepWaiting)
                    await Task.Delay(waitTime).ConfigureAwait(false);
                if(waitTime < 2000)
                    waitTime += 100;
                var printerState = await GetPrinterState();
                keepWaiting = printerState.FormatsInBuffer > 0;
            } while (keepAlive && keepWaiting);
        }


        public async Task<PrinterState> GetPrinterState()
        {
            var cmd = new HostStatusCommand();
            var response = await RawChannel.SendCommand(cmd);
            var state = new PrinterState() { ID = this.ID, Online = true };
            if (cmd.IsValidResponse)
            {
                state.Ready = !(cmd.HeadUp || cmd.Paused || cmd.PaperOut || cmd.RibbonOut || cmd.LabelWaitingPeelOff
                    || cmd.LabelsRemainingInBatch > 0 || cmd.PartialFormatInProgress
                    || cmd.NumberOfFormatsInBuffer > 0 || cmd.BufferFull);
                state.HeadOpen = cmd.HeadUp;
                state.Paused = cmd.Paused;
                state.PaperOut = cmd.PaperOut;
                state.RibbonOut = cmd.RibbonOut;
                state.FormatsInBuffer = cmd.NumberOfFormatsInBuffer;
                if(!lastSeenState.Equals(state))
                {
                    lastSeenState = state;
                    events.Send(new PrinterJobEvent(CompanyID, PrinterJobEventType.PrinterStatus, new CompactPrinterState(state)));
                }
            }
            return state;
        }


        private bool ParsePrinterResponse(string data, out string tid, out string epc)
        {
            int idx1, idx2;
            tid = epc = null;
            idx1 = data.IndexOf("TID:[");
            if (idx1 >= 0)
            {
                idx1 += 5;
                idx2 = data.IndexOf("]", idx1);
                if (idx2 >= idx1)
                {
                    tid = data.Substring(idx1, idx2 - idx1);
                }
            }
            idx1 = data.IndexOf("EPC:[");
            if (idx1 >= 0)
            {
                idx1 += 5;
                idx2 = data.IndexOf("]", idx1);
                if (idx2 >= idx1)
                {
                    epc = data.Substring(idx1, idx2 - idx1);
                }
            }
            return !(String.IsNullOrWhiteSpace(tid) || String.IsNullOrWhiteSpace(epc));
        }


        private async Task<IPrinterJobDetail> GetNextTask(IPrinterJob job)
        {
            var repo = factory.GetInstance<IPrinterJobRepository>();
            var details = await repo.JobDetailsAsync(job.ID);
            var task = details.Where(p => p.Printed < p.Quantity + p.Extras).OrderBy(p=>p.ID).FirstOrDefault();
            return task;
        }


        private void UpdateJobState(int jobid, JobStatus status)
        {
            var repo = factory.GetInstance<IPrinterJobRepository>();
            repo.UpdateJobState(jobid, status);
        }


        private async Task SendHeader(IVariableData data, string headerFields, IPrinterSettings settings)
        {
            var headerLabel = factory.GetInstance<IPrintLabelCommand>();

            var headerData = new Dictionary<string, string>();
            var fields = headerFields.Split(',', ';');
            int fieldNumber = 1;
            foreach (var field in fields)
            {
                var value = data[field];
                if (value != null)
                {
                    if (field.Length > 20)
                        headerData.Add($"Field{fieldNumber}", field.Substring(0, 20));
                    else
                        headerData.Add($"Field{fieldNumber}", field);
                    if(value.Length > 40)
                        headerData.Add($"Value{fieldNumber}", value.Substring(0, 40));
                    else
                        headerData.Add($"Value{fieldNumber}", value);
                }
                else
                {
                    headerData.Add($"Field{fieldNumber}", "");
                    headerData.Add($"Value{fieldNumber}", "");
                }
                fieldNumber++;
            }
            for (; fieldNumber < 4; fieldNumber++)
            {
                headerData.Add($"Field{fieldNumber}", "");
                headerData.Add($"Value{fieldNumber}", "");
            }

            await headerLabel.PrepareHeader(new VariableData(headerData), settings, DriverName);
            await WaitForPrinterReady();
            if (keepAlive)
            {
                await RawChannel.SendCommand(headerLabel);
                await WaitForPrinterReady();
            }
        }


        public void Dispose()
        {
            events.Unsubscribe<EntityEvent>(eventToken);
            var st = new PrinterState() { ID = ID, Online = false };
            events.Send(new PrinterJobEvent(CompanyID, PrinterJobEventType.PrinterStatus, new CompactPrinterState(st)));
            keepAlive = false;
            if (MainChannel != null)
                MainChannel.Disconnect();
            if (RawChannel != null)
                RawChannel.Disconnect();
        }
    }


    public class AlertCondition : IAlertCondition
    {
        public AlertCondition() { }

        public AlertCondition(string name, bool toogle)
        {
            AlertName = name;
            IsToggle = toogle;
        }

        public string AlertName { get; set; }

        public bool IsToggle { get; set; } = true;

        public bool IsSet { get; set; } = false;

        public DateTime UpdateDate { get; set; } = DateTime.Now;
    }


    class SentLabelInfo
    {
        public SentLabelInfo(bool encodeRFID, string articleCode, string productCode, string epc, long serial, string accPwd, string killPwd)
        {
            EncodeRFID = encodeRFID;
            ArticleCode = articleCode;
            ProductCode = productCode;
            EPC = epc;
            Serial = serial;
            AccessPassword = accPwd;
            KillPassword = killPwd;
            Date = DateTime.Now;
        }
        public bool EncodeRFID;
        public string ArticleCode;
        public string ProductCode;
        public string EPC;
        public long Serial;
        public string AccessPassword;
        public string KillPassword;
        public DateTime Date;
    }
}