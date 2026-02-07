using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{

    public class NextOrderState
    {
        public OrderStatus State { get; set; }

        public string Description { get { return State.GetText(); } }

        public bool IsDefault { get; set; }

        public bool IsCurrent { get; set; }

        //public bool IsEnableUserInteraction { get; set; } // no solve issues, can be  removed

        public NextOrderStateDirection Direction { get; set; }



    }

    public enum NextOrderStateDirection
    {
        None,
        Backward,
        Forward
    }


    public enum NextOrderStateIncludeCurrentOption
    {
        None,
        AtFirst,
        AtLast

    }

    public static class OrderUtil
    {
        // order flow configuration
        public static IEnumerable<NextOrderState> NextStates(OrderStatus os, NextOrderStateIncludeCurrentOption includeOption = NextOrderStateIncludeCurrentOption.None)
        {
            var ret = new List<NextOrderState>();

            switch (os)
            {
                case OrderStatus.Received:
                    ret.Add( new NextOrderState() { State = OrderStatus.Processed, IsDefault = true });
                    
                    break;
                case OrderStatus.Processed:

                    ret.Add(new NextOrderState() { State = OrderStatus.InFlow, IsDefault = true });

                    break;

                case OrderStatus.InFlow:

                    ret.Add(new NextOrderState() { State = OrderStatus.Validated, IsDefault = true });

                    break;

                case OrderStatus.Validated:

                    ret.Add(new NextOrderState() { State = OrderStatus.Billed, IsDefault = true });
                    ret.Add(new NextOrderState() { State = OrderStatus.InFlow, IsDefault = false/*, IsEnableUserInteraction = true*/ });

                    break;

                case OrderStatus.Billed:

                    ret.Add(new NextOrderState() { State = OrderStatus.ProdReady, IsDefault = true });
                    ret.Add(new NextOrderState() { State = OrderStatus.InFlow, IsDefault = false/*, IsEnableUserInteraction = true*/ });

                    break;


                case OrderStatus.ProdReady:

                    ret.Add(new NextOrderState() { State = OrderStatus.Printing, IsDefault = true });
                    ret.Add(new NextOrderState() { State = OrderStatus.InFlow, IsDefault = false/*, IsEnableUserInteraction = true*/ });
                    ret.Add(new NextOrderState() { State = OrderStatus.Validated, IsDefault = false/*, IsEnableUserInteraction = true*/ });
                    ret.Add(new NextOrderState() { State = OrderStatus.Billed, IsDefault = false/*, IsEnableUserInteraction = true*/ });

                    break;

                case OrderStatus.Printing:

                    ret.Add(new NextOrderState() { State = OrderStatus.Completed, IsDefault = true });
                    ret.Add(new NextOrderState() { State = OrderStatus.InFlow, IsDefault = false });
                    ret.Add(new NextOrderState() { State = OrderStatus.Validated, IsDefault = false });
                    ret.Add(new NextOrderState() { State = OrderStatus.Billed, IsDefault = false });

                    break;

                case OrderStatus.Cancelled:
                    ret.Add(new NextOrderState() { State = OrderStatus.Received, IsDefault = true });
                    break;

            }
            if (os != OrderStatus.Cancelled)
            {
                ret.Add(new NextOrderState() { State = OrderStatus.Cancelled, IsDefault = false });
            }

            _IncludeCurrent(ret, os, includeOption);

            return ret;
        }

        // user order flow available
        public static IEnumerable<NextOrderState> NextManualStates(OrderStatus os, NextOrderStateIncludeCurrentOption includeOption = NextOrderStateIncludeCurrentOption.None)
        {
            var ret = new List<NextOrderState>();

            switch (os)
            {
                case OrderStatus.Received:
                    ret.Add(new NextOrderState() { State = OrderStatus.Processed, IsDefault = true });

                    break;
                case OrderStatus.Processed:

                    ret.Add(new NextOrderState() { State = OrderStatus.Received, IsDefault = true });
                    ret.Add(new NextOrderState() { State = OrderStatus.InFlow, IsDefault = true });

                    break;

                case OrderStatus.InFlow:

                    ret.Add(new NextOrderState() { State = OrderStatus.Validated, IsDefault = true });

                    break;

                case OrderStatus.Validated:

                    ret.Add(new NextOrderState() { State = OrderStatus.Billed, IsDefault = true });
                    ret.Add(new NextOrderState() { State = OrderStatus.InFlow, IsDefault = false });

                    break;

                case OrderStatus.Billed:

                    ret.Add(new NextOrderState() { State = OrderStatus.ProdReady, IsDefault = true });
                    ret.Add(new NextOrderState() { State = OrderStatus.InFlow, IsDefault = false });

                    break;


                case OrderStatus.ProdReady:

                    //ret.Add(new NextOrderState() { State = OrderStatus.Printing, IsDefault = true });
                    ret.Add(new NextOrderState() { State = OrderStatus.InFlow, IsDefault = false });
                    ret.Add(new NextOrderState() { State = OrderStatus.Validated, IsDefault = false }); // regenerar printpackage
                    ret.Add(new NextOrderState() { State = OrderStatus.Billed, IsDefault = false });

                    break;

                case OrderStatus.Printing:

                    ret.Add(new NextOrderState() { State = OrderStatus.Completed, IsDefault = true });
                    ret.Add(new NextOrderState() { State = OrderStatus.InFlow, IsDefault = false });
                    ret.Add(new NextOrderState() { State = OrderStatus.Validated, IsDefault = false });
                    ret.Add(new NextOrderState() { State = OrderStatus.Billed, IsDefault = false });

                    break;

            }

            ret.Add(new NextOrderState() { State = OrderStatus.Cancelled, IsDefault = false });

            _IncludeCurrent(ret, os, includeOption);

            return ret;
        }


        private static void _IncludeCurrent(IList<NextOrderState> list, OrderStatus current, NextOrderStateIncludeCurrentOption includeOption)
        {

            if (NextOrderStateIncludeCurrentOption.None == includeOption)
            {
                return;
            }


            var currentNextOrderState = new NextOrderState()
            {
                State = current,
                IsDefault = false,
                IsCurrent = true,
                Direction = NextOrderStateDirection.None
            };

            if (NextOrderStateIncludeCurrentOption.AtFirst == includeOption)
            {
                list.Insert(0, currentNextOrderState);
            }

            if ((NextOrderStateIncludeCurrentOption.AtLast == includeOption))
            {
                list.Add(currentNextOrderState);
                
            }

            


        }
    }


}
