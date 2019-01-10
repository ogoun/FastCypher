using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;

namespace FastCypher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string BaseDirectory =
           System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.CodeBase)?.
           Replace("file:\\", string.Empty);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void bEnc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var problems = new StringBuilder();
                var files = Directory.GetFiles(BaseDirectory, "*.*",
                        (cbRecursive.IsChecked ?? false) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).OrderBy(f => f).ToArray();
                int proceed = files.Length;
                ManualResetEvent completedEvent = new ManualResetEvent(false);
                foreach (var _file in files)
                {
                    if (_file.IndexOf("FastCypher.exe", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (Interlocked.Decrement(ref proceed) == 0)
                        {
                            completedEvent.Set();
                        }
                        continue;
                    }
                    ThreadPool.QueueUserWorkItem(s =>
                    {
                        string file = (string)s;
                        try
                        {
                            var currentDirectory = System.IO.Path.GetDirectoryName(file);
                            var tempo = System.IO.Path.Combine(currentDirectory, System.IO.Path.GetFileName(file) + ".__tempo__");
                            using (var input = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                            {
                                using (var output = new FileStream(tempo, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    __FastCypher.Encode(input, output, pb.Password);
                                    output.Flush();
                                }
                            }
                            File.Delete(file);
                            File.Move(tempo, file);
                        }
                        catch (Exception ex)
                        {
                            problems.AppendLine($"File {file} fault encoded. {ex.Message}");
                        }
                        finally
                        {
                            if (Interlocked.Decrement(ref proceed) == 0)
                            {
                                completedEvent.Set();
                            }
                        }
                    }, _file);
                }
                completedEvent.WaitOne();
                completedEvent.Close();
                completedEvent = null;
                MessageBox.Show("Completed.\r\n" + problems.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fault encode. " + ex.Message);
            }
        }

        private void bDec_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var problems = new StringBuilder();
                var files = Directory.GetFiles(BaseDirectory, "*.*",
                        (cbRecursive.IsChecked ?? false) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).OrderBy(f => f).ToArray();
                int proceed = files.Length;
                ManualResetEvent completedEvent = new ManualResetEvent(false);
                foreach (var _file in files)
                {
                    if (_file.IndexOf("FastCypher.exe", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (Interlocked.Decrement(ref proceed) == 0)
                        {
                            completedEvent.Set();
                        }
                        continue;
                    }
                    ThreadPool.QueueUserWorkItem(s =>
                    {
                        string file = (string)s;
                        try
                        {
                            var currentDirectory = System.IO.Path.GetDirectoryName(file);
                            var tempo = System.IO.Path.Combine(currentDirectory, System.IO.Path.GetFileName(file) + ".__tempo__");
                            using (var input = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                            {
                                using (var output = new FileStream(tempo, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    __FastCypher.Decode(input, output, pb.Password);
                                    output.Flush();
                                }
                            }
                            File.Delete(file);
                            File.Move(tempo, file);
                        }
                        catch (Exception ex)
                        {
                            problems.AppendLine($"File {file} fault decoded. {ex.Message}");
                        }
                        finally
                        {
                            if (Interlocked.Decrement(ref proceed) == 0)
                            {
                                completedEvent.Set();
                            }
                        }
                    }, _file);
                }
                completedEvent.WaitOne();
                completedEvent.Close();
                completedEvent = null;
                MessageBox.Show("Completed.\r\n" + problems.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fault decode. " + ex.Message);
            }
        }
    }
}
