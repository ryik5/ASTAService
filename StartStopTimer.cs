using System;

namespace ASTAService
{
    internal interface IStartStopTimer
    {
        void WaitTime();

        string GetTime();
    }

    internal class StartStopTimer : IStartStopTimer
    {
        private int _seconds = 0;
        private string _sizeTime;
        private string time1 = "";
        private string time2 = "";

        /// <summary>
        /// if sizeTime="seconds" then sizeTime is milliseconds
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="sizeTime"></param>
        public StartStopTimer(int seconds, string sizeTime = "seconds")
        {
            _seconds = seconds;
            _sizeTime = sizeTime;
        }

        public void WaitTime() //MidleLoad not Precision
        {
            time1 = DateTime.Now.ToString("yyyyMMddhhMMssmmm");//.ToYYYYMMDDHHMMSSmmm();
            WaitSeconds(_seconds);
            time2 = DateTime.Now.ToString("yyyyMMddhhMMssmmm");//.ToYYYYMMDDHHMMSSmmm();
        }

        public string GetTime()
        {
            return time1 + "\n" + time2;
        }

        private void WaitSeconds(int seconds) //high precision timer
        {
            int interval = 10000;
            //No need to risk overflowing an int
            //No need to count iterations, just set them
            //Really need to account for CPU speed, etc. though, as a
            //CPU twice as fast runs this loop twice as many times,
            //while a slow one may greatly overshoot the desired time
            //interval. Iterations too high = overshoot, too low = excessive overhead

            var swt = new System.Threading.SpinWait();
            if (_sizeTime != "seconds")
            { interval = 100; }

            while (swt.Count < seconds * interval)
            {
                // The NextSpinWillYield property returns true if
                // calling sw.SpinOnce() will result in yielding the
                // processor instead of simply spinning.
                swt.SpinOnce();
            }
        }
    }

    /* использование
                 int num = 0;
            Int32.TryParse(textBoxGroup.Text, out num);
            Task.Run(() => MessageBox.Show("seconds: " + num + "\nfrequencies: " + System.Diagnostics.Stopwatch.Frequency.ToString()));

            IStartStopTimer startStopTimerA = new StartStopTimerA(num);

            startStopTimerA.WaitTime();
            Task.Run(() => MessageBox.Show("1\n" + startStopTimerA.GetTime()));

            IStartStopTimer startStopTimerB = new StartStopTimerB(num);
            startStopTimerB.WaitTime();
            Task.Run(() => MessageBox.Show("2\n" + startStopTimerB.GetTime()));

            IStartStopTimer startStopTimerC = new StartStopTimerC(num);
            startStopTimerC.WaitTime();
            Task.Run(() => MessageBox.Show("3\n" + startStopTimerC.GetTime()));

            IStartStopTimer startStopTimerD = new StartStopTimerD(num);
            startStopTimerD.WaitTime();
            Task.Run(() => MessageBox.Show("4\n" + startStopTimerD.GetTime()));

         */

    /*
// SetUpTimer(new TimeSpan(1, 1, 0, 0)); //runs on 1st at 1:00:00
private void SetUpTimer(TimeSpan alertTime)
{
DateTime current = DateTime.Now;
TimeSpan timeToGo = alertTime - current.TimeOfDay;
if (timeToGo < TimeSpan.Zero)
{
    return;//time already passed
}
timer = new System.Threading.Timer(x =>
 {
     SelectMailingDoAction();
 }, null, timeToGo, System.Threading.Timeout.InfiniteTimeSpan);
}
*/

    /*
string stime1 = "18:40";
string stime2 = "19:35";
DateTime t1 = DateTime.Parse(stime1);
DateTime t2 = DateTime.Parse(stime2);
TimeSpan ts = t2 - t1;
int minutes = (int)ts.TotalMinutes;
int seconds = (int)ts.TotalSeconds;
*/

    /*
        class StartStopTimerA : IStartStopTimer
        {
            int _seconds = 0;
            string time1 = "";
            string time2 = "";

            public StartStopTimerA(int seconds)
            {
                _seconds = seconds;
            }

            public void WaitTime() //MiserableLoad HighPrecision
            {
                time1 = DateTime.Now.ToYYYYMMDDHHMMSSmmm();
                WaitSeconds(_seconds);
                time2 = DateTime.Now.ToYYYYMMDDHHMMSSmmm();
            }

            public string GetTime()
            {
                return time1 + "\n" + time2;
            }

            private void WaitSeconds(int seconds) //high precision timer
            {
                System.Threading.Thread.Sleep(seconds * 1000);
            }
        }

        class StartStopTimerB : IStartStopTimer
        {
            int _seconds = 0;
            string time1 = "";
            string time2 = "";

            public StartStopTimerB(int seconds)
            {
                _seconds = seconds;
            }

            public void WaitTime()//LowLoad has problem
            {
                time1 = DateTime.Now.ToYYYYMMDDHHMMSSmmm();
                WaitSeconds(_seconds);
                time2 = DateTime.Now.ToYYYYMMDDHHMMSSmmm();
            }

            public string GetTime()
            {
                return time1 + "\n" + time2;
            }

            private void WaitSeconds(int seconds) //high precision timer
            {
                System.Threading.Thread.SpinWait(seconds * 26000000);
            }
        }
        */
    /*
    class StartStopTimerD : IStartStopTimer
    {
        int _seconds = 0;
        string time1 = "";
        string time2 = "";

        public StartStopTimerD(int seconds)
        {
            _seconds = seconds;
        }

        public void WaitTime() //HighLoad HighPrecision
        {
            time1 = DateTime.Now.ToYYYYMMDDHHMMSSmmm();
            WaitSeconds(_seconds);
            time2 = DateTime.Now.ToYYYYMMDDHHMMSSmmm();
        }

        public string GetTime()
        {
            return time1 + "\n" + time2;
        }

        private void WaitSeconds(int seconds) //high precision timer
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            //No need to risk overflowing an int
            while (true)
            {
                //No need to stop the stopwatch, simply "mark the lap"
                if (sw.ElapsedMilliseconds >= seconds * 1000)
                {
                    break;
                }
            }
            sw.Stop();
        }
    }
    */
}
