using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


namespace FileCopy.Model
{
    
    internal class WindowModel :INotifyPropertyChanged
    {
        private enum State
        {
            Idle,
            Copy
        }


        private string? toPath;

        private CancellationTokenSource tokenSource;

        private CancellationToken token;

        private string? fromPath;

        private string copyCountStr = "1";

        private int progress = 0, progressMax = 1, copyCount = 1;
        private State curentState = State.Idle;

        private void openFrom()
        {
            OpenFileDialog ofd = new();
            if (ofd.ShowDialog() == true)
            {
                FromPath = ofd.FileName;
                Progress = 0;
            }
        }

        private void openTo()
        {
            SaveFileDialog sfd = new()
            {
                Filter = $"{Path.GetExtension(FromPath)[1..].ToUpper()} files (*{Path.GetExtension(FromPath)})|*{Path.GetExtension(FromPath)}|All files (*.*)|*.*"
            };
            if (sfd.ShowDialog() == true)
            {
                ToPath = sfd.FileName;
                Progress = 0; 
            }
        }

        private void copyFile(int index,CancellationToken token)
        {
            
        }

        private async void copy()
        {
            CurentState = State.Copy;
            tokenSource?.Dispose();
            tokenSource = new();
            token = tokenSource.Token;
            Progress = 0;
            //List<Task> tasks = new();
            //for (int i = 0; i < copyCount; i++)
            //{
            //    int index = i;
            //    tasks.Add(Task.Run(
            //        () =>
            //        {
            //            if (token.IsCancellationRequested) return;
            //            string newName = Path.GetFileName(toPath);
            //            File.Copy(fromPath, index == 0 ? toPath : Path.Combine(Path.GetDirectoryName(toPath), $"Copy({index})_{newName}"), true);
            //            //  Thread.Sleep(400);
            //            Interlocked.Increment(ref progress);
            //            OnPropertyChanged("Progress");
            //        }, token));
            //}
            //try{await Task.WhenAll(tasks);}cath{}
            try
            {
                await Parallel.ForEachAsync(Enumerable.Range(0, copyCount), token, async (index, tk) =>
                {
                    await Task.Run(() =>
                    {
                        if (tk.IsCancellationRequested) return;
                        string newName = Path.GetFileName(toPath);
                        File.Copy(fromPath, index == 0 ? toPath : Path.Combine(Path.GetDirectoryName(toPath), $"Copy({index})_{newName}"), true);
                        //  Thread.Sleep(400);
                    }, tk);
                    Interlocked.Increment(ref progress);
                    OnPropertyChanged("Progress");
                });
            }
            catch { }
            MessageBox.Show($"{Progress} files copied to \"{Path.GetDirectoryName(ToPath)}\" directory");
            CurentState = State.Idle;
        }

        private void stop() => tokenSource?.Cancel();

        private State CurentState 
        { 
            get => curentState;
            set
            {
                curentState = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }
      
        public string? ToPath 
        {
            get => toPath;
            set
            {
                toPath = value;
                OnPropertyChanged();
            }
        }

        public string? FromPath
        {
            get => fromPath;
            set
            {
                fromPath = value;
                ToPath = "";
                OnPropertyChanged("ToPath");
                OnPropertyChanged();
            }
        }

        public string CopyCountStr
        {
            get => copyCountStr;
            set
            {
                copyCountStr = value ;
                if (!int.TryParse(copyCountStr, out copyCount) || copyCount < 1)
                    copyCountStr = "1";
                Progress = 0;
                OnPropertyChanged();
            }
        }

        
        public int Progress
        {
            get => progress;
            set
            {
                progress = value;
                OnPropertyChanged();
            }
        }

                

        public RelayCommand From => new((o) => openFrom(), (o) => CurentState == State.Idle);

        public RelayCommand To => new((o) => openTo(), (o) => CurentState == State.Idle && Path.Exists(FromPath));

        public RelayCommand Stop => new((o) => stop(),(o)=> CurentState == State.Copy);

        public RelayCommand Copy => new((o) => copy(), (o) => CurentState == State.Idle && Path.Exists(Path.GetDirectoryName(ToPath)));

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string? prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
