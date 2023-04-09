using System.Timers;

namespace EveHypernetNotification.Services.Base;

public abstract class TimedService
{
    protected readonly WebApplication App;
    protected readonly System.Timers.Timer Timer;

    /// <param name="app"></param>
    /// <param name="interval">time in ms</param>
    protected TimedService(WebApplication app, double interval)
    {
        App = app;
        Timer = new System.Timers.Timer();
        Timer.Interval = interval;
        Timer.Elapsed += OnTimedEvent;
        Timer.AutoReset = true;
    }

    public void Start()
    {
        Timer.Start();
        OnTimerElapsed();
    }

    private void OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        try
        {
            Task.Run(OnTimerElapsed).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            App.Logger.LogError(exception, "Error in timed service");
        }
    }

    protected abstract Task OnTimerElapsed();
}