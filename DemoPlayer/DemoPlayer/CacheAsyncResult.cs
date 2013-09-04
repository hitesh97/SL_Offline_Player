using System;
using System.Threading;

namespace DemoPlayer
{
	public class CacheAsyncResult : IAsyncResult
	{
		public string strUrl { get; set; }

		public object AsyncState { get; private set; }

		public WaitHandle AsyncWaitHandle { get { return _completeEvent; } }

		public bool CompletedSynchronously { get; private set; }

		public bool IsCompleted { get; private set; }

		// Contains all the output result of the GetChunk API
		public Object Result { get; private set; }

		internal TimeSpan Timestamp { get; private set; }

		/// <summary>
		/// Callback function when GetChunk is completed. Used in asynchronous mode only.
		/// Should be null for synchronous mode.
		/// </summary>
		private AsyncCallback _callback;

		/// <summary>
		/// Event is used to signal the completion of the operation
		/// </summary>
		private ManualResetEvent _completeEvent = new ManualResetEvent(false);

		/// <summary>
		/// Called when the operation is completed
		/// </summary>
		public void Complete(Object result, bool completedSynchronously)
		{
			Result = result;
			CompletedSynchronously = completedSynchronously;

			IsCompleted = true;
			_completeEvent.Set();

			if (null != _callback) { ;  }
		}

	}
}
