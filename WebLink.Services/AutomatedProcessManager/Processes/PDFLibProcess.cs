using System;
using System.Collections.Generic;
using Service.Contracts;
using WebLink.Contracts.Models;
using Service.Contracts.LabelService;

namespace WebLink.Services.Automated
{
    public class PDFLibUpdateStatusEvent : EQEventInfo
    {
        public int JobFileID { get; set; }
        public JobStatus JobStatus { get; set; }
        public int ProjectID { get; set; }

        public PDFLibUpdateStatusEvent(int JobFileID, JobStatus JobStatus)
        {
            this.JobFileID = JobFileID;
            this.JobStatus = JobStatus;
        }
    }

    public class PDFLibSendRequestEvent : EQEventInfo
    {
        public int JobFileID { get; set; }
        public PrintToFileRequest Data { get; set; }

        public PDFLibSendRequestEvent(PrintToFileRequest Data, int JobFileID)
        {
            this.Data = Data;
            this.JobFileID = JobFileID;
        }
    }

    public class PDFLibThrowExceptionEvent: EQEventInfo
    {
        public int JobFileID { get; set; }
        public Exception Exception { get; set; }

        public PDFLibThrowExceptionEvent(Exception Exception, int JobFileID)
        {
            this.Exception = Exception;
            this.JobFileID = JobFileID;
        }
    }

    public class PDFLibSuccessEvent : EQEventInfo
    {
        public int JobFileID { get; set; }
        public LabelServiceResponse ResponseFile { get; set; }
        public LabelServiceContentResponse ResponseContentFile { get; set; }

        public PDFLibSuccessEvent(LabelServiceResponse ResponseFile, int JobFileID)
        {
            this.ResponseFile = ResponseFile;
            this.JobFileID = JobFileID;
        }

        public PDFLibSuccessEvent(LabelServiceContentResponse ResponseContentFile, int JobFileID)
        {
            this.ResponseContentFile = ResponseContentFile;
            this.JobFileID = JobFileID;
        }
    }


    public class PDFLibProcess : IAutomatedProcess
    {
        private Dictionary<string, string> tokens;

        private IFactory factory;
        private IEventQueue events;

        public PDFLibProcess(
            IFactory factory,
            IEventQueue eventService
            )
        {
            tokens = new Dictionary<string, string>();
            this.factory = factory;
            this.events = eventService;
        }

        public TimeSpan GetIdleTime()
        {
            return TimeSpan.MaxValue;  // returning MaxValue means that this process does not execute at regular intervals
        }

        public void OnLoad()
        {
            tokens["PDFLibUpdateStatusEvent"] = events.Subscribe<PDFLibUpdateStatusEvent>(PDFLibUpdateStatusHandler);
            tokens["PDFLibSendRequestEvent"] = events.Subscribe<PDFLibSendRequestEvent>(PDFLibSendRequestHandler);
            tokens["PDFLibThrowExceptionEvent"] = events.Subscribe<PDFLibThrowExceptionEvent>(PDFLibThrowExceptionHandler);
            tokens["PDFLibSuccessEvent"] = events.Subscribe<PDFLibSuccessEvent>(PDFLibSuccessHandler);
        }

        public void OnExecute()
        {
            throw new NotImplementedException();
        }

        public void OnUnload()
        {
            events.Unsubscribe<PDFLibUpdateStatusEvent>(tokens["PDFLibUpdateStatusEvent"]);
            events.Unsubscribe<PDFLibSendRequestEvent>(tokens["PDFLibSendRequestEvent"]);
            events.Unsubscribe<PDFLibThrowExceptionEvent>(tokens["PDFLibThrowExceptionEvent"]);
            events.Unsubscribe<PDFLibSuccessEvent>(tokens["PDFLibSuccessEvent"]);
        }

        private void PDFLibUpdateStatusHandler(PDFLibUpdateStatusEvent e)
        {
            
        }

        private void PDFLibSendRequestHandler(PDFLibSendRequestEvent e)
        {
            
        }

        private void PDFLibThrowExceptionHandler(PDFLibThrowExceptionEvent e)
        {
            
        }

        private void PDFLibSuccessHandler(PDFLibSuccessEvent e)
        {
            
        }
    }
}
