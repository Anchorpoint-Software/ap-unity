using System;
using System.Collections.Generic;
using System.Threading;

namespace Anchorpoint.Wrapper
{
    public class CommandQueue
    {
        private Queue<Action> _commandQueue = new Queue<Action>();
        private bool _isProcessing = false;
        private object _lock = new object();

        // Add a new command to the queue
        public void EnqueueCommand(Action command)
        {
            lock (_lock)
            {
                _commandQueue.Enqueue(command);
                if (!_isProcessing)
                {
                    _isProcessing = true;
                    ProcessNextCommand();
                }
            }
        }

        // Process the next command in the queue
        private void ProcessNextCommand()
        {
            lock (_lock)
            {
                if (_commandQueue.Count > 0)
                {
                    Action command = _commandQueue.Dequeue();
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        command();
                        ProcessNextCommand(); // Process the next command after this one finishes
                    });
                }
                else
                {
                    _isProcessing = false; // No more commands to process
                }
            }
        }
    }
}