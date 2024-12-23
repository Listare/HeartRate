using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace HeartRate
{
	static class AsyncUtils
	{
		public static Task<T> AsTask<T>(this IAsyncOperation<T> operation)
		{
			var tcs = new TaskCompletionSource<T>();
			operation.Completed = (asyncInfo, asyncStatus) =>
			{
				switch (asyncStatus)
				{
					case AsyncStatus.Completed:
						tcs.SetResult(asyncInfo.GetResults());
						break;
					case AsyncStatus.Error:
						tcs.SetException(new Exception(asyncInfo.ErrorCode.Message));
						break;
					case AsyncStatus.Canceled:
						tcs.SetCanceled();
						break;
				}
			};
			return tcs.Task;
		}
	}
}
