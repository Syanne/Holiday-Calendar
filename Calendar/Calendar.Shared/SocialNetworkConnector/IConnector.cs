using Calendar.Models;
using System.Collections.Generic;


namespace Calendar.SocialNetworkConnector
{
    public interface IConnector
    {
        List<ItemBase> GetHolidayList();
    }

    public interface ICalendarConnector
    {
        void AddItem(ItemBase item);
        void AddItemList(List<ItemBase> itemList);
    }
}
