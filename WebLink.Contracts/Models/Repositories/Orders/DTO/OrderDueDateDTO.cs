using System;

namespace WebLink.Contracts.Models
{
    public class OrderDueDateDTO
    {
        public int OrderID { get; set; }
        public DateTime? OrderDueDate { get; set; }
    }
}
