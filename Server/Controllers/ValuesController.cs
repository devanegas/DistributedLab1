using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shared;
using System.Threading;

namespace Server.Controllers
{
    public static class Counter
    {
        private static long currentRequests;
        public static long CurrentRequests
        {
            get { return Interlocked.Read(ref currentRequests); }
        }

        internal static void Increment()
        {
            Interlocked.Increment(ref currentRequests);
        }

        internal static void Decrement()
        {
            Interlocked.Decrement(ref currentRequests);
        }

    }


    public static class WorkerManager
    {
        public static int NumberofFailures = 10;
        public static double  WaitAfterFailure = 10;
        public static double WaitAfterSuccess = 10;

        public static Queue<IEnumerable<Job>> WorkQueue { get; private set; }
        public static Random random = new Random();

        static WorkerManager()
        {
            
            WorkQueue = new Queue<IEnumerable<Job>>();

            var jobSource = new[]
            {
                new Job(new []{new MessageDto("A","1"),
                               new MessageDto("B","2"),
                               new MessageDto("C","3"),
                               new MessageDto("D","4"),
                               new MessageDto("E","5")}),
                new Job(new []{new MessageDto("B","6"),
                               new MessageDto("D","7"),
                               new MessageDto("E","8"),
                               new MessageDto("F","1"),
                               new MessageDto("G","2")})
             };

            for (int i = 0; i < jobSource.Length; i++)
            {
                var shuffledJobs = RandomPermutation(jobSource);
                WorkQueue.Enqueue(shuffledJobs);
            }
        }

        

        public static IEnumerable<T> RandomPermutation<T>(IEnumerable<T> sequence)
        {
            T[] retArray = sequence.ToArray();


            for (int i = 0; i < retArray.Length - 1; i += 1)
            {
                int swapIndex = random.Next(i, retArray.Length);
                if (swapIndex != i)
                {
                    T temp = retArray[i];
                    retArray[i] = retArray[swapIndex];
                    retArray[swapIndex] = temp;
                }
            }

            return retArray;
        }
    }




    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<Job>> Get()
        {
            var jobs = WorkerManager.WorkQueue.Dequeue();
            return new ActionResult<IEnumerable<Job>>(jobs);
        }

        public ActionResult<Job> DoJob([FromBody]Job job)
        {
            Counter.Increment();
            Thread.Sleep(500);

            var dictionary = new Dictionary<string, FileStream>();
            var messageQueue = new Queue<MessageDto>(job.Messages);
            var failures = 0;
            var couldOpen = true;
            while (messageQueue.Count > 0)
            {
                var message = messageQueue.Peek();
                try
                {
                    FileStream stream = System.IO.File.OpenWrite(message.Key);
                    stream.Lock(0, stream.Length);
                    dictionary.Add(message.Key, stream);
                    messageQueue.Dequeue();
                }
                catch (System.IO.IOException)
                {
                    if(failures++ > WorkerManager.NumberofFailures)
                    {
                        couldOpen = false;
                        break;
                    }

                    Thread.Sleep((int)WorkerManager.WaitAfterFailure*1000);
                }
            }

            if (couldOpen)
            {
                Thread.Sleep((int)WorkerManager.WaitAfterSuccess*1000);
                foreach (var item in dictionary)
                {
                    using (var writer = new StreamWriter(item.Value))
                    {
                        var msg = job.Messages.Single(m => m.Key == item.Key);
                        writer.WriteLineAsync(msg.Value);
                    }
                }
                Console.WriteLine($"Success!  Saved all values from {job.Requestor}");
                job.ResultMessage = $"Saved on server at {DateTime.Now} (Current Users: {Counter.CurrentRequests})";
            }
            else
            {
                job.ResultMessage = "Fail";
            }

            job.Success = couldOpen;

            foreach (var item in dictionary)
            {
                item.Value.Close();
                item.Value.Unlock(0, item.Value.Length);
            }
            Counter.Decrement();
            return job;
        }

    }  
}
