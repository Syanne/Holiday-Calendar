using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkConnector
{
    interface IConnector
    {
        List<ItemBase> GetHolidayList();
    }

    interface ICalendarConnector
    {
        void AddItem(ItemBase item);
        void AddItemList(List<ItemBase> itemList);
    }
}
