using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Calendar.SocialNetworkConnector;

namespace CalendarTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CanGetData()
        {
            //arrange
            GoogleCalendarConnector conn = new GoogleCalendarConnector();

            //act
            var collection = conn.GetHolidayList();

            //assert
            Assert.IsNotNull(collection);

        }
    }
}
