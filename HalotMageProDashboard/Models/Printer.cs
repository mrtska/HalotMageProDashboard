using HalotMageProDashboard.Entities;
using HalotMageProSharp;
using Livet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.IO;
using System.Net;

namespace HalotMageProDashboard.Models {
    /// <summary>
    /// プリンターを制御するクラス
    /// </summary>
    public class Printer : NotificationObject, IDisposable {

        private string _Name = default!;
        /// <summary>
        /// プリンターの名前
        /// </summary>
        public string Name {
            get { return _Name; }
            set {
                if (_Name == value)
                    return;
                _Name = value;
                RaisePropertyChanged();

                var entity = DbContext.SavedPrinters.SingleOrDefault(w => w.Key == Key);
                if (entity != null) {
                    entity.Name = value;
                    DbContext.SaveChanges();
                    DbContext.ChangeTracker.Clear();
                }
            }
        }

        private string _Host = default!;
        /// <summary>
        /// ホスト名
        /// </summary>
        public string Host {
            get { return _Host; }
            set {
                if (_Host == value)
                    return;
                _Host = value;
                RaisePropertyChanged();
            }
        }

        private bool _IsConnected;
        /// <summary>
        /// WebSocketが繋がっていればTrue
        /// </summary>
        public bool IsConnected {
            get { return _IsConnected; }
            set {
                if (_IsConnected == value)
                    return;
                _IsConnected = value;
                RaisePropertyChanged();
            }
        }

        private string _Filename = string.Empty;
        /// <summary>
        /// 印刷中のファイル名
        /// </summary>
        public string Filename {
            get { return _Filename; }
            set {
                if (_Filename == value)
                    return;
                _Filename = value;
                RaisePropertyChanged();
            }
        }

        private float _Progress = 0;
        /// <summary>
        /// 進捗率
        /// </summary>
        public float Progress {
            get { return _Progress; }
            set {
                if (_Progress == value)
                    return;
                _Progress = value;
                RaisePropertyChanged();
            }
        }


        private int _CurrentSliceLayer;
        /// <summary>
        /// 印刷中のレイヤー
        /// </summary>
        public int CurrentSliceLayer {
            get { return _CurrentSliceLayer; }
            set {
                if (_CurrentSliceLayer == value)
                    return;
                _CurrentSliceLayer = value;
                RaisePropertyChanged();
            }
        }


        private int _TotalLayerCount;
        /// <summary>
        /// レイヤー数
        /// </summary>
        public int TotalLayerCount {
            get { return _TotalLayerCount; }
            set {
                if (_TotalLayerCount == value)
                    return;
                _TotalLayerCount = value;
                RaisePropertyChanged();
            }
        }



        private TimeSpan? _RemainingTime;
        /// <summary>
        /// 残り時間
        /// </summary>
        public TimeSpan? RemainingTime {
            get { return _RemainingTime; }
            set {
                if (_RemainingTime == value)
                    return;
                _RemainingTime = value;
                RaisePropertyChanged();
            }
        }

        private string _Status = default!;
        /// <summary>
        /// ステータス
        /// </summary>
        public string Status {
            get { return _Status; }
            set {
                if (_Status == value)
                    return;
                _Status = value;
                RaisePropertyChanged();
            }
        }

        private bool _IsPrinting;
        /// <summary>
        /// 印刷中かどうか
        /// </summary>
        public bool IsPrinting {
            get { return _IsPrinting; }
            set {
                if (_IsPrinting == value)
                    return;
                _IsPrinting = value;
                RaisePropertyChanged();
            }
        }


        private bool _IsCompleted;
        /// <summary>
        /// 印刷完了したかどうか
        /// </summary>
        public bool IsCompleted {
            get { return _IsCompleted; }
            set {
                if (_IsCompleted == value)
                    return;
                _IsCompleted = value;
                RaisePropertyChanged();
            }
        }

        private readonly Guid Key;
        private readonly ApplicationDbContext DbContext;
        private readonly HalotMageProClient Client;

