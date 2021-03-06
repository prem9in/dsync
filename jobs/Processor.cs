namespace jobs
{
	using System;
    using Azure.Storage.Queues;
	using Azure.Storage.Queues.Models;
    using Microsoft.Extensions.Logging;  
    using System.Threading;
    using System.Threading.Tasks;
	using System.Collections.Generic;

    public class Processor<T> : IDisposable
	{
		private bool _disposed;
		private CancellationTokenSource _cancellationSource;
		private readonly object _lockObject = new object();
		private readonly QueueClient _queueClient;
		private readonly Func<T, Task<bool>> _action;
		private readonly ILogger _logger;
		private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

		public Processor(QueueClient queueClient, Func<T, Task<bool>> action, ILogger logger)
		{			
			_queueClient = queueClient;
			_action = action;
			_logger = logger;
		}

		public async Task Start()
		{
			ThrowIfDisposed();
			await semaphore.WaitAsync();
			try
			{				
				_cancellationSource = new CancellationTokenSource();
				await ReceiveAndProcessMessagesAsync(_cancellationSource.Token);
			}
			finally
			{
				// guaranteed execution of release of semaphore slim
                semaphore.Release();
			}
		}

		public async Task Stop()
		{
			await semaphore.WaitAsync();
			try
			{
				using (_cancellationSource)
				{
					if (_cancellationSource != null)
					{
						_cancellationSource.Cancel();
						_cancellationSource = null;
					}
				}				
			}
			finally
			{
				// guaranteed execution of release of semaphore slim
                semaphore.Release();
			}
		}

		private async Task ReceiveAndProcessMessagesAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(TimeSpan.FromSeconds(1));
					var msg = await _queueClient.ReceiveMessagesAsync(4, TimeSpan.FromMinutes(10), cancellationToken).ConfigureAwait(false);
					if (msg != null && msg.Value != null && msg.Value.Length > 0)
					{
						var runTasks = new List<Task<bool>>();
						for (int i = 0; i < msg.Value.Length; i++)
						{
							_logger.LogInformation($"Received message : {msg.Value[i].MessageId}");
							var trun = ProcessMessageAsync(msg.Value[i]); 
							runTasks.Add(trun);   				
							_logger.LogInformation($"Processed message : {msg.Value[i].MessageId}");
						}

						await Task.WhenAll(runTasks);
					}					
				}
				catch (Exception e)
				{
					_logger.LogError(e.ToString());
				}				
			}
		}

		private async Task<bool> ProcessMessageAsync(QueueMessage message)
		{			
			try
			{
				var content = message.Body;
				var messageObject = content.ToObjectFromJson<T>();
				return await _action(messageObject);
			}
			catch (Exception e)
			{
				_logger.LogInformation($"Error processing message : {message.MessageId}");
				_logger.LogError(e.ToString());
			}
			finally
			{
				//ReleaseMessage
				_logger.LogInformation($"Release message : {message.MessageId}");
				await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);				
			}

			return true;
		}

		private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("Processor");
        }

		/// <summary>
        /// Disposes the resources used by the processor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the resources used by the processor.
        /// </summary>
        protected virtual async void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    await Stop();
                    _disposed = true;                    
                }
            }
        }

        ~Processor()
        {
            Dispose(false);
        }
	}
}