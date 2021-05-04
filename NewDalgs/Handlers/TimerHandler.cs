using System;
using System.Timers;

namespace NewDalgs.Handlers
{
    class TimerHandler
    {
        private Timer _timer = new Timer();

        public TimerHandler(Action<object, object> task)
        {
            _timer.AutoReset = false;
            _timer.Elapsed += new ElapsedEventHandler(task);
        }

        public void ScheduleTask(int delay)
        {
            _timer.Interval = delay;
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