        public Printer(ApplicationDbContext dbContext, SavedPrinter savedPrinter) {
            DbContext = dbContext;
            Key = savedPrinter.Key;
            Client = new HalotMageProClient(IPAddress.Parse(savedPrinter.Host), savedPrinter.Password);

            Client.OnTokenError += (sender, e) => {
                Status = "パスワードエラー";
            };
            Client.OnDisconnected += (sender, e) => {
                IsConnected = false;
                Status = "切断されました";

                Connect();
            };
            Client.OnConnected += (sender, e) => {
                IsConnected = true;
                Status = "接続されました";
            };

            Client.OnGetPrinterStatus += (sender, e) => {
                Status = e.PrintStatus switch {
                    "PRINT_GENERAL" => "アイドル",
                    "PRINT_PROCESSING" => "印刷中",
                    "PRINT_STOPPING" => "停止処理中",
                    "PRINT_STOP" => "一時停止",
                    "PRINT_COMPLETING" => "完了処理中",
                    "PRINT_COMPLETE" => "印刷完了",
                    _ => e.PrintStatus,
                };
                Filename = e.Filename ?? "";

                IsPrinting = e.PrintStatus is not "PRINT_GENERAL" or "PRINT_STOPPING" or "PRINT_STOP";
                if (e.PrintStatus == "PRINT_GENERAL" && !string.IsNullOrEmpty(e.Filename)) {
                    IsPrinting = true;
                    Status = "開始中";
                }
                if (e.PrintStatus == "PRINT_COMPLETE") {
                    IsCompleted = true;
                } else {
                    IsCompleted = false;
                }

                if (e.SliceLayerCount != null && e.CurrentSliceLayer != null) {
                    Progress = (float)e.CurrentSliceLayer / (float)e.SliceLayerCount;
                    CurrentSliceLayer = (int)e.CurrentSliceLayer;
                    TotalLayerCount = (int)e.SliceLayerCount;
                } else {
                    Progress = 0;
                    CurrentSliceLayer = 0;
                    TotalLayerCount = 0;
                }

                if (e.PrintRemainTime != null) {
                    RemainingTime = TimeSpan.FromSeconds((double)e.PrintRemainTime);
                } else {
                    RemainingTime = null;
                }
            };

            Client.OnSendFileProgress += (sender, e) => {
                Status = "ファイル送信中";
                Progress = e.Size / e.Received;
            };

            Client.OnPausePrint += (sender, e) => {
                Status = "処理中";
                Thread.Sleep(100);
            };

            Client.OnStopPrint += (sender, e) => {
                Status = "印刷停止中";
                Thread.Sleep(100);
                Refresh();
            };

            Client.OnSendFile += (sender, e) => {
                Status = "印刷開始中";
                Client.StartPrint(e.Filename);
            };
            Client.OnStartPrint += (sender, e) => {
                Refresh();
            };

            Name = savedPrinter.Name;
            Host = savedPrinter.Host;
        }

        /// <summary>
        /// WebSocket接続
        /// </summary>
        public void Connect() {
            Client.Connect();
        }

        /// <summary>
        /// ステータスを更新
        /// </summary>
        public void Refresh() {
            if (IsConnected) {
                Client.GetPrinterStatus();
            }
        }

        /// <summary>
        /// 一時停止/再開
        /// </summary>
        public void PauseResume() {
            Client.PausePrint();
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop() {
            Client.StopPrint();
        }

        /// <summary>
        /// ファイル選択ダイアログを開いてファイルを送信
        /// </summary>
        public void SendFile() {

            var dialog = new OpenFileDialog {
                Filter = "Creality CXDLPv4|*.cxdlpv4"
            };
            var result = dialog.ShowDialog();
            if (result != true) {
                return;
            }
            var filename = dialog.FileName;
            Client.SendFile(dialog.SafeFileName, File.Open(filename, FileMode.Open));
        }

        /// <summary>
        /// パスワードを設定
        /// </summary>
        /// <param name="password">パスワード</param>
        public void SetPassword(string password) {
            Client.Password = password;

            var entity = DbContext.SavedPrinters.SingleOrDefault(w => w.Key == Key);
            if (entity != null) {
                entity.Password = password;
                DbContext.SaveChanges();
                DbContext.ChangeTracker.Clear();
            }
            Refresh();
        }

        /// <summary>
        /// 保存されたプリンターを取得
        /// </summary>
        /// <returns>SQLiteのエンティティクラス</returns>
        public SavedPrinter? GetSavedPrinter() {
            return DbContext.SavedPrinters.AsNoTracking().SingleOrDefault(w => w.Key == Key);
        }

        /// <summary>
        /// パスワードを取得
        /// </summary>
        /// <returns>パスワード</returns>
        public string GetPassword() {
            return Client.Password;
        }

        public void Dispose() {
            DbContext.Dispose();
            Client.Dispose();
        }
    }
}
