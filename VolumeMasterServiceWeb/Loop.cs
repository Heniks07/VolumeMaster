namespace VolumeMasterServiceWeb;

public partial class Worker
{
    private async Task Loop(CancellationToken stoppingToken, DateTime lastConfigUpdate)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            //Check if the config file was updated. If so, update the config and set the lastConfigUpdate to now, so it doesn't update again for the next 10 seconds
            lastConfigUpdate = CheckConfigForUpdates(lastConfigUpdate);

            try
            {
                //Wait for approx. the time it takes the Arduino to send the volume data
                Thread.Sleep(_timeout);
                //Get the volume data from the Arduino
                var changes = VolumeMasterCom.GetVolume();
                //Get the indexes that changed, the new volume, the actual volume and if the slider is manually overridden
                var indexesChanged = changes.SliderIndexesChanged;
                var volume = changes.Volume;
                var actualVolume = changes.ActualVolume;
                var overrideActive = changes.OverrideActive;

                //Set the current data to the last data which is used in the API calls to ensure that the api can actually return something.
                //Otherwise, there is a race condition where the data might not be set yet.
                LastVolume.Clear();
                LastVolume.AddRange(volume);
                LastActualVolume.Clear();
                LastActualVolume.AddRange(actualVolume);
                LastOverrideActive.Clear();
                LastOverrideActive.AddRange(overrideActive);


                //Get the current config
                var config = VolumeMasterCom.Config;


                ChangeVolume(indexesChanged, volume, config, AudioApi, actualVolume, overrideActive);
            }
            catch (UnauthorizedAccessException)
            {
                //This error occurs when the Arduino is disconnected. Log the error and wait for 1 second before trying to reconnect
                await Task.Delay(1000, stoppingToken);
                _logger?.LogWarning("Please reconnect the Arduino");
            }
            catch (Exception? exception)
            {
                //Log the error and continue
                _logger?.LogError(exception, "Error while changing volume");
            }
        }
    }

    private DateTime CheckConfigForUpdates(DateTime lastConfigUpdate)
    {
        if ((DateTime.Now - lastConfigUpdate).TotalMilliseconds > 10000)
        {
            lastConfigUpdate = DateTime.Now;
            VolumeMasterCom.ConfigHelper();
        }

        return lastConfigUpdate;
    }
}