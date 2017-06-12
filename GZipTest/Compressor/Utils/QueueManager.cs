using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    /// <summary>
    /// Имплементация Producer/Consumer
    /// </summary>
    public class QueueManager
    {
        public bool closed = false;

        private int idCounter = 0;
        private int queueCounter = 0;
        private Queue<Block> queue = new Queue<Block>();
        private int maxSize;


        public QueueManager(int maxSize)
        {
            this.maxSize = maxSize;
        }

        public void Close()
        {
            lock (queue)
            {
                closed = true;
                Monitor.PulseAll(queue);
            }
        }

        public void Enqueue(Block chunk)
        {
            int id = chunk.ID;
            lock (queue)
            {
                while (queueCounter >= maxSize || id != idCounter)
                {
                    Monitor.Wait(queue);
                }
                queue.Enqueue(chunk);
                idCounter++;
                queueCounter++;
                Monitor.PulseAll(queue);
            }
        }

        public void EnqueueBytes(byte[] buffer)
        {
            lock (queue)
            {
                while (queueCounter >= maxSize)
                {
                    Monitor.Wait(queue);
                }
                Block chunk = new Block(idCounter, buffer);
                queue.Enqueue(chunk);
                idCounter++;
                queueCounter++;
                Monitor.PulseAll(queue);
            }
        }

        public bool TryDequeue(out Block chunk)
        {
            lock (queue)
            {
                while (queueCounter == 0)
                {
                    if (closed)
                    {
                        chunk = new Block(0, new byte[0]);
                        return false;
                    }
                    Monitor.Wait(queue);
                }
                chunk = queue.Dequeue();
                queueCounter--;

                Monitor.PulseAll(queue);

                return true;
            }
        }
    }
}