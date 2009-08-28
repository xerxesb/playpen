using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace playpen
{
    [TestFixture]
    public class TestSyncronisation
    {
        [Test]
        public void TestSynchronise()
        {
            new SyncTester().Test();
            Thread.Sleep(500);
        }
    }

    public class SomeObject
    {
        public string DoSomething()
        {
            throw new AbandonedMutexException("you cunto");
            return "blah";
        }
    }

    public class ThreadedObject
    {
        private readonly Action _methodWhichCallsSomeObject;

        public ThreadedObject(Action methodWhichCallsSomeObject)
        {
            _methodWhichCallsSomeObject = methodWhichCallsSomeObject;
        }

        public void DoSomethingThreadedly()
        {
            _methodWhichCallsSomeObject();
        }
    }

    public class SyncTester
    {
        private SomeObject _someObject = new SomeObject();

        public void Test()
        {
            var threadedObject = new ThreadedObject(MethodWhichCallsSomeObject);

            var t = new Thread(threadedObject.DoSomethingThreadedly);
            t.Start();
        }

        private void MethodWhichCallsSomeObject()
        {
            _someObject.DoSomething();
        }
    }
}
