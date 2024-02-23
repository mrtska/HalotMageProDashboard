using System.ComponentModel.DataAnnotations;

namespace HalotMageProDashboard.Entities {
    /// <summary>
    /// 保存済みの3Dプリンターを管理するテーブル
    /// </summary>
    public class SavedPrinter {
        /// <summary>
        /// 主キー
        /// </summary>
        [Key]
        public Guid Key { get; set; }

        /// <summary>
        /// プリンターのMacアドレス
        /// </summary>
        public string? MacAddress { get; set; }

        /// <summary>
        /// プリンターの名前
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// プリンターのIPアドレス、ホスト名
        /// </summary>
        public string Host { get; set; } = default!;

        /// <summary>
        /// プリンターのパスワード
        /// </summary>
        public string Password { get; set; } = default!;
    }
}
