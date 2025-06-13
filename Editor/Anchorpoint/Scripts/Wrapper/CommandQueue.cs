using System;
using System.Collections.Generic;
using System.Threading;

namespace Anchorpoint.Wrapper
{
    /// <summary>
    /// Manages a thread-safe queue of commands to be executed sequentially.
    /// Ensures that only one command is processed at a time.
    /// </summary>
    public class CommandQueue
    {
        private Queue<Action> _commandQueue = new Queue<Action>(); // Queue to hold the commands
        private bool _isProcessing = false; // Flag to check if processing is in progress
        private object _lock = new object(); // Lock object for thread-safety

        /// <summary>
        /// Adds a new command to the queue. Starts processing if not already running.
        /// </summary>
        /// <param name="command">The action to be enqueued and executed.</param>
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

        /// <summary>
        /// Processes the next command in the queue using the ThreadPool.
        /// Continues processing subsequent commands until the queue is empty.
        /// </summary>
        private void ProcessNextCommand()
        {
            lock (_lock)
            {
                if (_commandQueue.Count > 0)
                {
                    Action command = _commandQueue.Dequeue();
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        command(); // Execute the command
                        ProcessNextCommand(); // Recursively process the next command
                    });
                }
                else
                {
                    _isProcessing = false; // Reset the flag when the queue is empty
                }
            }
        }
    }
}