using System.Timers;

namespace NewDalgs.Handlers
{
    class TimerHandler
    {
        private Timer _timer = new Timer();

        public void ScheduleTask(ElapsedEventHandler task, int delay)
        {
            _timer.Interval = delay;
            _timer.Elapsed += task;
            _timer.Start();
        }

        public void Stop()
        {
            if (_timer.Enabled)
            {
                // TODO meh - this triggers again the task
                _timer.Stop();
            }
        }
    }
}
