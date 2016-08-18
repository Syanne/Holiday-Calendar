using System;
using Windows.ApplicationModel.Background;

namespace Calendar.Services
{
    public partial class SharedSettingsServices
    {
        public bool IsTileSet = false;
        public bool IsToastSet = false;
        public bool IsGoogleService = false;
        public bool IsSmartTileSet = false;
        
        public async void BackgroundTaskCreator(string name, string entryPoint, uint time)
        {
            //clean
            foreach (var task in BackgroundTaskRegistration.AllTasks)
                if (task.Value.Name == name)
                    task.Value.Unregister(true);

            //register
            var timerTrigger = new TimeTrigger(time, false);

            await BackgroundExecutionManager.RequestAccessAsync();

            var builder = new BackgroundTaskBuilder();

            builder.Name = name;
            builder.TaskEntryPoint = entryPoint;
            builder.SetTrigger(timerTrigger);
            builder.AddCondition(new SystemCondition(SystemConditionType.UserPresent));

            BackgroundTaskRegistration tTask = builder.Register();
        }

        public void SmartTileController(bool needEnableService, string daysAverage)
        {
            string name = "SmartTileBackgroundTask";
            string entryPoint = "BackgroundUpdater.SmartTileBackgroundTask";

            DataManager.SmartTileFile(daysAverage);
            TileEnableController(name, entryPoint, needEnableService, ref IsSmartTileSet);
        }

        public void TileEnableController(string name, string entryPoint, bool needEnableService, ref bool tileIdentifer)
        {
            if (!tileIdentifer)
            {
                if (needEnableService)
                    BackgroundTaskCreator(name, entryPoint, 15);
                else
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                        if (task.Value.Name == name)
                            task.Value.Unregister(true);
                }
            }

            tileIdentifer = false;
        }

    }
}
