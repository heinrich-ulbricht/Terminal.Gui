using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using Xunit.Sdk;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.Core {
	public class MainLoopTests {

		[Fact]
		public void Constructor_Setups_Driver ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			Assert.NotNull (ml.Driver);
		}

		// Idle Handler tests
		[Fact]
		public void AddIdle_Adds_And_Removes ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			Func<bool> fnTrue = () => true;
			Func<bool> fnFalse = () => false;

			ml.AddIdle (fnTrue);
			ml.AddIdle (fnFalse);

			Assert.Equal (2, ml.IdleHandlers.Count);
			Assert.Equal (fnTrue, ml.IdleHandlers [0]);
			Assert.NotEqual (fnFalse, ml.IdleHandlers [0]);

			Assert.True (ml.RemoveIdle (fnTrue));
			Assert.Single (ml.IdleHandlers);

			// BUGBUG: This doesn't throw or indicate an error. Ideally RemoveIdle would either 
			// throw an exception in this case, or return an error.
			// No. Only need to return a boolean.
			Assert.False (ml.RemoveIdle (fnTrue));

			Assert.True (ml.RemoveIdle (fnFalse));

			// BUGBUG: This doesn't throw an exception or indicate an error. Ideally RemoveIdle would either 
			// throw an exception in this case, or return an error.
			// No. Only need to return a boolean.
			Assert.False (ml.RemoveIdle (fnFalse));

			// Add again, but with dupe
			ml.AddIdle (fnTrue);
			ml.AddIdle (fnTrue);

			Assert.Equal (2, ml.IdleHandlers.Count);
			Assert.Equal (fnTrue, ml.IdleHandlers [0]);
			Assert.True (ml.IdleHandlers [0] ());
			Assert.Equal (fnTrue, ml.IdleHandlers [1]);
			Assert.True (ml.IdleHandlers [1] ());

			Assert.True (ml.RemoveIdle (fnTrue));
			Assert.Single (ml.IdleHandlers);
			Assert.Equal (fnTrue, ml.IdleHandlers [0]);
			Assert.NotEqual (fnFalse, ml.IdleHandlers [0]);

			Assert.True (ml.RemoveIdle (fnTrue));
			Assert.Empty (ml.IdleHandlers);

			// BUGBUG: This doesn't throw an exception or indicate an error. Ideally RemoveIdle would either 
			// throw an exception in this case, or return an error.
			// No. Only need to return a boolean.
			Assert.False (ml.RemoveIdle (fnTrue));
		}

		[Fact]
		public void AddIdle_Function_GetsCalled_OnIteration ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				return true;
			};

			ml.AddIdle (fn);
			ml.MainIteration ();
			Assert.Equal (1, functionCalled);
		}

		[Fact]
		public void RemoveIdle_Function_NotCalled ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				return true;
			};

			Assert.False (ml.RemoveIdle (fn));
			ml.MainIteration ();
			Assert.Equal (0, functionCalled);
		}

		[Fact]
		public void AddThenRemoveIdle_Function_NotCalled ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				return true;
			};

			ml.AddIdle (fn);
			Assert.True (ml.RemoveIdle (fn));
			ml.MainIteration ();
			Assert.Equal (0, functionCalled);
		}

		[Fact]
		public void AddTwice_Function_CalledTwice ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				return true;
			};

			ml.AddIdle (fn);
			ml.AddIdle (fn);
			ml.MainIteration ();
			Assert.Equal (2, functionCalled);
			Assert.Equal (2, ml.IdleHandlers.Count);

			functionCalled = 0;
			Assert.True (ml.RemoveIdle (fn));
			Assert.Single (ml.IdleHandlers);
			ml.MainIteration ();
			Assert.Equal (1, functionCalled);

			functionCalled = 0;
			Assert.True (ml.RemoveIdle (fn));
			Assert.Empty (ml.IdleHandlers);
			ml.MainIteration ();
			Assert.Equal (0, functionCalled);
			Assert.False (ml.RemoveIdle (fn));
		}

		[Fact]
		public void False_Idle_Stops_It_Being_Called_Again ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn1 = () => {
				functionCalled++;
				if (functionCalled == 10) {
					return false;
				}
				return true;
			};

			// Force stop if 20 iterations
			var stopCount = 0;
			Func<bool> fnStop = () => {
				stopCount++;
				if (stopCount == 20) {
					ml.Stop ();
				}
				return true;
			};

			ml.AddIdle (fnStop);
			ml.AddIdle (fn1);
			ml.Run ();
			Assert.True (ml.RemoveIdle (fnStop));
			Assert.False (ml.RemoveIdle (fn1));

			Assert.Equal (10, functionCalled);
			Assert.Equal (20, stopCount);
		}

		[Fact]
		public void AddIdle_Twice_Returns_False_Called_Twice ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn1 = () => {
				functionCalled++;
				return false;
			};

			// Force stop if 10 iterations
			var stopCount = 0;
			Func<bool> fnStop = () => {
				stopCount++;
				if (stopCount == 10) {
					ml.Stop ();
				}
				return true;
			};

			ml.AddIdle (fnStop);
			ml.AddIdle (fn1);
			ml.AddIdle (fn1);
			ml.Run ();
			Assert.True (ml.RemoveIdle (fnStop));
			Assert.False (ml.RemoveIdle (fn1));
			Assert.False (ml.RemoveIdle (fn1));

			Assert.Equal (2, functionCalled);
		}

		[Fact]
		public void Run_Runs_Idle_Stop_Stops_Idle ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var functionCalled = 0;
			Func<bool> fn = () => {
				functionCalled++;
				if (functionCalled == 10) {
					ml.Stop ();
				}
				return true;
			};

			ml.AddIdle (fn);
			ml.Run ();
			Assert.True (ml.RemoveIdle (fn));

			Assert.Equal (10, functionCalled);
		}

		// Timeout Handler Tests
		[Fact]
		public void AddTimer_Adds_Removes_NoFaults ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = 100;

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				return true;
			};

			var token = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback);

			Assert.True (ml.RemoveTimeout (token));

			// BUGBUG: This should probably fault?
			// Must return a boolean.
			Assert.False (ml.RemoveTimeout (token));
		}

		[Fact]
		public void AddTimer_Run_Called ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = 100;

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				ml.Stop ();
				return true;
			};

			var token = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback);
			ml.Run ();
			Assert.True (ml.RemoveTimeout (token));

			Assert.Equal (1, callbackCount);
		}

		[Fact]
		public async Task AddTimer_Duplicate_Keys_Not_Allowed ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			const int ms = 100;
			object token1 = null, token2 = null;

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				if (callbackCount == 2) {
					ml.Stop ();
				}
				return true;
			};

			var task1 = new Task (() => token1 = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback));
			var task2 = new Task (() => token2 = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback));
			Assert.Null (token1);
			Assert.Null (token2);
			task1.Start ();
			task2.Start ();
			ml.Run ();
			Assert.NotNull (token1);
			Assert.NotNull (token2);
			await Task.WhenAll (task1, task2);
			Assert.True (ml.RemoveTimeout (token1));
			Assert.True (ml.RemoveTimeout (token2));

			Assert.Equal (2, callbackCount);
		}

		[Fact]
		public void AddTimer_In_Parallel_Wont_Throw ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			const int ms = 100;
			object token1 = null, token2 = null;

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				if (callbackCount == 2) {
					ml.Stop ();
				}
				return true;
			};

			Parallel.Invoke (
				() => token1 = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback),
				() => token2 = ml.AddTimeout (TimeSpan.FromMilliseconds (ms), callback)
			);
			ml.Run ();
			Assert.NotNull (token1);
			Assert.NotNull (token2);
			Assert.True (ml.RemoveTimeout (token1));
			Assert.True (ml.RemoveTimeout (token2));

			Assert.Equal (2, callbackCount);
		}


		class MillisecondTolerance : IEqualityComparer<TimeSpan> {
			int _tolerance = 0;
			public MillisecondTolerance (int tolerance) { _tolerance = tolerance; }
			public bool Equals (TimeSpan x, TimeSpan y) => Math.Abs (x.Milliseconds - y.Milliseconds) <= _tolerance;
			public int GetHashCode (TimeSpan obj) => obj.GetHashCode ();
		}

		[Fact]
		public void AddTimer_Run_CalledAtApproximatelyRightTime ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = TimeSpan.FromMilliseconds (50);
			var watch = new System.Diagnostics.Stopwatch ();

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				watch.Stop ();
				callbackCount++;
				ml.Stop ();
				return true;
			};

			var token = ml.AddTimeout (ms, callback);
			watch.Start ();
			ml.Run ();
			// +/- 100ms should be good enuf
			// https://github.com/xunit/assert.xunit/pull/25
			Assert.Equal<TimeSpan> (ms * callbackCount, watch.Elapsed, new MillisecondTolerance (100));

			Assert.True (ml.RemoveTimeout (token));
			Assert.Equal (1, callbackCount);
		}

		[Fact]
		public void AddTimer_Run_CalledTwiceApproximatelyRightTime ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = TimeSpan.FromMilliseconds (50);
			var watch = new System.Diagnostics.Stopwatch ();

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				if (callbackCount == 2) {
					watch.Stop ();
					ml.Stop ();
				}
				return true;
			};

			var token = ml.AddTimeout (ms, callback);
			watch.Start ();
			ml.Run ();
			// +/- 100ms should be good enuf
			// https://github.com/xunit/assert.xunit/pull/25
			Assert.Equal<TimeSpan> (ms * callbackCount, watch.Elapsed, new MillisecondTolerance (100));

			Assert.True (ml.RemoveTimeout (token));
			Assert.Equal (2, callbackCount);
		}

		[Fact]
		public void AddTimer_Remove_NotCalled ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = TimeSpan.FromMilliseconds (50);

			// Force stop if 10 iterations
			var stopCount = 0;
			Func<bool> fnStop = () => {
				stopCount++;
				if (stopCount == 10) {
					ml.Stop ();
				}
				return true;
			};
			ml.AddIdle (fnStop);

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				return true;
			};

			var token = ml.AddTimeout (ms, callback);
			Assert.True (ml.RemoveTimeout (token));
			ml.Run ();
			Assert.Equal (0, callbackCount);
		}

		[Fact]
		public void AddTimer_ReturnFalse_StopsBeingCalled ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			var ms = TimeSpan.FromMilliseconds (50);

			// Force stop if 10 iterations
			var stopCount = 0;
			Func<bool> fnStop = () => {
				Thread.Sleep (10); // Sleep to enable timer to fire
				stopCount++;
				if (stopCount == 10) {
					ml.Stop ();
				}
				return true;
			};
			ml.AddIdle (fnStop);

			var callbackCount = 0;
			Func<MainLoop, bool> callback = (MainLoop loop) => {
				callbackCount++;
				return false;
			};

			var token = ml.AddTimeout (ms, callback);
			ml.Run ();
			Assert.Equal (1, callbackCount);
			Assert.Equal (10, stopCount);
			Assert.False (ml.RemoveTimeout (token));
		}

		// Invoke Tests
		// TODO: Test with threading scenarios
		[Fact]
		public void Invoke_Adds_Idle ()
		{
			var ml = new MainLoop (new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var actionCalled = 0;
			ml.Invoke (() => { actionCalled++; });
			ml.MainIteration ();
			Assert.Equal (1, actionCalled);
		}

		[Fact]
		public void Internal_Tests ()
		{
			var testMainloop = new TestMainloop ();
			var mainloop = new MainLoop (testMainloop);
			Assert.Empty (mainloop.timeouts);
			Assert.Empty (mainloop.idleHandlers);
			Assert.NotNull (new MainLoop.Timeout () {
				Span = new TimeSpan (),
				Callback = (_) => true
			});
		}

		private class TestMainloop : IMainLoopDriver {
			private MainLoop mainLoop;

			public bool EventsPending (bool wait)
			{
				throw new NotImplementedException ();
			}

			public void MainIteration ()
			{
				throw new NotImplementedException ();
			}

			public void Setup (MainLoop mainLoop)
			{
				this.mainLoop = mainLoop;
			}

			public void Wakeup ()
			{
				throw new NotImplementedException ();
			}
		}

		// TODO: EventsPending tests
		// - wait = true
		// - wait = false

		// TODO: Add IMainLoop tests

		volatile static int tbCounter = 0;
		static ManualResetEventSlim _wakeUp = new ManualResetEventSlim (false);

		private static void Launch (Random r, TextField tf, int target)
		{
			Task.Run (() => {
				Thread.Sleep (r.Next (2, 4));
				Application.MainLoop.Invoke (() => {
					tf.Text = $"index{r.Next ()}";
					Interlocked.Increment (ref tbCounter);
					if (target == tbCounter) {
						// On last increment wake up the check
						_wakeUp.Set ();
					}
				});
			});
		}

		private static void RunTest (Random r, TextField tf, int numPasses, int numIncrements, int pollMs)
		{
			for (int j = 0; j < numPasses; j++) {

				_wakeUp.Reset ();
				for (var i = 0; i < numIncrements; i++) {
					Launch (r, tf, (j + 1) * numIncrements);
				}


				while (tbCounter != (j + 1) * numIncrements) // Wait for tbCounter to reach expected value
				{
					var tbNow = tbCounter;
					_wakeUp.Wait (pollMs);
					if (tbCounter == tbNow) {
						// No change after wait: Idle handlers added via Application.MainLoop.Invoke have gone missing
						Application.MainLoop.Invoke (() => Application.RequestStop ());
						throw new TimeoutException (
							$"Timeout: Increment lost. tbCounter ({tbCounter}) didn't " +
							$"change after waiting {pollMs} ms. Failed to reach {(j + 1) * numIncrements} on pass {j + 1}");
					}
				};
			}
			Application.MainLoop.Invoke (() => Application.RequestStop ());
		}

		[Fact]
		[AutoInitShutdown]
		public async Task InvokeLeakTest ()
		{
			Random r = new ();
			TextField tf = new ();
			Application.Top.Add (tf);

			const int numPasses = 10;
			const int numIncrements = 10000;
			const int pollMs = 20000;

			var task = Task.Run (() => RunTest (r, tf, numPasses, numIncrements, pollMs));

			// blocks here until the RequestStop is processed at the end of the test
			Application.Run ();

			await task; // Propagate exception if any occurred

			Assert.Equal ((numIncrements * numPasses), tbCounter);
		}
	}
}
