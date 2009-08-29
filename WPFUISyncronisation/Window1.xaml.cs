using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WPFUISyncronisation {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window 
	{
        public Window1() 
        {
            InitializeComponent();
        }

    	private void PerformIt(object sender, RoutedEventArgs e)
        {
            var threadedObject = new ThreadedObject(MethodWhichCallsSomeObject);

            var t = new Thread(threadedObject.DoSomethingThreadedly);
            t.Start();
//			threadedObject.DoSomethingThreadedly();
        }

        private void MethodWhichCallsSomeObject()
        {
        	Dispatcher.Invoke(DispatcherPriority.Normal, (Action) (() => progress1.Value += 1));
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
			for (var i = 0; i < 2; i++)
			{
				Thread.Sleep(2000);
				_methodWhichCallsSomeObject();
			}
		}
	}

}
