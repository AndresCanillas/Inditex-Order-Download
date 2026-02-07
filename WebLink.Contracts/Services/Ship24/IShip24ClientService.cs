using System;

namespace WebLink.Contracts.Services.Ship24
{
    // Note: All properties starts in lowercase for Json serializing requirements 
    public class Recipient
    {
        public string email { get; set; }
        public string name { get; set; }
    }

    public class Settings
    {
        public bool restrictTrackingToCourierCode { get; set; }
    }

    public class Ship24TrackingInfo
    {
        public string trackingNumber { get; set; }
        public string shipmentReference { get; set; }
        public string clientTrackerId { get; set; }
        public string originCountryCode { get; set; }   // <ISO 3166-1 alpha-2/alpha-3>
        public string destinationCountryCode { get; set; } // "<ISO 3166-1 alpha-2/alpha-3>"
        public string destinationPostCode { get; set; }
        public DateTime? shippingDate { get; set; }
        public string[] courierCode { get; set; }
        public string courierName { get; set; }
        public string trackingUrl { get; set; }
        public string orderNumber { get; set; }
        public string title { get; set; }
        public Recipient recipient { get; set; }
        public Settings settings { get; set; }
    }

    public class Ship24Error
    {
        public string code { get; set; }
        public string message { get; set; }
    }

    public class Tracker
    {
        string trackerId { get; set; }
        string trackingNumber { get; set; }
        string shipmentReference { get; set; }
        string[] courierCode { get; set; }
        string clientTrackerId { get; set; }
        bool isSubscribed { get; set; }
        bool isTracked { get; set; }
        DateTime createdAt { get; set; }
    }

    public class Delivery
    {
        DateTime etimatedDeliveryDate { get; set; }
        string service { get; set; }    
        string signedBy { get; set; }   
    }

    public class trackingNumber
    {
        string tn { get; set; }
    }

    public class Shipment
    {
        string shipmentId { get; set; }
        string statusCode { get; set; }
        string statusCategory { get; set; }
        string statusMilestone { get; set; }
        string originCountryCode { get; set; }  
        string destinationCountryCode { get; set; }
        Delivery delivery { get; set; }
        trackingNumber[] trackingNumbers { get; set; }
        ResponseRecipient recipient { get; set; }   
    }

    public class ResponseRecipient
    {
        string name { get; set; }    
        string address { get; set; }
        string postCode { get; set; }
        string subdivision { get; set; }
    }   

    public class Event
    {
        string eventId { get; set; }
        string trackingNumber { get; set; }
        string eventTrackingNumber { get; set; }
        string status { get; set; }
        DateTime occurrenceDatetime { get; set; }
        int order { get; set; } 
        string location { get; set; }
        string sourceCode { get; set; }
        string courierCode { get; set; }    
        string statusCode { get; set; }
        string statusCategory { get; set; }
        string statusMilestone { get; set; }

    }

    public class Timestamp
    {
        DateTime infoReceivedDatetime { get; set; }
        DateTime inTransitDatetime { get; set; }
        DateTime outForDeliveryDatetime { get; set; }
        DateTime failedAttemptDatetime { get; set; }
        DateTime availableForPickupDatetime { get; set; }
        DateTime exceptionDatetime { get; set; }
        DateTime deliveredDatetime { get; set; }
    }

    public class Statistics {
        Timestamp timestamps { get; set; }
    }

    public class Ship24Tracking
    {
        Tracker tracker { get; set; }   
        Shipment shipment { get; set; }
        Event[] events { get; set; }
        Statistics statistics { get; set; }
    }

    public class Ship24Data
    {
        public Ship24Tracking[] trackings { get; set; }
    }

    public class Ship24TrackingResult
    {
        public Ship24Error[] errors { get; set; }
        public Ship24Data data { get; set; }
    }

    public interface IShip24ClientService
    {
        string CreateTrackerAndGetTrackingResults(Ship24TrackingInfo trackingInfo);
        string CreateTracker(Ship24TrackingInfo trackingInfo);
    }
}
