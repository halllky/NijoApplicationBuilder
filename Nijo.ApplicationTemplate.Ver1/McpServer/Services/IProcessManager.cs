namespace McpServer.Services {
    public interface IProcessManager {
        /// <summary>
        /// アプリケーションを起動します。reactとWebApiの両方を起動します。
        /// </summary>
        /// <returns>起動結果</returns>
        Task<bool> StartAsync();

        /// <summary>
        /// WebApiのみを再ビルドして再起動します。
        /// </summary>
        /// <returns>再ビルド結果</returns>
        Task<bool> RebuildWebApiAsync();

        /// <summary>
        /// 起動しているすべてのプロセスを停止します。
        /// </summary>
        /// <returns>停止結果</returns>
        Task<bool> StopAsync();

        /// <summary>
        /// 現在の実行状態を取得します。
        /// </summary>
        /// <returns>実行状態</returns>
        ProcessStatus GetStatus();
    }

    public class ProcessStatus {
        public bool IsReactRunning { get; set; }
        public bool IsWebApiRunning { get; set; }
        public DateTime? ReactStartTime { get; set; }
        public DateTime? WebApiStartTime { get; set; }
    }
}
