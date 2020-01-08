Here is a Console Application, which is, you will be suprized, doing some job. 
And the problem is that the application, probably, consumes a lot of RAM and CPU time.
From time to time it takes **40-90% CPU**, **210-230 threads**, **RAM** constantly grouth up to **40MB+**.

What it does:
* writes logs to \Output\data.txt file (time, ID of process, etc.);
* writes a report with amount of entries for each process by ID to \Output\Statistics.txt;
* automatically stop all processes after 5 minutes;

**The task for you**:
* minimize number of threads created;
* remove reasons of exceptions are throwing to Console;
* avoid RAM consuming.
