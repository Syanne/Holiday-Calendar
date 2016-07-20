using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkConnector
{
    public interface IConnector
    {
        IList<ItemBase> GetHolidayList();
    }

    public interface ICalendarConnector
    {
        void AddItem(ItemBase item);
        void AddItemList(IList<ItemBase> itemList);
    }
}
